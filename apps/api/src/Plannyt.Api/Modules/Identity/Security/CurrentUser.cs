using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Plannyt.Api.Modules.Identity.Security;

public interface ICurrentUser
{
    Guid UserAccountId { get; }

    Guid SessionId { get; }
}

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid UserAccountId => GetGuidClaim(JwtRegisteredClaimNames.Sub);

    public Guid SessionId => GetGuidClaim("sid");

    private Guid GetGuidClaim(string claimType)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        return Guid.TryParse(value, out var result)
            ? result
            : throw new InvalidOperationException(
                "La solicitud no contiene una identidad autenticada válida.");
    }
}
