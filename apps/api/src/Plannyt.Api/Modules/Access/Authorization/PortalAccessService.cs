using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Access.Authorization;

public sealed class PortalAccessService(
    PlannytDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<PortalEventAccess> RequireAsync(
        Guid eventId,
        string permission,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var access = await dbContext.EventAccesses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.EventId == eventId
                    && entity.UserAccountId == currentUser.UserAccountId
                    && entity.Status == EventAccessStatus.Active
                    && entity.StartsAt <= now
                    && (entity.ExpiresAt == null || entity.ExpiresAt > now)
                    && entity.RevokedAt == null,
                cancellationToken);
        if (access is null)
        {
            throw new ForbiddenException(
                "No tienes acceso activo al evento solicitado.");
        }

        var contextIsActive = await dbContext.Events
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.Id == access.EventId)
            .Join(
                dbContext.Organizations.AsNoTracking(),
                eventEntity => eventEntity.OrganizationId,
                organization => organization.Id,
                (eventEntity, organization) => organization.Status)
            .AnyAsync(
                status => status == OrganizationStatus.Active,
                cancellationToken);
        if (!contextIsActive)
        {
            throw new ForbiddenException(
                "El contexto del evento ya no está activo.");
        }

        var grants = await dbContext.PermissionGrants
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.UserAccountId == currentUser.UserAccountId
                && (entity.Scope == PermissionScope.Organization
                    || (entity.Scope == PermissionScope.Event
                        && entity.EventId == access.EventId)))
            .ToListAsync(cancellationToken);
        var permissions = EffectivePermissionResolver.Resolve(
            RolePermissionCatalog.GetFor(access.BaseRole),
            grants,
            now);
        if (!permissions.Contains(permission))
        {
            throw new ForbiddenException(
                "No tienes permiso para consultar este contenido.");
        }

        return new PortalEventAccess(
            access.OrganizationId,
            access.EventId,
            access.Id,
            access.BaseRole,
            permissions);
    }
}

public sealed record PortalEventAccess(
    Guid OrganizationId,
    Guid EventId,
    Guid EventAccessId,
    EventAccessRole Role,
    IReadOnlySet<string> Permissions);
