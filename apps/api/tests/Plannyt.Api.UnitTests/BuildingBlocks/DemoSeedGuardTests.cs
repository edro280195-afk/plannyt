using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Plannyt.Api.BuildingBlocks.Configuration;

namespace Plannyt.Api.UnitTests.BuildingBlocks;

public sealed class DemoSeedGuardTests
{
    [Fact]
    public void Validate_WhenDisabledOutsideDevelopment_DoesNotThrow()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var configuration = BuildConfiguration(enabled: false);

        var exception = Record.Exception(
            () => DemoSeedGuard.Validate(environment, configuration));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhenEnabledOutsideDevelopment_Throws()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var configuration = BuildConfiguration(enabled: true);

        var exception = Assert.Throws<InvalidOperationException>(
            () => DemoSeedGuard.Validate(environment, configuration));

        Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenEnabledWithoutCredentials_Throws()
    {
        var environment = new TestHostEnvironment(Environments.Development);
        var configuration = BuildConfiguration(
            enabled: true,
            email: string.Empty,
            password: string.Empty);

        var exception = Assert.Throws<InvalidOperationException>(
            () => DemoSeedGuard.Validate(environment, configuration));

        Assert.Contains("correo y contraseña", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenEnabledInDevelopmentWithCredentials_DoesNotThrow()
    {
        var environment = new TestHostEnvironment(Environments.Development);
        var configuration = BuildConfiguration(
            enabled: true,
            email: "planner@example.invalid",
            password: "local-only-password");

        var exception = Record.Exception(
            () => DemoSeedGuard.Validate(environment, configuration));

        Assert.Null(exception);
    }

    private static IConfiguration BuildConfiguration(
        bool enabled,
        string email = "planner@example.invalid",
        string password = "local-only-password") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoSeed:Enabled"] = enabled.ToString(),
                ["DemoSeed:PlannerEmail"] = email,
                ["DemoSeed:PlannerPassword"] = password
            })
            .Build();

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Plannyt.Api.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
