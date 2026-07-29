using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class GuestTransportSelectionHistory : ITenantEntity
{
    private GuestTransportSelectionHistory() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid EventGuestId { get; private set; }
    public Guid EventTransportOptionId { get; private set; }
    public TransportSelectionStatus? PreviousStatus { get; private set; }
    public TransportSelectionStatus NewStatus { get; private set; }
    public Guid SubmissionId { get; private set; }
    public long? WaitlistSequence { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    public static GuestTransportSelectionHistory Create(
        Guid organizationId,
        Guid eventId,
        Guid eventGuestId,
        Guid eventTransportOptionId,
        TransportSelectionStatus? previousStatus,
        TransportSelectionStatus newStatus,
        Guid submissionId,
        long? waitlistSequence,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            EventGuestId = eventGuestId,
            EventTransportOptionId = eventTransportOptionId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            SubmissionId = submissionId,
            WaitlistSequence = waitlistSequence,
            OccurredAt = now
        };
}
