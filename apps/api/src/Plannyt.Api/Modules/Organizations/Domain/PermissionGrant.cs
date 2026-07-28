using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Organizations.Domain;

public sealed class PermissionGrant : ITenantEntity
{
    private PermissionGrant()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid? UserAccountId { get; private set; }

    public Guid? OrganizationMembershipId { get; private set; }

    public string Permission { get; private set; } = string.Empty;

    public PermissionEffect Effect { get; private set; }

    public PermissionScope Scope { get; private set; }

    public Guid? EventId { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static PermissionGrant Create(
        Guid organizationId,
        Guid? userAccountId,
        Guid? organizationMembershipId,
        string permission,
        PermissionEffect effect,
        PermissionScope scope,
        Guid? eventId,
        DateTimeOffset? expiresAt,
        DateTimeOffset now)
    {
        if ((userAccountId is null) == (organizationMembershipId is null))
        {
            throw new DomainRuleException(
                "El permiso debe dirigirse exactamente a una cuenta o membresía.");
        }

        if (scope == PermissionScope.Event && eventId is null)
        {
            throw new DomainRuleException(
                "Un permiso con alcance de evento requiere EventId.");
        }

        if (scope == PermissionScope.Organization && eventId is not null)
        {
            throw new DomainRuleException(
                "Un permiso de organización no puede incluir EventId.");
        }

        return new PermissionGrant
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserAccountId = userAccountId,
            OrganizationMembershipId = organizationMembershipId,
            Permission = permission,
            Effect = effect,
            Scope = scope,
            EventId = eventId,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
