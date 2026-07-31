using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Invitations.Security;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Invitations.Application;

public sealed class GuestLinkService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    GuestAccessTokenService tokenService,
    FrontendPublicUrlResolver frontendPublicUrlResolver,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<GuestAccessLinkResponse>> GetLinksAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestLinksView,
            cancellationToken);
        var links = await dbContext.GuestAccessLinks.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId)
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);
        return links
            .Select(link => ToResponse(link, BuildPublicUrl(link)))
            .ToList();
    }

    public async Task<GuestAccessLinkResponse> GenerateAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        GenerateGuestLinkRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestLinksGenerate,
            cancellationToken);
        return await CreateLinkAsync(
            organizationId,
            eventId,
            groupId,
            request,
            access,
            null,
            cancellationToken);
    }

    public async Task<GuestAccessLinkResponse> RegenerateAsync(
        Guid organizationId,
        Guid eventId,
        Guid linkId,
        GenerateGuestLinkRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestLinksRegenerate,
            cancellationToken);
        var oldLink = await FindAsync(
            organizationId,
            eventId,
            linkId,
            cancellationToken);
        return await CreateLinkAsync(
            organizationId,
            eventId,
            oldLink.InvitationGroupId,
            request,
            access,
            oldLink,
            cancellationToken);
    }

    public async Task RevokeAsync(
        Guid organizationId,
        Guid eventId,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestLinksRevoke,
            cancellationToken);
        var link = await FindAsync(
            organizationId,
            eventId,
            linkId,
            cancellationToken);
        if (link.Status != GuestAccessLinkStatus.Active)
        {
            throw new ConflictException("El enlace ya no está activo.");
        }

        link.Revoke(timeProvider.GetUtcNow());
        var group = await dbContext.InvitationGroups.SingleAsync(entity =>
            entity.OrganizationId == organizationId
            && entity.EventId == eventId
            && entity.Id == link.InvitationGroupId,
            cancellationToken);
        group.ChangeStatus(InvitationGroupStatus.Revoked, timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.GuestLinkRevoked,
            nameof(GuestAccessLink),
            link.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GuestAccessLinkResponse> MarkSharedAsync(
        Guid organizationId,
        Guid eventId,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestLinksMarkShared,
            cancellationToken);
        var link = await FindAsync(
            organizationId,
            eventId,
            linkId,
            cancellationToken);
        if (link.Status != GuestAccessLinkStatus.Active)
        {
            throw new ConflictException("El enlace ya no está activo.");
        }

        var now = timeProvider.GetUtcNow();
        link.MarkShared(now);
        var group = await dbContext.InvitationGroups.SingleAsync(entity =>
            entity.OrganizationId == organizationId
            && entity.EventId == eventId
            && entity.Id == link.InvitationGroupId,
            cancellationToken);
        group.ChangeStatus(InvitationGroupStatus.SharedManually, now);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.GuestLinkMarkedShared,
            nameof(GuestAccessLink),
            link.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(link, null);
    }

    private async Task<GuestAccessLinkResponse> CreateLinkAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        GenerateGuestLinkRequest request,
        TenantAccess access,
        GuestAccessLink? linkToReplace,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (request.ExpiresAt <= now
            || request.ExpiresAt > now.AddDays(365))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["expiresAt"] =
                    [
                        "La vigencia debe ser futura y no mayor a 365 días."
                    ]
                });
        }

        var group = await dbContext.InvitationGroups.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == groupId
                && entity.ArchivedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el grupo de invitación.");
        var experiencePublished = await dbContext.EventGuestExperiences.AsNoTracking()
            .AnyAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Status == GuestExperienceStatus.Published
                && entity.ActiveVersionId != null,
                cancellationToken);
        if (!experiencePublished)
        {
            throw new ConflictException(
                "Publica la experiencia digital antes de generar enlaces.");
        }

        var activeLinks = await dbContext.GuestAccessLinks.Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId
                && entity.Status == GuestAccessLinkStatus.Active)
            .ToListAsync(cancellationToken);
        if (linkToReplace is null && activeLinks.Count > 0)
        {
            throw new ConflictException("El grupo ya tiene un enlace activo.");
        }

        var linkId = Guid.NewGuid();
        var token = tokenService.Create(linkId);
        var link = GuestAccessLink.Create(
            linkId,
            organizationId,
            eventId,
            groupId,
            token.Hash,
            token.DerivationKeyId,
            request.ExpiresAt,
            access.UserAccountId,
            now);
        dbContext.GuestAccessLinks.Add(link);
        foreach (var active in activeLinks)
        {
            active.ReplaceWith(link.Id, now);
        }

        group.ChangeStatus(InvitationGroupStatus.LinkGenerated, now);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            linkToReplace is null
                ? AuditActions.GuestLinkGenerated
                : AuditActions.GuestLinkRegenerated,
            nameof(GuestAccessLink),
            link.Id,
            linkToReplace is null
                ? null
                : new Dictionary<string, object?>
                {
                    ["replacedLinkId"] = linkToReplace.Id
                });
        await dbContext.SaveChangesAsync(cancellationToken);
        var publicUrl = BuildPublicUrl(link);
        return ToResponse(link, publicUrl);
    }

    private async Task<GuestAccessLink> FindAsync(
        Guid organizationId,
        Guid eventId,
        Guid linkId,
        CancellationToken cancellationToken) =>
        await dbContext.GuestAccessLinks.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == linkId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el enlace.");

    private async Task<TenantAccess> RequireEventAsync(
        Guid organizationId,
        Guid eventId,
        string permission,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            permission,
            eventId,
            cancellationToken);
        if (!await dbContext.Events.AsNoTracking().AnyAsync(entity =>
                entity.OrganizationId == organizationId && entity.Id == eventId,
                cancellationToken))
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        return access;
    }

    private static GuestAccessLinkResponse ToResponse(
        GuestAccessLink link,
        string? publicUrl) =>
        new(
            link.Id,
            link.InvitationGroupId,
            link.Status,
            publicUrl,
            link.ExpiresAt,
            link.FirstOpenedAt,
            link.LastOpenedAt,
            link.OpenCount,
            link.SharedManuallyAt,
            link.CreatedAt);

    private string? BuildPublicUrl(GuestAccessLink link)
    {
        if (link.Status != GuestAccessLinkStatus.Active
            || link.IsExpired(timeProvider.GetUtcNow()))
        {
            return null;
        }

        var token = tokenService.Reveal(link.Id, link.DerivationKeyId);
        return $"{frontendPublicUrlResolver.Resolve()}/i/"
            + Uri.EscapeDataString(token);
    }
}
