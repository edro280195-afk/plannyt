namespace Plannyt.Api.Modules.Invitations.Application;

using Plannyt.Api.Modules.Guests.Application;

public static class PortalGuestEndpoints
{
    public static IEndpointRouteBuilder MapPortalGuestEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/client-portal/events/{eventId:guid}/guest-experience")
            .WithTags("Colaboración de invitados")
            .RequireAuthorization();

        group.MapGet(
            "/",
            async (
                Guid eventId,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAsync(eventId, cancellationToken)));

        group.MapPost(
            "/groups",
            async (
                Guid eventId,
                PortalInvitationGroupRequest request,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateGroupAsync(
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/client-portal/events/{eventId}/guest-experience/groups/{result.Id}",
                    result);
            });

        group.MapPut(
            "/groups/{groupId:guid}",
            async (
                Guid eventId,
                Guid groupId,
                PortalInvitationGroupRequest request,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateGroupAsync(
                    eventId,
                    groupId,
                    request,
                    cancellationToken)));

        group.MapDelete(
            "/groups/{groupId:guid}",
            async (
                Guid eventId,
                Guid groupId,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
            {
                await service.ArchiveGroupAsync(eventId, groupId, cancellationToken);
                return Results.NoContent();
            });

        group.MapPost(
            "/guests",
            async (
                Guid eventId,
                PortalGuestRequest request,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateGuestAsync(
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/client-portal/events/{eventId}/guest-experience/guests/{result.Id}",
                    result);
            });

        group.MapPut(
            "/guests/{guestId:guid}",
            async (
                Guid eventId,
                Guid guestId,
                PortalGuestRequest request,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateGuestAsync(
                    eventId,
                    guestId,
                    request,
                    cancellationToken)));

        group.MapDelete(
            "/guests/{guestId:guid}",
            async (
                Guid eventId,
                Guid guestId,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
            {
                await service.ArchiveGuestAsync(eventId, guestId, cancellationToken);
                return Results.NoContent();
            });

        group.MapPut(
            "/designs/{designId:guid}",
            async (
                Guid eventId,
                Guid designId,
                UpdateInvitationDesignRequest request,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateDesignAsync(
                    eventId,
                    designId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/designs/{designId:guid}/versions/{versionId:guid}/comments",
            async (
                Guid eventId,
                Guid designId,
                Guid versionId,
                InvitationCommentRequest request,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ReviewAsync(
                    eventId,
                    designId,
                    versionId,
                    Domain.InvitationReviewDecision.Comment,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/designs/{designId:guid}/versions/{versionId:guid}/approve",
            async (
                Guid eventId,
                Guid designId,
                Guid versionId,
                InvitationCommentRequest request,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ReviewAsync(
                    eventId,
                    designId,
                    versionId,
                    Domain.InvitationReviewDecision.Approved,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/designs/{designId:guid}/versions/{versionId:guid}/request-changes",
            async (
                Guid eventId,
                Guid designId,
                Guid versionId,
                InvitationCommentRequest request,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ReviewAsync(
                    eventId,
                    designId,
                    versionId,
                    Domain.InvitationReviewDecision.ChangesRequested,
                    request,
                    cancellationToken)));

        group.MapGet(
            "/links",
            async (
                Guid eventId,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetLinksAsync(eventId, cancellationToken)));

        group.MapGet(
            "/duplicates",
            async (
                Guid eventId,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetDuplicateSuggestionsAsync(
                    eventId,
                    cancellationToken)));

        group.MapPost(
            "/links/{linkId:guid}/mark-shared",
            async (
                Guid eventId,
                Guid linkId,
                PortalGuestCollaborationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.MarkLinkSharedAsync(
                    eventId,
                    linkId,
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
                    Guid eventId,
                    IFormFile file,
                    GuestCsvImportService service,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await service.AnalyzePortalAsync(
                        eventId,
                        file,
                        cancellationToken)))
            .DisableAntiforgery();

        group.MapPut(
            "/imports/{importId:guid}/mapping",
            async (
                Guid eventId,
                Guid importId,
                UpdateGuestImportMappingRequest request,
                GuestCsvImportService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdatePortalMappingAsync(
                    eventId,
                    importId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/imports/{importId:guid}/confirm",
            async (
                Guid eventId,
                Guid importId,
                GuestCsvImportService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ConfirmPortalAsync(
                    eventId,
                    importId,
                    cancellationToken)));

        group.MapGet(
            "/imports/{importId:guid}/report",
            async (
                Guid eventId,
                Guid importId,
                GuestCsvImportService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetPortalResultAsync(
                    eventId,
                    importId,
                    cancellationToken)));

        return endpoints;
    }
}
