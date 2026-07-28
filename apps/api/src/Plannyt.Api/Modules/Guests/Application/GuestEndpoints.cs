namespace Plannyt.Api.Modules.Guests.Application;

public static class GuestEndpoints
{
    public static IEndpointRouteBuilder MapGuestEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/events/{eventId:guid}/guests")
            .WithTags("Invitados")
            .RequireAuthorization();

        group.MapGet(
            "/dashboard",
            async (
                Guid organizationId,
                Guid eventId,
                GuestService service,
                string? search = null,
                Guid? groupId = null,
                Guid? tagId = null,
                bool includeArchived = false,
                CancellationToken cancellationToken = default) =>
                Results.Ok(await service.GetDashboardAsync(
                    organizationId,
                    eventId,
                    search,
                    groupId,
                    tagId,
                    includeArchived,
                    cancellationToken)));

        group.MapPost(
            "/groups",
            async (
                Guid organizationId,
                Guid eventId,
                InvitationGroupRequest request,
                GuestService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateGroupAsync(
                    organizationId,
                    eventId,
                    request,
                    "Manual",
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/guests/groups/{result.Id}",
                    result);
            });

        group.MapPut(
            "/groups/{groupId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid groupId,
                InvitationGroupRequest request,
                GuestService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateGroupAsync(
                    organizationId,
                    eventId,
                    groupId,
                    request,
                    cancellationToken)));

        group.MapDelete(
            "/groups/{groupId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid groupId,
                GuestService service,
                CancellationToken cancellationToken) =>
            {
                await service.ArchiveGroupAsync(
                    organizationId,
                    eventId,
                    groupId,
                    cancellationToken);
                return Results.NoContent();
            });

        group.MapPost(
            "/",
            async (
                Guid organizationId,
                Guid eventId,
                EventGuestRequest request,
                GuestService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateGuestAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/guests/{result.Id}",
                    result);
            });

        group.MapPut(
            "/{guestId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid guestId,
                EventGuestRequest request,
                GuestService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateGuestAsync(
                    organizationId,
                    eventId,
                    guestId,
                    request,
                    cancellationToken)));

        group.MapDelete(
            "/{guestId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid guestId,
                GuestService service,
                CancellationToken cancellationToken) =>
            {
                await service.ArchiveGuestAsync(
                    organizationId,
                    eventId,
                    guestId,
                    cancellationToken);
                return Results.NoContent();
            });

        group.MapGet(
            "/tags",
            async (
                Guid organizationId,
                Guid eventId,
                GuestService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetTagsAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapPost(
            "/tags",
            async (
                Guid organizationId,
                Guid eventId,
                GuestTagRequest request,
                GuestService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateTagAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/guests/tags/{result.Id}",
                    result);
            });

        group.MapDelete(
            "/tags/{tagId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid tagId,
                GuestService service,
                CancellationToken cancellationToken) =>
            {
                await service.ArchiveTagAsync(
                    organizationId,
                    eventId,
                    tagId,
                    cancellationToken);
                return Results.NoContent();
            });

        group.MapPut(
            "/tags/{tagId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid tagId,
                GuestTagRequest request,
                GuestService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateTagAsync(
                    organizationId,
                    eventId,
                    tagId,
                    request,
                    cancellationToken)));

        group.MapGet(
            "/duplicates",
            async (
                Guid organizationId,
                Guid eventId,
                GuestService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetDuplicateSuggestionsAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapGet(
            "/imports/template",
            () => Results.File(
                GuestCsvImportService.GetTemplate(),
                "text/csv; charset=utf-8",
                "plantilla-invitados.csv"));

        group.MapPost(
                "/imports/analyze",
                async (
                    Guid organizationId,
                    Guid eventId,
                    IFormFile file,
                    GuestCsvImportService service,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await service.AnalyzeAsync(
                        organizationId,
                        eventId,
                        file,
                        cancellationToken)))
            .DisableAntiforgery();

        group.MapPut(
            "/imports/{importId:guid}/mapping",
            async (
                Guid organizationId,
                Guid eventId,
                Guid importId,
                UpdateGuestImportMappingRequest request,
                GuestCsvImportService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateMappingAsync(
                    organizationId,
                    eventId,
                    importId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/imports/{importId:guid}/confirm",
            async (
                Guid organizationId,
                Guid eventId,
                Guid importId,
                GuestCsvImportService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ConfirmAsync(
                    organizationId,
                    eventId,
                    importId,
                    cancellationToken)));

        group.MapGet(
            "/imports/{importId:guid}/report",
            async (
                Guid organizationId,
                Guid eventId,
                Guid importId,
                GuestCsvImportService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetResultAsync(
                    organizationId,
                    eventId,
                    importId,
                    cancellationToken)));

        group.MapGet(
            "/export",
            async (
                Guid organizationId,
                Guid eventId,
                GuestCsvImportService service,
                CancellationToken cancellationToken) =>
                Results.File(
                    await service.ExportAsync(
                        organizationId,
                        eventId,
                        cancellationToken),
                    "text/csv; charset=utf-8",
                    $"invitados-{eventId:N}.csv"));

        return endpoints;
    }
}
