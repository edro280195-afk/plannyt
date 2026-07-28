using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Invitations.Application;

public sealed class InvitationDesignService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<GuestExperienceResponse> GetExperienceAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsView,
            cancellationToken);
        var experience = await GetOrCreateExperienceAsync(
            organizationId,
            eventId,
            cancellationToken);
        return ToExperienceResponse(experience);
    }

    public async Task<GuestExperienceResponse> UpdateExperienceAsync(
        Guid organizationId,
        Guid eventId,
        GuestExperienceRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsUpdateDraft,
            cancellationToken);
        ValidateExperience(request);
        var experience = await GetOrCreateExperienceAsync(
            organizationId,
            eventId,
            cancellationToken);
        experience.UpdateSettings(
            request.Language.Trim().ToLowerInvariant(),
            request.PublicTitle.Trim(),
            request.CelebrantDisplayName.Trim(),
            Normalize(request.WelcomeMessage),
            Normalize(request.ClosingMessage),
            request.ShowEventName,
            request.ShowEventDate,
            request.ShowParticipantNames,
            request.ShowCity,
            request.PrivateAccessOnly,
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest_experience.updated",
            nameof(EventGuestExperience),
            experience.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToExperienceResponse(experience);
    }

    public async Task<GuestExperienceResponse> SuspendExperienceAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsPublish,
            cancellationToken);
        var experience = await FindExperienceAsync(
            organizationId,
            eventId,
            cancellationToken);
        experience.Suspend(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest_experience.suspended",
            nameof(EventGuestExperience),
            experience.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToExperienceResponse(experience);
    }

    public async Task<GuestExperienceResponse> ResumeExperienceAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsPublish,
            cancellationToken);
        var experience = await FindExperienceAsync(
            organizationId,
            eventId,
            cancellationToken);
        experience.Resume(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest_experience.resumed",
            nameof(EventGuestExperience),
            experience.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToExperienceResponse(experience);
    }

    public async Task<IReadOnlyList<InvitationTemplateResponse>> GetTemplatesAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsView,
            cancellationToken);
        var templates = await dbContext.InvitationTemplates.AsNoTracking()
            .Where(entity =>
                entity.IsActive
                && (entity.IsGlobal || entity.OrganizationId == organizationId))
            .OrderByDescending(entity => entity.IsGlobal)
            .ThenBy(entity => entity.Name)
            .ToListAsync(cancellationToken);
        return templates.Select(ToTemplateResponse).ToList();
    }

    public async Task<InvitationTemplateResponse> CreateTemplateAsync(
        Guid organizationId,
        Guid eventId,
        InvitationTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsManageTemplates,
            cancellationToken);
        InvitationContentValidator.Validate(request.Name, request.Theme, request.Blocks);
        if (string.IsNullOrWhiteSpace(request.Description)
            || request.Description.Trim().Length > 240)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["description"] =
                    [
                        "La descripción es obligatoria y admite 240 caracteres."
                    ]
                });
        }

        var template = InvitationTemplate.CreateForOrganization(
            organizationId,
            request.Name.Trim(),
            request.Description.Trim(),
            InvitationContentValidator.SerializeTheme(request.Theme),
            InvitationContentValidator.SerializeBlocks(request.Blocks),
            timeProvider.GetUtcNow());
        dbContext.InvitationTemplates.Add(template);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation_template.created",
            nameof(InvitationTemplate),
            template.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTemplateResponse(template);
    }

    public async Task<InvitationTemplateResponse> UpdateTemplateAsync(
        Guid organizationId,
        Guid eventId,
        Guid templateId,
        InvitationTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsManageTemplates,
            cancellationToken);
        InvitationContentValidator.Validate(request.Name, request.Theme, request.Blocks);
        ValidateTemplateDescription(request.Description);
        var template = await FindOrganizationTemplateAsync(
            organizationId,
            templateId,
            cancellationToken);
        template.Update(
            request.Name.Trim(),
            request.Description.Trim(),
            InvitationContentValidator.SerializeTheme(request.Theme),
            InvitationContentValidator.SerializeBlocks(request.Blocks),
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation_template.updated",
            nameof(InvitationTemplate),
            template.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToTemplateResponse(template);
    }

    public async Task ArchiveTemplateAsync(
        Guid organizationId,
        Guid eventId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsManageTemplates,
            cancellationToken);
        var template = await FindOrganizationTemplateAsync(
            organizationId,
            templateId,
            cancellationToken);
        template.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation_template.archived",
            nameof(InvitationTemplate),
            template.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvitationDesignResponse>> GetDesignsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsView,
            cancellationToken);
        var designs = await dbContext.InvitationDesigns.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null)
            .OrderByDescending(entity => entity.UpdatedAt)
            .ToListAsync(cancellationToken);
        var responses = new List<InvitationDesignResponse>(designs.Count);
        foreach (var design in designs)
        {
            responses.Add(await BuildDesignResponseAsync(design, cancellationToken));
        }

        return responses;
    }

    public async Task<InvitationDesignResponse> GetDesignAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        CancellationToken cancellationToken)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsView,
            cancellationToken);
        var design = await FindDesignAsync(
            organizationId,
            eventId,
            designId,
            cancellationToken);
        return await BuildDesignResponseAsync(design, cancellationToken);
    }

    public async Task<InvitationDesignResponse> CreateDesignAsync(
        Guid organizationId,
        Guid eventId,
        CreateInvitationDesignRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsCreate,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["name"] = ["El nombre es obligatorio y admite 120 caracteres."]
                });
        }

        var theme = InvitationTemplateCatalog.DefaultTheme();
        var blocks = InvitationTemplateCatalog.DefaultBlocks(request.Name.Trim());
        if (request.TemplateId is not null)
        {
            var template = await dbContext.InvitationTemplates.AsNoTracking()
                .SingleOrDefaultAsync(entity =>
                    entity.Id == request.TemplateId
                    && entity.IsActive
                    && (entity.IsGlobal || entity.OrganizationId == organizationId),
                    cancellationToken)
                ?? throw new NotFoundException("No se encontró la plantilla.");
            theme = InvitationContentValidator.DeserializeTheme(template.ThemeJson);
            blocks = InvitationContentValidator.DeserializeBlocks(template.ContentJson);
        }

        InvitationContentValidator.Validate(request.Name, theme, blocks);
        var now = timeProvider.GetUtcNow();
        var design = InvitationDesign.Create(
            organizationId,
            eventId,
            request.TemplateId,
            request.Name.Trim(),
            InvitationContentValidator.SerializeTheme(theme),
            InvitationContentValidator.SerializeBlocks(blocks),
            access.UserAccountId,
            now);
        dbContext.InvitationDesigns.Add(design);
        var experience = await GetOrCreateExperienceAsync(
            organizationId,
            eventId,
            cancellationToken);
        experience.MarkReady(now);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation_design.created",
            nameof(InvitationDesign),
            design.Id,
            new Dictionary<string, object?> { ["templateId"] = request.TemplateId });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDesignResponseAsync(design, cancellationToken);
    }

    public async Task<InvitationDesignResponse> UpdateDesignAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        UpdateInvitationDesignRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsUpdateDraft,
            cancellationToken);
        InvitationContentValidator.Validate(request.Name, request.Theme, request.Blocks);
        var design = await FindDesignAsync(
            organizationId,
            eventId,
            designId,
            cancellationToken);
        var invalidatedApproval = design.Status is InvitationDesignStatus.Approved
            or InvitationDesignStatus.Published;
        design.UpdateDraft(
            request.Name.Trim(),
            InvitationContentValidator.SerializeTheme(request.Theme),
            InvitationContentValidator.SerializeBlocks(request.Blocks),
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            invalidatedApproval
                ? "invitation_design.approval_invalidated"
                : "invitation_design.draft_updated",
            nameof(InvitationDesign),
            design.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDesignResponseAsync(design, cancellationToken);
    }

    public async Task<InvitationDesignResponse> SubmitReviewAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsSubmitReview,
            cancellationToken);
        var design = await FindDesignAsync(
            organizationId,
            eventId,
            designId,
            cancellationToken);
        ValidateStoredDesign(design);
        var now = timeProvider.GetUtcNow();
        var versionNumber = design.SubmitForReview(now);
        var version = InvitationDesignVersion.Create(
            design,
            versionNumber,
            access.UserAccountId,
            now);
        dbContext.InvitationDesignVersions.Add(version);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation_design.submitted_review",
            nameof(InvitationDesign),
            design.Id,
            new Dictionary<string, object?>
            {
                ["versionId"] = version.Id,
                ["versionNumber"] = version.VersionNumber
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDesignResponseAsync(design, cancellationToken);
    }

    public Task<InvitationDesignResponse> AddCommentAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        Guid versionId,
        InvitationCommentRequest request,
        CancellationToken cancellationToken) =>
        ReviewAsync(
            organizationId,
            eventId,
            designId,
            versionId,
            request,
            InvitationReviewDecision.Comment,
            Permissions.InvitationDesignsView,
            cancellationToken);

    public Task<InvitationDesignResponse> ApproveAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        Guid versionId,
        InvitationCommentRequest request,
        CancellationToken cancellationToken) =>
        ReviewAsync(
            organizationId,
            eventId,
            designId,
            versionId,
            request,
            InvitationReviewDecision.Approved,
            Permissions.InvitationDesignsApprove,
            cancellationToken);

    public Task<InvitationDesignResponse> RequestChangesAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        Guid versionId,
        InvitationCommentRequest request,
        CancellationToken cancellationToken) =>
        ReviewAsync(
            organizationId,
            eventId,
            designId,
            versionId,
            request,
            InvitationReviewDecision.ChangesRequested,
            Permissions.InvitationDesignsApprove,
            cancellationToken);

    public async Task<InvitationDesignResponse> PublishAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        PublishInvitationDesignRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsPublish,
            cancellationToken);
        if (request.BypassApprovalForTesting
            && !access.Permissions.Contains(Permissions.InvitationDesignsPublishTesting))
        {
            throw new ForbiddenException(
                "No tienes permiso para omitir la aprobación en pruebas.");
        }

        var eventEntity = await dbContext.Events.SingleAsync(entity =>
            entity.OrganizationId == organizationId && entity.Id == eventId,
            cancellationToken);
        if (eventEntity.Status is not (EventStatus.Confirmed or EventStatus.Planning)
            && !request.BypassApprovalForTesting)
        {
            throw new ConflictException(
                "Solo eventos confirmados o en planeación pueden publicar invitaciones.");
        }

        var design = await FindDesignAsync(
            organizationId,
            eventId,
            designId,
            cancellationToken);
        var validation = ValidateStoredDesign(design);
        if (validation.AccessibilityWarnings.Count > 0)
        {
            throw new ConflictException(
                $"Corrige accesibilidad antes de publicar: {string.Join(" ", validation.AccessibilityWarnings)}");
        }

        var now = timeProvider.GetUtcNow();
        InvitationDesignVersion version;
        if (design.ApprovedVersionId is null && request.BypassApprovalForTesting)
        {
            var versionNumber = design.SubmitForReview(now);
            version = InvitationDesignVersion.Create(
                design,
                versionNumber,
                access.UserAccountId,
                now);
            dbContext.InvitationDesignVersions.Add(version);
            design.Approve(version.Id, now);
            version.MarkApproved(access.UserAccountId, now);
        }
        else
        {
            version = await dbContext.InvitationDesignVersions.SingleOrDefaultAsync(entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId
                    && entity.InvitationDesignId == designId
                    && entity.Id == design.ApprovedVersionId,
                    cancellationToken)
                ?? throw new ConflictException(
                    "El diseño requiere una versión aprobada antes de publicarse.");
        }

        design.Publish(version.Id, now);
        version.MarkPublished(now);
        var experience = await GetOrCreateExperienceAsync(
            organizationId,
            eventId,
            cancellationToken);
        experience.Publish(design.Id, version.Id, now);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            request.BypassApprovalForTesting
                ? "invitation_design.published_with_testing_bypass"
                : "invitation_design.published",
            nameof(InvitationDesign),
            design.Id,
            new Dictionary<string, object?> { ["versionId"] = version.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDesignResponseAsync(design, cancellationToken);
    }

    public async Task ArchiveDesignAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationDesignsArchive,
            cancellationToken);
        var design = await FindDesignAsync(
            organizationId,
            eventId,
            designId,
            cancellationToken);
        var experience = await dbContext.EventGuestExperiences.SingleOrDefaultAsync(entity =>
            entity.OrganizationId == organizationId
            && entity.EventId == eventId
            && entity.ActiveInvitationDesignId == designId,
            cancellationToken);
        if (experience?.Status == GuestExperienceStatus.Published)
        {
            throw new ConflictException("Suspende la experiencia antes de archivar su diseño activo.");
        }

        design.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation_design.archived",
            nameof(InvitationDesign),
            design.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<InvitationDesignResponse> ReviewAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        Guid versionId,
        InvitationCommentRequest request,
        InvitationReviewDecision decision,
        string permission,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            permission,
            cancellationToken);
        var message = Normalize(request.Message);
        if (decision != InvitationReviewDecision.Approved
            && string.IsNullOrWhiteSpace(message))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["message"] = ["Escribe un comentario para registrar la revisión."]
                });
        }

        if (message?.Length > 2000)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["message"] = ["El comentario admite hasta 2,000 caracteres."]
                });
        }

        var design = await FindDesignAsync(
            organizationId,
            eventId,
            designId,
            cancellationToken);
        var version = await dbContext.InvitationDesignVersions.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationDesignId == designId
                && entity.Id == versionId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la versión del diseño.");
        var now = timeProvider.GetUtcNow();
        if (decision == InvitationReviewDecision.Approved)
        {
            design.Approve(version.Id, now);
            version.MarkApproved(access.UserAccountId, now);
        }
        else if (decision == InvitationReviewDecision.ChangesRequested)
        {
            design.RequestChanges(now);
        }

        dbContext.InvitationDesignComments.Add(InvitationDesignComment.Create(
            version,
            access.UserAccountId,
            decision,
            message ?? "Versión aprobada.",
            now));
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            $"invitation_design.review_{decision.ToString().ToLowerInvariant()}",
            nameof(InvitationDesign),
            design.Id,
            new Dictionary<string, object?> { ["versionId"] = version.Id });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDesignResponseAsync(design, cancellationToken);
    }

    private async Task<InvitationDesignResponse> BuildDesignResponseAsync(
        InvitationDesign design,
        CancellationToken cancellationToken)
    {
        var versions = await dbContext.InvitationDesignVersions.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == design.OrganizationId
                && entity.EventId == design.EventId
                && entity.InvitationDesignId == design.Id)
            .OrderByDescending(entity => entity.VersionNumber)
            .ToListAsync(cancellationToken);
        var comments = await dbContext.InvitationDesignComments.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == design.OrganizationId
                && entity.EventId == design.EventId
                && entity.InvitationDesignId == design.Id)
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new InvitationCommentResponse(
                entity.Id,
                entity.InvitationDesignVersionId,
                entity.Decision,
                entity.Message,
                entity.CreatedAt))
            .ToListAsync(cancellationToken);
        var theme = InvitationContentValidator.DeserializeTheme(design.DraftThemeJson);
        var blocks = InvitationContentValidator.DeserializeBlocks(design.DraftContentJson);
        return new InvitationDesignResponse(
            design.Id,
            design.EventId,
            design.Name,
            design.Status,
            theme,
            blocks,
            design.NextVersionNumber,
            design.ApprovedVersionId,
            versions.Select(version => new InvitationVersionResponse(
                version.Id,
                version.VersionNumber,
                InvitationContentValidator.DeserializeTheme(version.ThemeSnapshotJson),
                InvitationContentValidator.DeserializeBlocks(version.ContentSnapshotJson),
                version.CreatedAt,
                version.ApprovedAt,
                version.PublishedAt)).ToList(),
            comments,
            InvitationContentValidator.GetAccessibilityWarnings(theme, blocks),
            design.UpdatedAt);
    }

    private async Task<EventGuestExperience> GetOrCreateExperienceAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.EventGuestExperiences.SingleOrDefaultAsync(entity =>
            entity.OrganizationId == organizationId && entity.EventId == eventId,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var eventName = await dbContext.Events.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId && entity.Id == eventId)
            .Select(entity => entity.Name)
            .SingleAsync(cancellationToken);
        var experience = EventGuestExperience.Create(
            organizationId,
            eventId,
            eventName,
            timeProvider.GetUtcNow());
        dbContext.EventGuestExperiences.Add(experience);
        return experience;
    }

    private async Task<EventGuestExperience> FindExperienceAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken) =>
        await dbContext.EventGuestExperiences.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId && entity.EventId == eventId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la experiencia del evento.");

    private async Task<InvitationDesign> FindDesignAsync(
        Guid organizationId,
        Guid eventId,
        Guid designId,
        CancellationToken cancellationToken) =>
        await dbContext.InvitationDesigns.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == designId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el diseño de invitación.");

    private async Task<InvitationTemplate> FindOrganizationTemplateAsync(
        Guid organizationId,
        Guid templateId,
        CancellationToken cancellationToken) =>
        await dbContext.InvitationTemplates.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && !entity.IsGlobal
                && entity.Id == templateId
                && entity.IsActive,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la plantilla editable.");

    private async Task<TenantAccess> RequireEventAsync(
        Guid organizationId,
        Guid eventId,
        string permission,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            permission,
            eventId,
            cancellationToken);
        if (!await dbContext.Events.AsNoTracking().AnyAsync(entity =>
                entity.OrganizationId == organizationId && entity.Id == eventId,
                cancellationToken))
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        return access;
    }

    private static InvitationValidationResult ValidateStoredDesign(
        InvitationDesign design)
    {
        var theme = InvitationContentValidator.DeserializeTheme(design.DraftThemeJson);
        var blocks = InvitationContentValidator.DeserializeBlocks(design.DraftContentJson);
        return InvitationContentValidator.Validate(design.Name, theme, blocks);
    }

    private static void ValidateExperience(GuestExperienceRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Language is not ("es" or "en"))
        {
            errors["language"] = ["El idioma debe ser es o en."];
        }

        if (string.IsNullOrWhiteSpace(request.PublicTitle)
            || request.PublicTitle.Trim().Length > 160)
        {
            errors["publicTitle"] =
            [
                "El título público es obligatorio y admite 160 caracteres."
            ];
        }

        if (request.WelcomeMessage?.Length > 1000)
        {
            errors["welcomeMessage"] =
            [
                "El mensaje de bienvenida admite hasta 1,000 caracteres."
            ];
        }

        if (string.IsNullOrWhiteSpace(request.CelebrantDisplayName)
            || request.CelebrantDisplayName.Trim().Length > 200)
        {
            errors["celebrantDisplayName"] =
            [
                "El nombre visible de los celebrantes es obligatorio y admite 200 caracteres."
            ];
        }

        if (request.ClosingMessage?.Length > 1000)
        {
            errors["closingMessage"] =
            [
                "El mensaje de cierre admite hasta 1,000 caracteres."
            ];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static void ValidateTemplateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description)
            || description.Trim().Length > 240)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["description"] =
                    [
                        "La descripción es obligatoria y admite 240 caracteres."
                    ]
                });
        }
    }

    private static InvitationTemplateResponse ToTemplateResponse(
        InvitationTemplate template) =>
        new(
            template.Id,
            template.IsGlobal,
            template.Name,
            template.Description,
            InvitationContentValidator.DeserializeTheme(template.ThemeJson),
            InvitationContentValidator.DeserializeBlocks(template.ContentJson));

    private static GuestExperienceResponse ToExperienceResponse(
        EventGuestExperience experience) =>
        new(
            experience.Id,
            experience.EventId,
            experience.Status,
            experience.Language,
            experience.PublicTitle,
            experience.CelebrantDisplayName,
            experience.WelcomeMessage,
            experience.ClosingMessage,
            experience.ShowEventName,
            experience.ShowEventDate,
            experience.ShowParticipantNames,
            experience.ShowCity,
            experience.PrivateAccessOnly,
            experience.ActiveInvitationDesignId,
            experience.ActiveVersionId,
            experience.UpdatedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
