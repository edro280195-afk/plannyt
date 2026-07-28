using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Access.Application;

public sealed class EventAccessService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<EventAccessResponse>> GetAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsMembersView,
            eventId,
            cancellationToken);
        var eventExists = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId,
                cancellationToken);
        if (!eventExists)
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        return await dbContext.EventAccesses
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId)
            .Join(
                dbContext.UserAccounts.AsNoTracking(),
                access => access.UserAccountId,
                account => account.Id,
                (access, account) => new
                {
                    Access = access,
                    Account = account
                })
            .OrderBy(entity => entity.Account.Email)
            .Select(entity => new EventAccessResponse(
                entity.Access.Id,
                entity.Account.Id,
                entity.Account.Email,
                entity.Access.BaseRole,
                entity.Access.Status,
                entity.Access.StartsAt,
                entity.Access.ExpiresAt,
                entity.Access.RevokedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAsync(
        Guid organizationId,
        Guid eventId,
        Guid accessId,
        CancellationToken cancellationToken)
    {
        var actor = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsMembersRevoke,
            eventId,
            cancellationToken);
        var eventAccess = await dbContext.EventAccesses.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == accessId,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró el acceso.");
        if (eventAccess.Status == EventAccessStatus.Revoked)
        {
            throw new ConflictException("El acceso ya está revocado.");
        }

        eventAccess.Revoke(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            actor.UserAccountId,
            "event.access_revoked",
            nameof(EventAccess),
            eventAccess.Id,
            new Dictionary<string, object?>
            {
                ["targetUserAccountId"] = eventAccess.UserAccountId,
                ["role"] = eventAccess.BaseRole.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
