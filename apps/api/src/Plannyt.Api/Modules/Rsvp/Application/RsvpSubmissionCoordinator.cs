using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed class RsvpSubmissionCoordinator(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    PortalAccessService portalAccessService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Guid> SubmitPublicAsync(
        Guid accessLinkId,
        RsvpSubmissionRequest request,
        string? idempotencyKey,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var key = RsvpRequestFingerprint.ValidateIdempotencyKey(idempotencyKey);
        var fingerprint = new SubmissionFingerprint();

        try
        {
            return await SubmitPublicCoreAsync(
                accessLinkId,
                request,
                key,
                fingerprint,
                userAgent,
                ipAddress,
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsIdempotencyViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            var winner = await dbContext.RsvpSubmissions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    submission =>
                        submission.GuestAccessLinkId == accessLinkId
                        && submission.IdempotencyKey == key,
                    cancellationToken);
            return RsvpSubmissionConcurrencyPolicy.ResolveIdempotentRetry(
                winner,
                fingerprint.RequiredValue);
        }
    }

    public async Task<Guid> SubmitManualAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        ManualRsvpRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
        => await SubmitAdministrativeAsync(
            organizationId,
            eventId,
            groupId,
            request,
            idempotencyKey,
            isPortal: false,
            cancellationToken);

    public async Task<Guid> SubmitPortalAsync(
        Guid eventId,
        Guid groupId,
        ManualRsvpRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var portalAccess = await portalAccessService.RequireAsync(
            eventId,
            Permissions.RsvpResponsesCreateManual,
            cancellationToken);
        return await SubmitAdministrativeAsync(
            portalAccess.OrganizationId,
            eventId,
            groupId,
            request,
            idempotencyKey,
            isPortal: true,
            cancellationToken);
    }

    private async Task<Guid> SubmitAdministrativeAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        ManualRsvpRequest request,
        string? idempotencyKey,
        bool isPortal,
        CancellationToken cancellationToken)
    {
        var key = RsvpRequestFingerprint.ValidateIdempotencyKey(idempotencyKey);
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length is 0 or > 500)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["reason"] =
                    [
                        "El motivo es obligatorio y admite hasta 500 caracteres."
                    ]
                });
        }

        var fingerprint = new SubmissionFingerprint();
        try
        {
            return await SubmitManualCoreAsync(
                organizationId,
                eventId,
                groupId,
                request,
                reason,
                key,
                fingerprint,
                isPortal,
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsIdempotencyViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            var winner = await dbContext.RsvpSubmissions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    submission =>
                        submission.OrganizationId == organizationId
                        && submission.EventId == eventId
                        && submission.InvitationGroupId == groupId
                        && submission.IdempotencyKey == key,
                    cancellationToken);
            return RsvpSubmissionConcurrencyPolicy.ResolveIdempotentRetry(
                winner,
                fingerprint.RequiredValue);
        }
    }

    private async Task<Guid> SubmitPublicCoreAsync(
        Guid accessLinkId,
        RsvpSubmissionRequest request,
        string key,
        SubmissionFingerprint fingerprint,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var link = await dbContext.GuestAccessLinks
                       .SingleOrDefaultAsync(
                           entity => entity.Id == accessLinkId,
                           cancellationToken)
                   ?? throw new NotFoundException("Enlace no encontrado.");
        ValidateLink(link, now);
        await ValidatePublicExperienceAsync(link, cancellationToken);
        var group = await LockGroupAsync(
            link.OrganizationId,
            link.EventId,
            link.InvitationGroupId,
            cancellationToken);
        var settings = await dbContext.EventRsvpSettings
                           .SingleOrDefaultAsync(
                               entity =>
                                   entity.OrganizationId == link.OrganizationId
                                   && entity.EventId == link.EventId,
                               cancellationToken)
                       ?? throw new NotFoundException(
                           "No hay configuración RSVP.");
        var previous = await GetCurrentSubmissionAsync(
            link.OrganizationId,
            link.EventId,
            group.Id,
            cancellationToken);
        var groupException = await dbContext.RsvpGroupExceptions
            .Where(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.EventId == link.EventId
                && entity.InvitationGroupId == group.Id
                && entity.Status == RsvpGroupExceptionStatus.Active)
            .OrderByDescending(entity => entity.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var availability = RsvpAvailabilityEvaluator.Evaluate(
            settings,
            groupException,
            previous is not null,
            now);
        var formVersion = await GetSubmissionFormVersionAsync(
            link.OrganizationId,
            link.EventId,
            request.RsvpFormVersionId,
            previous,
            cancellationToken);
        var questionDefinitions =
            ParseSubmissionQuestions(formVersion.QuestionsSnapshot);
        var questionContext = await BuildQuestionContextAsync(
            link.OrganizationId,
            link.EventId,
            group.Id,
            request,
            cancellationToken);
        var requestWithGuestIds = request with
        {
            Guests = questionContext.NormalizedGuests
        };
        var parsedGuests = await ValidateAndParseGuestsAsync(
            link.OrganizationId,
            link.EventId,
            group,
            settings,
            requestWithGuestIds,
            cancellationToken);
        var questionValidation =
            RsvpQuestionEngine.ValidateAndNormalize(
                questionDefinitions.Questions,
                questionContext.EvaluationContext,
                request.Answers,
                request.ConsentSnapshot);
        var normalizedRequest = requestWithGuestIds with
        {
            Answers = questionValidation.Answers
                .Select(ToRequest)
                .ToList()
        };
        fingerprint.Value = RsvpRequestFingerprint.Compute(
            normalizedRequest,
            "public");
        var existing = await FindByIdempotencyAsync(
            link.OrganizationId,
            link.EventId,
            group.Id,
            key,
            cancellationToken);
        if (existing is not null)
        {
            var winnerId =
                RsvpSubmissionConcurrencyPolicy.ResolveIdempotentRetry(
                    existing,
                    fingerprint.RequiredValue);
            await transaction.CommitAsync(cancellationToken);
            return winnerId;
        }

        RsvpSubmissionConcurrencyPolicy.ValidateExpectedRevision(
            normalizedRequest.ExpectedRevision,
            previous);
        if (!availability.CanRespond)
        {
            throw new ConflictException(
                previous is null
                    ? "El RSVP no está abierto para respuestas."
                    : "La respuesta cambió o el periodo de cambios terminó.");
        }

        var submission = CreateSubmission(
            link.OrganizationId,
            link.EventId,
            group.Id,
            formVersion.Id,
            link.Id,
            previous,
            RsvpSubmissionSource.GuestPrivateLink,
            normalizedRequest,
            null,
            userAgent,
            ipAddress,
            key,
            fingerprint.RequiredValue,
            now);
        await PersistSubmissionAsync(
            submission,
            group,
            settings,
            normalizedRequest,
            parsedGuests,
            questionValidation.Answers,
            null,
            AuditActions.RsvpSubmitted,
            null,
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return submission.Id;
    }

    private async Task<Guid> SubmitManualCoreAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        ManualRsvpRequest manualRequest,
        string reason,
        string key,
        SubmissionFingerprint fingerprint,
        bool isPortal,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        Guid actorUserId;
        IReadOnlySet<string> permissions;
        if (isPortal)
        {
            var portalAccess = await portalAccessService.RequireAsync(
                eventId,
                Permissions.RsvpResponsesCreateManual,
                cancellationToken);
            if (portalAccess.OrganizationId != organizationId)
            {
                throw new ForbiddenException(
                    "El acceso del portal no pertenece a la organización solicitada.");
            }

            actorUserId = portalAccess.UserAccountId;
            permissions = portalAccess.Permissions;
        }
        else
        {
            var tenantAccess = await tenantAccessService.RequireAsync(
                organizationId,
                Permissions.RsvpResponsesCreateManual,
                eventId,
                cancellationToken);
            actorUserId = tenantAccess.UserAccountId;
            permissions = tenantAccess.Permissions;
        }
        if (manualRequest.Source == RsvpSubmissionSource.GuestPrivateLink)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["source"] =
                    [
                        "La captura administrativa no admite la fuente GuestPrivateLink."
                    ]
                });
        }

        if (manualRequest.Source == RsvpSubmissionSource.SupportCorrection)
        {
            RequirePermission(
                permissions,
                Permissions.RsvpResponsesCorrect);
        }

        if (ContainsSensitiveData(manualRequest.Submission))
        {
            RequirePermission(
                permissions,
                Permissions.GuestSensitiveDataManage);
        }

        var eventExists = await dbContext.Events.AnyAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == eventId,
            cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        var group = await LockGroupAsync(
            organizationId,
            eventId,
            groupId,
            cancellationToken);
        var settings = await dbContext.EventRsvpSettings
                           .SingleOrDefaultAsync(
                               entity =>
                                   entity.OrganizationId == organizationId
                                   && entity.EventId == eventId,
                               cancellationToken)
                       ?? throw new NotFoundException(
                           "No hay configuración RSVP.");
        var previous = await GetCurrentSubmissionAsync(
            organizationId,
            eventId,
            groupId,
            cancellationToken);
        var formVersion = await GetSubmissionFormVersionAsync(
            organizationId,
            eventId,
            manualRequest.Submission.RsvpFormVersionId,
            previous,
            cancellationToken);
        var questionDefinitions =
            ParseSubmissionQuestions(formVersion.QuestionsSnapshot);
        var questionContext = await BuildQuestionContextAsync(
            organizationId,
            eventId,
            groupId,
            manualRequest.Submission,
            cancellationToken);
        var requestWithGuestIds = manualRequest.Submission with
        {
            Guests = questionContext.NormalizedGuests
        };
        var parsedGuests = await ValidateAndParseGuestsAsync(
            organizationId,
            eventId,
            group,
            settings,
            requestWithGuestIds,
            cancellationToken);
        var questionValidation =
            RsvpQuestionEngine.ValidateAndNormalize(
                questionDefinitions.Questions,
                questionContext.EvaluationContext,
                manualRequest.Submission.Answers,
                manualRequest.Submission.ConsentSnapshot);
        if (questionValidation.ContainsSensitiveAnswers)
        {
            RequirePermission(
                permissions,
                Permissions.GuestSensitiveDataManage);
        }

        var normalizedRequest = requestWithGuestIds with
        {
            Answers = questionValidation.Answers
                .Select(ToRequest)
                .ToList()
        };
        fingerprint.Value = RsvpRequestFingerprint.Compute(
            normalizedRequest,
            $"manual:{manualRequest.Source}:{reason}");
        var existing = await FindByIdempotencyAsync(
            organizationId,
            eventId,
            groupId,
            key,
            cancellationToken);
        if (existing is not null)
        {
            var winnerId =
                RsvpSubmissionConcurrencyPolicy.ResolveIdempotentRetry(
                    existing,
                    fingerprint.RequiredValue);
            await transaction.CommitAsync(cancellationToken);
            return winnerId;
        }

        RsvpSubmissionConcurrencyPolicy.ValidateExpectedRevision(
            normalizedRequest.ExpectedRevision,
            previous);
        var now = timeProvider.GetUtcNow();
        var submission = CreateSubmission(
            organizationId,
            eventId,
            groupId,
            formVersion.Id,
            null,
            previous,
            manualRequest.Source,
            normalizedRequest,
            actorUserId,
            null,
            null,
            key,
            fingerprint.RequiredValue,
            now);
        var action = manualRequest.Source
                     == RsvpSubmissionSource.SupportCorrection
            ? AuditActions.RsvpSupportCorrected
            : AuditActions.RsvpManualCapture;
        await PersistSubmissionAsync(
            submission,
            group,
            settings,
            normalizedRequest,
            parsedGuests,
            questionValidation.Answers,
            actorUserId,
            action,
            reason,
            now,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return submission.Id;
    }

    private static void RequirePermission(
        IReadOnlySet<string> permissions,
        string permission)
    {
        if (!permissions.Contains(permission))
        {
            throw new ForbiddenException(
                "No tienes el permiso requerido para esta captura RSVP.");
        }
    }

    private async Task PersistSubmissionAsync(
        RsvpSubmission submission,
        InvitationGroup group,
        EventRsvpSettings settings,
        RsvpSubmissionRequest request,
        IReadOnlyList<ParsedGuestRequest> parsedGuests,
        IReadOnlyList<NormalizedRsvpAnswer> normalizedAnswers,
        Guid? actorUserId,
        AuditAction action,
        string? reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        dbContext.RsvpSubmissions.Add(submission);
        var companionSlot = 0;
        foreach (var parsed in parsedGuests)
        {
            var slot = parsed.Request.IsUnnamedCompanion
                ? ++companionSlot
                : (int?)null;
            dbContext.RsvpSubmissionGuests.Add(
                RsvpSubmissionGuest.Create(
                    submission.Id,
                    parsed.Request.ResponseGuestId,
                    parsed.Request.EventGuestId,
                    parsed.Request.DisplayName.Trim(),
                    parsed.Request.AgeCategory.Trim(),
                    parsed.Request.AttendanceStatus,
                    parsed.MenuJson,
                    parsed.TransportJson,
                    parsed.AccommodationJson,
                    parsed.DietaryJson,
                    parsed.Request.IsUnnamedCompanion,
                    slot));
            await UpsertCurrentGuestRsvpAsync(
                submission,
                parsed.Request,
                slot,
                actorUserId,
                now,
                cancellationToken);
        }

        foreach (var answer in normalizedAnswers)
        {
            dbContext.RsvpSubmissionAnswers.Add(
                RsvpSubmissionAnswer.Create(
                    submission.Id,
                    answer.QuestionId,
                    answer.GuestId,
                    answer.AnswerValue,
                    answer.DisplayValue,
                    answer.QuestionLabelSnapshot,
                    answer.QuestionTypeSnapshot,
                    answer.OptionLabelsSnapshot,
                    answer.GuestDisplayNameSnapshot,
                    answer.IsSensitive));
        }

        var sensitiveCount = await UpsertSensitiveDataAsync(
            submission,
            parsedGuests,
            now,
            cancellationToken);
        await ApplyTransportSelectionsAsync(
            submission,
            parsedGuests,
            actorUserId,
            now,
            cancellationToken);
        await ApplyAccommodationSelectionsAsync(
            submission,
            group,
            parsedGuests,
            now,
            cancellationToken);
        auditService.Add(
            submission.OrganizationId,
            submission.EventId,
            actorUserId,
            action,
            nameof(RsvpSubmission),
            submission.Id,
            new Dictionary<string, object?>
            {
                ["source"] = submission.Source.ToString(),
                ["revision"] = submission.RevisionNumber,
                ["reason"] = reason
            });
        if (sensitiveCount > 0)
        {
            auditService.Add(
                submission.OrganizationId,
                submission.EventId,
                actorUserId,
                AuditActions.GuestSensitiveDataUpdated,
                nameof(GuestDietaryAndAccessibility),
                submission.Id,
                SensitiveAuditMetadata(
                    sensitiveCount,
                    actorUserId is null
                        ? "guest-submission"
                        : "administrative-capture"));
        }
    }

    private async Task ApplyTransportSelectionsAsync(
        RsvpSubmission submission,
        IReadOnlyList<ParsedGuestRequest> parsedGuests,
        Guid? actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var namedGuests = parsedGuests
            .Where(guest => guest.Request.EventGuestId.HasValue)
            .ToList();
        if (namedGuests.Count == 0)
        {
            return;
        }

        var guestIds = namedGuests
            .Select(guest => guest.Request.EventGuestId!.Value)
            .ToList();
        var existing = await dbContext.GuestTransportSelections
            .Where(selection =>
                selection.OrganizationId == submission.OrganizationId
                && selection.EventId == submission.EventId
                && guestIds.Contains(selection.EventGuestId))
            .ToListAsync(cancellationToken);
        var affectedOptionIds = existing
            .Select(selection => selection.EventTransportOptionId)
            .Concat(namedGuests
                .Where(guest => guest.Transport?.TransportOptionId is not null)
                .Select(guest => guest.Transport!.TransportOptionId!.Value))
            .Distinct()
            .Order()
            .ToList();
        var options = new Dictionary<Guid, EventTransportOption>();
        var selectionsByOption =
            new Dictionary<Guid, List<GuestTransportSelection>>();
        foreach (var optionId in affectedOptionIds)
        {
            var option = await dbContext.EventTransportOptions
                             .FromSqlInterpolated(
                                 $"""
                                  SELECT *
                                  FROM event_transport_options
                                  WHERE organization_id = {submission.OrganizationId}
                                    AND event_id = {submission.EventId}
                                    AND id = {optionId}
                                  FOR UPDATE
                                  """)
                             .SingleOrDefaultAsync(cancellationToken)
                         ?? throw new RequestValidationException(
                             new Dictionary<string, string[]>
                             {
                                 ["transportSelectionJson"] =
                                 [
                                     "La opción de transporte no pertenece al evento."
                                 ]
                             });
            if (!option.IsActive)
            {
                throw new ConflictException(
                    "La opción de transporte seleccionada ya no está activa.");
            }

            options[optionId] = option;
            selectionsByOption[optionId] = await dbContext
                .GuestTransportSelections
                .Where(selection =>
                    selection.OrganizationId == submission.OrganizationId
                    && selection.EventId == submission.EventId
                    && selection.EventTransportOptionId == optionId)
                .ToListAsync(cancellationToken);
        }

        foreach (var guest in namedGuests)
        {
            var guestId = guest.Request.EventGuestId!.Value;
            var desiredOptionId =
                guest.Request.AttendanceStatus == GuestAttendanceStatus.Attending
                    ? guest.Transport?.TransportOptionId
                    : null;
            foreach (var current in existing.Where(selection =>
                         selection.EventGuestId == guestId
                         && selection.Status is
                             TransportSelectionStatus.Confirmed
                             or TransportSelectionStatus.Waitlisted
                             or TransportSelectionStatus.Requested
                         && selection.EventTransportOptionId
                         != desiredOptionId))
            {
                TransitionTransport(
                    current,
                    TransportSelectionStatus.Cancelled,
                    null,
                    submission,
                    actorUserId,
                    now);
            }

            if (desiredOptionId is null)
            {
                continue;
            }

            var option = options[desiredOptionId.Value];
            var optionSelections = selectionsByOption[desiredOptionId.Value];
            var selection = optionSelections.SingleOrDefault(item =>
                item.EventGuestId == guestId);
            if (selection?.Status == TransportSelectionStatus.Confirmed)
            {
                selection.UpdateStatus(
                    TransportSelectionStatus.Confirmed,
                    submission.Id,
                    null,
                    now);
                continue;
            }

            var confirmed = optionSelections.Count(item =>
                item.Status == TransportSelectionStatus.Confirmed);
            var nextStatus = RsvpTransportAllocationPolicy.DetermineStatus(
                option.Name,
                option.Capacity,
                confirmed,
                option.AllowWaitlist);
            long? sequence = nextStatus
                             == TransportSelectionStatus.Waitlisted
                ? NextWaitlistSequence(optionSelections)
                : null;
            if (selection is null)
            {
                selection = GuestTransportSelection.Create(
                    submission.OrganizationId,
                    submission.EventId,
                    guestId,
                    option.Id,
                    nextStatus,
                    submission.Id,
                    sequence,
                    now);
                dbContext.GuestTransportSelections.Add(selection);
                optionSelections.Add(selection);
                RecordTransportTransition(
                    selection,
                    null,
                    nextStatus,
                    submission,
                    actorUserId,
                    now);
            }
            else
            {
                TransitionTransport(
                    selection,
                    nextStatus,
                    sequence,
                    submission,
                    actorUserId,
                    now);
            }
        }

        foreach (var optionId in affectedOptionIds)
        {
            var option = options[optionId];
            if (!option.Capacity.HasValue)
            {
                continue;
            }

            var optionSelections = selectionsByOption[optionId];
            while (optionSelections.Count(selection =>
                       selection.Status
                       == TransportSelectionStatus.Confirmed)
                   < option.Capacity.Value)
            {
                var candidate =
                    RsvpTransportAllocationPolicy.SelectNextWaitlisted(
                        optionSelections);
                if (candidate is null)
                {
                    break;
                }

                var previous = candidate.Status;
                candidate.UpdateStatus(
                    TransportSelectionStatus.Confirmed,
                    submission.Id,
                    null,
                    now);
                RecordTransportTransition(
                    candidate,
                    previous,
                    TransportSelectionStatus.Confirmed,
                    submission,
                    actorUserId,
                    now,
                    promoted: true);
            }
        }
    }

    private async Task ApplyAccommodationSelectionsAsync(
        RsvpSubmission submission,
        InvitationGroup group,
        IReadOnlyList<ParsedGuestRequest> parsedGuests,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        foreach (var parsed in parsedGuests.Where(guest =>
                     guest.Request.EventGuestId.HasValue))
        {
            var payload = parsed.Accommodation;
            if (payload?.EventAccommodationOptionId is not null)
            {
                var validOption = await dbContext.EventAccommodationOptions
                    .AnyAsync(option =>
                        option.OrganizationId == submission.OrganizationId
                        && option.EventId == submission.EventId
                        && option.Id
                        == payload.EventAccommodationOptionId.Value
                        && option.IsActive,
                        cancellationToken);
                if (!validOption)
                {
                    throw new RequestValidationException(
                        new Dictionary<string, string[]>
                        {
                            ["accommodationSelectionJson"] =
                            [
                                "La opción de hospedaje no pertenece al evento o está archivada."
                            ]
                        });
                }
            }

            var guestId = parsed.Request.EventGuestId!.Value;
            var current = await dbContext.GuestAccommodationSelections
                .SingleOrDefaultAsync(selection =>
                    selection.OrganizationId == submission.OrganizationId
                    && selection.EventId == submission.EventId
                    && selection.EventGuestId == guestId,
                    cancellationToken);
            if (payload is null)
            {
                payload = new AccommodationPayload(
                    null,
                    AccommodationSelectionStatus.NotNeeded,
                    null,
                    null);
            }

            if (current is null)
            {
                dbContext.GuestAccommodationSelections.Add(
                    GuestAccommodationSelection.Create(
                        submission.OrganizationId,
                        submission.EventId,
                        guestId,
                        group.Id,
                        payload.EventAccommodationOptionId,
                        payload.Status,
                        Normalize(payload.ReservationName),
                        Normalize(payload.ConfirmationReference),
                        submission.Id,
                        now));
            }
            else
            {
                current.Update(
                    payload.EventAccommodationOptionId,
                    payload.Status,
                    Normalize(payload.ReservationName),
                    Normalize(payload.ConfirmationReference),
                    submission.Id,
                    now);
            }
        }
    }

    private async Task<int> UpsertSensitiveDataAsync(
        RsvpSubmission submission,
        IReadOnlyList<ParsedGuestRequest> parsedGuests,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var updated = 0;
        foreach (var parsed in parsedGuests.Where(guest =>
                     guest.Request.EventGuestId.HasValue
                     && guest.Dietary is not null))
        {
            var payload = parsed.Dietary!;
            var guestId = parsed.Request.EventGuestId!.Value;
            var current = await dbContext.GuestDietaryAndAccessibilities
                .SingleOrDefaultAsync(data =>
                    data.OrganizationId == submission.OrganizationId
                    && data.EventId == submission.EventId
                    && data.EventGuestId == guestId,
                    cancellationToken);
            if (current is null)
            {
                current = GuestDietaryAndAccessibility.Create(
                    submission.OrganizationId,
                    submission.EventId,
                    guestId,
                    now);
                dbContext.GuestDietaryAndAccessibilities.Add(current);
            }

            current.Update(
                Normalize(payload.Allergies),
                Normalize(payload.DietaryRestrictions),
                Normalize(payload.AccessibilityRequirements),
                Normalize(payload.AdditionalNotes),
                submission.Id,
                now);
            if (payload.ConsentGranted)
            {
                current.GrantConsent(now);
            }

            updated++;
        }

        return updated;
    }

    private async Task UpsertCurrentGuestRsvpAsync(
        RsvpSubmission submission,
        RsvpSubmissionGuestRequest guest,
        int? companionSlotNumber,
        Guid? actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        CurrentGuestRsvp? current;
        if (guest.EventGuestId.HasValue)
        {
            current = await dbContext.CurrentGuestRsvps
                .SingleOrDefaultAsync(entity =>
                    entity.OrganizationId == submission.OrganizationId
                    && entity.EventId == submission.EventId
                    && entity.EventGuestId == guest.EventGuestId.Value,
                    cancellationToken);
        }
        else
        {
            current = await dbContext.CurrentGuestRsvps
                .SingleOrDefaultAsync(entity =>
                    entity.OrganizationId == submission.OrganizationId
                    && entity.EventId == submission.EventId
                    && entity.InvitationGroupId
                    == submission.InvitationGroupId
                    && entity.IsUnnamedCompanion
                    && entity.CompanionSlotNumber == companionSlotNumber,
                    cancellationToken);
        }

        if (current is null)
        {
            current = CurrentGuestRsvp.Create(
                submission.OrganizationId,
                submission.EventId,
                submission.InvitationGroupId,
                guest.EventGuestId,
                guest.AttendanceStatus,
                guest.IsUnnamedCompanion,
                companionSlotNumber,
                guest.DisplayName.Trim(),
                submission.Id,
                now);
            current.SetUpdatedBy(actorUserId);
            dbContext.CurrentGuestRsvps.Add(current);
        }
        else
        {
            current.UpdateStatus(
                guest.AttendanceStatus,
                guest.DisplayName.Trim(),
                submission.Id,
                now);
            current.SetUpdatedBy(actorUserId);
        }
    }

    private async Task<IReadOnlyList<ParsedGuestRequest>>
        ValidateAndParseGuestsAsync(
            Guid organizationId,
            Guid eventId,
            InvitationGroup group,
            EventRsvpSettings settings,
            RsvpSubmissionRequest request,
            CancellationToken cancellationToken)
    {
        if (request.ExpectedRevision < 0)
        {
            throw Validation(
                "expectedRevision",
                "La revisión esperada no puede ser negativa.");
        }

        if (request.Guests.Count > group.AllowedGuestCount)
        {
            throw new ConflictException(
                "La respuesta excede los lugares autorizados para el grupo.");
        }

        var companions = request.Guests.Count(guest =>
            guest.IsUnnamedCompanion);
        if (companions > group.MaxUnnamedCompanions
            || (companions > 0 && !group.AllowUnnamedCompanions))
        {
            throw new ConflictException(
                "La respuesta excede los acompañantes permitidos.");
        }

        var namedIds = request.Guests
            .Where(guest => guest.EventGuestId.HasValue)
            .Select(guest => guest.EventGuestId!.Value)
            .ToList();
        if (namedIds.Count != namedIds.Distinct().Count())
        {
            throw Validation(
                "guests",
                "Un invitado no puede aparecer más de una vez.");
        }

        var eventGuests = await dbContext.EventGuests
            .AsNoTracking()
            .Where(guest =>
                guest.OrganizationId == organizationId
                && guest.EventId == eventId
                && guest.InvitationGroupId == group.Id
                && guest.ArchivedAt == null)
            .ToListAsync(cancellationToken);
        if (namedIds.Any(id => eventGuests.All(guest => guest.Id != id)))
        {
            throw Validation(
                "guests",
                "La respuesta contiene un invitado ajeno al grupo o evento.");
        }

        if (settings.RequireResponseForEveryNamedGuest
            && eventGuests.Any(guest => !namedIds.Contains(guest.Id)))
        {
            throw Validation(
                "guests",
                "Debes responder por cada invitado nombrado del grupo.");
        }

        var result = new List<ParsedGuestRequest>(request.Guests.Count);
        foreach (var guest in request.Guests)
        {
            if (string.IsNullOrWhiteSpace(guest.DisplayName)
                || guest.DisplayName.Trim().Length > 200)
            {
                throw Validation(
                    "guests.displayName",
                    "El nombre es obligatorio y admite hasta 200 caracteres.");
            }

            var menuJson = RequireJsonObjectOrArray(
                guest.MenuSelectionsJson,
                "menuSelectionsJson");
            var transportJson = RequireJsonObjectOrArray(
                guest.TransportSelectionJson,
                "transportSelectionJson");
            var accommodationJson = RequireJsonObjectOrArray(
                guest.AccommodationSelectionJson,
                "accommodationSelectionJson");
            var dietaryJson = RequireJsonObjectOrArray(
                guest.DietaryJson,
                "dietaryJson");
            var dietary = DeserializeOptional<DietaryPayload>(
                dietaryJson,
                "dietaryJson");
            if (dietary is not null
                && HasSensitiveContent(dietary)
                && !dietary.ConsentGranted)
            {
                throw Validation(
                    "dietaryJson.consentGranted",
                    "Debes otorgar consentimiento antes de enviar alergias, restricciones, necesidades o notas sensibles.");
            }

            result.Add(new ParsedGuestRequest(
                guest,
                menuJson,
                transportJson,
                accommodationJson,
                dietaryJson,
                DeserializeOptional<TransportPayload>(
                    transportJson,
                    "transportSelectionJson"),
                DeserializeOptional<AccommodationPayload>(
                    accommodationJson,
                    "accommodationSelectionJson"),
                dietary));
        }

        return result;
    }

    private static RsvpSubmission CreateSubmission(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        Guid formVersionId,
        Guid? accessLinkId,
        RsvpSubmission? previous,
        RsvpSubmissionSource source,
        RsvpSubmissionRequest request,
        Guid? actorUserId,
        string? userAgent,
        string? ipAddress,
        string key,
        string fingerprint,
        DateTimeOffset now) =>
        CreateSubmissionWithReservedRevision(
            organizationId,
            eventId,
            groupId,
            formVersionId,
            accessLinkId,
            previous,
            source,
            request.OverallStatus,
            actorUserId,
            Normalize(request.ContactName),
            Normalize(request.ContactEmail),
            Normalize(request.ContactPhone),
            NormalizeUserAgent(userAgent),
            Normalize(ipAddress),
            string.IsNullOrWhiteSpace(request.ConsentSnapshot)
                ? null
                : EnsureJsonValue(request.ConsentSnapshot),
            key,
            now,
            fingerprint);

    private static RsvpSubmission CreateSubmissionWithReservedRevision(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        Guid formVersionId,
        Guid? accessLinkId,
        RsvpSubmission? previous,
        RsvpSubmissionSource source,
        RsvpOverallStatus overallStatus,
        Guid? actorUserId,
        string? contactName,
        string? contactEmail,
        string? contactPhone,
        string? userAgent,
        string? ipAddress,
        string? consentSnapshot,
        string key,
        DateTimeOffset now,
        string fingerprint)
    {
        var revision =
            RsvpSubmissionConcurrencyPolicy.ReserveNextRevision(previous);
        return RsvpSubmission.Create(
            organizationId,
            eventId,
            groupId,
            formVersionId,
            accessLinkId,
            revision.RevisionNumber,
            source,
            overallStatus,
            actorUserId,
            contactName,
            contactEmail,
            contactPhone,
            userAgent,
            ipAddress,
            consentSnapshot,
            key,
            revision.PreviousSubmissionId,
            now,
            fingerprint);
    }

    private async Task<InvitationGroup> LockGroupAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        CancellationToken cancellationToken) =>
        await dbContext.InvitationGroups
            .FromSqlInterpolated(
                $"""
                 SELECT *
                 FROM invitation_groups
                 WHERE organization_id = {organizationId}
                   AND event_id = {eventId}
                   AND id = {groupId}
                 FOR UPDATE
                 """)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Grupo no encontrado.");

    private async Task ValidatePublicExperienceAsync(
        GuestAccessLink link,
        CancellationToken cancellationToken)
    {
        var eventStatus = await dbContext.Events
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.Id == link.EventId)
            .Select(entity => (EventStatus?)entity.Status)
            .SingleOrDefaultAsync(cancellationToken);
        if (eventStatus is null)
        {
            throw new NotFoundException("Evento no encontrado.");
        }

        if (eventStatus is EventStatus.Suspended
            or EventStatus.Cancelled
            or EventStatus.Closed
            or EventStatus.Archived)
        {
            throw new ConflictException(
                "El evento no admite respuestas en su estado actual.");
        }

        var experiencePublished = await dbContext.EventGuestExperiences
            .AsNoTracking()
            .AnyAsync(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.EventId == link.EventId
                && entity.Status == GuestExperienceStatus.Published,
                cancellationToken);
        if (!experiencePublished)
        {
            throw new ConflictException(
                "La experiencia pública no está activa.");
        }
    }

    private async Task<RsvpFormVersion> GetSubmissionFormVersionAsync(
        Guid organizationId,
        Guid eventId,
        Guid requestedVersionId,
        RsvpSubmission? previous,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.RsvpForms
                       .AsNoTracking()
                       .SingleOrDefaultAsync(entity =>
                           entity.OrganizationId == organizationId
                           && entity.EventId == eventId
                           && entity.ActivePublishedVersionId != null,
                           cancellationToken)
                   ?? throw new NotFoundException(
                       "No hay formulario publicado.");
        if (requestedVersionId == Guid.Empty)
        {
            throw VersionValidation(
                "form_version_mismatch",
                "La solicitud debe indicar la versión exacta del formulario presentado.");
        }

        var version = await dbContext.RsvpFormVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.RsvpFormId == form.Id
                    && entity.Id == requestedVersionId,
                cancellationToken);
        if (version?.PublishedAt is null)
        {
            throw VersionValidation(
                "form_version_mismatch",
                "La versión indicada no es una versión publicada de este formulario.");
        }

        var isCurrentVersion =
            previous is null
            && form.ActivePublishedVersionId == requestedVersionId;
        var continuesHistoricalEdition =
            previous?.RsvpFormVersionId == requestedVersionId;
        if (!isCurrentVersion && !continuesHistoricalEdition)
        {
            throw VersionValidation(
                "form_version_mismatch",
                "La versión indicada no corresponde al formulario vigente ni a la respuesta que se está editando.");
        }

        return version;
    }

    private static RsvpQuestionDefinitionSet ParseSubmissionQuestions(
        string snapshot)
    {
        try
        {
            return RsvpQuestionDefinitionParser.ParseAndValidate(snapshot);
        }
        catch (RequestValidationException)
        {
            throw VersionValidation(
                "form_version_invalid",
                "La versión publicada no puede utilizarse porque su definición no es válida.");
        }
    }

    private async Task<QuestionSubmissionContext> BuildQuestionContextAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        RsvpSubmissionRequest request,
        CancellationToken cancellationToken)
    {
        var eventGuests = await dbContext.EventGuests
            .AsNoTracking()
            .Where(guest =>
                guest.OrganizationId == organizationId
                && guest.EventId == eventId
                && guest.InvitationGroupId == groupId
                && guest.ArchivedAt == null)
            .ToDictionaryAsync(guest => guest.Id, cancellationToken);
        var normalizedGuests =
            new List<RsvpSubmissionGuestRequest>(request.Guests.Count);
        var evaluationGuests =
            new List<RsvpQuestionGuestContext>(request.Guests.Count);
        var responseIds = new HashSet<Guid>();
        foreach (var requestedGuest in request.Guests)
        {
            Guid responseGuestId;
            AgeCategory ageCategory;
            GuestType guestType;
            bool isPrimaryContact;
            if (requestedGuest.EventGuestId is { } eventGuestId)
            {
                if (!eventGuests.TryGetValue(eventGuestId, out var eventGuest))
                {
                    throw new RsvpValidationException(
                    [
                        new RsvpValidationError(
                            null,
                            eventGuestId,
                            "guest_not_in_group",
                            "El invitado no pertenece al grupo o evento.")
                    ]);
                }

                if (requestedGuest.ResponseGuestId != Guid.Empty
                    && requestedGuest.ResponseGuestId != eventGuestId)
                {
                    throw VersionValidation(
                        "invalid_scope",
                        "ResponseGuestId debe coincidir con EventGuestId para invitados nombrados.");
                }

                responseGuestId = eventGuestId;
                ageCategory = eventGuest.AgeCategory;
                guestType = eventGuest.GuestType;
                isPrimaryContact = eventGuest.IsPrimaryContact;
            }
            else
            {
                if (!requestedGuest.IsUnnamedCompanion
                    || requestedGuest.ResponseGuestId == Guid.Empty)
                {
                    throw VersionValidation(
                        "invalid_scope",
                        "Cada acompañante incluido debe tener un ResponseGuestId no vacío.");
                }

                if (!Enum.TryParse<AgeCategory>(
                        requestedGuest.AgeCategory,
                        ignoreCase: false,
                        out ageCategory))
                {
                    throw VersionValidation(
                        "invalid_value_type",
                        "La categoría de edad del acompañante no es válida.");
                }

                responseGuestId = requestedGuest.ResponseGuestId;
                guestType = GuestType.Other;
                isPrimaryContact = false;
            }

            if (!responseIds.Add(responseGuestId))
            {
                throw VersionValidation(
                    "invalid_scope",
                    "ResponseGuestId no puede repetirse dentro de la entrega.");
            }

            var normalizedGuest = requestedGuest with
            {
                ResponseGuestId = responseGuestId
            };
            normalizedGuests.Add(normalizedGuest);
            evaluationGuests.Add(new RsvpQuestionGuestContext(
                responseGuestId,
                requestedGuest.EventGuestId,
                requestedGuest.DisplayName.Trim(),
                ageCategory,
                guestType,
                requestedGuest.IsUnnamedCompanion,
                isPrimaryContact,
                requestedGuest.AttendanceStatus));
        }

        var groupTags = await (
                from assignment in dbContext.InvitationGroupTags
                    .AsNoTracking()
                join tag in dbContext.GuestTags.AsNoTracking()
                    on new
                    {
                        assignment.OrganizationId,
                        assignment.EventId,
                        Id = assignment.GuestTagId
                    }
                    equals new
                    {
                        tag.OrganizationId,
                        tag.EventId,
                        tag.Id
                    }
                where assignment.OrganizationId == organizationId
                      && assignment.EventId == eventId
                      && assignment.InvitationGroupId == groupId
                      && tag.ArchivedAt == null
                select tag.Name)
            .ToHashSetAsync(StringComparer.Ordinal, cancellationToken);
        return new QuestionSubmissionContext(
            normalizedGuests,
            new RsvpQuestionEvaluationContext(
                evaluationGuests,
                groupTags));
    }

    private static RsvpSubmissionAnswerRequest ToRequest(
        NormalizedRsvpAnswer answer) =>
        new(
            answer.QuestionId,
            answer.GuestId,
            answer.AnswerValue,
            answer.DisplayValue);

    private static RsvpValidationException VersionValidation(
        string code,
        string message) =>
        new(
        [
            new RsvpValidationError(
                null,
                null,
                code,
                message)
        ]);

    private async Task<RsvpSubmission?> GetCurrentSubmissionAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        CancellationToken cancellationToken) =>
        await dbContext.RsvpSubmissions
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId)
            .OrderByDescending(entity => entity.RevisionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<RsvpSubmission?> FindByIdempotencyAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        string key,
        CancellationToken cancellationToken) =>
        await dbContext.RsvpSubmissions
            .AsNoTracking()
            .SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId
                && entity.IdempotencyKey == key,
                cancellationToken);

    private static void ValidateLink(
        GuestAccessLink link,
        DateTimeOffset now)
    {
        if (link.Status != GuestAccessLinkStatus.Active)
        {
            throw new NotFoundException("El enlace ya no está activo.");
        }

        if (link.IsExpired(now))
        {
            throw new GoneException("El enlace de invitado expiró.");
        }
    }

    private void TransitionTransport(
        GuestTransportSelection selection,
        TransportSelectionStatus nextStatus,
        long? sequence,
        RsvpSubmission submission,
        Guid? actorUserId,
        DateTimeOffset now)
    {
        var previous = selection.Status;
        selection.UpdateStatus(
            nextStatus,
            submission.Id,
            sequence,
            now);
        RecordTransportTransition(
            selection,
            previous,
            nextStatus,
            submission,
            actorUserId,
            now);
    }

    private void RecordTransportTransition(
        GuestTransportSelection selection,
        TransportSelectionStatus? previous,
        TransportSelectionStatus nextStatus,
        RsvpSubmission submission,
        Guid? actorUserId,
        DateTimeOffset now,
        bool promoted = false)
    {
        dbContext.GuestTransportSelectionHistory.Add(
            GuestTransportSelectionHistory.Create(
                submission.OrganizationId,
                submission.EventId,
                selection.EventGuestId,
                selection.EventTransportOptionId,
                previous,
                nextStatus,
                submission.Id,
                selection.WaitlistSequence,
                now));
        var action = promoted
            ? AuditActions.TransportWaitlistPromoted
            : nextStatus switch
            {
                TransportSelectionStatus.Confirmed =>
                    AuditActions.TransportSelectionConfirmed,
                TransportSelectionStatus.Waitlisted =>
                    AuditActions.TransportSelectionWaitlisted,
                _ => AuditActions.TransportSelectionCancelled
            };
        auditService.Add(
            submission.OrganizationId,
            submission.EventId,
            actorUserId,
            action,
            nameof(GuestTransportSelection),
            selection.EventGuestId,
            new Dictionary<string, object?>
            {
                ["transportOptionId"] =
                    selection.EventTransportOptionId,
                ["status"] = nextStatus.ToString()
            });
    }

    private static long NextWaitlistSequence(
        IEnumerable<GuestTransportSelection> selections) =>
        selections
            .Where(selection => selection.WaitlistSequence.HasValue)
            .Select(selection => selection.WaitlistSequence!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

    private static bool ContainsSensitiveData(
        RsvpSubmissionRequest request) =>
        request.Guests.Any(guest =>
            DeserializeOptional<DietaryPayload>(
                RequireJsonObjectOrArray(
                    guest.DietaryJson,
                    "dietaryJson"),
                "dietaryJson") is { } data
            && (HasSensitiveContent(data) || data.ConsentGranted));

    private static bool HasSensitiveContent(DietaryPayload data) =>
        Normalize(data.Allergies) is not null
        || Normalize(data.DietaryRestrictions) is not null
        || Normalize(data.AccessibilityRequirements) is not null
        || Normalize(data.AdditionalNotes) is not null;

    private static IReadOnlyDictionary<string, object?>
        SensitiveAuditMetadata(int recordCount, string operationType) =>
        new Dictionary<string, object?>
        {
            ["recordCount"] = recordCount,
            ["operationType"] = operationType
        };

    private static string RequireJsonObjectOrArray(
        string? value,
        string field)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "{}" : value;
        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind is not (
                    JsonValueKind.Object or JsonValueKind.Array))
            {
                throw Validation(
                    field,
                    "El valor debe ser un objeto o arreglo JSON.");
            }

            return RsvpRequestFingerprint.CanonicalizeJson(normalized);
        }
        catch (JsonException)
        {
            throw Validation(field, "El valor no contiene JSON válido.");
        }
    }

    private static T? DeserializeOptional<T>(
        string json,
        string field)
        where T : class
    {
        if (json is "{}" or "[]")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            throw Validation(field, "El valor no tiene el formato esperado.");
        }
    }

    private static string EnsureJsonValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "\"\"";
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            return RsvpRequestFingerprint.CanonicalizeJson(value);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(value.Trim());
        }
    }

    private static RequestValidationException Validation(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeUserAgent(string? userAgent)
    {
        var normalized = Normalize(userAgent);
        return normalized is null
            ? null
            : normalized[..Math.Min(normalized.Length, 50)];
    }

    private static bool IsIdempotencyViolation(
        DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_rsvp_submissions_idempotency"
        };

    private sealed record ParsedGuestRequest(
        RsvpSubmissionGuestRequest Request,
        string MenuJson,
        string TransportJson,
        string AccommodationJson,
        string DietaryJson,
        TransportPayload? Transport,
        AccommodationPayload? Accommodation,
        DietaryPayload? Dietary);

    private sealed record QuestionSubmissionContext(
        List<RsvpSubmissionGuestRequest> NormalizedGuests,
        RsvpQuestionEvaluationContext EvaluationContext);

    private sealed class SubmissionFingerprint
    {
        public string? Value { get; set; }

        public string RequiredValue =>
            Value
            ?? throw new InvalidOperationException(
                "El fingerprint normalizado no fue calculado.");
    }

    private sealed record TransportPayload(Guid? TransportOptionId);

    private sealed record AccommodationPayload(
        Guid? EventAccommodationOptionId,
        AccommodationSelectionStatus Status,
        string? ReservationName,
        string? ConfirmationReference);

    private sealed record DietaryPayload(
        string? Allergies,
        string? DietaryRestrictions,
        string? AccessibilityRequirements,
        string? AdditionalNotes,
        bool ConsentGranted);
}
