namespace Plannyt.Api.Modules.Events.Application;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/events")
            .WithTags("Eventos")
            .RequireAuthorization();

        group.MapGet(
            "/",
            async (
                Guid organizationId,
                EventService service,
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
                CreateEventRequest request,
                EventService service,
                CancellationToken cancellationToken) =>
            {
                var eventEntity = await service.CreateAsync(
                    organizationId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventEntity.Id}",
                    eventEntity);
            });

        group.MapGet(
            "/{eventId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                EventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapPut(
            "/{eventId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                UpdateEventRequest request,
                EventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/{eventId:guid}/status",
            async (
                Guid organizationId,
                Guid eventId,
                ChangeEventStatusRequest request,
                EventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ChangeStatusAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/{eventId:guid}/archive",
            async (
                Guid organizationId,
                Guid eventId,
                ArchiveEventRequest request,
                EventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ArchiveAsync(
                    organizationId,
                    eventId,
                    request.Reason,
                    cancellationToken)));

        group.MapGet(
            "/{eventId:guid}/clients",
            async (
                Guid organizationId,
                Guid eventId,
                EventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetClientsAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapPost(
            "/{eventId:guid}/clients",
            async (
                Guid organizationId,
                Guid eventId,
                CreateEventClientRequest request,
                EventService service,
                CancellationToken cancellationToken) =>
            {
                var relation = await service.AddClientAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/clients/{relation.Id}",
                    relation);
            });

        group.MapDelete(
            "/{eventId:guid}/clients/{eventClientId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid eventClientId,
                EventService service,
                CancellationToken cancellationToken) =>
            {
                await service.RemoveClientAsync(
                    organizationId,
                    eventId,
                    eventClientId,
                    cancellationToken);
                return Results.NoContent();
            });

        group.MapGet(
            "/{eventId:guid}/participants",
            async (
                Guid organizationId,
                Guid eventId,
                EventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetParticipantsAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapPost(
            "/{eventId:guid}/participants",
            async (
                Guid organizationId,
                Guid eventId,
                UpsertEventParticipantRequest request,
                EventService service,
                CancellationToken cancellationToken) =>
            {
                var participant = await service.AddParticipantAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/participants/{participant.Id}",
                    participant);
            });

        group.MapPut(
            "/{eventId:guid}/participants/{participantId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid participantId,
                UpsertEventParticipantRequest request,
                EventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateParticipantAsync(
                    organizationId,
                    eventId,
                    participantId,
                    request,
                    cancellationToken)));

        return endpoints;
    }
}

public sealed record ArchiveEventRequest(string? Reason);
