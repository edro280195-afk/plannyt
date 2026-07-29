using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class EventMenu : ITenantEntity
{
    private EventMenu() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MenuCategory MenuCategory { get; private set; }
    public bool IsActive { get; private set; }
    public bool SelectionRequired { get; private set; }
    public int MinimumSelections { get; private set; }
    public int MaximumSelections { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public static EventMenu Create(
        Guid organizationId,
        Guid eventId,
        string name,
        string? description,
        MenuCategory menuCategory,
        bool selectionRequired,
        int minimumSelections,
        int maximumSelections,
        int sortOrder,
        DateTimeOffset now)
    {
        if (minimumSelections < 0 || maximumSelections < minimumSelections)
        {
            throw new DomainRuleException("Mínimo no puede ser negativo ni mayor que máximo.");
        }

        return new EventMenu
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            Name = name,
            Description = description,
            MenuCategory = menuCategory,
            IsActive = true,
            SelectionRequired = selectionRequired,
            MinimumSelections = minimumSelections,
            MaximumSelections = maximumSelections,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Archive(DateTimeOffset now)
    {
        IsActive = false;
        ArchivedAt = now;
        UpdatedAt = now;
    }
}
