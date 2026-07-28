using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Organizations.Authorization;

public static class EffectivePermissionResolver
{
    public static IReadOnlySet<string> Resolve(
        IEnumerable<string> basePermissions,
        IEnumerable<PermissionGrant> grants,
        DateTimeOffset now)
    {
        var effective = new HashSet<string>(basePermissions, StringComparer.Ordinal);
        var applicable = grants
            .Where(grant => grant.ExpiresAt is null || grant.ExpiresAt > now)
            .ToArray();

        foreach (var grant in applicable.Where(
                     grant => grant.Effect == PermissionEffect.Allow))
        {
            effective.Add(grant.Permission);
        }

        foreach (var grant in applicable.Where(
                     grant => grant.Effect == PermissionEffect.Deny))
        {
            effective.Remove(grant.Permission);
        }

        return effective;
    }
}
