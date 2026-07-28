namespace Plannyt.Api.BuildingBlocks.Http;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] =
                "camera=(), geolocation=(), microphone=(), payment=(), usb=()";
            headers.ContentSecurityPolicy =
                "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
            if (context.Request.Path.StartsWithSegments(
                    "/api/public/invitations"))
            {
                headers.CacheControl = "no-store, private";
                headers.Pragma = "no-cache";
                headers["Referrer-Policy"] = "no-referrer";
                headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
            }
            return Task.CompletedTask;
        });

        await next(context);
    }
}
