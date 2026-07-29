namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class RsvpSubmissionGuest
{
    private RsvpSubmissionGuest() { }

    public Guid Id { get; private set; }
    public Guid RsvpSubmissionId { get; private set; }
    public Guid? EventGuestId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string AgeCategory { get; private set; } = string.Empty;
    public GuestAttendanceStatus AttendanceStatus { get; private set; }
    public string MenuSelectionsSnapshot { get; private set; } = string.Empty;
    public string TransportSelectionSnapshot { get; private set; } = string.Empty;
    public string AccommodationSelectionSnapshot { get; private set; } = string.Empty;
    public string DietarySnapshot { get; private set; } = string.Empty;
    public bool IsUnnamedCompanion { get; private set; }
    public int? CompanionSlotNumber { get; private set; }

    public static RsvpSubmissionGuest Create(
        Guid rsvpSubmissionId,
        Guid? eventGuestId,
        string displayName,
        string ageCategory,
        GuestAttendanceStatus attendanceStatus,
        string menuSelectionsSnapshot,
        string transportSelectionSnapshot,
        string accommodationSelectionSnapshot,
        string dietarySnapshot,
        bool isUnnamedCompanion,
        int? companionSlotNumber = null)
    {
        return new RsvpSubmissionGuest
        {
            Id = Guid.NewGuid(),
            RsvpSubmissionId = rsvpSubmissionId,
            EventGuestId = eventGuestId,
            DisplayName = displayName,
            AgeCategory = ageCategory,
            AttendanceStatus = attendanceStatus,
            MenuSelectionsSnapshot = menuSelectionsSnapshot,
            TransportSelectionSnapshot = transportSelectionSnapshot,
            AccommodationSelectionSnapshot = accommodationSelectionSnapshot,
            DietarySnapshot = dietarySnapshot,
            IsUnnamedCompanion = isUnnamedCompanion,
            CompanionSlotNumber = companionSlotNumber
        };
    }
}
