using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Events.Domain;

public sealed class EventClient : ITenantEntity
{
    private EventClient()
    {
    }

    private EventClient(
        Guid id,
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        EventClientRelationshipType relationshipType,
        bool isPrimary,
        bool hasTransferAuthority,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        EventId = eventId;
        ClientId = clientId;
        RelationshipType = relationshipType;
        IsPrimary = isPrimary;
        HasTransferAuthority = hasTransferAuthority;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid EventId { get; private set; }

    public Guid ClientId { get; private set; }

    public EventClientRelationshipType RelationshipType { get; private set; }

    public bool IsPrimary { get; private set; }

    public bool HasTransferAuthority { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static EventClient Create(
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        EventClientRelationshipType relationshipType,
        bool isPrimary,
        bool hasTransferAuthority,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            organizationId,
            eventId,
            clientId,
            relationshipType,
            isPrimary,
            hasTransferAuthority,
            now);
}
