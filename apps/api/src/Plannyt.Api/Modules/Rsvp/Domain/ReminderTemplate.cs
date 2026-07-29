using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class ReminderTemplate : ITenantEntity
{
    private ReminderTemplate() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ReminderChannel Channel { get; private set; }
    public string SegmentType { get; private set; } = string.Empty;
    public string MessageTemplate { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ReminderTemplate Create(
        Guid organizationId,
        Guid? eventId,
        string name,
        ReminderChannel channel,
        string segmentType,
        string messageTemplate,
        DateTimeOffset now)
    {
        return new ReminderTemplate
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            Name = name,
            Channel = channel,
            SegmentType = segmentType,
            MessageTemplate = messageTemplate,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
