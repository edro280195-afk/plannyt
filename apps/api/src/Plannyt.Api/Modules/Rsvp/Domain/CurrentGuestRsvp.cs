using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class CurrentGuestRsvp : ITenantEntity
{
    private CurrentGuestRsvp() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid InvitationGroupId { get; private set; }
    public Guid? EventGuestId { get; private set; }
    public GuestAttendanceStatus AttendanceStatus { get; private set; }
    public bool IsUnnamedCompanion { get; private set; }
    public int? CompanionSlotNumber { get; private set; }
    public string? CurrentDisplayName { get; private set; }
    public Guid? LastSubmissionId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    public static CurrentGuestRsvp Create(
        Guid organizationId,
        Guid eventId,
        Guid invitationGroupId,
        Guid? eventGuestId,
        GuestAttendanceStatus attendanceStatus,
        bool isUnnamedCompanion,
        int? companionSlotNumber,
        string? currentDisplayName,
        Guid? lastSubmissionId,
        DateTimeOffset now)
    {
        return new CurrentGuestRsvp
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            InvitationGroupId = invitationGroupId,
            EventGuestId = eventGuestId,
            AttendanceStatus = attendanceStatus,
            IsUnnamedCompanion = isUnnamedCompanion,
            CompanionSlotNumber = companionSlotNumber,
            CurrentDisplayName = currentDisplayName,
            LastSubmissionId = lastSubmissionId,
            UpdatedAt = now
        };
    }

    public void UpdateStatus(
        GuestAttendanceStatus attendanceStatus,
        string? currentDisplayName,
        Guid? lastSubmissionId,
        DateTimeOffset now)
    {
        AttendanceStatus = attendanceStatus;
        CurrentDisplayName = currentDisplayName;
        LastSubmissionId = lastSubmissionId;
        UpdatedAt = now;
    }

    public void SetUpdatedBy(Guid? userAccountId)
    {
        UpdatedByUserId = userAccountId;
    }
}
