using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Contracts.Application;
using Plannyt.Api.Modules.Documents.Application;
using Plannyt.Api.Modules.Documents.Domain;
using Plannyt.Api.Modules.Documents.Storage;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Payments.Domain;

namespace Plannyt.Api.Modules.Payments.Application;

public sealed class PaymentService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    PortalAccessService portalAccessService,
    ICurrentUser currentUser,
    DocumentFileValidator documentFileValidator,
    IFileStorage fileStorage,
    ContractingReadinessService readinessService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PaymentPlanResponse>> GetPlansAsync(
        Guid organizationId,
        Guid? eventId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentPlansView,
            eventId,
            cancellationToken);
        var query = dbContext.PaymentPlans
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);
        if (eventId is not null)
        {
            query = query.Where(entity => entity.EventId == eventId);
        }

        var plans = await query
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);
        var responses = new List<PaymentPlanResponse>();
        foreach (var plan in plans)
        {
            responses.Add(await BuildPlanResponseAsync(
                plan,
                cancellationToken));
        }

        return responses;
    }

    public async Task<PaymentPlanResponse> GetPlanAsync(
        Guid organizationId,
        Guid planId,
        CancellationToken cancellationToken)
    {
        var plan = await FindPlanAsync(
            organizationId,
            planId,
            true,
            cancellationToken);
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentPlansView,
            plan.EventId,
            cancellationToken);
        return await BuildPlanResponseAsync(plan, cancellationToken);
    }

    public async Task<PaymentPlanResponse> CreatePlanAsync(
        Guid organizationId,
        CreatePaymentPlanRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePlanRequest(request);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentPlansCreate,
            request.EventId,
            cancellationToken);
        await EnsureEventClientAsync(
            organizationId,
            request.EventId,
            request.ClientId,
            cancellationToken);
        await ValidatePlanSourcesAsync(
            organizationId,
            request,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var plan = PaymentPlan.Create(
            organizationId,
            request.EventId,
            request.ClientId,
            request.ContractId,
            request.ProposalVersionId,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.TotalAmount,
            access.UserAccountId,
            now);
        var installments = await BuildInstallmentsAsync(
            plan,
            request.Installments,
            now,
            cancellationToken);
        dbContext.PaymentPlans.Add(plan);
        dbContext.PaymentInstallments.AddRange(installments);
        auditService.Add(
            organizationId,
            request.EventId,
            access.UserAccountId,
            "payment_plan.created",
            nameof(PaymentPlan),
            plan.Id,
            new Dictionary<string, object?>
            {
                ["totalAmount"] = plan.TotalAmount,
                ["currencyCode"] = plan.CurrencyCode,
                ["installmentCount"] = installments.Count
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPlanResponseAsync(plan, cancellationToken);
    }

    public async Task<PaymentPlanResponse> UpdatePlanAsync(
        Guid organizationId,
        Guid planId,
        CreatePaymentPlanRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePlanRequest(request);
        var plan = await FindPlanAsync(
            organizationId,
            planId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentPlansUpdateDraft,
            plan.EventId,
            cancellationToken);
        plan.EnsureDraft();
        if (request.EventId != plan.EventId
            || request.ClientId != plan.ClientId
            || request.ContractId != plan.ContractId
            || request.ProposalVersionId != plan.ProposalVersionId
            || !string.Equals(
                request.CurrencyCode,
                plan.CurrencyCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "El contexto y moneda del plan no pueden sustituirse.");
        }

        var now = timeProvider.GetUtcNow();
        plan.UpdateDraft(request.TotalAmount, now);
        var current = await dbContext.PaymentInstallments
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.PaymentPlanId == planId)
            .ToListAsync(cancellationToken);
        dbContext.PaymentInstallments.RemoveRange(current);
        var replacements = await BuildInstallmentsAsync(
            plan,
            request.Installments,
            now,
            cancellationToken);
        dbContext.PaymentInstallments.AddRange(replacements);
        auditService.Add(
            organizationId,
            plan.EventId,
            access.UserAccountId,
            "payment_plan.draft_updated",
            nameof(PaymentPlan),
            plan.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPlanResponseAsync(plan, cancellationToken);
    }

    public async Task<PaymentPlanResponse> ActivatePlanAsync(
        Guid organizationId,
        Guid planId,
        CancellationToken cancellationToken)
    {
        var plan = await FindPlanAsync(
            organizationId,
            planId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentPlansActivate,
            plan.EventId,
            cancellationToken);
        var total = await dbContext.PaymentInstallments
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.PaymentPlanId == planId
                && entity.Status != PaymentInstallmentStatus.Cancelled)
            .SumAsync(entity => entity.Amount, cancellationToken);
        plan.Activate(total, timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            plan.EventId,
            access.UserAccountId,
            "payment_plan.activated",
            nameof(PaymentPlan),
            plan.Id,
            new Dictionary<string, object?>
            {
                ["frozenTotal"] = plan.TotalAmount
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPlanResponseAsync(plan, cancellationToken);
    }

    public async Task CancelPlanAsync(
        Guid organizationId,
        Guid planId,
        CancellationToken cancellationToken)
    {
        var plan = await FindPlanAsync(
            organizationId,
            planId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentPlansCancel,
            plan.EventId,
            cancellationToken);
        plan.Cancel(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            plan.EventId,
            access.UserAccountId,
            "payment_plan.cancelled",
            nameof(PaymentPlan),
            plan.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentRecordResponse>> GetPaymentsAsync(
        Guid organizationId,
        Guid? eventId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.ResolveAsync(
            organizationId,
            eventId,
            timeProvider.GetUtcNow(),
            cancellationToken);
        if (!access.Permissions.Contains(Permissions.PaymentsView))
        {
            throw new ForbiddenException(
                "No tienes permiso para consultar pagos.");
        }

        var includeInternal =
            access.Permissions.Contains(Permissions.PaymentsViewInternal);
        var query = dbContext.PaymentRecords
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);
        if (eventId is not null)
        {
            query = query.Where(entity => entity.EventId == eventId);
        }

        var payments = await query
            .OrderByDescending(entity => entity.PaymentDate)
            .ThenByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);
        var responses = new List<PaymentRecordResponse>();
        foreach (var payment in payments)
        {
            responses.Add(await BuildPaymentResponseAsync(
                payment,
                includeInternal,
                cancellationToken));
        }

        return responses;
    }

    public async Task<PaymentRecordResponse> CreatePaymentAsync(
        Guid organizationId,
        CreatePaymentRecordRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePaymentRequest(request);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentsCreate,
            request.EventId,
            cancellationToken);
        await ValidatePaymentContextAsync(
            organizationId,
            request.EventId,
            request.ClientId,
            request.PaymentPlanId,
            request.CurrencyCode,
            cancellationToken);
        var payment = PaymentRecord.Create(
            organizationId,
            request.EventId,
            request.ClientId,
            request.PaymentPlanId,
            request.PaymentDate,
            request.Amount,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.Method,
            Normalize(request.Reference),
            Normalize(request.NotesShared),
            Normalize(request.InternalNotes),
            access.UserAccountId,
            false,
            timeProvider.GetUtcNow());
        dbContext.PaymentRecords.Add(payment);
        auditService.Add(
            organizationId,
            request.EventId,
            access.UserAccountId,
            "payment.recorded",
            nameof(PaymentRecord),
            payment.Id,
            new Dictionary<string, object?>
            {
                ["amount"] = payment.Amount,
                ["currencyCode"] = payment.CurrencyCode,
                ["method"] = payment.Method.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPaymentResponseAsync(
            payment,
            true,
            cancellationToken);
    }

    public async Task<PaymentRecordResponse> ApprovePaymentAsync(
        Guid organizationId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var payment = await FindPaymentAsync(
            organizationId,
            paymentId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentsApprove,
            payment.EventId,
            cancellationToken);
        payment.Approve(access.UserAccountId, timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            payment.EventId,
            access.UserAccountId,
            "payment.approved",
            nameof(PaymentRecord),
            payment.Id,
            new Dictionary<string, object?>
            {
                ["amount"] = payment.Amount
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPaymentResponseAsync(
            payment,
            true,
            cancellationToken);
    }

    public async Task<PaymentRecordResponse> RejectPaymentAsync(
        Guid organizationId,
        Guid paymentId,
        RejectPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Trim().Length > 1000)
        {
            throw Validation(
                "reason",
                "El motivo es obligatorio y admite 1,000 caracteres.");
        }

        var payment = await FindPaymentAsync(
            organizationId,
            paymentId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentsReject,
            payment.EventId,
            cancellationToken);
        payment.Reject(
            access.UserAccountId,
            request.Reason.Trim(),
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            payment.EventId,
            access.UserAccountId,
            "payment.rejected",
            nameof(PaymentRecord),
            payment.Id,
            new Dictionary<string, object?>
            {
                ["reason"] = request.Reason.Trim()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPaymentResponseAsync(
            payment,
            true,
            cancellationToken);
    }

    public async Task<PaymentRecordResponse> AllocatePaymentAsync(
        Guid organizationId,
        Guid paymentId,
        IReadOnlyList<PaymentAllocationRequest> requests,
        CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            throw Validation(
                "allocations",
                "Agrega al menos una asignación.");
        }

        var payment = await FindPaymentAsync(
            organizationId,
            paymentId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentsApprove,
            payment.EventId,
            cancellationToken);
        if (payment.Status != PaymentRecordStatus.Approved)
        {
            throw new ConflictException(
                "Solo un pago aprobado puede asignarse.");
        }

        var duplicate = requests
            .GroupBy(item => item.PaymentInstallmentId)
            .Any(group => group.Count() > 1);
        if (duplicate || requests.Any(item => item.Amount <= 0m))
        {
            throw Validation(
                "allocations",
                "Las parcialidades no deben repetirse y los importes deben ser positivos.");
        }

        var existingPaymentAllocation = await dbContext.PaymentAllocations
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.PaymentRecordId == paymentId
                && entity.ReversedAt == null)
            .SumAsync(entity => entity.Amount, cancellationToken);
        var requestedTotal = requests.Sum(item => item.Amount);
        if (existingPaymentAllocation + requestedTotal > payment.Amount)
        {
            throw new ConflictException(
                "La asignación excede el importe aprobado del pago.");
        }

        var installmentIds = requests
            .Select(item => item.PaymentInstallmentId)
            .ToHashSet();
        var installments = await dbContext.PaymentInstallments
            .Where(entity =>
                entity.OrganizationId == organizationId
                && installmentIds.Contains(entity.Id))
            .ToListAsync(cancellationToken);
        if (installments.Count != installmentIds.Count)
        {
            throw new NotFoundException(
                "No se encontró una de las parcialidades.");
        }

        var planIds = installments.Select(item => item.PaymentPlanId).Distinct();
        if (planIds.Count() != 1
            || payment.PaymentPlanId != planIds.Single())
        {
            throw new ConflictException(
                "Las parcialidades no pertenecen al plan del pago.");
        }

        var now = timeProvider.GetUtcNow();
        foreach (var request in requests)
        {
            var installment = installments.Single(
                item => item.Id == request.PaymentInstallmentId);
            var approved = await ApprovedForInstallmentAsync(
                organizationId,
                installment.Id,
                cancellationToken);
            if (approved + request.Amount > installment.Amount)
            {
                throw new ConflictException(
                    $"La asignación excede el pendiente de '{installment.Description}'.");
            }

            var allocation = PaymentAllocation.Create(
                organizationId,
                payment.Id,
                installment.Id,
                request.Amount,
                now);
            dbContext.PaymentAllocations.Add(allocation);
            installment.RefreshStatus(
                approved + request.Amount,
                DateOnly.FromDateTime(now.UtcDateTime),
                now);
            auditService.Add(
                organizationId,
                payment.EventId,
                access.UserAccountId,
                "payment.allocated",
                nameof(PaymentAllocation),
                allocation.Id,
                new Dictionary<string, object?>
                {
                    ["paymentId"] = payment.Id,
                    ["installmentId"] = installment.Id,
                    ["amount"] = allocation.Amount
                });
        }

        await CompletePlanIfPaidAsync(
            organizationId,
            payment.PaymentPlanId,
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await readinessService.TryAutomaticConfirmationAsync(
            organizationId,
            payment.EventId,
            access.UserAccountId,
            cancellationToken);
        return await BuildPaymentResponseAsync(
            payment,
            true,
            cancellationToken);
    }

    public Task<PaymentRecordResponse> CancelPaymentAsync(
        Guid organizationId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        ReversePaymentAsync(
            organizationId,
            paymentId,
            refund: false,
            cancellationToken);

    public Task<PaymentRecordResponse> RefundPaymentAsync(
        Guid organizationId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        ReversePaymentAsync(
            organizationId,
            paymentId,
            refund: true,
            cancellationToken);

    public async Task<PaymentReceiptResponse> UploadReceiptAsync(
        Guid organizationId,
        Guid paymentId,
        UploadPaymentReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await FindPaymentAsync(
            organizationId,
            paymentId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PaymentsCreate,
            payment.EventId,
            cancellationToken);
        return await StoreReceiptAsync(
            payment,
            request,
            access.UserAccountId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentPlanResponse>> GetPortalPlansAsync(
        Guid? eventId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var accesses = dbContext.EventAccesses
            .AsNoTracking()
            .Where(access =>
                access.UserAccountId == currentUser.UserAccountId
                && access.Status == EventAccessStatus.Active
                && access.StartsAt <= now
                && (access.ExpiresAt == null || access.ExpiresAt > now)
                && access.RevokedAt == null);
        if (eventId is not null)
        {
            accesses = accesses.Where(access => access.EventId == eventId);
        }

        var plans = await dbContext.PaymentPlans
            .AsNoTracking()
            .Join(
                accesses,
                plan => new { plan.OrganizationId, plan.EventId },
                access => new { access.OrganizationId, access.EventId },
                (plan, _) => plan)
            .Where(plan => plan.Status != PaymentPlanStatus.Cancelled)
            .OrderByDescending(plan => plan.CreatedAt)
            .ToListAsync(cancellationToken);
        var responses = new List<PaymentPlanResponse>();
        foreach (var plan in plans)
        {
            responses.Add(await BuildPlanResponseAsync(
                plan,
                cancellationToken));
        }

        return responses;
    }

    public async Task<PortalPaymentRecordResponse> CreatePortalPaymentAsync(
        PortalCreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var plan = await dbContext.PaymentPlans
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.Id == request.PaymentPlanId
                    && entity.Status == PaymentPlanStatus.Active,
                cancellationToken)
            ?? throw new NotFoundException(
                "No se encontró un plan activo.");
        await portalAccessService.RequireAsync(
            plan.EventId,
            Permissions.PaymentsCreate,
            cancellationToken);
        var paymentRequest = new CreatePaymentRecordRequest(
            plan.EventId,
            plan.ClientId,
            plan.Id,
            request.PaymentDate,
            request.Amount,
            plan.CurrencyCode,
            request.Method,
            request.Reference,
            request.NotesShared,
            null);
        ValidatePaymentRequest(paymentRequest);
        var payment = PaymentRecord.Create(
            plan.OrganizationId,
            plan.EventId,
            plan.ClientId,
            plan.Id,
            request.PaymentDate,
            request.Amount,
            plan.CurrencyCode,
            request.Method,
            Normalize(request.Reference),
            Normalize(request.NotesShared),
            null,
            currentUser.UserAccountId,
            true,
            timeProvider.GetUtcNow());
        dbContext.PaymentRecords.Add(payment);
        auditService.Add(
            plan.OrganizationId,
            plan.EventId,
            currentUser.UserAccountId,
            "payment.portal_submitted",
            nameof(PaymentRecord),
            payment.Id,
            new Dictionary<string, object?>
            {
                ["amount"] = payment.Amount,
                ["currencyCode"] = payment.CurrencyCode
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPortalPaymentResponseAsync(
            payment,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PortalPaymentRecordResponse>>
        GetPortalPaymentsAsync(
            Guid? eventId,
            CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var accesses = dbContext.EventAccesses
            .AsNoTracking()
            .Where(access =>
                access.UserAccountId == currentUser.UserAccountId
                && access.Status == EventAccessStatus.Active
                && access.StartsAt <= now
                && (access.ExpiresAt == null || access.ExpiresAt > now)
                && access.RevokedAt == null);
        if (eventId is not null)
        {
            accesses = accesses.Where(access => access.EventId == eventId);
        }

        var payments = await dbContext.PaymentRecords
            .AsNoTracking()
            .Join(
                accesses,
                payment => new
                {
                    payment.OrganizationId,
                    payment.EventId
                },
                access => new
                {
                    access.OrganizationId,
                    access.EventId
                },
                (payment, _) => payment)
            .OrderByDescending(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);
        var responses = new List<PortalPaymentRecordResponse>();
        foreach (var payment in payments)
        {
            responses.Add(await BuildPortalPaymentResponseAsync(
                payment,
                cancellationToken));
        }

        return responses;
    }

    public async Task<PaymentReceiptResponse> UploadPortalReceiptAsync(
        Guid paymentId,
        UploadPaymentReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var payment = await dbContext.PaymentRecords
            .SingleOrDefaultAsync(
                entity =>
                    entity.Id == paymentId
                    && entity.SubmittedByClient
                    && entity.RecordedBy == currentUser.UserAccountId,
                cancellationToken)
            ?? throw new NotFoundException(
                "No se encontró el pago enviado desde tu cuenta.");
        await portalAccessService.RequireAsync(
            payment.EventId,
            Permissions.PaymentsCreate,
            cancellationToken);
        return await StoreReceiptAsync(
            payment,
            request,
            currentUser.UserAccountId,
            cancellationToken);
    }

    private async Task<PaymentRecordResponse> ReversePaymentAsync(
        Guid organizationId,
        Guid paymentId,
        bool refund,
        CancellationToken cancellationToken)
    {
        var payment = await FindPaymentAsync(
            organizationId,
            paymentId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            refund ? Permissions.PaymentsRefund : Permissions.PaymentsCancel,
            payment.EventId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (refund)
        {
            payment.Refund(now);
        }
        else
        {
            payment.Cancel(now);
        }

        var allocations = await dbContext.PaymentAllocations
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.PaymentRecordId == paymentId
                && entity.ReversedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var allocation in allocations)
        {
            allocation.Reverse(now);
        }

        var installmentIds = allocations
            .Select(entity => entity.PaymentInstallmentId)
            .Distinct()
            .ToList();
        var installments = await dbContext.PaymentInstallments
            .Where(entity =>
                entity.OrganizationId == organizationId
                && installmentIds.Contains(entity.Id))
            .ToListAsync(cancellationToken);
        foreach (var installment in installments)
        {
            var approved = await ApprovedForInstallmentAsync(
                organizationId,
                installment.Id,
                cancellationToken,
                excludedPaymentId: paymentId);
            installment.RefreshStatus(
                approved,
                DateOnly.FromDateTime(now.UtcDateTime),
                now);
        }

        auditService.Add(
            organizationId,
            payment.EventId,
            access.UserAccountId,
            refund ? "payment.refunded" : "payment.cancelled",
            nameof(PaymentRecord),
            payment.Id,
            new Dictionary<string, object?>
            {
                ["reversedAllocationCount"] = allocations.Count
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildPaymentResponseAsync(
            payment,
            true,
            cancellationToken);
    }

    private async Task<PaymentReceiptResponse> StoreReceiptAsync(
        PaymentRecord payment,
        UploadPaymentReceiptRequest request,
        Guid uploadedBy,
        CancellationToken cancellationToken)
    {
        var validated = await documentFileValidator.ValidateAsync(
            new UploadDocumentRequest(
                request.File,
                "Comprobante de pago",
                DocumentVisibility.ClientShared),
            cancellationToken);
        string? storageKey = null;
        try
        {
            await using var source = request.File.OpenReadStream();
            storageKey = await fileStorage.SaveAsync(
                source,
                validated.Extension,
                cancellationToken);
            var now = timeProvider.GetUtcNow();
            var document = BasicDocument.Create(
                payment.OrganizationId,
                payment.EventId,
                payment.ClientId,
                validated.DocumentType,
                validated.SafeFileName,
                fileStorage.ProviderName,
                storageKey,
                validated.MimeType,
                validated.SizeBytes,
                DocumentVisibility.ClientShared,
                uploadedBy,
                now);
            var receipt = PaymentReceipt.Create(
                payment.OrganizationId,
                payment.Id,
                document.Id,
                now);
            dbContext.AddRange(document, receipt);
            auditService.Add(
                payment.OrganizationId,
                payment.EventId,
                uploadedBy,
                "payment.receipt_uploaded",
                nameof(PaymentReceipt),
                receipt.Id,
                new Dictionary<string, object?>
                {
                    ["paymentId"] = payment.Id,
                    ["documentId"] = document.Id,
                    ["mimeType"] = document.MimeType,
                    ["sizeBytes"] = document.SizeBytes
                });
            await dbContext.SaveChangesAsync(cancellationToken);
            return new PaymentReceiptResponse(
                document.Id,
                document.FileName,
                document.MimeType,
                document.SizeBytes,
                document.CreatedAt);
        }
        catch
        {
            if (storageKey is not null)
            {
                await fileStorage.DeleteAsync(
                    storageKey,
                    CancellationToken.None);
            }

            throw;
        }
    }

    private async Task<IReadOnlyList<PaymentInstallment>>
        BuildInstallmentsAsync(
            PaymentPlan plan,
            IReadOnlyList<PaymentInstallmentRequest> requests,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        IReadOnlyList<PaymentInstallmentRequest> source = requests;
        if (source.Count == 0)
        {
            var deposit = plan.ContractId is null
                ? 0m
                : await dbContext.ContractingRequirementSnapshots
                    .AsNoTracking()
                    .Where(entity =>
                        entity.OrganizationId == plan.OrganizationId
                        && entity.ContractId == plan.ContractId)
                    .Select(entity => entity.RequiredDepositAmount)
                    .SingleOrDefaultAsync(cancellationToken);
            var eventDate = await dbContext.Events
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == plan.OrganizationId
                    && entity.Id == plan.EventId)
                .Select(entity => DateOnly.FromDateTime(
                    entity.StartDateTime.UtcDateTime))
                .SingleAsync(cancellationToken);
            var generated = new List<PaymentInstallmentRequest>();
            if (deposit > 0m)
            {
                generated.Add(new PaymentInstallmentRequest(
                    1,
                    "Anticipo de contratación",
                    DateOnly.FromDateTime(now.UtcDateTime).AddDays(7),
                    Math.Min(deposit, plan.TotalAmount),
                    InstallmentType.Deposit));
            }

            var remainder = plan.TotalAmount - generated.Sum(item => item.Amount);
            if (remainder > 0m || generated.Count == 0)
            {
                generated.Add(new PaymentInstallmentRequest(
                    generated.Count + 1,
                    "Pago final",
                    eventDate,
                    remainder,
                    InstallmentType.FinalPayment));
            }

            source = generated;
        }

        var duplicateSequence = source
            .GroupBy(item => item.SequenceNumber)
            .Any(group => group.Count() > 1);
        if (duplicateSequence
            || source.Any(item =>
                item.SequenceNumber < 1
                || item.Amount < 0m
                || string.IsNullOrWhiteSpace(item.Description)))
        {
            throw Validation(
                "installments",
                "Las parcialidades contienen datos no válidos.");
        }

        var normalized = source
            .OrderBy(item => item.SequenceNumber)
            .ToList();
        var difference = plan.TotalAmount - normalized.Sum(item => item.Amount);
        if (difference != 0m)
        {
            if (Math.Abs(difference) > 0.05m)
            {
                throw new ConflictException(
                    "Las parcialidades deben sumar exactamente el total del plan.");
            }

            var last = normalized[^1];
            normalized[^1] = last with
            {
                Amount = last.Amount + difference
            };
        }

        return normalized
            .Select(item => PaymentInstallment.Create(
                plan.OrganizationId,
                plan.Id,
                item.SequenceNumber,
                item.Description.Trim(),
                item.DueDate,
                item.Amount,
                item.InstallmentType,
                now))
            .ToList();
    }

    private async Task<PaymentPlanResponse> BuildPlanResponseAsync(
        PaymentPlan plan,
        CancellationToken cancellationToken)
    {
        var installments = await dbContext.PaymentInstallments
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == plan.OrganizationId
                && entity.PaymentPlanId == plan.Id)
            .OrderBy(entity => entity.SequenceNumber)
            .ToListAsync(cancellationToken);
        var responses = new List<PaymentInstallmentResponse>();
        foreach (var installment in installments)
        {
            var approved = await ApprovedForInstallmentAsync(
                plan.OrganizationId,
                installment.Id,
                cancellationToken);
            responses.Add(new PaymentInstallmentResponse(
                installment.Id,
                installment.SequenceNumber,
                installment.Description,
                installment.DueDate,
                installment.Amount,
                approved,
                Math.Max(0m, installment.Amount - approved),
                installment.InstallmentType,
                ResolveStatus(installment, approved)));
        }

        var approvedTotal = responses.Sum(item => item.ApprovedAmount);
        return new PaymentPlanResponse(
            plan.Id,
            plan.EventId,
            plan.ClientId,
            plan.ContractId,
            plan.ProposalVersionId,
            plan.CurrencyCode,
            plan.TotalAmount,
            plan.Status,
            approvedTotal,
            Math.Max(0m, plan.TotalAmount - approvedTotal),
            responses,
            plan.CreatedAt,
            plan.UpdatedAt);
    }

    private async Task<PaymentRecordResponse> BuildPaymentResponseAsync(
        PaymentRecord payment,
        bool includeInternal,
        CancellationToken cancellationToken)
    {
        var allocations = await dbContext.PaymentAllocations
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == payment.OrganizationId
                && entity.PaymentRecordId == payment.Id)
            .OrderBy(entity => entity.CreatedAt)
            .Select(entity => new PaymentAllocationResponse(
                entity.Id,
                entity.PaymentInstallmentId,
                entity.Amount,
                entity.CreatedAt,
                entity.ReversedAt))
            .ToListAsync(cancellationToken);
        var receipts = await GetReceiptsAsync(
            payment.OrganizationId,
            payment.Id,
            cancellationToken);
        return new PaymentRecordResponse(
            payment.Id,
            payment.EventId,
            payment.ClientId,
            payment.PaymentPlanId,
            payment.PaymentDate,
            payment.Amount,
            payment.CurrencyCode,
            payment.Method,
            payment.Reference,
            payment.Status,
            payment.NotesShared,
            includeInternal ? payment.InternalNotes : null,
            payment.SubmittedByClient,
            payment.RecordedBy,
            payment.ApprovedBy,
            payment.ApprovedAt,
            payment.RejectedBy,
            payment.RejectedAt,
            payment.RejectionReason,
            allocations,
            receipts,
            payment.CreatedAt,
            payment.UpdatedAt);
    }

    private async Task<PortalPaymentRecordResponse>
        BuildPortalPaymentResponseAsync(
            PaymentRecord payment,
            CancellationToken cancellationToken) =>
        new(
            payment.Id,
            payment.PaymentDate,
            payment.Amount,
            payment.CurrencyCode,
            payment.Method,
            payment.Reference,
            payment.Status,
            payment.NotesShared,
            payment.RejectionReason,
            await GetReceiptsAsync(
                payment.OrganizationId,
                payment.Id,
                cancellationToken),
            payment.CreatedAt);

    private async Task<IReadOnlyList<PaymentReceiptResponse>> GetReceiptsAsync(
        Guid organizationId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        await dbContext.PaymentReceipts
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.PaymentRecordId == paymentId)
            .Join(
                dbContext.BasicDocuments.AsNoTracking()
                    .Where(document =>
                        document.Visibility == DocumentVisibility.ClientShared
                        && document.DeletedAt == null),
                receipt => new
                {
                    receipt.OrganizationId,
                    Id = receipt.DocumentId
                },
                document => new { document.OrganizationId, document.Id },
                (_, document) => new PaymentReceiptResponse(
                    document.Id,
                    document.FileName,
                    document.MimeType,
                    document.SizeBytes,
                    document.CreatedAt))
            .ToListAsync(cancellationToken);

    private async Task<decimal> ApprovedForInstallmentAsync(
        Guid organizationId,
        Guid installmentId,
        CancellationToken cancellationToken,
        Guid? excludedPaymentId = null) =>
        await (
            from allocation in dbContext.PaymentAllocations.AsNoTracking()
            join payment in dbContext.PaymentRecords.AsNoTracking()
                on new
                {
                    allocation.OrganizationId,
                    Id = allocation.PaymentRecordId
                }
                equals new { payment.OrganizationId, payment.Id }
            where allocation.OrganizationId == organizationId
                && allocation.PaymentInstallmentId == installmentId
                && allocation.ReversedAt == null
                && payment.Status == PaymentRecordStatus.Approved
                && (excludedPaymentId == null || payment.Id != excludedPaymentId)
            select allocation.Amount)
            .SumAsync(cancellationToken);

    private async Task CompletePlanIfPaidAsync(
        Guid organizationId,
        Guid? planId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (planId is null)
        {
            return;
        }

        var plan = await dbContext.PaymentPlans.SingleAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == planId,
            cancellationToken);
        var installments = await dbContext.PaymentInstallments
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.PaymentPlanId == planId
                && entity.Status != PaymentInstallmentStatus.Cancelled)
            .ToListAsync(cancellationToken);
        var allPaid = installments.Count > 0
            && installments.All(entity =>
                entity.Status == PaymentInstallmentStatus.Paid);
        if (allPaid && plan.Status == PaymentPlanStatus.Active)
        {
            plan.Complete(now);
        }
    }

    private async Task ValidatePlanSourcesAsync(
        Guid organizationId,
        CreatePaymentPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ContractId is not null)
        {
            var valid = await dbContext.Contracts.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == request.ContractId
                    && entity.EventId == request.EventId
                    && entity.ClientId == request.ClientId
                    && entity.CurrencyCode
                        == request.CurrencyCode.Trim().ToUpperInvariant(),
                cancellationToken);
            if (!valid)
            {
                throw new ConflictException(
                    "El contrato no corresponde al contexto del plan.");
            }
        }

        if (request.ProposalVersionId is not null
            && !await dbContext.ProposalVersions.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == request.ProposalVersionId
                    && entity.CurrencyCode
                        == request.CurrencyCode.Trim().ToUpperInvariant(),
                cancellationToken))
        {
            throw new ConflictException(
                "La versión de propuesta no corresponde al plan.");
        }
    }

    private async Task ValidatePaymentContextAsync(
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        Guid? planId,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        await EnsureEventClientAsync(
            organizationId,
            eventId,
            clientId,
            cancellationToken);
        if (planId is not null
            && !await dbContext.PaymentPlans.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == planId
                    && entity.EventId == eventId
                    && entity.ClientId == clientId
                    && entity.CurrencyCode
                        == currencyCode.Trim().ToUpperInvariant()
                    && entity.Status == PaymentPlanStatus.Active,
                cancellationToken))
        {
            throw new ConflictException(
                "El plan de pagos no está activo o no corresponde al pago.");
        }
    }

    private async Task EnsureEventClientAsync(
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.EventClients.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId
                    && entity.ClientId == clientId,
                cancellationToken))
        {
            throw new ConflictException(
                "El cliente no está relacionado con el evento.");
        }
    }

    private async Task<PaymentPlan> FindPlanAsync(
        Guid organizationId,
        Guid planId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.PaymentPlans.Where(entity =>
            entity.OrganizationId == organizationId
            && entity.Id == planId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(
                "No se encontró el plan de pagos.");
    }

    private async Task<PaymentRecord> FindPaymentAsync(
        Guid organizationId,
        Guid paymentId,
        CancellationToken cancellationToken) =>
        await dbContext.PaymentRecords.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == paymentId,
            cancellationToken)
        ?? throw new NotFoundException("No se encontró el pago.");

    private PaymentInstallmentStatus ResolveStatus(
        PaymentInstallment installment,
        decimal approved)
    {
        if (installment.Status == PaymentInstallmentStatus.Cancelled)
        {
            return installment.Status;
        }

        if (approved >= installment.Amount)
        {
            return PaymentInstallmentStatus.Paid;
        }

        if (approved > 0m)
        {
            return PaymentInstallmentStatus.PartiallyPaid;
        }

        return installment.DueDate
            < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)
            ? PaymentInstallmentStatus.Overdue
            : PaymentInstallmentStatus.Pending;
    }

    private static void ValidatePlanRequest(CreatePaymentPlanRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.TotalAmount < 0m)
        {
            errors["totalAmount"] = ["El total no puede ser negativo."];
        }

        if (string.IsNullOrWhiteSpace(request.CurrencyCode)
            || request.CurrencyCode.Trim().Length != 3)
        {
            errors["currencyCode"] =
                ["La moneda debe ser un código ISO de tres letras."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static void ValidatePaymentRequest(
        CreatePaymentRecordRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Amount <= 0m)
        {
            errors["amount"] = ["El importe debe ser mayor que cero."];
        }

        if (string.IsNullOrWhiteSpace(request.CurrencyCode)
            || request.CurrencyCode.Trim().Length != 3)
        {
            errors["currencyCode"] =
                ["La moneda debe ser un código ISO de tres letras."];
        }

        if (request.Reference?.Trim().Length > 200)
        {
            errors["reference"] = ["La referencia admite 200 caracteres."];
        }

        if (request.NotesShared?.Trim().Length > 2000)
        {
            errors["notesShared"] =
                ["Las notas compartidas admiten 2,000 caracteres."];
        }

        if (request.InternalNotes?.Trim().Length > 4000)
        {
            errors["internalNotes"] =
                ["Las notas internas admiten 4,000 caracteres."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RequestValidationException Validation(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
