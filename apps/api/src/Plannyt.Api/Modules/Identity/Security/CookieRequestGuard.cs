using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.BuildingBlocks.Http;

namespace Plannyt.Api.Modules.Identity.Security;

public sealed class CookieRequestGuard(IOptions<CorsOptions> corsOptions, IHostEnvironment environment)
{
    public void Validate(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        var clientHeader = context.Request.Headers["X-Plannyt-Client"].FirstOrDefault();

        var originIsAuthorized =
            string.Equals(origin, corsOptions.Value.AllowedOrigin, StringComparison.OrdinalIgnoreCase)
            || (environment.IsDevelopment() && LoopbackOrigin.IsLoopback(origin));

        if (!originIsAuthorized || !string.Equals(clientHeader, "web", StringComparison.Ordinal))
        {
            throw new ForbiddenException(
                "La solicitud basada en cookie no proviene del frontend autorizado.");
        }
    }
}
