using Plannyt.Api.BuildingBlocks.Http;
using Plannyt.Api.Modules.Identity.Security;

namespace Plannyt.Api.Modules.Identity.Application;

public static class AuthEndpoints
{
    private const string RefreshCookieName = "plannyt_refresh";

    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/auth")
            .WithTags("Identidad");

        group.MapPost(
                "/register-planner",
                async (
                    RegisterPlannerRequest request,
                    HttpContext context,
                    AuthService authService,
                    CancellationToken cancellationToken) =>
                {
                    var result = await authService.RegisterPlannerAsync(
                        request,
                        GetIpAddress(context),
                        GetUserAgent(context),
                        cancellationToken);
                    SetRefreshCookie(context.Response, result);
                    return Results.Created("/api/auth/me", result.Response);
                })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);

        group.MapPost(
                "/login",
                async (
                    LoginRequest request,
                    HttpContext context,
                    AuthService authService,
                    CancellationToken cancellationToken) =>
                {
                    var result = await authService.LoginAsync(
                        request,
                        GetIpAddress(context),
                        GetUserAgent(context),
                        cancellationToken);
                    SetRefreshCookie(context.Response, result);
                    return Results.Ok(result.Response);
                })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);

        group.MapPost(
                "/refresh",
                async (
                    HttpContext context,
                    AuthService authService,
                    CookieRequestGuard requestGuard,
                    CancellationToken cancellationToken) =>
                {
                    requestGuard.Validate(context);
                    var refreshToken = context.Request.Cookies[RefreshCookieName];
                    var result = await authService.RefreshAsync(
                        refreshToken ?? string.Empty,
                        GetIpAddress(context),
                        GetUserAgent(context),
                        cancellationToken);
                    SetRefreshCookie(context.Response, result);
                    return Results.Ok(result.Response);
                })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);

        group.MapPost(
                "/logout",
                async (
                    HttpContext context,
                    AuthService authService,
                    CookieRequestGuard requestGuard,
                    CancellationToken cancellationToken) =>
                {
                    requestGuard.Validate(context);
                    await authService.LogoutAsync(
                        context.Request.Cookies[RefreshCookieName],
                        cancellationToken);
                    DeleteRefreshCookie(context.Response);
                    return Results.NoContent();
                })
            .AllowAnonymous();

        group.MapPost(
                "/logout-all",
                async (
                    HttpContext context,
                    ICurrentUser currentUser,
                    AuthService authService,
                    CookieRequestGuard requestGuard,
                    CancellationToken cancellationToken) =>
                {
                    requestGuard.Validate(context);
                    await authService.LogoutAllAsync(
                        currentUser.UserAccountId,
                        cancellationToken);
                    DeleteRefreshCookie(context.Response);
                    return Results.NoContent();
                })
            .RequireAuthorization();

        group.MapGet(
                "/me",
                async (
                    ICurrentUser currentUser,
                    AuthService authService,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await authService.GetMeAsync(
                        currentUser.UserAccountId,
                        cancellationToken)))
            .RequireAuthorization();

        return endpoints;
    }

    private static void SetRefreshCookie(
        HttpResponse response,
        AuthSessionResult result)
    {
        var options = CreateCookieOptions();
        if (result.IsPersistent)
        {
            options.Expires = result.RefreshTokenExpiresAt;
            options.MaxAge = result.RefreshTokenExpiresAt - DateTimeOffset.UtcNow;
        }

        response.Cookies.Append(
            RefreshCookieName,
            result.RefreshToken,
            options);
    }

    private static void DeleteRefreshCookie(HttpResponse response) =>
        response.Cookies.Delete(
            RefreshCookieName,
            CreateCookieOptions());

    private static CookieOptions CreateCookieOptions() =>
        new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            IsEssential = true
        };

    private static string? GetIpAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString();

    private static string? GetUserAgent(HttpContext context) =>
        context.Request.Headers.UserAgent.FirstOrDefault();
}
