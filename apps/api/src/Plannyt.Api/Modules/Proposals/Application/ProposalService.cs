using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.BuildingBlocks.Http;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Proposals.Domain;
using Plannyt.Api.Modules.Proposals.Pdf;
using Plannyt.Api.Modules.Proposals.Security;

namespace Plannyt.Api.Modules.Proposals.Application;

public sealed class ProposalService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    ICurrentUser currentUser,
    ProposalTotalsCalculator totalsCalculator,
    ProposalTokenService tokenService,
    IProposalPdfGenerator pdfGenerator,
    AuditService auditService,
    ProspectStatusTransitionService prospectTransitionService,
    IOptions<FrontendOptions> frontendOptions,
    TimeProvider timeProvider)
{
    public async Task<PagedResponse<ProposalListItemResponse>> GetPageAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        string? status,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsView,
            null,
            cancellationToken);
        ValidatePage(page, pageSize);
        var query = dbContext.Proposals
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(entity =>
                EF.Functions.ILike(entity.ProposalNumber, $"%{term}%")
                || dbContext.Prospects.Any(prospect =>
                    prospect.OrganizationId == organizationId
                    && prospect.Id == entity.ProspectId
                    && EF.Functions.ILike(prospect.DisplayName, $"%{term}%"))
                || dbContext.Clients.Any(client =>
                    client.OrganizationId == organizationId
                    && client.Id == entity.ClientId
                    && EF.Functions.ILike(client.DisplayName, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProposalStatus>(status, true, out var parsed))
            {
                throw new RequestValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["status"] = ["El estado solicitado no es válido."]
                    });
            }

            query = query.Where(entity => entity.Status == parsed);
        }

        var count = await query.CountAsync(cancellationToken);
        var proposals = await query
            .OrderByDescending(entity => entity.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = new List<ProposalListItemResponse>(proposals.Count);
        foreach (var proposal in proposals)
        {
            var target = await GetRecipientNameAsync(
                proposal,
                cancellationToken);
            var grandTotal = await dbContext.ProposalVersions
                .AsNoTracking()
                .Where(version =>
                    version.OrganizationId == organizationId
                    && version.ProposalId == proposal.Id
                    && version.VersionNumber == proposal.CurrentVersionNumber)
                .Select(version => (decimal?)version.GrandTotal)
                .SingleOrDefaultAsync(cancellationToken);
            items.Add(new ProposalListItemResponse(
                proposal.Id,
                proposal.ProposalNumber,
                proposal.ProspectId,
                proposal.ClientId,
                proposal.EventId,
                target,
                proposal.Status,
                proposal.CurrentVersionNumber,
                proposal.CurrencyCode,
                proposal.ValidUntil,
                grandTotal,
                proposal.UpdatedAt));
        }

        return new PagedResponse<ProposalListItemResponse>(
            items,
            page,
            pageSize,
            count);
    }

    public async Task<ProposalResponse> GetAsync(
        Guid organizationId,
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsView,
            null,
            cancellationToken);
        var proposal = await FindAsync(
            organizationId,
            proposalId,
            true,
            cancellationToken);
        return await BuildAdminResponseAsync(
            proposal,
            access.Permissions.Contains(Permissions.ProposalsViewInternal),
            cancellationToken);
    }

    public async Task<ProposalResponse> CreateAsync(
        Guid organizationId,
        ProposalDraftRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsCreate,
            null,
            cancellationToken);
        ProposalRequestValidator.Validate(request);
        EnsureInternalNotesPermission(request.InternalNotes, access.Permissions);
        await ValidateReferencesAsync(
            organizationId,
            request,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var proposal = Proposal.Create(
            organizationId,
            request.ProspectId,
            request.ClientId,
            request.EventId,
            CreateProposalNumber(now),
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.ValidUntil,
            Normalize(request.SharedIntroduction),
            Normalize(request.SharedTerms),
            Normalize(request.InternalNotes),
            request.GeneralDiscountType,
            request.GeneralDiscountValue,
            request.CouponId,
            access.UserAccountId,
            now);
        dbContext.Proposals.Add(proposal);
        ReplaceDraftLines(proposal, request.Lines);
        await MoveProspectToDraftAsync(
            proposal,
            access.UserAccountId,
            now,
            cancellationToken);
        auditService.Add(
            organizationId,
            request.EventId,
            access.UserAccountId,
            "proposal.created",
            nameof(Proposal),
            proposal.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildAdminResponseAsync(
            proposal,
            access.Permissions.Contains(Permissions.ProposalsViewInternal),
            cancellationToken);
    }

    public async Task<ProposalResponse> UpdateDraftAsync(
        Guid organizationId,
        Guid proposalId,
        ProposalDraftRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsUpdateDraft,
            null,
            cancellationToken);
        ProposalRequestValidator.Validate(request);
        EnsureInternalNotesPermission(request.InternalNotes, access.Permissions);
        await ValidateReferencesAsync(
            organizationId,
            request,
            cancellationToken);
        var proposal = await FindAsync(
            organizationId,
            proposalId,
            false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (proposal.Status is ProposalStatus.ChangesRequested
            or ProposalStatus.Rejected)
        {
            proposal.StartRevision(now);
        }

        proposal.UpdateDraft(
            request.ProspectId,
            request.ClientId,
            request.EventId,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.ValidUntil,
            Normalize(request.SharedIntroduction),
            Normalize(request.SharedTerms),
            access.Permissions.Contains(Permissions.ProposalsViewInternal)
                ? Normalize(request.InternalNotes)
                : proposal.InternalNotes,
            request.GeneralDiscountType,
            request.GeneralDiscountValue,
            request.CouponId,
            now);
        var oldLines = await dbContext.ProposalDraftLines
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ProposalId == proposalId)
            .ToListAsync(cancellationToken);
        dbContext.ProposalDraftLines.RemoveRange(oldLines);
        ReplaceDraftLines(proposal, request.Lines);
        auditService.Add(
            organizationId,
            proposal.EventId,
            access.UserAccountId,
            "proposal.draft_updated",
            nameof(Proposal),
            proposal.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildAdminResponseAsync(
            proposal,
            access.Permissions.Contains(Permissions.ProposalsViewInternal),
            cancellationToken);
    }

    public async Task<ProposalVersionResponse> PublishAsync(
        Guid organizationId,
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsPublish,
            null,
            cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var proposal = await FindAsync(
            organizationId,
            proposalId,
            false,
            cancellationToken);
        proposal.EnsureDraftEditable();
        var draftLines = await GetDraftLinesAsync(
            organizationId,
            proposalId,
            cancellationToken);
        var coupon = await GetCouponForPublishAsync(
            proposal,
            cancellationToken);
        var calculation = Calculate(
            draftLines,
            proposal,
            coupon?.DiscountType ?? DiscountType.None,
            coupon?.DiscountValue ?? 0m);
        var now = timeProvider.GetUtcNow();
        var versionNumber = proposal.CurrentVersionNumber + 1;
        var version = ProposalVersion.Create(
            organizationId,
            proposal.Id,
            versionNumber,
            calculation.Subtotal,
            calculation.DiscountTotal,
            calculation.TaxTotal,
            calculation.GrandTotal,
            proposal.CurrencyCode,
            proposal.ValidUntil,
            proposal.SharedIntroduction,
            proposal.SharedTerms,
            proposal.GeneralDiscountType,
            proposal.GeneralDiscountValue,
            calculation.GeneralDiscountTotal,
            coupon?.Code,
            coupon?.Id,
            calculation.CouponDiscountTotal,
            access.UserAccountId,
            now);
        dbContext.ProposalVersions.Add(version);
        var versionLines = calculation.Lines.Select(line => ProposalLine.Create(
            organizationId,
            version.Id,
            line.Source.Description,
            line.Source.ServiceCatalogItemId,
            line.Source.PackageId,
            line.Source.Quantity,
            line.Source.UnitPrice,
            line.Source.DiscountType.ToString(),
            line.Source.DiscountValue,
            line.Source.TaxRate,
            line.LineSubtotal,
            line.LineDiscount,
            line.LineTax,
            line.LineTotal,
            line.Source.IsOptional,
            line.Source.SortOrder)).ToList();
        dbContext.ProposalLines.AddRange(versionLines);
        proposal.RecordPublishedVersion(versionNumber, now);
        await RevokeLinksAsync(proposal.Id, now, cancellationToken);
        auditService.Add(
            organizationId,
            proposal.EventId,
            access.UserAccountId,
            "proposal.version_published",
            nameof(ProposalVersion),
            version.Id,
            new Dictionary<string, object?>
            {
                ["proposalId"] = proposal.Id,
                ["versionNumber"] = versionNumber,
                ["grandTotal"] = version.GrandTotal
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToVersionResponse(version, versionLines);
    }

    public async Task<ProposalShareLinkResponse> SendAsync(
        Guid organizationId,
        Guid proposalId,
        SendProposalRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsSend,
            null,
            cancellationToken);
        var proposal = await FindAsync(
            organizationId,
            proposalId,
            false,
            cancellationToken);
        if (proposal.Status is ProposalStatus.Draft
            or ProposalStatus.Negotiation
            or ProposalStatus.ChangesRequested
            or ProposalStatus.Rejected)
        {
            throw new ConflictException(
                "Publica el borrador vigente antes de generar el enlace.");
        }

        var version = await GetCurrentVersionAsync(
            proposal,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var expiresAt = request.ExpiresAt ?? version.ValidUntil;
        if (expiresAt <= now || expiresAt > version.ValidUntil)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["expiresAt"] =
                    [
                        "La expiración debe ser futura y no superar la vigencia de la versión."
                    ]
                });
        }

        await RevokeLinksAsync(proposal.Id, now, cancellationToken);
        var token = tokenService.Create();
        var link = ProposalShareLink.Create(
            organizationId,
            proposal.Id,
            version.Id,
            token.Hash,
            expiresAt,
            access.UserAccountId,
            now);
        dbContext.ProposalShareLinks.Add(link);
        proposal.MarkSent(now);
        await MoveProspectToSentAsync(
            proposal,
            access.UserAccountId,
            now,
            cancellationToken);
        auditService.Add(
            organizationId,
            proposal.EventId,
            access.UserAccountId,
            "proposal.sent",
            nameof(Proposal),
            proposal.Id,
            new Dictionary<string, object?>
            {
                ["versionId"] = version.Id,
                ["expiresAt"] = expiresAt
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ProposalShareLinkResponse(
            link.Id,
            version.Id,
            expiresAt,
            $"{frontendOptions.Value.PublicUrl.TrimEnd('/')}/proposal/{token.Value}");
    }

    public async Task CancelAsync(
        Guid organizationId,
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsCancel,
            null,
            cancellationToken);
        var proposal = await FindAsync(
            organizationId,
            proposalId,
            false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        proposal.Cancel(now);
        await RevokeLinksAsync(proposal.Id, now, cancellationToken);
        auditService.Add(
            organizationId,
            proposal.EventId,
            access.UserAccountId,
            "proposal.cancelled",
            nameof(Proposal),
            proposal.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProposalCommentResponse> AddAdminCommentAsync(
        Guid organizationId,
        Guid proposalId,
        CreateProposalCommentRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsManageComments,
            null,
            cancellationToken);
        ProposalRequestValidator.Validate(request);
        if (request.Visibility == ProposalCommentVisibility.Internal
            && !access.Permissions.Contains(Permissions.ProposalsViewInternal))
        {
            throw new ForbiddenException(
                "No tienes permiso para administrar comentarios internos.");
        }

        var proposal = await FindAsync(
            organizationId,
            proposalId,
            true,
            cancellationToken);
        await EnsureCommentReferencesAsync(
            proposal,
            request.ProposalVersionId,
            request.ProposalLineId,
            request.ParentCommentId,
            cancellationToken);
        var comment = ProposalComment.Create(
            organizationId,
            proposal.Id,
            request.ProposalVersionId,
            request.ProposalLineId,
            access.UserAccountId,
            request.AuthorDisplayName.Trim(),
            request.Content.Trim(),
            request.Visibility,
            request.ParentCommentId,
            timeProvider.GetUtcNow());
        dbContext.ProposalComments.Add(comment);
        auditService.Add(
            organizationId,
            proposal.EventId,
            access.UserAccountId,
            "proposal.comment_created",
            nameof(ProposalComment),
            comment.Id,
            new Dictionary<string, object?>
            {
                ["proposalId"] = proposal.Id,
                ["visibility"] = comment.Visibility.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCommentResponse(comment);
    }

    public async Task<ProposalCommentResponse> ResolveCommentAsync(
        Guid organizationId,
        Guid proposalId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsManageComments,
            null,
            cancellationToken);
        var comment = await dbContext.ProposalComments.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.ProposalId == proposalId
                && entity.Id == commentId,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró el comentario.");
        if (comment.Visibility == ProposalCommentVisibility.Internal
            && !access.Permissions.Contains(Permissions.ProposalsViewInternal))
        {
            throw new NotFoundException("No se encontró el comentario.");
        }

        comment.Resolve(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCommentResponse(comment);
    }

    public async Task<ProposalPublicResponse> GetPublicAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var (link, proposal, version) = await ResolvePublicAsync(
            token,
            true,
            cancellationToken);
        return await BuildPublicResponseAsync(
            proposal,
            version,
            cancellationToken);
    }

    public async Task<ProposalCommentResponse> AddPublicCommentAsync(
        string token,
        ProposalPublicCommentRequest request,
        CancellationToken cancellationToken)
    {
        ProposalRequestValidator.Validate(request);
        var (_, proposal, version) = await ResolvePublicAsync(
            token,
            false,
            cancellationToken);
        await EnsureCommentReferencesAsync(
            proposal,
            version.Id,
            request.ProposalLineId,
            request.ParentCommentId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var comment = ProposalComment.Create(
            proposal.OrganizationId,
            proposal.Id,
            version.Id,
            request.ProposalLineId,
            null,
            request.AuthorDisplayName.Trim(),
            request.Content.Trim(),
            ProposalCommentVisibility.ClientShared,
            request.ParentCommentId,
            now);
        dbContext.ProposalComments.Add(comment);
        if (proposal.ProspectId is Guid prospectId)
        {
            dbContext.ProspectActivities.Add(ProspectActivity.Create(
                proposal.OrganizationId,
                prospectId,
                ProspectActivityType.ClientComment,
                "Comentario en propuesta",
                null,
                null,
                now,
                null,
                CommercialVisibility.ClientShared,
                proposal.CreatedBy,
                now));
        }

        auditService.Add(
            proposal.OrganizationId,
            proposal.EventId,
            null,
            "proposal.public_comment_created",
            nameof(ProposalComment),
            comment.Id,
            new Dictionary<string, object?>
            {
                ["proposalId"] = proposal.Id,
                ["versionId"] = version.Id
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCommentResponse(comment);
    }

    public Task<ProposalPublicResponse> RequestChangesAsync(
        string token,
        ProposalDecisionRequest request,
        CancellationToken cancellationToken) =>
        ApplyPublicDecisionAsync(
            token,
            request,
            PublicDecision.RequestChanges,
            cancellationToken);

    public Task<ProposalPublicResponse> AcceptAsync(
        string token,
        ProposalDecisionRequest request,
        CancellationToken cancellationToken) =>
        ApplyPublicDecisionAsync(
            token,
            request,
            PublicDecision.Accept,
            cancellationToken);

    public Task<ProposalPublicResponse> RejectAsync(
        string token,
        ProposalDecisionRequest request,
        CancellationToken cancellationToken) =>
        ApplyPublicDecisionAsync(
            token,
            request,
            PublicDecision.Reject,
            cancellationToken);

    public async Task<byte[]> GetPublicPdfAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var publicProposal = await GetPublicAsync(token, cancellationToken);
        return pdfGenerator.Generate(publicProposal);
    }

    public async Task<byte[]> GetAdminVersionPdfAsync(
        Guid organizationId,
        Guid proposalId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsView,
            null,
            cancellationToken);
        var proposal = await FindAsync(
            organizationId,
            proposalId,
            true,
            cancellationToken);
        var version = await dbContext.ProposalVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.ProposalId == proposalId
                    && entity.Id == versionId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la versión.");
        var publicProposal = await BuildPublicResponseAsync(
            proposal,
            version,
            cancellationToken);
        return pdfGenerator.Generate(publicProposal);
    }

    public async Task<ProposalResponse> DuplicateAsync(
        Guid organizationId,
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsCreate,
            null,
            cancellationToken);
        var source = await FindAsync(
            organizationId,
            proposalId,
            true,
            cancellationToken);
        var version = await GetCurrentVersionAsync(source, cancellationToken);
        var sourceLines = await dbContext.ProposalLines
            .AsNoTracking()
            .Where(line =>
                line.OrganizationId == organizationId
                && line.ProposalVersionId == version.Id)
            .OrderBy(line => line.SortOrder)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var duplicate = Proposal.Create(
            organizationId,
            source.ProspectId,
            source.ClientId,
            source.EventId,
            CreateProposalNumber(now),
            version.CurrencyCode,
            now.AddDays(14),
            version.SharedIntroduction,
            version.SharedTerms,
            null,
            version.GeneralDiscountType,
            version.GeneralDiscountValue,
            version.CouponId,
            access.UserAccountId,
            now);
        dbContext.Proposals.Add(duplicate);
        dbContext.ProposalDraftLines.AddRange(sourceLines.Select(line =>
            ProposalDraftLine.Create(
                organizationId,
                duplicate.Id,
                line.Description,
                line.ServiceCatalogItemId,
                line.PackageId,
                line.Quantity,
                line.UnitPrice,
                Enum.Parse<DiscountType>(line.DiscountType),
                line.DiscountValue,
                line.TaxRate,
                line.IsOptional,
                line.SortOrder)));
        auditService.Add(
            organizationId,
            duplicate.EventId,
            access.UserAccountId,
            "proposal.duplicated",
            nameof(Proposal),
            duplicate.Id,
            new Dictionary<string, object?>
            {
                ["sourceProposalId"] = source.Id,
                ["sourceVersionId"] = version.Id
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildAdminResponseAsync(
            duplicate,
            access.Permissions.Contains(Permissions.ProposalsViewInternal),
            cancellationToken);
    }

    public async Task<Guid> LinkPreliminaryEventAsync(
        Guid organizationId,
        Guid proposalId,
        LinkProposalEventRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            request.ExistingEventId is null
                ? Permissions.EventsCreate
                : Permissions.EventsUpdate,
            null,
            cancellationToken);
        var proposal = await FindAsync(
            organizationId,
            proposalId,
            false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        Event eventEntity;
        if (request.ExistingEventId is Guid existingId)
        {
            eventEntity = await dbContext.Events.SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == existingId
                    && entity.Status == EventStatus.Preliminary,
                cancellationToken)
                ?? throw new NotFoundException(
                    "No se encontró el evento preliminar.");
        }
        else
        {
            ValidateNewEvent(request);
            eventEntity = Event.Create(
                organizationId,
                request.Name!.Trim(),
                request.EventType!.Trim(),
                request.StartDateTime!.Value,
                null,
                request.TimeZone!.Trim(),
                request.City!.Trim(),
                request.CountryCode!.Trim().ToUpperInvariant(),
                null,
                request.EstimatedGuestCount,
                access.UserAccountId,
                now);
            dbContext.Events.Add(eventEntity);
        }

        proposal.LinkEvent(eventEntity.Id, now);
        if (proposal.ClientId is Guid clientId
            && !await dbContext.EventClients.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventEntity.Id
                    && entity.ClientId == clientId,
                cancellationToken))
        {
            dbContext.EventClients.Add(EventClient.Create(
                organizationId,
                eventEntity.Id,
                clientId,
                EventClientRelationshipType.PrimaryClient,
                true,
                true,
                now));
        }

        auditService.Add(
            organizationId,
            eventEntity.Id,
            access.UserAccountId,
            request.ExistingEventId is null
                ? "proposal.preliminary_event_created"
                : "proposal.event_linked",
            nameof(Proposal),
            proposal.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return eventEntity.Id;
    }

    public async Task<IReadOnlyList<ProposalListItemResponse>>
        GetPortalProposalsAsync(CancellationToken cancellationToken)
    {
        var clientIds = await GetCurrentUserClientIdsAsync(cancellationToken);
        var eventIds = await GetCurrentUserEventIdsAsync(cancellationToken);
        var proposals = await dbContext.Proposals
            .AsNoTracking()
            .Where(entity =>
                ((entity.ClientId != null
                    && clientIds.Contains(entity.ClientId.Value))
                 || (entity.EventId != null
                    && eventIds.Contains(entity.EventId.Value)))
                && entity.CurrentVersionNumber > 0
                && entity.Status != ProposalStatus.Cancelled)
            .OrderByDescending(entity => entity.UpdatedAt)
            .ToListAsync(cancellationToken);
        var responses = new List<ProposalListItemResponse>();
        foreach (var proposal in proposals)
        {
            var version = await GetCurrentVersionAsync(
                proposal,
                cancellationToken);
            responses.Add(new ProposalListItemResponse(
                proposal.Id,
                proposal.ProposalNumber,
                proposal.ProspectId,
                proposal.ClientId,
                proposal.EventId,
                await GetRecipientNameAsync(proposal, cancellationToken),
                proposal.Status,
                proposal.CurrentVersionNumber,
                proposal.CurrencyCode,
                proposal.ValidUntil,
                version.GrandTotal,
                proposal.UpdatedAt));
        }

        return responses;
    }

    public async Task<ProposalPublicResponse> GetPortalProposalAsync(
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var clientIds = await GetCurrentUserClientIdsAsync(cancellationToken);
        var eventIds = await GetCurrentUserEventIdsAsync(cancellationToken);
        var proposal = await dbContext.Proposals
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.Id == proposalId
                    && ((entity.ClientId != null
                        && clientIds.Contains(entity.ClientId.Value))
                     || (entity.EventId != null
                        && eventIds.Contains(entity.EventId.Value)))
                    && entity.CurrentVersionNumber > 0
                    && entity.Status != ProposalStatus.Cancelled,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la propuesta.");
        var version = await GetCurrentVersionAsync(proposal, cancellationToken);
        return await BuildPublicResponseAsync(
            proposal,
            version,
            cancellationToken);
    }

    public async Task<byte[]> GetPortalProposalPdfAsync(
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var proposal = await GetPortalProposalAsync(
            proposalId,
            cancellationToken);
        return pdfGenerator.Generate(proposal);
    }

    private async Task<ProposalPublicResponse> ApplyPublicDecisionAsync(
        string token,
        ProposalDecisionRequest request,
        PublicDecision decision,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var (_, proposal, version) = await ResolvePublicAsync(
            token,
            false,
            cancellationToken);
        if (proposal.CurrentVersionNumber != version.VersionNumber)
        {
            throw new ConflictException(
                "Esta versión fue sustituida por una más reciente.");
        }

        var now = timeProvider.GetUtcNow();
        switch (decision)
        {
            case PublicDecision.RequestChanges:
                proposal.RequestChanges(now);
                await MoveProspectToNegotiationAsync(
                    proposal,
                    now,
                    cancellationToken);
                break;
            case PublicDecision.Accept:
                if (version.ValidUntil < now)
                {
                    throw new ConflictException("La propuesta está vencida.");
                }

                if (version.CouponId is Guid couponId)
                {
                    var coupon = await dbContext.Coupons.SingleAsync(
                        entity =>
                            entity.OrganizationId == proposal.OrganizationId
                            && entity.Id == couponId,
                        cancellationToken);
                    coupon.RegisterSnapshotUse(now);
                }

                proposal.Accept(version.Id, now);
                break;
            case PublicDecision.Reject:
                proposal.Reject(now);
                break;
            default:
                throw new InvalidOperationException("Decisión pública no soportada.");
        }

        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            var comment = ProposalComment.Create(
                proposal.OrganizationId,
                proposal.Id,
                version.Id,
                null,
                null,
                Normalize(request.AuthorDisplayName) ?? "Destinatario",
                request.Reason.Trim(),
                ProposalCommentVisibility.ClientShared,
                null,
                now);
            dbContext.ProposalComments.Add(comment);
        }

        var action = decision switch
        {
            PublicDecision.RequestChanges => "proposal.changes_requested",
            PublicDecision.Accept => "proposal.accepted",
            PublicDecision.Reject => "proposal.rejected",
            _ => throw new InvalidOperationException()
        };
        auditService.Add(
            proposal.OrganizationId,
            proposal.EventId,
            null,
            action,
            nameof(Proposal),
            proposal.Id,
            new Dictionary<string, object?>
            {
                ["versionId"] = version.Id,
                ["versionNumber"] = version.VersionNumber
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await BuildPublicResponseAsync(
            proposal,
            version,
            cancellationToken);
    }

    private async Task<(ProposalShareLink Link, Proposal Proposal, ProposalVersion Version)>
        ResolvePublicAsync(
            string token,
            bool markViewed,
            CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.Hash(token);
        if (string.IsNullOrEmpty(tokenHash))
        {
            throw new NotFoundException("No se encontró la propuesta.");
        }

        var link = await dbContext.ProposalShareLinks.SingleOrDefaultAsync(
            entity => entity.TokenHash == tokenHash,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró la propuesta.");
        var now = timeProvider.GetUtcNow();
        if (!link.IsAvailable(now))
        {
            throw new GoneException(
                "El enlace de la propuesta venció o fue revocado.");
        }

        var proposal = await dbContext.Proposals.SingleAsync(
            entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.Id == link.ProposalId,
            cancellationToken);
        if (proposal.Status == ProposalStatus.Cancelled)
        {
            throw new GoneException("La propuesta fue cancelada.");
        }

        var version = await dbContext.ProposalVersions
            .AsNoTracking()
            .SingleAsync(
                entity =>
                    entity.OrganizationId == link.OrganizationId
                    && entity.Id == link.ProposalVersionId
                    && entity.ProposalId == proposal.Id,
                cancellationToken);
        if (markViewed)
        {
            var firstView = link.FirstViewedAt is null;
            link.MarkViewed(now);
            proposal.MarkViewed(now);
            if (firstView)
            {
                auditService.Add(
                    proposal.OrganizationId,
                    proposal.EventId,
                    null,
                    "proposal.share_opened",
                    nameof(ProposalShareLink),
                    link.Id,
                    new Dictionary<string, object?>
                    {
                        ["proposalId"] = proposal.Id,
                        ["versionId"] = version.Id
                    });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return (link, proposal, version);
    }

    private async Task<ProposalPublicResponse> BuildPublicResponseAsync(
        Proposal proposal,
        ProposalVersion version,
        CancellationToken cancellationToken)
    {
        var organizationName = await dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.Id == proposal.OrganizationId)
            .Select(entity => entity.Name)
            .SingleAsync(cancellationToken);
        var lines = await dbContext.ProposalLines
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.ProposalVersionId == version.Id)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => new ProposalPublicLineResponse(
                entity.Id,
                entity.Description,
                entity.Quantity,
                entity.UnitPrice,
                entity.LineDiscount,
                entity.LineTax,
                entity.LineTotal,
                entity.IsOptional,
                entity.SortOrder))
            .ToListAsync(cancellationToken);
        var comments = await dbContext.ProposalComments
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.ProposalVersionId == version.Id
                && entity.Visibility == ProposalCommentVisibility.ClientShared)
            .OrderBy(entity => entity.CreatedAt)
            .Select(entity => new ProposalPublicCommentResponse(
                entity.Id,
                entity.ProposalLineId,
                entity.AuthorDisplayName,
                entity.Content,
                entity.Status,
                entity.ParentCommentId,
                entity.CreatedAt))
            .ToListAsync(cancellationToken);
        string? eventSummary = null;
        if (proposal.EventId is Guid eventId)
        {
            var eventDetails = await dbContext.Events
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == proposal.OrganizationId
                    && entity.Id == eventId)
                .Select(entity => new
                {
                    entity.Name,
                    entity.StartDateTime,
                    entity.City
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (eventDetails is not null)
            {
                eventSummary =
                    $"{eventDetails.Name} · "
                    + $"{eventDetails.StartDateTime:dd/MM/yyyy} · "
                    + eventDetails.City;
            }
        }
        return new ProposalPublicResponse(
            proposal.Id,
            version.Id,
            proposal.ProposalNumber,
            version.VersionNumber,
            organizationName,
            await GetRecipientNameAsync(proposal, cancellationToken),
            eventSummary,
            proposal.Status,
            version.CurrencyCode,
            version.ValidUntil,
            version.SharedIntroduction,
            version.SharedTerms,
            new ProposalTotalsResponse(
                version.Subtotal,
                version.DiscountTotal,
                version.GeneralDiscountTotal,
                version.CouponDiscountTotal,
                version.TaxTotal,
                version.GrandTotal),
            lines,
            comments);
    }

    private async Task<ProposalResponse> BuildAdminResponseAsync(
        Proposal proposal,
        bool includeInternal,
        CancellationToken cancellationToken)
    {
        var draftLines = await GetDraftLinesAsync(
            proposal.OrganizationId,
            proposal.Id,
            cancellationToken);
        var coupon = proposal.CouponId is Guid couponId
            ? await dbContext.Coupons
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    entity =>
                        entity.OrganizationId == proposal.OrganizationId
                        && entity.Id == couponId,
                    cancellationToken)
            : null;
        var calculation = Calculate(
            draftLines,
            proposal,
            coupon?.DiscountType ?? DiscountType.None,
            coupon?.DiscountValue ?? 0m);
        var versions = await dbContext.ProposalVersions
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.ProposalId == proposal.Id)
            .OrderByDescending(entity => entity.VersionNumber)
            .Select(entity => new ProposalVersionSummaryResponse(
                entity.Id,
                entity.VersionNumber,
                entity.GrandTotal,
                entity.CurrencyCode,
                entity.ValidUntil,
                entity.PublishedAt))
            .ToListAsync(cancellationToken);
        var commentQuery = dbContext.ProposalComments
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.ProposalId == proposal.Id);
        if (!includeInternal)
        {
            commentQuery = commentQuery.Where(
                entity =>
                    entity.Visibility == ProposalCommentVisibility.ClientShared);
        }

        var comments = await commentQuery
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new ProposalCommentResponse(
                entity.Id,
                entity.ProposalVersionId,
                entity.ProposalLineId,
                entity.AuthorUserId,
                entity.AuthorDisplayName,
                entity.Content,
                entity.Visibility,
                entity.Status,
                entity.ParentCommentId,
                entity.CreatedAt))
            .ToListAsync(cancellationToken);
        return new ProposalResponse(
            proposal.Id,
            proposal.ProposalNumber,
            proposal.ProspectId,
            proposal.ClientId,
            proposal.EventId,
            proposal.Status,
            proposal.CurrentVersionNumber,
            proposal.CurrencyCode,
            proposal.ValidUntil,
            proposal.SharedIntroduction,
            proposal.SharedTerms,
            includeInternal ? proposal.InternalNotes : null,
            proposal.GeneralDiscountType,
            proposal.GeneralDiscountValue,
            proposal.CouponId,
            ToTotals(calculation),
            calculation.Lines.Select(ToDraftLineResponse).ToList(),
            versions,
            comments,
            proposal.AcceptedVersionId,
            proposal.AcceptedAt,
            proposal.RejectedAt,
            proposal.CreatedAt,
            proposal.UpdatedAt);
    }

    private async Task<Proposal> FindAsync(
        Guid organizationId,
        Guid proposalId,
        bool noTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Proposals.Where(entity =>
            entity.OrganizationId == organizationId
            && entity.Id == proposalId);
        if (noTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró la propuesta.");
    }

    private async Task<ProposalVersion> GetCurrentVersionAsync(
        Proposal proposal,
        CancellationToken cancellationToken)
    {
        if (proposal.CurrentVersionNumber < 1)
        {
            throw new ConflictException(
                "La propuesta todavía no tiene una versión publicada.");
        }

        return await dbContext.ProposalVersions
            .AsNoTracking()
            .SingleAsync(
                entity =>
                    entity.OrganizationId == proposal.OrganizationId
                    && entity.ProposalId == proposal.Id
                    && entity.VersionNumber == proposal.CurrentVersionNumber,
                cancellationToken);
    }

    private async Task<List<ProposalDraftLine>> GetDraftLinesAsync(
        Guid organizationId,
        Guid proposalId,
        CancellationToken cancellationToken) =>
        await dbContext.ProposalDraftLines
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ProposalId == proposalId)
            .OrderBy(entity => entity.SortOrder)
            .ToListAsync(cancellationToken);

    private ProposalCalculation Calculate(
        IReadOnlyCollection<ProposalDraftLine> draftLines,
        Proposal proposal,
        DiscountType couponDiscountType,
        decimal couponDiscountValue) =>
        totalsCalculator.Calculate(
            draftLines.Select(line => new ProposalCalculationLine(
                line.Id,
                line.Description,
                line.ServiceCatalogItemId,
                line.PackageId,
                line.Quantity,
                line.UnitPrice,
                line.DiscountType,
                line.DiscountValue,
                line.TaxRate,
                line.IsOptional,
                line.SortOrder)).ToList(),
            proposal.GeneralDiscountType,
            proposal.GeneralDiscountValue,
            couponDiscountType,
            couponDiscountValue);

    private async Task<Coupon?> GetCouponForPublishAsync(
        Proposal proposal,
        CancellationToken cancellationToken)
    {
        if (proposal.CouponId is not Guid couponId)
        {
            return null;
        }

        var coupon = await dbContext.Coupons.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.Id == couponId,
            cancellationToken)
            ?? throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["couponId"] = ["No se encontró el cupón."]
                });
        if (!coupon.IsAvailable(timeProvider.GetUtcNow()))
        {
            throw new ConflictException(
                "El cupón ya no está disponible.");
        }

        return coupon;
    }

    private async Task ValidateReferencesAsync(
        Guid organizationId,
        ProposalDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ProspectId is Guid prospectId
            && !await dbContext.Prospects.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == prospectId
                    && entity.ArchivedAt == null,
                cancellationToken))
        {
            throw Validation("prospectId", "No se encontró el prospecto.");
        }

        if (request.ClientId is Guid clientId
            && !await dbContext.Clients.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == clientId
                    && entity.Status == ClientStatus.Active,
                cancellationToken))
        {
            throw Validation("clientId", "No se encontró el cliente.");
        }

        if (request.EventId is Guid eventId
            && !await dbContext.Events.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId
                    && entity.Status != EventStatus.Archived,
                cancellationToken))
        {
            throw Validation("eventId", "No se encontró el evento.");
        }

        if (request.CouponId is Guid couponId
            && !await dbContext.Coupons.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == couponId,
                cancellationToken))
        {
            throw Validation("couponId", "No se encontró el cupón.");
        }

        var serviceIds = request.Lines
            .Where(line => line.ServiceCatalogItemId is not null)
            .Select(line => line.ServiceCatalogItemId!.Value)
            .Distinct()
            .ToList();
        var servicesFound = await dbContext.ServiceCatalogItems.CountAsync(
            entity =>
                entity.OrganizationId == organizationId
                && serviceIds.Contains(entity.Id)
                && entity.ArchivedAt == null,
            cancellationToken);
        if (servicesFound != serviceIds.Count)
        {
            throw Validation(
                "lines",
                "Uno o más servicios no pertenecen al catálogo activo.");
        }

        var packageIds = request.Lines
            .Where(line => line.PackageId is not null)
            .Select(line => line.PackageId!.Value)
            .Distinct()
            .ToList();
        var packagesFound = await dbContext.Packages.CountAsync(
            entity =>
                entity.OrganizationId == organizationId
                && packageIds.Contains(entity.Id)
                && entity.ArchivedAt == null,
            cancellationToken);
        if (packagesFound != packageIds.Count)
        {
            throw Validation(
                "lines",
                "Uno o más paquetes no pertenecen al catálogo activo.");
        }
    }

    private void ReplaceDraftLines(
        Proposal proposal,
        IReadOnlyList<ProposalDraftLineRequest> lines)
    {
        dbContext.ProposalDraftLines.AddRange(lines.Select(line =>
            ProposalDraftLine.Create(
                proposal.OrganizationId,
                proposal.Id,
                line.Description.Trim(),
                line.ServiceCatalogItemId,
                line.PackageId,
                line.Quantity,
                line.UnitPrice,
                line.DiscountType,
                line.DiscountValue,
                line.TaxRate,
                line.IsOptional,
                line.SortOrder)));
    }

    private async Task RevokeLinksAsync(
        Guid proposalId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var links = await dbContext.ProposalShareLinks
            .Where(entity =>
                entity.ProposalId == proposalId
                && entity.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var link in links)
        {
            link.Revoke(now);
        }
    }

    private async Task EnsureCommentReferencesAsync(
        Proposal proposal,
        Guid versionId,
        Guid? lineId,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        var versionExists = await dbContext.ProposalVersions.AnyAsync(
            entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.ProposalId == proposal.Id
                && entity.Id == versionId,
            cancellationToken);
        if (!versionExists)
        {
            throw Validation(
                "proposalVersionId",
                "No se encontró la versión.");
        }

        if (lineId is Guid proposalLineId
            && !await dbContext.ProposalLines.AnyAsync(
                entity =>
                    entity.OrganizationId == proposal.OrganizationId
                    && entity.ProposalVersionId == versionId
                    && entity.Id == proposalLineId,
                cancellationToken))
        {
            throw Validation("proposalLineId", "No se encontró el concepto.");
        }

        if (parentId is Guid parentCommentId
            && !await dbContext.ProposalComments.AnyAsync(
                entity =>
                    entity.OrganizationId == proposal.OrganizationId
                    && entity.ProposalVersionId == versionId
                    && entity.Id == parentCommentId,
                cancellationToken))
        {
            throw Validation(
                "parentCommentId",
                "No se encontró el comentario padre.");
        }
    }

    private async Task MoveProspectToDraftAsync(
        Proposal proposal,
        Guid actorId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (proposal.ProspectId is not Guid prospectId)
        {
            return;
        }

        var prospect = await dbContext.Prospects.SingleAsync(
            entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.Id == prospectId,
            cancellationToken);
        if (prospect.Status == ProspectStatus.Opportunity)
        {
            dbContext.ProspectStatusHistory.Add(
                prospectTransitionService.ChangeStatus(
                    prospect,
                    ProspectStatus.ProposalDraft,
                    actorId,
                    now,
                    "Propuesta creada"));
        }
    }

    private async Task MoveProspectToSentAsync(
        Proposal proposal,
        Guid actorId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (proposal.ProspectId is not Guid prospectId)
        {
            return;
        }

        var prospect = await dbContext.Prospects.SingleAsync(
            entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.Id == prospectId,
            cancellationToken);
        if (prospect.Status is ProspectStatus.Opportunity
            or ProspectStatus.ProposalDraft)
        {
            dbContext.ProspectStatusHistory.Add(
                prospectTransitionService.ChangeStatus(
                    prospect,
                    ProspectStatus.ProposalSent,
                    actorId,
                    now,
                    "Propuesta enviada"));
        }

        dbContext.ProspectActivities.Add(ProspectActivity.Create(
            proposal.OrganizationId,
            prospectId,
            ProspectActivityType.ProposalSent,
            $"Propuesta {proposal.ProposalNumber} enviada",
            null,
            null,
            now,
            null,
            CommercialVisibility.Internal,
            actorId,
            now));
    }

    private async Task MoveProspectToNegotiationAsync(
        Proposal proposal,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (proposal.ProspectId is not Guid prospectId)
        {
            return;
        }

        var prospect = await dbContext.Prospects.SingleAsync(
            entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.Id == prospectId,
            cancellationToken);
        if (prospect.Status == ProspectStatus.ProposalSent)
        {
            dbContext.ProspectStatusHistory.Add(
                prospectTransitionService.ChangeStatus(
                    prospect,
                    ProspectStatus.Negotiation,
                    proposal.CreatedBy,
                    now,
                    "El destinatario solicitó cambios"));
        }
    }

    private async Task<string> GetRecipientNameAsync(
        Proposal proposal,
        CancellationToken cancellationToken)
    {
        if (proposal.ClientId is Guid clientId)
        {
            return await dbContext.Clients
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == proposal.OrganizationId
                    && entity.Id == clientId)
                .Select(entity => entity.DisplayName)
                .SingleAsync(cancellationToken);
        }

        return await dbContext.Prospects
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == proposal.OrganizationId
                && entity.Id == proposal.ProspectId)
            .Select(entity => entity.DisplayName)
            .SingleAsync(cancellationToken);
    }

    private async Task<List<Guid>> GetCurrentUserClientIdsAsync(
        CancellationToken cancellationToken)
    {
        var personIds = dbContext.People
            .AsNoTracking()
            .Where(entity =>
                entity.LinkedUserAccountId == currentUser.UserAccountId
                && entity.ArchivedAt == null)
            .Select(entity => new
            {
                entity.OrganizationId,
                entity.Id
            });
        return await dbContext.Clients
            .AsNoTracking()
            .Where(client =>
                client.PersonId != null
                && client.Status == ClientStatus.Active
                && personIds.Any(person =>
                    person.OrganizationId == client.OrganizationId
                    && person.Id == client.PersonId))
            .Select(client => client.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> GetCurrentUserEventIdsAsync(
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await dbContext.EventAccesses
            .AsNoTracking()
            .Where(entity =>
                entity.UserAccountId == currentUser.UserAccountId
                && entity.Status == EventAccessStatus.Active
                && entity.RevokedAt == null
                && entity.StartsAt <= now
                && (entity.ExpiresAt == null || entity.ExpiresAt > now))
            .Select(entity => entity.EventId)
            .ToListAsync(cancellationToken);
    }

    private static ProposalVersionResponse ToVersionResponse(
        ProposalVersion version,
        IReadOnlyCollection<ProposalLine> lines) =>
        new(
            version.Id,
            version.VersionNumber,
            new ProposalTotalsResponse(
                version.Subtotal,
                version.DiscountTotal,
                version.GeneralDiscountTotal,
                version.CouponDiscountTotal,
                version.TaxTotal,
                version.GrandTotal),
            version.CurrencyCode,
            version.ValidUntil,
            version.SharedIntroduction,
            version.SharedTerms,
            version.CouponCode,
            lines
                .OrderBy(line => line.SortOrder)
                .Select(line => new ProposalDraftLineResponse(
                    line.Id,
                    line.Description,
                    line.ServiceCatalogItemId,
                    line.PackageId,
                    line.Quantity,
                    line.UnitPrice,
                    Enum.Parse<DiscountType>(line.DiscountType),
                    line.DiscountValue,
                    line.TaxRate,
                    line.LineSubtotal,
                    line.LineDiscount,
                    line.LineTax,
                    line.LineTotal,
                    line.IsOptional,
                    line.SortOrder))
                .ToList(),
            version.PublishedAt);

    private static ProposalDraftLineResponse ToDraftLineResponse(
        CalculatedProposalLine line) =>
        new(
            line.Source.DraftLineId,
            line.Source.Description,
            line.Source.ServiceCatalogItemId,
            line.Source.PackageId,
            line.Source.Quantity,
            line.Source.UnitPrice,
            line.Source.DiscountType,
            line.Source.DiscountValue,
            line.Source.TaxRate,
            line.LineSubtotal,
            line.LineDiscount,
            line.LineTax,
            line.LineTotal,
            line.Source.IsOptional,
            line.Source.SortOrder);

    private static ProposalTotalsResponse ToTotals(
        ProposalCalculation calculation) =>
        new(
            calculation.Subtotal,
            calculation.DiscountTotal,
            calculation.GeneralDiscountTotal,
            calculation.CouponDiscountTotal,
            calculation.TaxTotal,
            calculation.GrandTotal);

    private static ProposalCommentResponse ToCommentResponse(
        ProposalComment comment) =>
        new(
            comment.Id,
            comment.ProposalVersionId,
            comment.ProposalLineId,
            comment.AuthorUserId,
            comment.AuthorDisplayName,
            comment.Content,
            comment.Visibility,
            comment.Status,
            comment.ParentCommentId,
            comment.CreatedAt);

    private static void EnsureInternalNotesPermission(
        string? internalNotes,
        IReadOnlySet<string> permissions)
    {
        if (!string.IsNullOrWhiteSpace(internalNotes)
            && !permissions.Contains(Permissions.ProposalsViewInternal))
        {
            throw new ForbiddenException(
                "No tienes permiso para administrar notas internas.");
        }
    }

    private static void ValidateNewEvent(LinkProposalEventRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["El nombre es obligatorio."];
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            errors["eventType"] = ["El tipo de evento es obligatorio."];
        }

        if (request.StartDateTime is null)
        {
            errors["startDateTime"] = ["La fecha estimada es obligatoria."];
        }

        if (string.IsNullOrWhiteSpace(request.TimeZone))
        {
            errors["timeZone"] = ["La zona horaria es obligatoria."];
        }
        else
        {
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone.Trim());
            }
            catch (Exception exception)
                when (exception is TimeZoneNotFoundException
                    or InvalidTimeZoneException)
            {
                errors["timeZone"] = ["La zona horaria IANA no es válida."];
            }
        }

        if (string.IsNullOrWhiteSpace(request.City))
        {
            errors["city"] = ["La ciudad es obligatoria."];
        }

        if (request.CountryCode?.Trim().Length != 2)
        {
            errors["countryCode"] =
                ["El país debe indicarse con dos letras."];
        }

        if (request.EstimatedGuestCount < 0)
        {
            errors["estimatedGuestCount"] =
                ["La cantidad de invitados no puede ser negativa."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static RequestValidationException Validation(
        string field,
        string message) =>
        new(new Dictionary<string, string[]>
        {
            [field] = [message]
        });

    private static string CreateProposalNumber(DateTimeOffset now) =>
        $"P-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..19].ToUpperInvariant();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidatePage(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["La página debe ser mayor o igual a 1."];
        }

        if (pageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["El tamaño de página debe estar entre 1 y 100."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private enum PublicDecision
    {
        RequestChanges,
        Accept,
        Reject
    }
}
