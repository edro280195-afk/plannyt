using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Plannyt.Api.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.4")
        .WithDatabase("plannyt_test")
        .WithUsername("plannyt")
        .WithPassword("test-only-password")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => _postgres.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Database:MigrateOnStartup"] = "true"
            });
        });
    }
}
