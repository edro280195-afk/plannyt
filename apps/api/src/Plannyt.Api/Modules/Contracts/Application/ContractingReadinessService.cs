using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Contracts.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Payments.Domain;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.Modules.Contracts.Application;

public sealed class ContractingReadinessService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    PortalAccessService portalAccessService,
    EventStatusTransitionService eventStatusTransitionService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<ContractingReadinessResponse> GetPortalAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.ContractsView,
            cancellationToken);
        return await CalculateAsync(
            access.OrganizationId,
            access.EventId,
            cancellationToken);
    }

    public async Task<ContractingReadinessResponse> GetAdminAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsView,
            eventId,
            cancellationToken);
        return await CalculateAsync(
            organizationId,
            eventId,
            cancellationToken);
    }

    public async Task<ContractingReadinessResponse> ConfirmAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsConfirm,
            eventId,
            cancellationToken);
        var readiness = await CalculateAsync(
            organizationId,
            eventId,
            cancellationToken);
        if (!readiness.ReadyForConfirmation)
        {
            throw new ConflictException(
                $"Faltan requisitos: {string.Join(", ", readiness.MissingRequirements)}.");
        }

        var eventEntity = await dbContext.Events
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el evento.");
        if (eventEntity.Status == EventStatus.Confirmed)
        {
            return readiness;
        }

        if (eventEntity.Status != EventStatus.Preliminary)
        {
            throw new ConflictException(
                "Solo un evento preliminar puede confirmarse.");
        }

        var now = timeProvider.GetUtcNow();
        var history = eventStatusTransitionService.ChangeStatus(
            eventEntity,
            EventStatus.Confirmed,
            access.UserAccountId,
            now,
            "Requisitos de contratación validados manualmente.");
        dbContext.EventStatusHistory.Add(history);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "event.confirmed",
            nameof(Event),
            eventId,
            new Dictionary<string, object?>
            {
                ["mode"] = ConfirmationMode.ManualAfterRequirements.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return readiness with { EventStatus = EventStatus.Confirmed.ToString() };
    }

    public async Task<bool> TryAutomaticConfirmationAsync(
        Guid organizationId,
        Guid eventId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var readiness = await CalculateAsync(
            organizationId,
            eventId,
            cancellationToken);
        if (!readiness.ReadyForConfirmation
            || readiness.ConfirmationMode != ConfirmationMode.Automatic)
        {
            return false;
        }

        var eventEntity = await dbContext.Events
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el evento.");
        if (eventEntity.Status == EventStatus.Confirmed)
        {
            return false;
        }

        if (eventEntity.Status != EventStatus.Preliminary)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow();
        var history = eventStatusTransitionService.ChangeStatus(
            eventEntity,
            EventStatus.Confirmed,
            actorUserId,
            now,
            "Confirmación automática al completar requisitos de contratación.");
        dbContext.EventStatusHistory.Add(history);
        auditService.Add(
            organizationId,
            eventId,
            actorUserId,
            "event.confirmed",
            nameof(Event),
            eventId,
            new Dictionary<string, object?>
            {
                ["mode"] = ConfirmationMode.Automatic.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ContractingReadinessResponse> CalculateAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var eventStatus = await dbContext.Events
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.Id == eventId)
            .Select(entity => (EventStatus?)entity.Status)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró el evento.");
        var contract = await dbContext.Contracts
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Status != ContractStatus.Cancelled)
            .OrderByDescending(entity => entity.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        bool requireAcceptedProposal;
        bool requireCompletedContract;
        decimal requiredDepositAmount;
        ConfirmationMode confirmationMode;
        if (contract is not null)
        {
            var snapshot = await dbContext.ContractingRequirementSnapshots
                .AsNoTracking()
                .SingleAsync(
                    entity =>
                        entity.OrganizationId == organizationId
                        && entity.ContractId == contract.Id,
                    cancellationToken);
            requireAcceptedProposal = snapshot.RequireAcceptedProposal;
            requireCompletedContract = snapshot.RequireCompletedContract;
            requiredDepositAmount = snapshot.RequiredDepositAmount;
            confirmationMode = snapshot.ConfirmationMode;
        }
        else
        {
            var policy = await dbContext.OrganizationContractingPolicies
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    entity => entity.OrganizationId == organizationId,
                    cancellationToken);
            requireAcceptedProposal = policy?.RequireAcceptedProposal ?? true;
            requireCompletedContract = policy?.RequireCompletedContract ?? true;
            requiredDepositAmount =
                policy?.DepositRequirementType == DepositRequirementType.FixedAmount
                    ? policy.DepositRequirementValue
                    : 0m;
            confirmationMode =
                policy?.ConfirmationMode
                ?? ConfirmationMode.ManualAfterRequirements;
        }

        var proposalAccepted = contract?.AcceptedProposalId is Guid proposalId
            ? await dbContext.Proposals.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == proposalId
                    && entity.EventId == eventId
                    && entity.Status == ProposalStatus.Accepted
                    && entity.AcceptedVersionId
                        == contract.AcceptedProposalVersionId,
                cancellationToken)
            : await dbContext.Proposals.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId
                    && entity.Status == ProposalStatus.Accepted,
                cancellationToken);
        var contractCompleted = contract?.Status == ContractStatus.Completed;
        var approvedDeposit = contract is null
            ? 0m
            : await (
                from allocation in dbContext.PaymentAllocations.AsNoTracking()
                join payment in dbContext.PaymentRecords.AsNoTracking()
                    on new
                    {
                        allocation.OrganizationId,
                        Id = allocation.PaymentRecordId
                    }
                    equals new { payment.OrganizationId, payment.Id }
                join installment in dbContext.PaymentInstallments.AsNoTracking()
                    on new
                    {
                        allocation.OrganizationId,
                        Id = allocation.PaymentInstallmentId
                    }
                    equals new { installment.OrganizationId, installment.Id }
                join plan in dbContext.PaymentPlans.AsNoTracking()
                    on new
                    {
                        installment.OrganizationId,
                        Id = installment.PaymentPlanId
                    }
                    equals new { plan.OrganizationId, plan.Id }
                where allocation.OrganizationId == organizationId
                    && allocation.ReversedAt == null
                    && payment.Status == PaymentRecordStatus.Approved
                    && installment.InstallmentType == InstallmentType.Deposit
                    && plan.ContractId == contract.Id
                select allocation.Amount)
                .SumAsync(cancellationToken);
        var depositSatisfied = approvedDeposit >= requiredDepositAmount;
        var missingSigners = contract is null
            ? 0
            : await dbContext.ContractSigners
                .AsNoTracking()
                .CountAsync(
                    entity =>
                        entity.OrganizationId == organizationId
                        && entity.ContractId == contract.Id
                        && entity.IsRequired
                        && entity.Status != ContractSignerStatus.Signed,
                    cancellationToken);
        var missing = new List<string>();
        if (requireAcceptedProposal && !proposalAccepted)
        {
            missing.Add("Propuesta por aceptar");
        }

        if (requireCompletedContract && !contractCompleted)
        {
            missing.Add("Contrato por completar");
        }

        if (!depositSatisfied)
        {
            missing.Add("Anticipo por cubrir");
        }

        if (missingSigners > 0)
        {
            missing.Add("Firmas pendientes");
        }

        var ready = missing.Count == 0;
        return new ContractingReadinessResponse(
            proposalAccepted,
            contractCompleted,
            requiredDepositAmount,
            approvedDeposit,
            depositSatisfied,
            missingSigners,
            missing,
            ready,
            confirmationMode,
            eventStatus.ToString());
    }
}
