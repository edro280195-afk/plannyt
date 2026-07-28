using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Documents.Storage;

namespace Plannyt.Api.UnitTests.Documents;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _storageRoot = Path.Combine(
        Path.GetTempPath(),
        "plannyt-unit-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task OpenRead_WhenKeyEscapesRoot_IsRejected()
    {
        var storage = CreateStorage();

        var exception = await Assert.ThrowsAsync<ForbiddenException>(
            () => storage.OpenReadAsync("../secret.pdf", CancellationToken.None));

        Assert.Contains(
            "salir del directorio",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Guard_WhenEnvironmentIsProduction_RejectsLocalStorage()
    {
        var environment = new TestHostEnvironment(Environments.Production);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FileStorageGuard.Validate(environment));

        Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storageRoot))
        {
            Directory.Delete(_storageRoot, true);
        }
    }

    private LocalFileStorage CreateStorage() =>
        new(
            Options.Create(new FileStorageOptions
            {
                RootPath = _storageRoot
            }),
            new TestHostEnvironment(Environments.Development),
            TimeProvider.System);

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Plannyt.Api.UnitTests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
