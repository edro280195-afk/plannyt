using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class GuestAccommodationSelection : ITenantEntity
{
    private GuestAccommodationSelection() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid? EventGuestId { get; private set; }
    public Guid InvitationGroupId { get; private set; }
    public Guid? EventAccommodationOptionId { get; private set; }
    public AccommodationSelectionStatus Status { get; private set; }
    public string? ReservationName { get; private set; }
    public string? ConfirmationReference { get; private set; }
    public Guid? LastSubmissionId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static GuestAccommodationSelection Create(
        Guid organizationId,
        Guid eventId,
        Guid? eventGuestId,
        Guid invitationGroupId,
        Guid? eventAccommodationOptionId,
        AccommodationSelectionStatus status,
        string? reservationName,
        string? confirmationReference,
        Guid? lastSubmissionId,
        DateTimeOffset now)
    {
        return new GuestAccommodationSelection
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            EventGuestId = eventGuestId,
            InvitationGroupId = invitationGroupId,
            EventAccommodationOptionId = eventAccommodationOptionId,
            Status = status,
            ReservationName = reservationName,
            ConfirmationReference = confirmationReference,
            LastSubmissionId = lastSubmissionId,
            UpdatedAt = now
        };
    }

    public void Update(
        Guid? eventAccommodationOptionId,
        AccommodationSelectionStatus status,
        string? reservationName,
        string? confirmationReference,
        Guid lastSubmissionId,
        DateTimeOffset now)
    {
        EventAccommodationOptionId = eventAccommodationOptionId;
        Status = status;
        ReservationName = reservationName;
        ConfirmationReference = confirmationReference;
        LastSubmissionId = lastSubmissionId;
        UpdatedAt = now;
    }
}
