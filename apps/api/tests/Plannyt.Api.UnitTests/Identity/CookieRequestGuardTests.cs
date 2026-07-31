using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Identity.Security;

namespace Plannyt.Api.UnitTests.Identity;

public sealed class CookieRequestGuardTests
{
    private const string ConfiguredOrigin = "http://localhost:4200";

    [Fact]
    public void Validate_WhenOriginMatchesConfiguredValue_DoesNotThrow()
    {
        var guard = CreateGuard(Environments.Production);
        var context = CreateContext(ConfiguredOrigin, "web");

        var exception = Record.Exception(() => guard.Validate(context));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhenOriginIsAnotherLocalPortInDevelopment_DoesNotThrow()
    {
        // Regresión QA-016: el frontend puede correr en un puerto distinto al
        // configurado (por ejemplo 4210 en vez de 4200 por un conflicto de
        // puerto local documentado en next-session-prompt.md). En Development,
        // cualquier origen loopback debe seguir siendo aceptado.
        var guard = CreateGuard(Environments.Development);
        var context = CreateContext("http://localhost:4210", "web");

        var exception = Record.Exception(() => guard.Validate(context));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhenOriginIsAnotherLocalPortOutsideDevelopment_Throws()
    {
        var guard = CreateGuard(Environments.Production);
        var context = CreateContext("http://localhost:4210", "web");

        Assert.Throws<ForbiddenException>(() => guard.Validate(context));
    }

    [Fact]
    public void Validate_WhenOriginIsLoopbackOverHttpsInDevelopment_DoesNotThrow()
    {
        var guard = CreateGuard(Environments.Development);
        var context = CreateContext("https://127.0.0.1:4210", "web");

        var exception = Record.Exception(() => guard.Validate(context));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhenOriginIsUnrelatedHostInDevelopment_Throws()
    {
        var guard = CreateGuard(Environments.Development);
        var context = CreateContext("https://evil.example.invalid", "web");

        Assert.Throws<ForbiddenException>(() => guard.Validate(context));
    }

    [Fact]
    public void Validate_WhenClientHeaderIsMissing_ThrowsEvenWithMatchingOrigin()
    {
        var guard = CreateGuard(Environments.Development);
        var context = CreateContext(ConfiguredOrigin, clientHeader: null);

        Assert.Throws<ForbiddenException>(() => guard.Validate(context));
    }

    private static CookieRequestGuard CreateGuard(string environmentName) =>
        new(
            Options.Create(new CorsOptions { AllowedOrigin = ConfiguredOrigin }),
            new TestHostEnvironment(environmentName));

    private static DefaultHttpContext CreateContext(string? origin, string? clientHeader)
    {
        var context = new DefaultHttpContext();

        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        if (clientHeader is not null)
        {
            context.Request.Headers["X-Plannyt-Client"] = clientHeader;
        }

        return context;
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Plannyt.Api.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
