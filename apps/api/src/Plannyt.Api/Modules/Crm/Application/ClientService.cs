using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.BuildingBlocks.Http;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Crm.Application;

public sealed class ClientService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<PagedResponse<ClientListItemResponse>> GetPageAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsView,
            null,
            cancellationToken);
        ValidatePage(page, pageSize);
        var query = dbContext.Clients
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);

        if (!includeArchived)
        {
            query = query.Where(entity => entity.Status != ClientStatus.Archived);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(entity =>
                EF.Functions.ILike(entity.DisplayName, $"%{term}%")
                || (entity.CompanyName != null
                    && EF.Functions.ILike(entity.CompanyName, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(entity => entity.DisplayName)
            .ThenBy(entity => entity.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new ClientListItemResponse(
                entity.Id,
                entity.ClientType,
                entity.DisplayName,
                entity.CompanyName,
                entity.Status,
                entity.Source,
                entity.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResponse<ClientListItemResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<ClientResponse> GetAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsView,
            null,
            cancellationToken);
        var client = await FindClientAsync(
            organizationId,
            clientId,
            true,
            cancellationToken);
        return await BuildResponseAsync(client, cancellationToken);
    }

    public async Task<ClientResponse> CreateAsync(
        Guid organizationId,
        CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsCreate,
            null,
            cancellationToken);
        ClientRequestValidator.Validate(request);
        var now = timeProvider.GetUtcNow();
        Client client;

        if (request.ClientType == ClientType.Person)
        {
            var profile = request.Person
                ?? throw new InvalidOperationException("El perfil fue validado.");
            var person = CreatePerson(organizationId, profile, now);
            client = Client.CreatePerson(
                organizationId,
                person.Id,
                request.DisplayName.Trim(),
                Normalize(request.Source),
                now);
            dbContext.People.Add(person);
        }
        else
        {
            client = Client.CreateCompany(
                organizationId,
                request.CompanyName!.Trim(),
                request.DisplayName.Trim(),
                Normalize(request.Source),
                now);
        }

        dbContext.Clients.Add(client);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "client.created",
            nameof(Client),
            client.Id,
            new Dictionary<string, object?>
            {
                ["clientType"] = client.ClientType.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(client, cancellationToken);
    }

    public async Task<ClientResponse> UpdateAsync(
        Guid organizationId,
        Guid clientId,
        UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsUpdate,
            null,
            cancellationToken);
        var client = await FindClientAsync(
            organizationId,
            clientId,
            false,
            cancellationToken);
        ClientRequestValidator.Validate(client.ClientType, request);
        var now = timeProvider.GetUtcNow();
        client.Update(
            request.DisplayName.Trim(),
            Normalize(request.CompanyName),
            Normalize(request.Source),
            now);

        if (client.ClientType == ClientType.Person)
        {
            var person = await dbContext.People.SingleAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == client.PersonId,
                cancellationToken);
            UpdatePerson(
                person,
                request.Person
                    ?? throw new InvalidOperationException("El perfil fue validado."),
                now);
        }

        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "client.updated",
            nameof(Client),
            client.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(client, cancellationToken);
    }

    public async Task ArchiveAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsArchive,
            null,
            cancellationToken);
        var client = await FindClientAsync(
            organizationId,
            clientId,
            false,
            cancellationToken);
        if (client.Status == ClientStatus.Archived)
        {
            throw new ConflictException("El cliente ya está archivado.");
        }

        client.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "client.archived",
            nameof(Client),
            client.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClientContactResponse>> GetContactsAsync(
        Guid organizationId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsView,
            null,
            cancellationToken);
        _ = await FindClientAsync(
            organizationId,
            clientId,
            true,
            cancellationToken);
        return await QueryContacts(organizationId, clientId)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientContactResponse> AddContactAsync(
        Guid organizationId,
        Guid clientId,
        UpsertClientContactRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsUpdate,
            null,
            cancellationToken);
        ClientRequestValidator.Validate(request);
        _ = await FindClientAsync(
            organizationId,
            clientId,
            false,
            cancellationToken);
        await EnsurePrimaryIsAvailableAsync(
            organizationId,
            clientId,
            null,
            request.IsPrimary,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var person = CreatePerson(
            organizationId,
            ToPersonProfile(request),
            now);
        var contact = ClientContact.Create(
            organizationId,
            clientId,
            person.Id,
            request.ContactRole.Trim(),
            request.IsPrimary,
            now);
        dbContext.AddRange(person, contact);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "client.contact_created",
            nameof(ClientContact),
            contact.Id,
            new Dictionary<string, object?>
            {
                ["clientId"] = clientId
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToContactResponse(contact, person);
    }

    public async Task<ClientContactResponse> UpdateContactAsync(
        Guid organizationId,
        Guid clientId,
        Guid contactId,
        UpsertClientContactRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsUpdate,
            null,
            cancellationToken);
        ClientRequestValidator.Validate(request);
        var contact = await dbContext.ClientContacts.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.ClientId == clientId
                && entity.Id == contactId,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró el contacto.");
        await EnsurePrimaryIsAvailableAsync(
            organizationId,
            clientId,
            contactId,
            request.IsPrimary,
            cancellationToken);
        var person = await dbContext.People.SingleAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == contact.PersonId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        UpdatePerson(person, ToPersonProfile(request), now);
        contact.Update(request.ContactRole.Trim(), request.IsPrimary, now);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "client.contact_updated",
            nameof(ClientContact),
            contact.Id,
            new Dictionary<string, object?>
            {
                ["clientId"] = clientId
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToContactResponse(contact, person);
    }

    private async Task<Client> FindClientAsync(
        Guid organizationId,
        Guid clientId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Clients
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.Id == clientId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró el cliente.");
    }

    private async Task<ClientResponse> BuildResponseAsync(
        Client client,
        CancellationToken cancellationToken)
    {
        PersonProfileResponse? personResponse = null;
        if (client.PersonId is Guid personId)
        {
            personResponse = await dbContext.People
                .AsNoTracking()
                .Where(entity =>
                    entity.OrganizationId == client.OrganizationId
                    && entity.Id == personId)
                .Select(entity => new PersonProfileResponse(
                    entity.Id,
                    entity.FirstName,
                    entity.LastName,
                    entity.DisplayName,
                    entity.ContactEmail,
                    entity.ContactPhone,
                    entity.PreferredLanguage,
                    entity.TimeZone))
                .SingleAsync(cancellationToken);
        }

        var contacts = await QueryContacts(client.OrganizationId, client.Id)
            .ToListAsync(cancellationToken);
        return new ClientResponse(
            client.Id,
            client.ClientType,
            client.DisplayName,
            client.CompanyName,
            client.Status,
            client.Source,
            personResponse,
            contacts,
            client.CreatedAt,
            client.UpdatedAt,
            client.ArchivedAt);
    }

    private IQueryable<ClientContactResponse> QueryContacts(
        Guid organizationId,
        Guid clientId) =>
        dbContext.ClientContacts
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ClientId == clientId)
            .Join(
                dbContext.People.AsNoTracking(),
                contact => new
                {
                    contact.OrganizationId,
                    Id = contact.PersonId
                },
                person => new { person.OrganizationId, person.Id },
                (contact, person) => new
                {
                    Contact = contact,
                    Person = person
                })
            .OrderByDescending(entity => entity.Contact.IsPrimary)
            .ThenBy(entity => entity.Person.DisplayName)
            .Select(entity => new ClientContactResponse(
                entity.Contact.Id,
                entity.Person.Id,
                entity.Person.DisplayName,
                entity.Person.ContactEmail,
                entity.Person.ContactPhone,
                entity.Contact.ContactRole,
                entity.Contact.IsPrimary));

    private async Task EnsurePrimaryIsAvailableAsync(
        Guid organizationId,
        Guid clientId,
        Guid? exceptContactId,
        bool isPrimary,
        CancellationToken cancellationToken)
    {
        if (!isPrimary)
        {
            return;
        }

        var primaryExists = await dbContext.ClientContacts.AnyAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.ClientId == clientId
                && entity.IsPrimary
                && entity.Id != exceptContactId,
            cancellationToken);
        if (primaryExists)
        {
            throw new ConflictException(
                "El cliente ya tiene un contacto principal.");
        }
    }

    private static Person CreatePerson(
        Guid organizationId,
        PersonProfileRequest request,
        DateTimeOffset now) =>
        Person.Create(
            organizationId,
            null,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            Normalize(request.ContactEmail),
            Normalize(request.ContactPhone),
            request.PreferredLanguage.Trim(),
            request.TimeZone.Trim(),
            now);

    private static void UpdatePerson(
        Person person,
        PersonProfileRequest request,
        DateTimeOffset now) =>
        person.UpdateProfile(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            Normalize(request.ContactEmail),
            Normalize(request.ContactPhone),
            request.PreferredLanguage.Trim(),
            request.TimeZone.Trim(),
            now);

    private static PersonProfileRequest ToPersonProfile(
        UpsertClientContactRequest request) =>
        new(
            request.FirstName,
            request.LastName,
            request.ContactEmail,
            request.ContactPhone,
            request.PreferredLanguage,
            request.TimeZone);

    private static ClientContactResponse ToContactResponse(
        ClientContact contact,
        Person person) =>
        new(
            contact.Id,
            person.Id,
            person.DisplayName,
            person.ContactEmail,
            person.ContactPhone,
            contact.ContactRole,
            contact.IsPrimary);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidatePage(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["La página debe ser mayor o igual a 1."];
        }

        if (pageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["El tamaño de página debe estar entre 1 y 100."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
