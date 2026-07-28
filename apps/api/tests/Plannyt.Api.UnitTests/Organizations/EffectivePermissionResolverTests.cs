using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.UnitTests.Organizations;

public sealed class EffectivePermissionResolverTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_CombinesRoleAndAllowGrants()
    {
        var grants = new[]
        {
            PermissionGrant.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                Permissions.ClientsArchive,
                PermissionEffect.Allow,
                PermissionScope.Organization,
                null,
                null,
                Now)
        };

        var result = EffectivePermissionResolver.Resolve(
            [Permissions.ClientsView],
            grants,
            Now);

        Assert.Contains(Permissions.ClientsView, result);
        Assert.Contains(Permissions.ClientsArchive, result);
    }

    [Fact]
    public void Resolve_WhenApplicableDenyExists_RemovesPermission()
    {
        var organizationId = Guid.NewGuid();
        var userAccountId = Guid.NewGuid();
        var grants = new[]
        {
            PermissionGrant.Create(
                organizationId,
                userAccountId,
                null,
                Permissions.ClientsView,
                PermissionEffect.Allow,
                PermissionScope.Organization,
                null,
                null,
                Now),
            PermissionGrant.Create(
                organizationId,
                userAccountId,
                null,
                Permissions.ClientsView,
                PermissionEffect.Deny,
                PermissionScope.Organization,
                null,
                null,
                Now)
        };

        var result = EffectivePermissionResolver.Resolve(
            [Permissions.ClientsView],
            grants,
            Now);

        Assert.DoesNotContain(Permissions.ClientsView, result);
    }

    [Fact]
    public void Resolve_IgnoresExpiredGrants()
    {
        var grants = new[]
        {
            PermissionGrant.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                Permissions.ClientsArchive,
                PermissionEffect.Allow,
                PermissionScope.Organization,
                null,
                Now.AddMinutes(-1),
                Now.AddDays(-1))
        };

        var result = EffectivePermissionResolver.Resolve(
            [Permissions.ClientsView],
            grants,
            Now);

        Assert.DoesNotContain(Permissions.ClientsArchive, result);
    }
}
