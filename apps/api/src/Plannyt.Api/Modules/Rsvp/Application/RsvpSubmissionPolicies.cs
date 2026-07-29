using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed record RsvpRevisionReservation(
    int RevisionNumber,
    Guid? PreviousSubmissionId);

public static class RsvpSubmissionConcurrencyPolicy
{
    public static void ValidateExpectedRevision(
        int expectedRevision,
        RsvpSubmission? current)
    {
        var currentRevision = current?.RevisionNumber ?? 0;
        if (expectedRevision != currentRevision)
        {
            throw new RsvpRevisionConflictException(
                expectedRevision,
                currentRevision);
        }
    }

    public static Guid ResolveIdempotentRetry(
        RsvpSubmission? persistedSubmission,
        string requestFingerprint)
    {
        if (persistedSubmission is null
            || !string.Equals(
                persistedSubmission.RequestFingerprint,
                requestFingerprint,
                StringComparison.Ordinal))
        {
            throw new IdempotencyConflictException();
        }

        return persistedSubmission.Id;
    }

    public static RsvpRevisionReservation ReserveNextRevision(
        RsvpSubmission? current) =>
        new(
            (current?.RevisionNumber ?? 0) + 1,
            current?.Id);
}

public static class RsvpTransportAllocationPolicy
{
    public static bool CanAllocate(
        int? capacity,
        int confirmedCount,
        bool allowWaitlist) =>
        !capacity.HasValue
        || confirmedCount < capacity.Value
        || allowWaitlist;

    public static TransportSelectionStatus DetermineStatus(
        string optionName,
        int? capacity,
        int confirmedCount,
        bool allowWaitlist)
    {
        if (!capacity.HasValue || confirmedCount < capacity.Value)
        {
            return TransportSelectionStatus.Confirmed;
        }

        if (allowWaitlist)
        {
            return TransportSelectionStatus.Waitlisted;
        }

        throw new ConflictException(
            $"No quedan lugares en {optionName} y la lista de espera está deshabilitada.");
    }

    public static GuestTransportSelection? SelectNextWaitlisted(
        IEnumerable<GuestTransportSelection> selections) =>
        selections
            .Where(selection =>
                selection.Status == TransportSelectionStatus.Waitlisted)
            .OrderBy(selection =>
                selection.WaitlistSequence ?? long.MaxValue)
            .ThenBy(selection => selection.RequestedAt)
            .ThenBy(selection => selection.EventGuestId)
            .FirstOrDefault();
}
