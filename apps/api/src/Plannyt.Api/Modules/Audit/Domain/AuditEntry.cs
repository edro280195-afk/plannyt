using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Audit.Domain;

public sealed class AuditEntry : ITenantEntity
{
    private AuditEntry()
    {
    }

    private AuditEntry(
        Guid id,
        Guid organizationId,
        Guid? eventId,
        Guid? actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string metadata,
        DateTimeOffset occurredAt,
        string correlationId,
        string? ipAddress)
    {
        Id = id;
        OrganizationId = organizationId;
        EventId = eventId;
        ActorUserId = actorUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Metadata = metadata;
        OccurredAt = occurredAt;
        CorrelationId = correlationId;
        IpAddress = ipAddress;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid? EventId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string Metadata { get; private set; } = "{}";

    public DateTimeOffset OccurredAt { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public string? IpAddress { get; private set; }

    public static AuditEntry Create(
        Guid organizationId,
        Guid? eventId,
        Guid? actorUserId,
        string action,
        string entityType,
        Guid entityId,
        string metadata,
        DateTimeOffset occurredAt,
        string correlationId,
        string? ipAddress) =>
        new(
            Guid.NewGuid(),
            organizationId,
            eventId,
            actorUserId,
            action,
            entityType,
            entityId,
            metadata,
            occurredAt,
            correlationId,
            ipAddress);
}
