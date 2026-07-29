using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class EventTransportOption : ITenantEntity
{
    private EventTransportOption() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TransportDirection Direction { get; private set; }
    public string? PickupPoint { get; private set; }
    public DateTimeOffset? DepartureAt { get; private set; }
    public DateTimeOffset? ReturnAt { get; private set; }
    public int? Capacity { get; private set; }
    public bool AllowWaitlist { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static EventTransportOption Create(
        Guid organizationId,
        Guid eventId,
        string name,
        string? description,
        TransportDirection direction,
        string? pickupPoint,
        DateTimeOffset? departureAt,
        DateTimeOffset? returnAt,
        int? capacity,
        bool allowWaitlist,
        int sortOrder,
        DateTimeOffset now)
    {
        if (capacity.HasValue && capacity.Value < 0)
        {
            throw new DomainRuleException("La capacidad no puede ser negativa.");
        }

        return new EventTransportOption
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            Name = name,
            Description = description,
            Direction = direction,
            PickupPoint = pickupPoint,
            DepartureAt = departureAt,
            ReturnAt = returnAt,
            Capacity = capacity,
            AllowWaitlist = allowWaitlist,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
