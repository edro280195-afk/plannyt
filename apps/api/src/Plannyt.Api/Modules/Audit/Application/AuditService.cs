using System.Text.Json;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Domain;

namespace Plannyt.Api.Modules.Audit.Application;

public sealed class AuditService(
    PlannytDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider)
{
    public void Add(
        Guid? organizationId,
        Guid? eventId,
        Guid? actorUserId,
        AuditAction action,
        string entityType,
        Guid entityId,
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        Add(
            organizationId,
            eventId,
            actorUserId,
            action.Value,
            entityType,
            entityId,
            metadata);

    public void Add(
        Guid? organizationId,
        Guid? eventId,
        Guid? actorUserId,
        string action,
        string entityType,
        Guid entityId,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var safeMetadata = metadata is null
            ? "{}"
            : JsonSerializer.Serialize(metadata);

        dbContext.AuditEntries.Add(AuditEntry.Create(
            organizationId,
            eventId,
            actorUserId,
            action,
            entityType,
            entityId,
            safeMetadata,
            timeProvider.GetUtcNow(),
            httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N"),
            httpContext?.Connection.RemoteIpAddress?.ToString()));
    }
}
