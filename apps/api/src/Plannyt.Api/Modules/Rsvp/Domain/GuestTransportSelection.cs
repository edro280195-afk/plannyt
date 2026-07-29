using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class GuestTransportSelection : ITenantEntity
{
    private GuestTransportSelection() { }

    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid EventGuestId { get; private set; }
    public Guid EventTransportOptionId { get; private set; }
    public TransportSelectionStatus Status { get; private set; }
    public Guid? LastSubmissionId { get; private set; }
    public long? WaitlistSequence { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static GuestTransportSelection Create(
        Guid organizationId,
        Guid eventId,
        Guid eventGuestId,
        Guid eventTransportOptionId,
        TransportSelectionStatus status,
        Guid lastSubmissionId,
        long? waitlistSequence,
        DateTimeOffset now)
    {
        return new GuestTransportSelection
        {
            OrganizationId = organizationId,
            EventId = eventId,
            EventGuestId = eventGuestId,
            EventTransportOptionId = eventTransportOptionId,
            Status = status,
            LastSubmissionId = lastSubmissionId,
            WaitlistSequence = waitlistSequence,
            RequestedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateStatus(
        TransportSelectionStatus status,
        Guid lastSubmissionId,
        long? waitlistSequence,
        DateTimeOffset now)
    {
        Status = status;
        LastSubmissionId = lastSubmissionId;
        WaitlistSequence = waitlistSequence;
        UpdatedAt = now;
    }
}
