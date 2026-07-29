namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class EventReminderLog
{
    private EventReminderLog() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid InvitationGroupId { get; private set; }
    public Guid ReminderTemplateId { get; private set; }
    public ReminderChannel Channel { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string? Note { get; private set; }

    public static EventReminderLog Create(
        Guid organizationId,
        Guid eventId,
        Guid invitationGroupId,
        Guid reminderTemplateId,
        ReminderChannel channel,
        Guid createdBy,
        string? note,
        DateTimeOffset now)
    {
        return new EventReminderLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            InvitationGroupId = invitationGroupId,
            ReminderTemplateId = reminderTemplateId,
            Channel = channel,
            CreatedAt = now,
            CreatedBy = createdBy,
            Note = note
        };
    }
}
