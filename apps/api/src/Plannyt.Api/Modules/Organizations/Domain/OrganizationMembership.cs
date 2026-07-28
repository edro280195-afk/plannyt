using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Organizations.Domain;

public sealed class OrganizationMembership : ITenantEntity
{
    private OrganizationMembership()
    {
    }

    private OrganizationMembership(
        Guid id,
        Guid organizationId,
        Guid userAccountId,
        Guid personId,
        OrganizationRole baseRole,
        MembershipStatus status,
        DateTimeOffset joinedAt,
        DateTimeOffset? expiresAt,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        UserAccountId = userAccountId;
        PersonId = personId;
        BaseRole = baseRole;
        Status = status;
        JoinedAt = joinedAt;
        ExpiresAt = expiresAt;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid UserAccountId { get; private set; }

    public Guid PersonId { get; private set; }

    public OrganizationRole BaseRole { get; private set; }

    public MembershipStatus Status { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static OrganizationMembership CreateOwner(
        Guid organizationId,
        Guid userAccountId,
        Guid personId,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            organizationId,
            userAccountId,
            personId,
            OrganizationRole.Owner,
            MembershipStatus.Active,
            now,
            null,
            now);

    public static OrganizationMembership Create(
        Guid organizationId,
        Guid userAccountId,
        Guid personId,
        OrganizationRole role,
        DateTimeOffset joinedAt,
        DateTimeOffset? expiresAt,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            organizationId,
            userAccountId,
            personId,
            role,
            MembershipStatus.Active,
            joinedAt,
            expiresAt,
            now);

    public bool IsActiveAt(DateTimeOffset now) =>
        Status == MembershipStatus.Active
        && JoinedAt <= now
        && (ExpiresAt is null || ExpiresAt > now);

    public void Revoke(DateTimeOffset now)
    {
        Status = MembershipStatus.Revoked;
        UpdatedAt = now;
    }
}
