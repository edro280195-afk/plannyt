using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed class RsvpProjectionReconciliationService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<RsvpProjectionReconciliationResponse> DiagnoseAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken) =>
        ReconcileAsync(
            organizationId,
            eventId,
            repair: false,
            cancellationToken);

    public Task<RsvpProjectionReconciliationResponse> RepairAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken) =>
        ReconcileAsync(
            organizationId,
            eventId,
            repair: true,
            cancellationToken);

    private async Task<RsvpProjectionReconciliationResponse> ReconcileAsync(
        Guid organizationId,
        Guid eventId,
        bool repair,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            repair
                ? Permissions.RsvpResponsesCorrect
                : Permissions.RsvpResponsesView,
            eventId,
            cancellationToken);
        await using var transaction = repair
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var now = timeProvider.GetUtcNow();
        var submissions = await dbContext.RsvpSubmissions
            .Where(submission =>
                submission.OrganizationId == organizationId
                && submission.EventId == eventId)
            .OrderByDescending(submission => submission.RevisionNumber)
            .ThenByDescending(submission => submission.SubmittedAt)
            .ToListAsync(cancellationToken);
        var latestSubmissions = submissions
            .DistinctBy(submission => submission.InvitationGroupId)
            .ToList();
        var submissionIds = latestSubmissions
            .Select(submission => submission.Id)
            .ToList();
        var snapshots = await dbContext.RsvpSubmissionGuests
            .Where(snapshot =>
                submissionIds.Contains(snapshot.RsvpSubmissionId))
            .ToListAsync(cancellationToken);
        var currentRsvps = await dbContext.CurrentGuestRsvps
            .Where(current =>
                current.OrganizationId == organizationId
                && current.EventId == eventId)
            .ToListAsync(cancellationToken);
        var sensitiveData = await dbContext.GuestDietaryAndAccessibilities
            .Where(data =>
                data.OrganizationId == organizationId
                && data.EventId == eventId)
            .ToListAsync(cancellationToken);
        var eventGuestIds = await dbContext.EventGuests
            .Where(guest =>
                guest.OrganizationId == organizationId
                && guest.EventId == eventId
                && guest.ArchivedAt == null)
            .Select(guest => guest.Id)
            .ToListAsync(cancellationToken);
        var validGuestIds = eventGuestIds.ToHashSet();
        var transportOptions = repair
            ? await dbContext.EventTransportOptions
                .FromSqlInterpolated(
                    $"""
                     SELECT *
                     FROM event_transport_options
                     WHERE organization_id = {organizationId}
                       AND event_id = {eventId}
                     ORDER BY id
                     FOR UPDATE
                     """)
                .ToListAsync(cancellationToken)
            : await dbContext.EventTransportOptions
                .AsNoTracking()
                .Where(option =>
                    option.OrganizationId == organizationId
                    && option.EventId == eventId)
                .ToListAsync(cancellationToken);
        var transportSelections = await dbContext.GuestTransportSelections
            .Where(selection =>
                selection.OrganizationId == organizationId
                && selection.EventId == eventId)
            .ToListAsync(cancellationToken);
        var accommodationOptions = await dbContext.EventAccommodationOptions
            .AsNoTracking()
            .Where(option =>
                option.OrganizationId == organizationId
                && option.EventId == eventId)
            .ToListAsync(cancellationToken);
        var accommodationSelections = await dbContext
            .GuestAccommodationSelections
            .Where(selection =>
                selection.OrganizationId == organizationId
                && selection.EventId == eventId)
            .ToListAsync(cancellationToken);

        var issues = new List<RsvpProjectionIssueResponse>();
        var repairedCount = 0;
        foreach (var submission in latestSubmissions)
        {
            var groupSnapshots = snapshots
                .Where(snapshot =>
                    snapshot.RsvpSubmissionId == submission.Id)
                .OrderBy(snapshot => snapshot.CompanionSlotNumber)
                .ThenBy(snapshot => snapshot.EventGuestId)
                .ToList();
            foreach (var snapshot in groupSnapshots)
            {
                var current = FindCurrent(
                    currentRsvps,
                    submission.InvitationGroupId,
                    snapshot);
                if (current is null)
                {
                    AddIssue(
                        issues,
                        "current_guest_rsvp.missing",
                        "CurrentGuestRsvp",
                        submission,
                        snapshot.EventGuestId,
                        repairable: true,
                        "Falta la proyección vigente del invitado.");
                    if (repair)
                    {
                        current = CurrentGuestRsvp.Create(
                            organizationId,
                            eventId,
                            submission.InvitationGroupId,
                            snapshot.EventGuestId,
                            snapshot.AttendanceStatus,
                            snapshot.IsUnnamedCompanion,
                            snapshot.CompanionSlotNumber,
                            snapshot.DisplayName,
                            submission.Id,
                            now);
                        current.SetUpdatedBy(access.UserAccountId);
                        dbContext.CurrentGuestRsvps.Add(current);
                        currentRsvps.Add(current);
                        repairedCount++;
                    }
                }
                else if (current.LastSubmissionId != submission.Id
                         || current.AttendanceStatus
                         != snapshot.AttendanceStatus
                         || current.CurrentDisplayName != snapshot.DisplayName)
                {
                    AddIssue(
                        issues,
                        "current_guest_rsvp.mismatch",
                        "CurrentGuestRsvp",
                        submission,
                        snapshot.EventGuestId,
                        repairable: true,
                        "La proyección vigente no coincide con la última entrega.");
                    if (repair)
                    {
                        current.UpdateStatus(
                            snapshot.AttendanceStatus,
                            snapshot.DisplayName,
                            submission.Id,
                            now);
                        current.SetUpdatedBy(access.UserAccountId);
                        repairedCount++;
                    }
                }

                if (!snapshot.EventGuestId.HasValue)
                {
                    continue;
                }

                var guestId = snapshot.EventGuestId.Value;
                repairedCount += ReconcileSensitiveData(
                    issues,
                    sensitiveData,
                    submission,
                    snapshot,
                    organizationId,
                    eventId,
                    now,
                    repair);
                repairedCount += ReconcileAccommodation(
                    issues,
                    accommodationSelections,
                    accommodationOptions,
                    submission,
                    snapshot,
                    organizationId,
                    eventId,
                    now,
                    repair);
                repairedCount += ReconcileTransport(
                    issues,
                    transportSelections,
                    transportOptions,
                    submission,
                    snapshot,
                    organizationId,
                    eventId,
                    now,
                    repair);

                if (!validGuestIds.Contains(guestId))
                {
                    AddIssue(
                        issues,
                        "projection.invalid_guest",
                        "OperationalProjections",
                        submission,
                        guestId,
                        repairable: false,
                        "La proyección apunta a un invitado archivado o inexistente.");
                }
            }
        }

        repairedCount += ReconcileTransportCapacity(
            issues,
            transportSelections,
            transportOptions,
            latestSubmissions,
            organizationId,
            eventId,
            now,
            repair);
        foreach (var selection in transportSelections)
        {
            var option = transportOptions.SingleOrDefault(candidate =>
                candidate.Id == selection.EventTransportOptionId);
            if (option is not null && !option.IsActive
                                   && selection.Status is
                                       TransportSelectionStatus.Confirmed
                                       or TransportSelectionStatus.Waitlisted)
            {
                AddIssue(
                    issues,
                    "transport.archived_option",
                    "GuestTransportSelection",
                    null,
                    selection.EventGuestId,
                    selection.LastSubmissionId,
                    repairable: false,
                    "Una selección activa apunta a una opción archivada.");
            }
        }

        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            repair
                ? AuditActions.RsvpProjectionRepaired
                : AuditActions.RsvpProjectionDiagnosed,
            "RsvpProjection",
            eventId,
            new Dictionary<string, object?>
            {
                ["groupsEvaluated"] = latestSubmissions.Count,
                ["issuesDetected"] = issues.Count,
                ["issuesRepaired"] = repairedCount,
                ["mode"] = repair ? "repair" : "diagnostic"
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return new RsvpProjectionReconciliationResponse(
            repair,
            latestSubmissions.Count,
            issues.Count,
            repairedCount,
            issues);
    }

    private int ReconcileSensitiveData(
        ICollection<RsvpProjectionIssueResponse> issues,
        ICollection<GuestDietaryAndAccessibility> currentData,
        RsvpSubmission submission,
        RsvpSubmissionGuest snapshot,
        Guid organizationId,
        Guid eventId,
        DateTimeOffset now,
        bool repair)
    {
        var payload = DeserializeOptional<DietaryPayload>(
            snapshot.DietarySnapshot);
        if (payload is null)
        {
            return 0;
        }

        var guestId = snapshot.EventGuestId!.Value;
        var current = currentData.SingleOrDefault(data =>
            data.EventGuestId == guestId);
        var mismatch = current is null
                       || current.LastSubmissionId != submission.Id
                       || Normalize(current.Allergies)
                       != Normalize(payload.Allergies)
                       || Normalize(current.DietaryRestrictions)
                       != Normalize(payload.DietaryRestrictions)
                       || Normalize(current.AccessibilityRequirements)
                       != Normalize(payload.AccessibilityRequirements)
                       || Normalize(current.AdditionalNotes)
                       != Normalize(payload.AdditionalNotes)
                       || current.ConsentGrantedAt.HasValue
                       != payload.ConsentGranted;
        if (!mismatch)
        {
            return 0;
        }

        AddIssue(
            issues,
            current is null
                ? "sensitive_data.missing"
                : "sensitive_data.mismatch",
            "GuestDietaryAndAccessibility",
            submission,
            guestId,
            repairable: true,
            "La proyección sensible no coincide con la última entrega.");
        if (!repair)
        {
            return 0;
        }

        if (current is null)
        {
            current = GuestDietaryAndAccessibility.Create(
                organizationId,
                eventId,
                guestId,
                now);
            dbContext.GuestDietaryAndAccessibilities.Add(current);
            currentData.Add(current);
        }

        current.Update(
            Normalize(payload.Allergies),
            Normalize(payload.DietaryRestrictions),
            Normalize(payload.AccessibilityRequirements),
            Normalize(payload.AdditionalNotes),
            submission.Id,
            now);
        current.SetConsent(payload.ConsentGranted, now);
        return 1;
    }

    private int ReconcileAccommodation(
        ICollection<RsvpProjectionIssueResponse> issues,
        ICollection<GuestAccommodationSelection> selections,
        IReadOnlyCollection<EventAccommodationOption> options,
        RsvpSubmission submission,
        RsvpSubmissionGuest snapshot,
        Guid organizationId,
        Guid eventId,
        DateTimeOffset now,
        bool repair)
    {
        var payload = DeserializeOptional<AccommodationPayload>(
                          snapshot.AccommodationSelectionSnapshot)
                      ?? new AccommodationPayload(
                          null,
                          AccommodationSelectionStatus.NotNeeded,
                          null,
                          null);
        var guestId = snapshot.EventGuestId!.Value;
        var current = selections.SingleOrDefault(selection =>
            selection.EventGuestId == guestId);
        var optionIsValid = !payload.EventAccommodationOptionId.HasValue
                            || options.Any(option =>
                                option.Id
                                == payload.EventAccommodationOptionId.Value
                                && option.IsActive);
        if (!optionIsValid)
        {
            AddIssue(
                issues,
                "accommodation.archived_or_missing_option",
                "GuestAccommodationSelection",
                submission,
                guestId,
                repairable: false,
                "La última entrega apunta a una opción de hospedaje inválida.");
            return 0;
        }

        var mismatch = current is null
                       || current.LastSubmissionId != submission.Id
                       || current.EventAccommodationOptionId
                       != payload.EventAccommodationOptionId
                       || current.Status != payload.Status
                       || Normalize(current.ReservationName)
                       != Normalize(payload.ReservationName)
                       || Normalize(current.ConfirmationReference)
                       != Normalize(payload.ConfirmationReference);
        if (!mismatch)
        {
            return 0;
        }

        AddIssue(
            issues,
            current is null
                ? "accommodation.missing"
                : "accommodation.mismatch",
            "GuestAccommodationSelection",
            submission,
            guestId,
            repairable: true,
            "La proyección de hospedaje no coincide con la última entrega.");
        if (!repair)
        {
            return 0;
        }

        if (current is null)
        {
            current = GuestAccommodationSelection.Create(
                organizationId,
                eventId,
                guestId,
                submission.InvitationGroupId,
                payload.EventAccommodationOptionId,
                payload.Status,
                Normalize(payload.ReservationName),
                Normalize(payload.ConfirmationReference),
                submission.Id,
                now);
            dbContext.GuestAccommodationSelections.Add(current);
            selections.Add(current);
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

        return 1;
    }

    private int ReconcileTransport(
        ICollection<RsvpProjectionIssueResponse> issues,
        ICollection<GuestTransportSelection> selections,
        IReadOnlyCollection<EventTransportOption> options,
        RsvpSubmission submission,
        RsvpSubmissionGuest snapshot,
        Guid organizationId,
        Guid eventId,
        DateTimeOffset now,
        bool repair)
    {
        var payload = DeserializeOptional<TransportPayload>(
            snapshot.TransportSelectionSnapshot);
        var desiredOptionId = snapshot.AttendanceStatus
                              == GuestAttendanceStatus.Attending
            ? payload?.TransportOptionId
            : null;
        var guestId = snapshot.EventGuestId!.Value;
        var active = selections.Where(selection =>
                selection.EventGuestId == guestId
                && selection.Status is
                    TransportSelectionStatus.Confirmed
                    or TransportSelectionStatus.Waitlisted
                    or TransportSelectionStatus.Requested)
            .ToList();
        var desired = desiredOptionId.HasValue
            ? active.SingleOrDefault(selection =>
                selection.EventTransportOptionId == desiredOptionId.Value)
            : null;
        var mismatch = active.Any(selection =>
                           selection.EventTransportOptionId
                           != desiredOptionId)
                       || (desiredOptionId.HasValue
                           && (desired is null
                               || desired.LastSubmissionId != submission.Id));
        if (!mismatch)
        {
            return 0;
        }

        var option = desiredOptionId.HasValue
            ? options.SingleOrDefault(candidate =>
                candidate.Id == desiredOptionId.Value)
            : null;
        var confirmedCount = desiredOptionId.HasValue
            ? selections.Count(selection =>
                selection.EventTransportOptionId == desiredOptionId.Value
                && selection.Status == TransportSelectionStatus.Confirmed)
            : 0;
        var canCreateDesiredSelection = option is { IsActive: true }
                                        && RsvpTransportAllocationPolicy
                                            .CanAllocate(
                                                option.Capacity,
                                                confirmedCount,
                                                option.AllowWaitlist);
        var canRepair = !desiredOptionId.HasValue
                        || desired is not null
                        || canCreateDesiredSelection;
        AddIssue(
            issues,
            "transport.mismatch",
            "GuestTransportSelection",
            submission,
            guestId,
            canRepair,
            "La selección operativa de transporte no coincide con la última entrega.");
        if (!repair || !canRepair)
        {
            return 0;
        }

        foreach (var obsolete in active.Where(selection =>
                     selection.EventTransportOptionId != desiredOptionId))
        {
            RecordTransportRepair(
                obsolete,
                TransportSelectionStatus.Cancelled,
                submission.Id,
                null,
                now);
        }

        if (!desiredOptionId.HasValue)
        {
            return 1;
        }

        if (desired is not null)
        {
            desired.UpdateStatus(
                desired.Status,
                submission.Id,
                desired.WaitlistSequence,
                now);
            return 1;
        }

        var status = !option!.Capacity.HasValue
                     || confirmedCount < option.Capacity.Value
            ? TransportSelectionStatus.Confirmed
            : option.AllowWaitlist
                ? TransportSelectionStatus.Waitlisted
                : throw new InvalidOperationException(
                    "La reparación no puede exceder la capacidad sin lista de espera.");
        long? sequence = status == TransportSelectionStatus.Waitlisted
            ? NextWaitlistSequence(selections, option.Id)
            : null;
        var created = GuestTransportSelection.Create(
            organizationId,
            eventId,
            guestId,
            option.Id,
            status,
            submission.Id,
            sequence,
            now);
        dbContext.GuestTransportSelections.Add(created);
        selections.Add(created);
        dbContext.GuestTransportSelectionHistory.Add(
            GuestTransportSelectionHistory.Create(
                organizationId,
                eventId,
                guestId,
                option.Id,
                null,
                status,
                submission.Id,
                sequence,
                now));
        return 1;
    }

    private int ReconcileTransportCapacity(
        ICollection<RsvpProjectionIssueResponse> issues,
        ICollection<GuestTransportSelection> selections,
        IReadOnlyCollection<EventTransportOption> options,
        IReadOnlyCollection<RsvpSubmission> latestSubmissions,
        Guid organizationId,
        Guid eventId,
        DateTimeOffset now,
        bool repair)
    {
        var repaired = 0;
        foreach (var option in options.Where(candidate =>
                     candidate.Capacity.HasValue))
        {
            var confirmed = selections
                .Where(selection =>
                    selection.EventTransportOptionId == option.Id
                    && selection.Status
                    == TransportSelectionStatus.Confirmed)
                .OrderBy(selection => selection.RequestedAt)
                .ThenBy(selection => selection.EventGuestId)
                .ToList();
            var overflow = confirmed.Count - option.Capacity!.Value;
            if (overflow <= 0)
            {
                continue;
            }

            AddIssue(
                issues,
                "transport.over_capacity",
                "GuestTransportSelection",
                null,
                null,
                null,
                option.AllowWaitlist,
                "La opción de transporte supera su capacidad configurada.");
            if (!repair || !option.AllowWaitlist)
            {
                continue;
            }

            foreach (var selection in confirmed
                         .TakeLast(overflow)
                         .Reverse())
            {
                var submissionId = selection.LastSubmissionId
                                   ?? latestSubmissions
                                       .OrderByDescending(item =>
                                           item.SubmittedAt)
                                       .Select(item => item.Id)
                                       .FirstOrDefault();
                if (submissionId == Guid.Empty)
                {
                    continue;
                }

                var sequence = NextWaitlistSequence(
                    selections,
                    option.Id);
                RecordTransportRepair(
                    selection,
                    TransportSelectionStatus.Waitlisted,
                    submissionId,
                    sequence,
                    now);
                repaired++;
            }
        }

        return repaired;
    }

    private void RecordTransportRepair(
        GuestTransportSelection selection,
        TransportSelectionStatus status,
        Guid submissionId,
        long? waitlistSequence,
        DateTimeOffset now)
    {
        var previous = selection.Status;
        selection.UpdateStatus(
            status,
            submissionId,
            waitlistSequence,
            now);
        dbContext.GuestTransportSelectionHistory.Add(
            GuestTransportSelectionHistory.Create(
                selection.OrganizationId,
                selection.EventId,
                selection.EventGuestId,
                selection.EventTransportOptionId,
                previous,
                status,
                submissionId,
                waitlistSequence,
                now));
    }

    private static CurrentGuestRsvp? FindCurrent(
        IEnumerable<CurrentGuestRsvp> currentRsvps,
        Guid groupId,
        RsvpSubmissionGuest snapshot) =>
        snapshot.EventGuestId.HasValue
            ? currentRsvps.SingleOrDefault(current =>
                current.EventGuestId == snapshot.EventGuestId)
            : currentRsvps.SingleOrDefault(current =>
                current.InvitationGroupId == groupId
                && current.IsUnnamedCompanion
                && current.CompanionSlotNumber
                == snapshot.CompanionSlotNumber);

    private static void AddIssue(
        ICollection<RsvpProjectionIssueResponse> issues,
        string code,
        string projection,
        RsvpSubmission? submission,
        Guid? eventGuestId,
        bool repairable,
        string description) =>
        AddIssue(
            issues,
            code,
            projection,
            submission?.InvitationGroupId,
            eventGuestId,
            submission?.Id,
            repairable,
            description);

    private static void AddIssue(
        ICollection<RsvpProjectionIssueResponse> issues,
        string code,
        string projection,
        Guid? groupId,
        Guid? eventGuestId,
        Guid? submissionId,
        bool repairable,
        string description) =>
        issues.Add(
            new RsvpProjectionIssueResponse(
                code,
                projection,
                groupId,
                eventGuestId,
                submissionId,
                repairable,
                description));

    private static T? DeserializeOptional<T>(string? json)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Array
            && document.RootElement.GetArrayLength() == 0)
        {
            return null;
        }

        if (document.RootElement.ValueKind == JsonValueKind.Object
            && !document.RootElement.EnumerateObject().Any())
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static long NextWaitlistSequence(
        IEnumerable<GuestTransportSelection> selections,
        Guid optionId) =>
        selections
            .Where(selection =>
                selection.EventTransportOptionId == optionId
                && selection.WaitlistSequence.HasValue)
            .Select(selection => selection.WaitlistSequence!.Value)
            .DefaultIfEmpty(0)
            .Max() + 1;

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
