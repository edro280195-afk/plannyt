using Plannyt.Api.Modules.Identity.Application;

namespace Plannyt.Api.Modules.Identity.Security;

public sealed class RefreshCookieService(TimeProvider timeProvider)
{
    public const string CookieName = "plannyt_refresh";

    public void Set(HttpResponse response, AuthSessionResult result)
    {
        var options = CreateOptions();
        if (result.IsPersistent)
        {
            options.Expires = result.RefreshTokenExpiresAt;
            options.MaxAge =
                result.RefreshTokenExpiresAt - timeProvider.GetUtcNow();
        }

        response.Cookies.Append(CookieName, result.RefreshToken, options);
    }

    public void Delete(HttpResponse response) =>
        response.Cookies.Delete(CookieName, CreateOptions());

    private static CookieOptions CreateOptions() =>
        new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            IsEssential = true
        };
}
