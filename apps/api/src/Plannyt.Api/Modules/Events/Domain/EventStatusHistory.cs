using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Events.Domain;

public sealed class EventStatusHistory : ITenantEntity
{
    private EventStatusHistory()
    {
    }

    private EventStatusHistory(
        Guid id,
        Guid organizationId,
        Guid eventId,
        EventStatus previousStatus,
        EventStatus newStatus,
        string? reason,
        Guid changedBy,
        DateTimeOffset changedAt)
    {
        Id = id;
        OrganizationId = organizationId;
        EventId = eventId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Reason = reason;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid EventId { get; private set; }

    public EventStatus PreviousStatus { get; private set; }

    public EventStatus NewStatus { get; private set; }

    public string? Reason { get; private set; }

    public Guid ChangedBy { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    public static EventStatusHistory Create(
        Guid organizationId,
        Guid eventId,
        EventStatus previousStatus,
        EventStatus newStatus,
        string? reason,
        Guid changedBy,
        DateTimeOffset changedAt) =>
        new(
            Guid.NewGuid(),
            organizationId,
            eventId,
            previousStatus,
            newStatus,
            reason,
            changedBy,
            changedAt);
}
