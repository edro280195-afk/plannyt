using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;

namespace Plannyt.Api.Modules.Identity.Security;

public sealed class CookieRequestGuard(IOptions<CorsOptions> corsOptions)
{
    public void Validate(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        var clientHeader = context.Request.Headers["X-Plannyt-Client"].FirstOrDefault();

        if (!string.Equals(
                origin,
                corsOptions.Value.AllowedOrigin,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(clientHeader, "web", StringComparison.Ordinal))
        {
            throw new ForbiddenException(
                "La solicitud basada en cookie no proviene del frontend autorizado.");
        }
    }
}
