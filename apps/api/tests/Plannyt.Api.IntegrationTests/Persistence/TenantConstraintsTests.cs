using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Documents.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public sealed class TenantConstraintsTests(ApiFactory factory)
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EventClient_WhenClientBelongsToAnotherTenant_IsRejectedByDatabase()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var seed = await SeedTwoOrganizationsAsync(dbContext);

        var eventEntity = CreateEvent(seed.OrganizationA.Id, seed.Account.Id);
        var clientB = Client.CreateCompany(
            seed.OrganizationB.Id,
            "Cliente B",
            "Cliente B",
            null,
            Now);
        dbContext.AddRange(eventEntity, clientB);
        await dbContext.SaveChangesAsync();

        dbContext.EventClients.Add(EventClient.Create(
            seed.OrganizationA.Id,
            eventEntity.Id,
            clientB.Id,
            EventClientRelationshipType.PrimaryClient,
            true,
            false,
            Now));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task ClientContact_WhenPersonBelongsToAnotherTenant_IsRejectedByDatabase()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var seed = await SeedTwoOrganizationsAsync(dbContext);

        var clientA = Client.CreateCompany(
            seed.OrganizationA.Id,
            "Cliente A",
            "Cliente A",
            null,
            Now);
        var personB = Person.Create(
            seed.OrganizationB.Id,
            null,
            "Persona",
            "Externa",
            "Persona Externa",
            null,
            null,
            "es",
            "America/Matamoros",
            Now);
        dbContext.AddRange(clientA, personB);
        await dbContext.SaveChangesAsync();

        dbContext.ClientContacts.Add(ClientContact.Create(
            seed.OrganizationA.Id,
            clientA.Id,
            personB.Id,
            "Contacto",
            true,
            Now));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Document_WhenEventBelongsToAnotherTenant_IsRejectedByDatabase()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var seed = await SeedTwoOrganizationsAsync(dbContext);

        var eventB = CreateEvent(seed.OrganizationB.Id, seed.Account.Id);
        dbContext.Events.Add(eventB);
        await dbContext.SaveChangesAsync();

        dbContext.BasicDocuments.Add(BasicDocument.Create(
            seed.OrganizationA.Id,
            eventB.Id,
            null,
            "General",
            "archivo.pdf",
            "Local",
            Guid.NewGuid().ToString("N"),
            "application/pdf",
            100,
            DocumentVisibility.Internal,
            seed.Account.Id,
            Now));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
        await transaction.RollbackAsync();
    }

    private static async Task<SeedResult> SeedTwoOrganizationsAsync(
        PlannytDbContext dbContext)
    {
        var account = UserAccount.Create(
            $"{Guid.NewGuid():N}@example.invalid",
            $"{Guid.NewGuid():N}@EXAMPLE.INVALID",
            "not-a-real-password-hash",
            Now);
        var organizationA = CreateOrganization($"a-{Guid.NewGuid():N}");
        var organizationB = CreateOrganization($"b-{Guid.NewGuid():N}");
        dbContext.AddRange(account, organizationA, organizationB);
        await dbContext.SaveChangesAsync();
        return new SeedResult(account, organizationA, organizationB);
    }

    private static Organization CreateOrganization(string slug) =>
        Organization.Create(
            $"Organización {slug}",
            slug,
            OrganizationType.Agency,
            "America/Matamoros",
            "MX",
            "MXN",
            Now);

    private static Event CreateEvent(Guid organizationId, Guid accountId) =>
        Event.Create(
            organizationId,
            "Evento de prueba",
            "Wedding",
            Now.AddMonths(1),
            Now.AddMonths(1).AddHours(8),
            "America/Matamoros",
            "Matamoros",
            "MX",
            "Descripción compartida",
            100,
            accountId,
            Now);

    private sealed record SeedResult(
        UserAccount Account,
        Organization OrganizationA,
        Organization OrganizationB);
}
