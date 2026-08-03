using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Organizations.Application;

public sealed class OrganizationService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<OrganizationResponse> GetAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.OrganizationView,
            null,
            cancellationToken);
        return await dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.Id == organizationId)
            .Select(entity => ToResponse(entity))
            .SingleAsync(cancellationToken);
    }

    public async Task<OrganizationResponse> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.OrganizationUpdate,
            null,
            cancellationToken);
        OrganizationRequestValidator.Validate(request);
        var organization = await dbContext.Organizations.SingleAsync(
            entity => entity.Id == organizationId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        organization.Update(
            request.Name.Trim(),
            request.OrganizationType,
            request.TimeZone.Trim(),
            request.CountryCode.Trim().ToUpperInvariant(),
            request.CurrencyCode.Trim().ToUpperInvariant(),
            now);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "organization.updated",
            nameof(Organization),
            organizationId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(organization);
    }

    public async Task<IReadOnlyList<OrganizationMemberResponse>> GetMembersAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.OrganizationMembersView,
            null,
            cancellationToken);

        return await dbContext.OrganizationMemberships
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId)
            .Join(
                dbContext.People.AsNoTracking(),
                membership => new
                {
                    membership.OrganizationId,
                    Id = membership.PersonId
                },
                person => new { person.OrganizationId, person.Id },
                (membership, person) => new
                {
                    Membership = membership,
                    Person = person
                })
            .Join(
                dbContext.UserAccounts.AsNoTracking(),
                item => item.Membership.UserAccountId,
                account => account.Id,
                (item, account) => new
                {
                    item.Membership,
                    item.Person,
                    Account = account
                })
            .OrderBy(item => item.Person.DisplayName)
            .Select(item => new OrganizationMemberResponse(
                item.Membership.Id,
                item.Account.Id,
                item.Person.Id,
                item.Person.DisplayName,
                item.Account.Email,
                item.Membership.BaseRole,
                item.Membership.Status,
                item.Membership.JoinedAt,
                item.Membership.ExpiresAt))
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeMemberAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.OrganizationMembersRevoke,
            null,
            cancellationToken);
        var membership = await dbContext.OrganizationMemberships
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == membershipId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la membresía.");

        if (membership.Status != MembershipStatus.Active)
        {
            throw new ConflictException("La membresía ya no está activa.");
        }

        if (membership.BaseRole == OrganizationRole.Owner)
        {
            var ownerCheckAt = timeProvider.GetUtcNow();
            var activeOwnerCount = await dbContext.OrganizationMemberships.CountAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.BaseRole == OrganizationRole.Owner
                    && entity.Status == MembershipStatus.Active
                    && entity.JoinedAt <= ownerCheckAt
                    && (entity.ExpiresAt == null
                        || entity.ExpiresAt > ownerCheckAt),
                cancellationToken);
            if (activeOwnerCount <= 1)
            {
                throw new ConflictException(
                    "No se puede revocar al único Owner activo.");
            }
        }

        var now = timeProvider.GetUtcNow();
        membership.Revoke(now);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "organization.member_revoked",
            nameof(OrganizationMembership),
            membership.Id,
            new Dictionary<string, object?>
            {
                ["targetUserAccountId"] = membership.UserAccountId,
                ["role"] = membership.BaseRole.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static OrganizationResponse ToResponse(Organization organization) =>
        new(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.OrganizationType,
            organization.TimeZone,
            organization.CountryCode,
            organization.CurrencyCode,
            organization.Status,
            organization.CreatedAt,
            organization.UpdatedAt);
}
