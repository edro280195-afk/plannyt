using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;

namespace Plannyt.Api.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        var options = app.Services
            .GetRequiredService<IOptions<DatabaseOptions>>()
            .Value;

        if (!options.MigrateOnStartup)
        {
            return;
        }

        if (!app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Las migraciones automáticas solo pueden habilitarse en Development.");
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
