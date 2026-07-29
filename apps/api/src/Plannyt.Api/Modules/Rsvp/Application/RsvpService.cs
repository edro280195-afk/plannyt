using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Guests.Application;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed class RsvpService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    PortalAccessService portalAccessService,
    GuestPlanLimitService planLimitService,
    AuditService auditService,
    RsvpSubmissionCoordinator submissionCoordinator,
    TimeProvider timeProvider)
{
    public async Task<RsvpSettingsResponse> GetSettingsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpSettingsView,
            ct);
        var settings = await dbContext.EventRsvpSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId,
                ct);
        if (settings is null)
        {
            throw new NotFoundException(
                "No hay configuración RSVP para este evento.");
        }

        return MapSettings(settings);
    }

    public async Task<RsvpSettingsResponse> CreateOrUpdateDraftAsync(
        Guid organizationId,
        Guid eventId,
        RsvpSettingsRequest request,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpSettingsManage,
            ct);
        var now = timeProvider.GetUtcNow();
        var settings = await dbContext.EventRsvpSettings
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId,
                ct);
        if (settings is null)
        {
            settings = EventRsvpSettings.Create(
                organizationId,
                eventId,
                request.TimeZone,
                now);
            dbContext.EventRsvpSettings.Add(settings);
        }

        settings.UpdateDraft(
            request.OpensAt,
            request.ClosesAt,
            request.TimeZone,
            request.AllowChangesAfterSubmission,
            request.ChangesCloseAt,
            request.AllowTentativeResponse,
            request.AllowGroupDecline,
            request.RequireResponseForEveryNamedGuest,
            request.RequireCompanionNames,
            request.AllowContactInformationUpdate,
            request.ShowAttendanceSummaryAfterSubmission,
            request.ConfirmationTitle,
            request.ConfirmationMessage,
            request.DeclineMessage,
            request.ClosedMessage,
            request.PrivacyNotice,
            request.SensitiveDataConsentText,
            now);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpSettingsUpdated,
            "EventRsvpSettings",
            settings.Id);
        await dbContext.SaveChangesAsync(ct);
        return MapSettings(settings);
    }

    public async Task<RsvpSettingsResponse> PublishSettingsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpSettingsPublish,
            ct);
        var settings = await FindSettingsAsync(
            organizationId,
            eventId,
            ct);
        settings.MarkReady(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpSettingsPublished,
            "EventRsvpSettings",
            settings.Id);
        await dbContext.SaveChangesAsync(ct);
        return MapSettings(settings);
    }

    public async Task<RsvpSettingsResponse> OpenAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpSettingsOpenClose,
            ct);
        var settings = await FindSettingsAsync(
            organizationId,
            eventId,
            ct);
        settings.Open(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpSettingsOpened,
            "EventRsvpSettings",
            settings.Id);
        await dbContext.SaveChangesAsync(ct);
        return MapSettings(settings);
    }

    public async Task<RsvpSettingsResponse> CloseAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpSettingsOpenClose,
            ct);
        var settings = await FindSettingsAsync(
            organizationId,
            eventId,
            ct);
        settings.Close(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpSettingsClosed,
            "EventRsvpSettings",
            settings.Id);
        await dbContext.SaveChangesAsync(ct);
        return MapSettings(settings);
    }

    public async Task<RsvpFormResponse> GetFormAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpFormsView,
            ct);
        var form = await dbContext.RsvpForms
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId,
                ct);
        if (form is null)
        {
            throw new NotFoundException(
                "No hay formulario RSVP para este evento.");
        }

        return new RsvpFormResponse(
            form.Id,
            form.Status,
            form.CurrentDraftVersion,
            form.ActivePublishedVersionId,
            form.UpdatedAt);
    }

    public async Task<RsvpFormResponse> CreateFormAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpFormsCreate,
            ct);
        var existing = await dbContext.RsvpForms
            .AsNoTracking()
            .AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId,
                ct);
        if (existing)
        {
            throw new ConflictException(
                "Ya existe un formulario RSVP para este evento.");
        }

        var form = RsvpForm.Create(
            organizationId,
            eventId,
            access.UserAccountId,
            timeProvider.GetUtcNow());
        dbContext.RsvpForms.Add(form);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpFormCreated,
            "RsvpForm",
            form.Id);
        await dbContext.SaveChangesAsync(ct);
        return new RsvpFormResponse(
            form.Id,
            form.Status,
            form.CurrentDraftVersion,
            form.ActivePublishedVersionId,
            form.UpdatedAt);
    }

    public async Task<RsvpFormVersionResponse> CreateVersionAsync(
        Guid organizationId,
        Guid eventId,
        string questionsJson,
        string menuJson,
        string transportJson,
        string accommodationJson,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpFormsUpdateDraft,
            ct);
        var form = await dbContext.RsvpForms
                       .SingleOrDefaultAsync(
                           entity =>
                               entity.OrganizationId == organizationId
                               && entity.EventId == eventId,
                           ct)
                   ?? throw new NotFoundException("No hay formulario RSVP.");
        var settings = await FindSettingsAsync(
            organizationId,
            eventId,
            ct);
        var settingsJson =
            System.Text.Json.JsonSerializer.Serialize(MapSettings(settings));
        var version = RsvpFormVersion.Create(
            organizationId,
            form.Id,
            form.CurrentDraftVersion,
            settingsJson,
            questionsJson,
            menuJson,
            transportJson,
            accommodationJson,
            access.UserAccountId,
            timeProvider.GetUtcNow());
        dbContext.RsvpFormVersions.Add(version);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpFormVersionCreated,
            "RsvpFormVersion",
            version.Id);
        await dbContext.SaveChangesAsync(ct);
        return MapVersion(version);
    }

    public async Task<RsvpFormResponse> SubmitForReviewAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpFormsSubmitReview,
            ct);
        var form = await dbContext.RsvpForms
                       .SingleOrDefaultAsync(
                           entity =>
                               entity.OrganizationId == organizationId
                               && entity.EventId == eventId,
                           ct)
                   ?? throw new NotFoundException("No hay formulario RSVP.");
        form.SubmitForReview(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpFormSubmittedReview,
            "RsvpForm",
            form.Id);
        await dbContext.SaveChangesAsync(ct);
        return new RsvpFormResponse(
            form.Id,
            form.Status,
            form.CurrentDraftVersion,
            form.ActivePublishedVersionId,
            form.UpdatedAt);
    }

    public async Task<RsvpFormVersionResponse> ApproveFormAsync(
        Guid organizationId,
        Guid eventId,
        Guid versionId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpFormsApprove,
            ct);
        var form = await dbContext.RsvpForms
                       .SingleOrDefaultAsync(
                           entity =>
                               entity.OrganizationId == organizationId
                               && entity.EventId == eventId,
                           ct)
                   ?? throw new NotFoundException("No hay formulario RSVP.");
        form.Approve(timeProvider.GetUtcNow());
        var version = await dbContext.RsvpFormVersions
                          .SingleOrDefaultAsync(
                              entity =>
                                  entity.Id == versionId
                                  && entity.OrganizationId == organizationId
                                  && entity.RsvpFormId == form.Id,
                              ct)
                      ?? throw new NotFoundException(
                          "No se encontró la versión.");
        version.Approve(access.UserAccountId, timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpFormApproved,
            "RsvpFormVersion",
            version.Id);
        await dbContext.SaveChangesAsync(ct);
        return MapVersion(version);
    }

    public async Task<RsvpFormVersionResponse> PublishFormAsync(
        Guid organizationId,
        Guid eventId,
        Guid versionId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpFormsPublish,
            ct);
        var form = await dbContext.RsvpForms
                       .SingleOrDefaultAsync(
                           entity =>
                               entity.OrganizationId == organizationId
                               && entity.EventId == eventId,
                           ct)
                   ?? throw new NotFoundException("No hay formulario RSVP.");
        var version = await dbContext.RsvpFormVersions
                          .SingleOrDefaultAsync(
                              entity =>
                                  entity.Id == versionId
                                  && entity.OrganizationId == organizationId
                                  && entity.RsvpFormId == form.Id,
                              ct)
                      ?? throw new NotFoundException(
                          "No se encontró la versión.");
        version.Publish(timeProvider.GetUtcNow());
        form.Publish(versionId, timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpFormPublished,
            "RsvpFormVersion",
            version.Id);
        await dbContext.SaveChangesAsync(ct);
        return MapVersion(version);
    }

    public async Task<GuestRsvpStateResponse> GetGuestRsvpStateByTokenAsync(
        string token,
        CancellationToken ct)
    {
        var link = await ResolveAccessLinkAsync(token, ct);
        return await GetGuestRsvpStateAsync(link.Id, ct);
    }

    public async Task<RsvpSubmissionResponse> SubmitRsvpByTokenAsync(
        string token,
        RsvpSubmissionRequest request,
        string? idempotencyKey,
        string? userAgent,
        string? ipAddress,
        CancellationToken ct)
    {
        var link = await ResolveAccessLinkAsync(token, ct);
        return await SubmitRsvpAsync(
            link.Id,
            request,
            idempotencyKey,
            userAgent,
            ipAddress,
            ct);
    }

    public async Task<GuestRsvpStateResponse> GetGuestRsvpStateAsync(
        Guid accessLinkId,
        CancellationToken ct)
    {
        var link = await dbContext.GuestAccessLinks
                       .AsNoTracking()
                       .SingleOrDefaultAsync(
                           entity => entity.Id == accessLinkId,
                           ct)
                   ?? throw new NotFoundException("Enlace no encontrado.");
        if (link.Status
            != Invitations.Domain.GuestAccessLinkStatus.Active)
        {
            throw new NotFoundException(
                "El enlace ya no está activo.");
        }

        var orgId = link.OrganizationId;
        var eventId = link.EventId;
        var group = await dbContext.InvitationGroups
                        .AsNoTracking()
                        .SingleOrDefaultAsync(
                            entity =>
                                entity.OrganizationId == orgId
                                && entity.EventId == eventId
                                && entity.Id == link.InvitationGroupId,
                            ct)
                    ?? throw new NotFoundException(
                        "Grupo no encontrado.");
        if (link.IsExpired(timeProvider.GetUtcNow()))
        {
            throw new GoneException("El enlace de invitado expiró.");
        }

        var experienceAvailable = await dbContext.EventGuestExperiences
            .AsNoTracking()
            .AnyAsync(entity =>
                entity.OrganizationId == orgId
                && entity.EventId == eventId
                && entity.Status == GuestExperienceStatus.Published,
                ct);
        var eventAvailable = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(entity =>
                entity.OrganizationId == orgId
                && entity.Id == eventId
                && entity.Status != Modules.Events.Domain.EventStatus.Suspended
                && entity.Status != Modules.Events.Domain.EventStatus.Cancelled
                && entity.Status != Modules.Events.Domain.EventStatus.Closed
                && entity.Status != Modules.Events.Domain.EventStatus.Archived,
                ct);
        var settings = await dbContext.EventRsvpSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == orgId
                    && entity.EventId == eventId,
                ct);
        var now = timeProvider.GetUtcNow();
        RsvpFormVersionResponse? activeForm = null;
        RsvpSubmissionResponse? currentResponse = null;
        var lastSubmission = await dbContext.RsvpSubmissions
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == orgId
                && entity.EventId == eventId
                && entity.InvitationGroupId == link.InvitationGroupId)
            .OrderByDescending(entity => entity.RevisionNumber)
            .FirstOrDefaultAsync(ct);
        var groupException = await dbContext.RsvpGroupExceptions
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == orgId
                && entity.EventId == eventId
                && entity.InvitationGroupId == link.InvitationGroupId
                && entity.Status == RsvpGroupExceptionStatus.Active)
            .OrderByDescending(entity => entity.CreatedAt)
            .FirstOrDefaultAsync(ct);
        var availability = RsvpAvailabilityEvaluator.Evaluate(
            settings,
            groupException,
            lastSubmission is not null,
            now);
        var canRespond = experienceAvailable
                         && eventAvailable
                         && availability.CanRespond;
        var canModify = experienceAvailable
                        && eventAvailable
                        && availability.CanModify;
        if (canRespond || lastSubmission is not null)
        {
            var form = await dbContext.RsvpForms
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    entity =>
                        entity.OrganizationId == orgId
                        && entity.EventId == eventId
                        && entity.Status == RsvpFormStatus.Published,
                    ct);
            if (form?.ActivePublishedVersionId is not null)
            {
                activeForm = await GetVersionAsync(
                    orgId,
                    form.Id,
                    form.ActivePublishedVersionId.Value,
                    ct);
            }

            if (lastSubmission is not null)
            {
                currentResponse = await MapSubmissionAsync(
                    lastSubmission,
                    ct,
                    includeSensitiveData: false);
            }
        }

        var namedGuests = await dbContext.EventGuests
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == orgId
                && entity.EventId == eventId
                && entity.InvitationGroupId == group.Id
                && entity.ArchivedAt == null)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => new GuestRsvpInviteeResponse(
                entity.Id,
                (entity.FirstName + " " + entity.LastName).Trim(),
                entity.AgeCategory.ToString()))
            .ToListAsync(ct);

        return new GuestRsvpStateResponse(
            group.Id,
            group.DisplayName,
            group.AllowedGuestCount,
            group.MaxUnnamedCompanions,
            group.AllowUnnamedCompanions,
            canRespond,
            canModify,
            settings?.ClosedMessage,
            settings is not null ? MapSettings(settings) : null,
            activeForm,
            currentResponse,
            lastSubmission?.RevisionNumber ?? 0,
            namedGuests);
    }

    public async Task<RsvpSubmissionResponse> SubmitRsvpAsync(
        Guid accessLinkId,
        RsvpSubmissionRequest request,
        string? clientIdempotencyKey,
        string? userAgent,
        string? ipAddress,
        CancellationToken ct)
    {
        var submissionId = await submissionCoordinator.SubmitPublicAsync(
            accessLinkId,
            request,
            clientIdempotencyKey,
            userAgent,
            ipAddress,
            ct);
        var persistedSubmission = await dbContext.RsvpSubmissions
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == submissionId, ct);
        return await MapSubmissionAsync(
            persistedSubmission,
            ct,
            includeSensitiveData: false);
    }

    public async Task<RsvpSubmissionResponse> ManualCaptureAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        ManualRsvpRequest request,
        string? idempotencyKey,
        CancellationToken ct)
    {
        var submissionId = await submissionCoordinator.SubmitManualAsync(
            organizationId,
            eventId,
            groupId,
            request,
            idempotencyKey,
            ct);
        var persistedSubmission = await dbContext.RsvpSubmissions
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == submissionId, ct);
        return await MapSubmissionAsync(
            persistedSubmission,
            ct,
            includeSensitiveData: false);
    }

    public async Task<RsvpDashboardResponse> GetDashboardAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpResponsesView,
            ct);
        var canViewSensitiveData = access.Permissions.Contains(
            Permissions.GuestSensitiveDataView);
        return await BuildDashboardAsync(
            organizationId,
            eventId,
            canViewSensitiveData,
            ct);
    }

    public async Task<RsvpDashboardResponse> GetPortalDashboardAsync(
        Guid eventId,
        CancellationToken ct)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.RsvpResponsesView,
            ct);
        return await BuildDashboardAsync(
            access.OrganizationId,
            eventId,
            access.Permissions.Contains(
                Permissions.GuestSensitiveDataView),
            ct);
    }

    public async Task<RsvpSubmissionResponse> PortalManualCaptureAsync(
        Guid eventId,
        Guid groupId,
        ManualRsvpRequest request,
        string? idempotencyKey,
        CancellationToken ct)
    {
        var submissionId = await submissionCoordinator.SubmitPortalAsync(
            eventId,
            groupId,
            request,
            idempotencyKey,
            ct);
        var persistedSubmission = await dbContext.RsvpSubmissions
            .AsNoTracking()
            .SingleAsync(entity => entity.Id == submissionId, ct);
        return await MapSubmissionAsync(
            persistedSubmission,
            ct,
            includeSensitiveData: false);
    }

    private async Task<RsvpDashboardResponse> BuildDashboardAsync(
        Guid organizationId,
        Guid eventId,
        bool canViewSensitiveData,
        CancellationToken ct)
    {
        var guestsWithSensitiveData = canViewSensitiveData
            ? (await dbContext.GuestDietaryAndAccessibilities
                .AsNoTracking()
                .Where(data =>
                    data.OrganizationId == organizationId
                    && data.EventId == eventId)
                .Select(data => data.EventGuestId)
                .ToListAsync(ct))
                .ToHashSet()
            : [];
        _ = await planLimitService.GetUsageAsync(
            organizationId,
            eventId,
            ct);
        var groups = await dbContext.InvitationGroups
            .AsNoTracking()
            .Where(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId
                    && entity.ArchivedAt == null)
            .ToListAsync(ct);
        var results = new List<RsvpGroupSummaryResponse>();
        var totalConfirmed = 0;
        var totalNotAttending = 0;
        var totalTentative = 0;
        var totalPending = 0;
        var partial = 0;
        var changed = 0;
        foreach (var group in groups)
        {
            var submissions = await dbContext.RsvpSubmissions
                .AsNoTracking()
                .Where(
                    entity =>
                        entity.OrganizationId == organizationId
                        && entity.EventId == eventId
                        && entity.InvitationGroupId == group.Id)
                .OrderByDescending(entity => entity.RevisionNumber)
                .ToListAsync(ct);
            var last = submissions.FirstOrDefault();
            var currentRsvps = await dbContext.CurrentGuestRsvps
                .AsNoTracking()
                .Where(
                    entity =>
                        entity.OrganizationId == organizationId
                        && entity.EventId == eventId
                        && entity.InvitationGroupId == group.Id)
                .ToListAsync(ct);
            var confirmed = currentRsvps.Count(
                entity =>
                    entity.AttendanceStatus
                    == GuestAttendanceStatus.Attending);
            var declined = currentRsvps.Count(
                entity =>
                    entity.AttendanceStatus
                    == GuestAttendanceStatus.NotAttending);
            var pending = currentRsvps.Count(
                entity =>
                    entity.AttendanceStatus
                    == GuestAttendanceStatus.Pending);
            totalConfirmed += confirmed;
            totalNotAttending += declined;
            totalPending += pending;
            if (submissions.Count > 1)
            {
                changed++;
            }

            if (pending > 0 && confirmed == 0 && declined == 0)
            {
                partial++;
            }

            var hasMenu = last is not null
                && await dbContext.RsvpSubmissionGuests.AnyAsync(
                    entity =>
                        entity.RsvpSubmissionId == last.Id
                        && entity.MenuSelectionsSnapshot != "[]"
                        && entity.MenuSelectionsSnapshot != "{}",
                    ct);
            var hasTransport = last is not null
                && await dbContext.RsvpSubmissionGuests.AnyAsync(
                    entity =>
                        entity.RsvpSubmissionId == last.Id
                        && entity.TransportSelectionSnapshot != "[]"
                        && entity.TransportSelectionSnapshot != "{}",
                    ct);
            var hasAccommodation = last is not null
                && await dbContext.RsvpSubmissionGuests.AnyAsync(
                    entity =>
                        entity.RsvpSubmissionId == last.Id
                        && entity.AccommodationSelectionSnapshot != "[]"
                        && entity.AccommodationSelectionSnapshot != "{}",
                    ct);
            var hasSensitiveData = canViewSensitiveData
                && currentRsvps.Any(current =>
                    current.EventGuestId.HasValue
                    && guestsWithSensitiveData.Contains(
                        current.EventGuestId.Value));
            results.Add(
                new RsvpGroupSummaryResponse(
                    group.Id,
                    group.DisplayName,
                    last?.OverallStatus,
                    confirmed,
                    declined,
                    pending,
                    hasMenu,
                    hasTransport,
                    hasAccommodation,
                    hasSensitiveData,
                    last?.SubmittedAt));
        }

        var settings = await dbContext.EventRsvpSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId,
                ct);
        return new RsvpDashboardResponse(
            groups.Count,
            groups.Sum(g => g.AllowedGuestCount),
            totalConfirmed,
            totalNotAttending,
            totalTentative,
            totalPending,
            partial,
            changed,
            settings?.ClosesAt,
            results);
    }

    public async Task<IReadOnlyList<EventMenuResponse>> GetMenusAsync(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        await RequireEventAsync(organizationId, eventId, Permissions.EventMenusView, ct);
        var menus = await dbContext.EventMenus.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId && m.EventId == eventId && m.ArchivedAt == null)
            .OrderBy(m => m.SortOrder).ToListAsync(ct);
        var result = new List<EventMenuResponse>();
        foreach (var menu in menus)
        {
            var options = await dbContext.EventMenuOptions.AsNoTracking()
                .Where(o => o.OrganizationId == organizationId && o.EventMenuId == menu.Id && o.IsActive)
                .OrderBy(o => o.SortOrder).ToListAsync(ct);
            var optionResponses = new List<EventMenuOptionResponse>();
            foreach (var opt in options)
            {
                var selectionCount = await dbContext.RsvpSubmissionGuests.AsNoTracking()
                    .CountAsync(g => g.MenuSelectionsSnapshot.Contains(opt.Id.ToString()), ct);
                optionResponses.Add(new EventMenuOptionResponse(opt.Id, opt.Name, opt.Description, opt.DietaryTags, opt.IsActive, opt.Capacity, selectionCount, opt.SortOrder));
            }
            result.Add(new EventMenuResponse(menu.Id, menu.Name, menu.Description, menu.MenuCategory, menu.IsActive, menu.SelectionRequired, menu.MinimumSelections, menu.MaximumSelections, menu.SortOrder, optionResponses, menu.UpdatedAt));
        }
        return result;
    }

    public async Task<EventMenuResponse> CreateMenuAsync(
        Guid organizationId, Guid eventId, EventMenuRequest request, CancellationToken ct)
    {
        var access = await RequireEventAsync(organizationId, eventId, Permissions.EventMenusManage, ct);
        var menu = EventMenu.Create(organizationId, eventId, request.Name, request.Description,
            request.MenuCategory, request.SelectionRequired, request.MinimumSelections,
            request.MaximumSelections, request.SortOrder, timeProvider.GetUtcNow());
        dbContext.EventMenus.Add(menu);
        await dbContext.SaveChangesAsync(ct);
        return new EventMenuResponse(menu.Id, menu.Name, menu.Description, menu.MenuCategory, menu.IsActive,
            menu.SelectionRequired, menu.MinimumSelections, menu.MaximumSelections, menu.SortOrder, [], menu.UpdatedAt);
    }

    public async Task<EventMenuOptionResponse> AddMenuOptionAsync(
        Guid organizationId, Guid eventId, Guid menuId, EventMenuOptionRequest request, CancellationToken ct)
    {
        var access = await RequireEventAsync(organizationId, eventId, Permissions.EventMenusManage, ct);
        var menu = await dbContext.EventMenus.SingleOrDefaultAsync(m => m.Id == menuId && m.OrganizationId == organizationId && m.EventId == eventId, ct)
            ?? throw new NotFoundException("Menú no encontrado.");
        var option = EventMenuOption.Create(organizationId, menuId, request.Name, request.Description,
            request.DietaryTags, request.Capacity, request.SortOrder, timeProvider.GetUtcNow());
        dbContext.EventMenuOptions.Add(option);
        await dbContext.SaveChangesAsync(ct);
        return new EventMenuOptionResponse(option.Id, option.Name, option.Description, option.DietaryTags, option.IsActive, option.Capacity, 0, option.SortOrder);
    }

    public async Task<IReadOnlyList<EventTransportOptionResponse>> GetTransportOptionsAsync(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        await RequireEventAsync(organizationId, eventId, Permissions.GuestTravelView, ct);
        var options = await dbContext.EventTransportOptions.AsNoTracking()
            .Where(t => t.OrganizationId == organizationId && t.EventId == eventId && t.IsActive)
            .OrderBy(t => t.SortOrder).ToListAsync(ct);
        var result = new List<EventTransportOptionResponse>();
        foreach (var opt in options)
        {
            var confirmed = await dbContext.GuestTransportSelections.AsNoTracking()
                .CountAsync(s => s.EventTransportOptionId == opt.Id && s.Status == TransportSelectionStatus.Confirmed, ct);
            var waitlist = await dbContext.GuestTransportSelections.AsNoTracking()
                .CountAsync(s => s.EventTransportOptionId == opt.Id && s.Status == TransportSelectionStatus.Waitlisted, ct);
            result.Add(new EventTransportOptionResponse(opt.Id, opt.Name, opt.Description, opt.Direction, opt.PickupPoint,
                opt.DepartureAt, opt.ReturnAt, opt.Capacity, opt.AllowWaitlist, opt.IsActive, opt.SortOrder, confirmed, waitlist));
        }
        return result;
    }

    public async Task<EventTransportOptionResponse> CreateTransportOptionAsync(
        Guid organizationId, Guid eventId, EventTransportOptionRequest request, CancellationToken ct)
    {
        var access = await RequireEventAsync(organizationId, eventId, Permissions.GuestTravelManage, ct);
        var option = EventTransportOption.Create(organizationId, eventId, request.Name, request.Description,
            request.Direction, request.PickupPoint, request.DepartureAt, request.ReturnAt,
            request.Capacity, request.AllowWaitlist, request.SortOrder, timeProvider.GetUtcNow());
        dbContext.EventTransportOptions.Add(option);
        await dbContext.SaveChangesAsync(ct);
        return new EventTransportOptionResponse(option.Id, option.Name, option.Description, option.Direction,
            option.PickupPoint, option.DepartureAt, option.ReturnAt, option.Capacity, option.AllowWaitlist, option.IsActive, option.SortOrder, 0, 0);
    }

    public async Task<IReadOnlyList<EventAccommodationOptionResponse>> GetAccommodationOptionsAsync(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        await RequireEventAsync(organizationId, eventId, Permissions.GuestTravelView, ct);
        var options = await dbContext.EventAccommodationOptions.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.EventId == eventId && a.IsActive)
            .OrderBy(a => a.SortOrder).ToListAsync(ct);
        var result = new List<EventAccommodationOptionResponse>();
        foreach (var opt in options)
        {
            var interested = await dbContext.GuestAccommodationSelections.AsNoTracking()
                .CountAsync(s => s.EventAccommodationOptionId == opt.Id, ct);
            result.Add(new EventAccommodationOptionResponse(opt.Id, opt.Name, opt.Description, opt.Address,
                opt.BookingUrl, opt.BookingCode, opt.BookingDeadline, opt.ContactInformation, opt.IsActive, opt.SortOrder, interested));
        }
        return result;
    }

    public async Task<EventAccommodationOptionResponse> CreateAccommodationOptionAsync(
        Guid organizationId, Guid eventId, EventAccommodationOptionRequest request, CancellationToken ct)
    {
        var access = await RequireEventAsync(organizationId, eventId, Permissions.GuestTravelManage, ct);
        var option = EventAccommodationOption.Create(organizationId, eventId, request.Name, request.Description,
            request.Address, request.BookingUrl, request.BookingCode, request.BookingDeadline,
            request.ContactInformation, request.SortOrder, timeProvider.GetUtcNow());
        dbContext.EventAccommodationOptions.Add(option);
        await dbContext.SaveChangesAsync(ct);
        return new EventAccommodationOptionResponse(option.Id, option.Name, option.Description, option.Address,
            option.BookingUrl, option.BookingCode, option.BookingDeadline, option.ContactInformation, option.IsActive, option.SortOrder, 0);
    }

    public async Task<IReadOnlyList<ReminderTemplateResponse>>
        GetTemplatesAsync(
            Guid organizationId,
            Guid? eventId,
            CancellationToken ct)
    {
        await RequireEventAsync(
            organizationId,
            eventId ?? Guid.Empty,
            Permissions.GuestRemindersView,
            ct);
        return await dbContext.ReminderTemplates
            .AsNoTracking()
            .Where(
                entity =>
                    entity.OrganizationId == organizationId
                    && (entity.EventId == null
                        || entity.EventId == eventId)
                    && entity.IsActive)
            .OrderBy(entity => entity.Name)
            .Select(
                entity => new ReminderTemplateResponse(
                    entity.Id,
                    entity.Name,
                    entity.Channel,
                    entity.SegmentType,
                    entity.MessageTemplate,
                    entity.IsActive,
                    entity.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<ReminderTemplateResponse> CreateTemplateAsync(
        Guid organizationId,
        Guid? eventId,
        ReminderTemplateRequest request,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId ?? Guid.Empty,
            Permissions.GuestRemindersManage,
            ct);
        var template = ReminderTemplate.Create(
            organizationId,
            eventId,
            request.Name,
            request.Channel,
            request.SegmentType,
            request.MessageTemplate,
            timeProvider.GetUtcNow());
        dbContext.ReminderTemplates.Add(template);
        await dbContext.SaveChangesAsync(ct);
        return new ReminderTemplateResponse(
            template.Id,
            template.Name,
            template.Channel,
            template.SegmentType,
            template.MessageTemplate,
            template.IsActive,
            template.UpdatedAt);
    }

    public async Task MarkReminderSentAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        Guid templateId,
        MarkReminderRequest request,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestRemindersMarkSent,
            ct);
        var template = await dbContext.ReminderTemplates
                           .SingleOrDefaultAsync(
                               entity =>
                                   entity.Id == templateId
                                   && entity.OrganizationId == organizationId,
                               ct)
                       ?? throw new NotFoundException(
                           "Plantilla no encontrada.");
        var log = EventReminderLog.Create(
            organizationId,
            eventId,
            groupId,
            templateId,
            template.Channel,
            access.UserAccountId,
            request.Note,
            timeProvider.GetUtcNow());
        dbContext.EventReminderLogs.Add(log);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task OpenGroupExceptionAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        DateTimeOffset expiresAt,
        string reason,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpResponsesReopen,
            ct);
        var existing = await dbContext.RsvpGroupExceptions
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId
                    && entity.InvitationGroupId == groupId
                    && entity.Status == RsvpGroupExceptionStatus.Active,
                ct);
        if (existing is not null)
        {
            throw new ConflictException(
                "Ya existe una excepción activa para este grupo.");
        }

        var exception = RsvpGroupException.Create(
            organizationId,
            eventId,
            groupId,
            expiresAt,
            reason,
            access.UserAccountId,
            timeProvider.GetUtcNow());
        dbContext.RsvpGroupExceptions.Add(exception);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpGroupExceptionOpened,
            "RsvpGroupException",
            exception.Id,
            new Dictionary<string, object?>
            {
                ["reason"] = reason,
                ["expiresAt"] = expiresAt
            });
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task CloseGroupExceptionAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.RsvpResponsesReopen,
            ct);
        var groupException = await dbContext.RsvpGroupExceptions
                                 .SingleOrDefaultAsync(
                                     entity =>
                                         entity.OrganizationId
                                         == organizationId
                                         && entity.EventId == eventId
                                         && entity.InvitationGroupId == groupId
                                         && entity.Status
                                         == RsvpGroupExceptionStatus.Active,
                                     ct)
                             ?? throw new NotFoundException(
                                 "No hay una excepción activa para este grupo.");
        groupException.Close(
            access.UserAccountId,
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.RsvpGroupExceptionClosed,
            nameof(RsvpGroupException),
            groupException.Id);
        await dbContext.SaveChangesAsync(ct);
    }

    private async Task<TenantAccess> RequireEventAsync(
        Guid organizationId,
        Guid eventId,
        string permission,
        CancellationToken ct)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            permission,
            eventId,
            ct);
        if (!await dbContext.Events.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId,
                ct))
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        return access;
    }

    private async Task<EventRsvpSettings> FindSettingsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken ct) =>
        await dbContext.EventRsvpSettings
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId,
                ct)
        ?? throw new NotFoundException(
            "No hay configuración RSVP.");

    private static RsvpSettingsResponse MapSettings(EventRsvpSettings s) =>
        new(
            s.Id,
            s.Status,
            s.OpensAt,
            s.ClosesAt,
            s.TimeZone,
            s.AllowChangesAfterSubmission,
            s.ChangesCloseAt,
            s.AllowTentativeResponse,
            s.AllowGroupDecline,
            s.RequireResponseForEveryNamedGuest,
            s.RequireCompanionNames,
            s.AllowContactInformationUpdate,
            s.ShowAttendanceSummaryAfterSubmission,
            s.ConfirmationTitle,
            s.ConfirmationMessage,
            s.DeclineMessage,
            s.ClosedMessage,
            s.PrivacyNotice,
            s.SensitiveDataConsentText,
            s.UpdatedAt);

    private async Task<RsvpFormVersionResponse?> GetVersionAsync(
        Guid orgId,
        Guid formId,
        Guid versionId,
        CancellationToken ct)
    {
        var v = await dbContext.RsvpFormVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.Id == versionId
                    && entity.OrganizationId == orgId
                    && entity.RsvpFormId == formId,
                ct);
        return v is null ? null : MapVersion(v);
    }

    private static RsvpFormVersionResponse MapVersion(RsvpFormVersion v) =>
        new(
            v.Id,
            v.RsvpFormId,
            v.VersionNumber,
            v.SettingsSnapshot,
            v.QuestionsSnapshot,
            v.MenuSnapshot,
            v.TransportSnapshot,
            v.AccommodationSnapshot,
            v.CreatedAt,
            v.ApprovedBy,
            v.ApprovedAt,
            v.PublishedAt);

    private async Task<RsvpSubmissionResponse> MapSubmissionAsync(
        RsvpSubmission s,
        CancellationToken ct,
        bool includeSensitiveData = false)
    {
        var guests = await dbContext.RsvpSubmissionGuests
            .AsNoTracking()
            .Where(entity => entity.RsvpSubmissionId == s.Id)
            .ToListAsync(ct);
        var answers = await dbContext.RsvpSubmissionAnswers
            .AsNoTracking()
            .Where(entity => entity.RsvpSubmissionId == s.Id)
            .ToListAsync(ct);
        var confirmationCode =
            $"RSVP-{s.Id.ToString("N")[..6].ToUpper()}";
        return new RsvpSubmissionResponse(
            s.Id,
            s.InvitationGroupId,
            s.RevisionNumber,
            s.Source,
            s.OverallStatus,
            s.SubmittedAt,
            s.ContactNameSnapshot,
            s.ContactEmailSnapshot,
            s.ContactPhoneSnapshot,
            confirmationCode,
            guests
                .Select(
                    g => new RsvpSubmissionGuestResponse(
                        g.EventGuestId,
                        g.DisplayName,
                        g.AgeCategory,
                        g.AttendanceStatus,
                        g.MenuSelectionsSnapshot,
                        g.TransportSelectionSnapshot,
                        g.AccommodationSelectionSnapshot,
                        includeSensitiveData ? g.DietarySnapshot : "{}",
                        g.IsUnnamedCompanion))
                .ToList(),
            answers
                .Select(
                    a => new RsvpSubmissionAnswerResponse(
                        a.QuestionId,
                        a.GuestId,
                        ReadJsonString(a.AnswerValue),
                        a.DisplayValueSnapshot))
                .ToList());
    }

    private static string ReadJsonString(string value)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(value);
            return document.RootElement.ValueKind
                   == System.Text.Json.JsonValueKind.String
                ? document.RootElement.GetString() ?? string.Empty
                : document.RootElement.GetRawText();
        }
        catch (System.Text.Json.JsonException)
        {
            return value;
        }
    }

    private async Task UpsertCurrentGuestRsvpAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        RsvpSubmissionGuestRequest guestReq,
        Guid submissionId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        CurrentGuestRsvp? existing = null;
        if (guestReq.EventGuestId.HasValue)
        {
            existing = await dbContext.CurrentGuestRsvps
                .SingleOrDefaultAsync(
                    entity =>
                        entity.EventGuestId == guestReq.EventGuestId.Value
                        && entity.OrganizationId == organizationId,
                    ct);
        }

        if (existing is not null)
        {
            existing.UpdateStatus(
                guestReq.AttendanceStatus,
                guestReq.DisplayName,
                submissionId,
                now);
        }
        else
        {
            var rsvp = CurrentGuestRsvp.Create(
                organizationId,
                eventId,
                groupId,
                guestReq.EventGuestId,
                guestReq.AttendanceStatus,
                guestReq.IsUnnamedCompanion,
                null,
                guestReq.DisplayName,
                submissionId,
                now);
            dbContext.CurrentGuestRsvps.Add(rsvp);
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Invitations.Domain.GuestAccessLink> ResolveAccessLinkAsync(
        string token,
        CancellationToken ct)
    {
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(token)));
        return await dbContext.GuestAccessLinks
                   .SingleOrDefaultAsync(
                       entity => entity.TokenHash == hash
                                 && entity.Status
                                 == Invitations.Domain.GuestAccessLinkStatus.Active,
                       ct)
               ?? throw new NotFoundException(
                   "Enlace no encontrado o no está activo.");
    }
}
