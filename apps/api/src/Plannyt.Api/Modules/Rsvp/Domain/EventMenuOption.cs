using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class EventMenuOption : ITenantEntity
{
    private EventMenuOption() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventMenuId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string DietaryTags { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public int? Capacity { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static EventMenuOption Create(
        Guid organizationId,
        Guid eventMenuId,
        string name,
        string? description,
        string dietaryTags,
        int? capacity,
        int sortOrder,
        DateTimeOffset now)
    {
        if (capacity.HasValue && capacity.Value < 0)
        {
            throw new DomainRuleException("La capacidad no puede ser negativa.");
        }

        return new EventMenuOption
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventMenuId = eventMenuId,
            Name = name,
            Description = description,
            DietaryTags = dietaryTags,
            IsActive = true,
            Capacity = capacity,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
