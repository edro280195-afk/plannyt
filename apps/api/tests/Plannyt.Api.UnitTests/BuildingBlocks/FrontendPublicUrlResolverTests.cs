using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;

namespace Plannyt.Api.UnitTests.BuildingBlocks;

public sealed class FrontendPublicUrlResolverTests
{
    private const string ConfiguredPublicUrl = "http://localhost:4200";

    [Fact]
    public void Resolve_InProduction_IgnoresRequestOriginAndReturnsConfiguredValue()
    {
        // El origen fijo de producción es la frontera de seguridad real; el
        // origen de la petición nunca debe poder sustituirlo fuera de Development.
        var resolver = CreateResolver(
            Environments.Production,
            requestOrigin: "http://localhost:4210");

        Assert.Equal(ConfiguredPublicUrl, resolver.Resolve());
    }

    [Fact]
    public void Resolve_InDevelopmentWithoutActiveRequest_ReturnsConfiguredValue()
    {
        var resolver = CreateResolver(Environments.Development, requestOrigin: null);

        Assert.Equal(ConfiguredPublicUrl, resolver.Resolve());
    }

    [Fact]
    public void Resolve_InDevelopmentWithAlternateLoopbackPort_ReturnsRequestOrigin()
    {
        // Regresión QA-017: el frontend puede correr en un puerto distinto al
        // configurado (4210 en vez de 4200); los enlaces públicos generados
        // (propuestas, firmas, invitaciones) deben apuntar al origen real
        // desde el que se hizo la solicitud, no a un valor fijo obsoleto.
        var resolver = CreateResolver(
            Environments.Development,
            requestOrigin: "http://localhost:4210");

        Assert.Equal("http://localhost:4210", resolver.Resolve());
    }

    [Fact]
    public void Resolve_InDevelopmentWithNonLoopbackOrigin_FallsBackToConfiguredValue()
    {
        var resolver = CreateResolver(
            Environments.Development,
            requestOrigin: "https://evil.example.invalid");

        Assert.Equal(ConfiguredPublicUrl, resolver.Resolve());
    }

    [Fact]
    public void Resolve_TrimsTrailingSlashFromConfiguredValue()
    {
        var resolver = CreateResolver(
            Environments.Production,
            requestOrigin: null,
            configuredPublicUrl: "https://plannyt.invalid/");

        Assert.Equal("https://plannyt.invalid", resolver.Resolve());
    }

    private static FrontendPublicUrlResolver CreateResolver(
        string environmentName,
        string? requestOrigin,
        string configuredPublicUrl = ConfiguredPublicUrl)
    {
        var context = new DefaultHttpContext();
        if (requestOrigin is not null)
        {
            context.Request.Headers.Origin = requestOrigin;
        }

        var accessor = new HttpContextAccessor
        {
            HttpContext = requestOrigin is null ? null : context
        };

        return new FrontendPublicUrlResolver(
            Options.Create(new FrontendOptions { PublicUrl = configuredPublicUrl }),
            accessor,
            new TestHostEnvironment(environmentName));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Plannyt.Api.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
