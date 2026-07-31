using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Http;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class FrontendPublicUrlResolver(
    IOptions<FrontendOptions> frontendOptions,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment)
{
    public string Resolve()
    {
        if (environment.IsDevelopment())
        {
            var requestOrigin = httpContextAccessor.HttpContext?
                .Request.Headers.Origin.FirstOrDefault();

            if (LoopbackOrigin.TryParse(requestOrigin, out var origin))
            {
                return origin!.GetLeftPart(UriPartial.Authority);
            }
        }

        return frontendOptions.Value.PublicUrl.TrimEnd('/');
    }
}
