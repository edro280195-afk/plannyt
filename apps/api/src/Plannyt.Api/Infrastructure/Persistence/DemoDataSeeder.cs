using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Application;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Infrastructure.Persistence;

public sealed class DemoDataSeeder(
    PlannytDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    OrganizationSlugGenerator slugGenerator,
    EventStatusTransitionService statusTransitionService,
    IOptions<DemoSeedOptions> options,
    TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seedOptions = options.Value;
        if (!seedOptions.Enabled)
        {
            return;
        }

        var normalizedPlannerEmail = EmailNormalizer.Normalize(
            seedOptions.PlannerEmail);
        if (await dbContext.UserAccounts.AnyAsync(
                entity => entity.NormalizedEmail == normalizedPlannerEmail,
                cancellationToken))
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var plannerAccount = CreateAccount(
            seedOptions.PlannerEmail,
            seedOptions.PlannerPassword,
            now);
        var organization = Organization.Create(
            "Armonía Eventos",
            await slugGenerator.GenerateAsync(
                "Armonía Eventos",
                cancellationToken),
            OrganizationType.Agency,
            "America/Matamoros",
            "MX",
            "MXN",
            now);
        var plannerPerson = Person.Create(
            organization.Id,
            plannerAccount.Id,
            "Mariana",
            "Torres",
            "Mariana Torres",
            seedOptions.PlannerEmail,
            null,
            "es",
            organization.TimeZone,
            now);
        var membership = OrganizationMembership.CreateOwner(
            organization.Id,
            plannerAccount.Id,
            plannerPerson.Id,
            now);

        var normalizedClientEmail = EmailNormalizer.Normalize(
            seedOptions.ClientEmail);
        var clientAccount = await dbContext.UserAccounts.SingleOrDefaultAsync(
            entity => entity.NormalizedEmail == normalizedClientEmail,
            cancellationToken);
        var clientAccountIsNew = clientAccount is null;
        clientAccount ??= CreateAccount(
            seedOptions.ClientEmail,
            seedOptions.PlannerPassword,
            now);
        var ana = Person.Create(
            organization.Id,
            clientAccount.Id,
            "Ana",
            "Martínez",
            "Ana Martínez",
            seedOptions.ClientEmail,
            null,
            "es",
            organization.TimeZone,
            now);
        var carlos = Person.Create(
            organization.Id,
            null,
            "Carlos",
            "Ramírez",
            "Carlos Ramírez",
            null,
            null,
            "es",
            organization.TimeZone,
            now);
        var client = Client.CreatePerson(
            organization.Id,
            ana.Id,
            ana.DisplayName,
            "Datos demo",
            now);
        var start = now.AddMonths(6);
        var eventEntity = Event.Create(
            organization.Id,
            "Ana & Carlos",
            "Boda",
            start,
            start.AddHours(7),
            organization.TimeZone,
            "Monterrey",
            "MX",
            "Una celebración de demostración preparada para explorar Plannyt.",
            180,
            plannerAccount.Id,
            now);
        var eventClient = EventClient.Create(
            organization.Id,
            eventEntity.Id,
            client.Id,
            EventClientRelationshipType.PrimaryClient,
            true,
            true,
            now);
        var anaParticipant = EventParticipant.Create(
            organization.Id,
            eventEntity.Id,
            ana.Id,
            "Novia",
            1,
            true,
            "Protagonista del evento",
            now);
        var carlosParticipant = EventParticipant.Create(
            organization.Id,
            eventEntity.Id,
            carlos.Id,
            "Novio",
            2,
            true,
            "Protagonista del evento",
            now);
        var clientAccess = EventAccess.CreateAccepted(
            organization.Id,
            eventEntity.Id,
            clientAccount.Id,
            EventAccessRole.ClientPrimary,
            now,
            null,
            plannerAccount.Id,
            now,
            now);
        var confirmedHistory = statusTransitionService.ChangeStatus(
            eventEntity,
            EventStatus.Confirmed,
            plannerAccount.Id,
            now,
            "Datos demo");
        var planningHistory = statusTransitionService.ChangeStatus(
            eventEntity,
            EventStatus.Planning,
            plannerAccount.Id,
            now,
            "Datos demo");
        var auditEntry = AuditEntry.Create(
            organization.Id,
            eventEntity.Id,
            plannerAccount.Id,
            "demo.seeded",
            nameof(Organization),
            organization.Id,
            "{}",
            now,
            "demo-seed",
            null);

        dbContext.AddRange(
            plannerAccount,
            organization,
            plannerPerson,
            membership,
            ana,
            carlos,
            client,
            eventEntity,
            eventClient,
            anaParticipant,
            carlosParticipant,
            clientAccess,
            confirmedHistory,
            planningHistory,
            auditEntry);
        if (clientAccountIsNew)
        {
            dbContext.Add(clientAccount);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private UserAccount CreateAccount(
        string email,
        string password,
        DateTimeOffset now)
    {
        var trimmedEmail = email.Trim();
        var account = UserAccount.Create(
            trimmedEmail,
            EmailNormalizer.Normalize(trimmedEmail),
            string.Empty,
            now);
        account.SetPasswordHash(
            passwordHasher.HashPassword(account, password),
            now);
        return account;
    }
}

public static class DemoDataInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync();
    }
}
