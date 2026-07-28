using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Application;
using Plannyt.Api.Modules.Identity.Domain;

namespace Plannyt.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public sealed class DemoDataSeederTests(ApiFactory factory)
{
    [Fact]
    public async Task SeedAsync_WhenEnabled_CreatesCompleteIdempotentDemo()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlannytDbContext>();
        var email = $"demo-{Guid.NewGuid():N}@example.invalid";
        var clientEmail = $"client-demo-{Guid.NewGuid():N}@example.invalid";
        var seeder = new DemoDataSeeder(
            dbContext,
            services.GetRequiredService<IPasswordHasher<UserAccount>>(),
            services.GetRequiredService<OrganizationSlugGenerator>(),
            services.GetRequiredService<EventStatusTransitionService>(),
            Options.Create(new DemoSeedOptions
            {
                Enabled = true,
                PlannerEmail = email,
                PlannerPassword = "Demo-Only-Secure-Password-2026!",
                ClientEmail = clientEmail
            }),
            services.GetRequiredService<TimeProvider>());

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var normalizedEmail = EmailNormalizer.Normalize(email);
        var planner = await dbContext.UserAccounts.SingleAsync(
            entity => entity.NormalizedEmail == normalizedEmail);
        var membership = await dbContext.OrganizationMemberships.SingleAsync(
            entity => entity.UserAccountId == planner.Id);
        var eventEntity = await dbContext.Events.SingleAsync(
            entity => entity.OrganizationId == membership.OrganizationId
                && entity.Name == "Ana & Carlos");

        Assert.Equal(EventStatus.Planning, eventEntity.Status);
        Assert.Equal(
            1,
            await dbContext.Clients.CountAsync(
                entity => entity.OrganizationId == membership.OrganizationId
                    && entity.DisplayName == "Ana Martínez"));
        Assert.Equal(
            2,
            await dbContext.EventParticipants.CountAsync(
                entity => entity.EventId == eventEntity.Id));
        Assert.Equal(
            2,
            await dbContext.EventStatusHistory.CountAsync(
                entity => entity.EventId == eventEntity.Id));
        Assert.Equal(
            1,
            await dbContext.EventAccesses.CountAsync(
                entity => entity.EventId == eventEntity.Id));
        Assert.Equal(
            1,
            await dbContext.AuditEntries.CountAsync(
                entity => entity.Action == "demo.seeded"
                    && entity.EntityId == membership.OrganizationId));
    }
}
