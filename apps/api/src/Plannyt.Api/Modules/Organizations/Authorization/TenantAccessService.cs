using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Organizations.Authorization;

public sealed class TenantAccessService(
    PlannytDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<TenantAccess> RequireAsync(
        Guid organizationId,
        string permission,
        Guid? eventId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var access = await ResolveAsync(
            organizationId,
            eventId,
            now,
            cancellationToken);

        if (!access.Permissions.Contains(permission))
        {
            throw new ForbiddenException(
                "No tienes el permiso requerido dentro de esta organización.");
        }

        return access;
    }

    public async Task<TenantAccess> ResolveAsync(
        Guid organizationId,
        Guid? eventId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var membership = await dbContext.OrganizationMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.UserAccountId == currentUser.UserAccountId
                    && entity.Status == MembershipStatus.Active
                    && entity.JoinedAt <= now
                    && (entity.ExpiresAt == null || entity.ExpiresAt > now),
                cancellationToken);
        var organizationIsActive = await dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(
                entity =>
                    entity.Id == organizationId
                    && entity.Status == OrganizationStatus.Active,
                cancellationToken);

        if (membership is null || !organizationIsActive)
        {
            throw new ForbiddenException(
                "No tienes acceso activo a la organización solicitada.");
        }

        var grants = await dbContext.PermissionGrants
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && (entity.UserAccountId == currentUser.UserAccountId
                    || entity.OrganizationMembershipId == membership.Id)
                && (entity.Scope == PermissionScope.Organization
                    || (eventId != null
                        && entity.Scope == PermissionScope.Event
                        && entity.EventId == eventId)))
            .ToListAsync(cancellationToken);
        var permissions = EffectivePermissionResolver.Resolve(
            RolePermissionCatalog.GetFor(membership.BaseRole),
            grants,
            now);

        return new TenantAccess(
            organizationId,
            currentUser.UserAccountId,
            membership.Id,
            membership.PersonId,
            membership.BaseRole,
            permissions);
    }
}

public sealed record TenantAccess(
    Guid OrganizationId,
    Guid UserAccountId,
    Guid MembershipId,
    Guid PersonId,
    OrganizationRole Role,
    IReadOnlySet<string> Permissions);
