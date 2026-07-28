namespace Plannyt.Api.Modules.Crm.Application;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/clients")
            .WithTags("Clientes")
            .RequireAuthorization();

        group.MapGet(
            "/",
            async (
                Guid organizationId,
                ClientService service,
                int page = 1,
                int pageSize = 20,
                string? search = null,
                bool includeArchived = false,
                CancellationToken cancellationToken = default) =>
                Results.Ok(await service.GetPageAsync(
                    organizationId,
                    page,
                    pageSize,
                    search,
                    includeArchived,
                    cancellationToken)));

        group.MapPost(
            "/",
            async (
                Guid organizationId,
                CreateClientRequest request,
                ClientService service,
                CancellationToken cancellationToken) =>
            {
                var client = await service.CreateAsync(
                    organizationId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/clients/{client.Id}",
                    client);
            });

        group.MapGet(
            "/{clientId:guid}",
            async (
                Guid organizationId,
                Guid clientId,
                ClientService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAsync(
                    organizationId,
                    clientId,
                    cancellationToken)));

        group.MapPut(
            "/{clientId:guid}",
            async (
                Guid organizationId,
                Guid clientId,
                UpdateClientRequest request,
                ClientService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateAsync(
                    organizationId,
                    clientId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/{clientId:guid}/archive",
            async (
                Guid organizationId,
                Guid clientId,
                ClientService service,
                CancellationToken cancellationToken) =>
            {
                await service.ArchiveAsync(
                    organizationId,
                    clientId,
                    cancellationToken);
                return Results.NoContent();
            });

        group.MapGet(
            "/{clientId:guid}/contacts",
            async (
                Guid organizationId,
                Guid clientId,
                ClientService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetContactsAsync(
                    organizationId,
                    clientId,
                    cancellationToken)));

        group.MapPost(
            "/{clientId:guid}/contacts",
            async (
                Guid organizationId,
                Guid clientId,
                UpsertClientContactRequest request,
                ClientService service,
                CancellationToken cancellationToken) =>
            {
                var contact = await service.AddContactAsync(
                    organizationId,
                    clientId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/clients/{clientId}/contacts/{contact.Id}",
                    contact);
            });

        group.MapPut(
            "/{clientId:guid}/contacts/{contactId:guid}",
            async (
                Guid organizationId,
                Guid clientId,
                Guid contactId,
                UpsertClientContactRequest request,
                ClientService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateContactAsync(
                    organizationId,
                    clientId,
                    contactId,
                    request,
                    cancellationToken)));

        return endpoints;
    }
}
