using Plannyt.Api.BuildingBlocks.Http;
using Plannyt.Api.Modules.Identity.Security;

namespace Plannyt.Api.Modules.Access.Application;

public static class AccessEndpoints
{
    public static IEndpointRouteBuilder MapAccessEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        MapInvitationEndpoints(endpoints);
        MapAdministrationEndpoints(endpoints);
        MapPortalEndpoints(endpoints);
        return endpoints;
    }

    private static void MapInvitationEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/access-invitations")
            .WithTags("Invitaciones");

        group.MapGet(
                "/{token}",
                async (
                    string token,
                    InvitationService service,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetPublicAsync(
                        token,
                        cancellationToken)))
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);

        group.MapPost(
                "/{token}/register-and-accept",
                async (
                    string token,
                    RegisterAndAcceptInvitationRequest request,
                    HttpContext context,
                    InvitationService service,
                    RefreshCookieService cookieService,
                    CancellationToken cancellationToken) =>
                {
                    var result = await service.RegisterAndAcceptAsync(
                        token,
                        request,
                        context.Connection.RemoteIpAddress?.ToString(),
                        context.Request.Headers.UserAgent.FirstOrDefault(),
                        cancellationToken);
                    cookieService.Set(context.Response, result);
                    return Results.Ok(result.Response);
                })
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);

        group.MapPost(
                "/{token}/accept",
                async (
                    string token,
                    AcceptInvitationRequest request,
                    ICurrentUser currentUser,
                    InvitationService service,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await service.AcceptAsync(
                        token,
                        currentUser.UserAccountId,
                        request,
                        cancellationToken)))
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);
    }

    private static void MapAdministrationEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/organizations/{organizationId:guid}/members/invitations",
                async (
                    Guid organizationId,
                    CreateOrganizationInvitationRequest request,
                    InvitationService service,
                    CancellationToken cancellationToken) =>
                {
                    var invitation =
                        await service.CreateOrganizationInvitationAsync(
                            organizationId,
                            request,
                            cancellationToken);
                    return Results.Created(
                        $"/api/organizations/{organizationId}/members/invitations/{invitation.Id}",
                        invitation);
                })
            .WithTags("Invitaciones")
            .RequireAuthorization();

        endpoints.MapDelete(
                "/api/organizations/{organizationId:guid}/members/invitations/{invitationId:guid}",
                async (
                    Guid organizationId,
                    Guid invitationId,
                    InvitationService service,
                    CancellationToken cancellationToken) =>
                {
                    await service.RevokeOrganizationInvitationAsync(
                        organizationId,
                        invitationId,
                        cancellationToken);
                    return Results.NoContent();
                })
            .WithTags("Invitaciones")
            .RequireAuthorization();

        var eventAccess = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/events/{eventId:guid}/access")
            .WithTags("Accesos de evento")
            .RequireAuthorization();

        eventAccess.MapGet(
            "/",
            async (
                Guid organizationId,
                Guid eventId,
                EventAccessService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        eventAccess.MapPost(
            "/invitations",
            async (
                Guid organizationId,
                Guid eventId,
                CreateEventInvitationRequest request,
                InvitationService service,
                CancellationToken cancellationToken) =>
            {
                var invitation = await service.CreateEventInvitationAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/access/invitations/{invitation.Id}",
                    invitation);
            });

        eventAccess.MapDelete(
            "/invitations/{invitationId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid invitationId,
                InvitationService service,
                CancellationToken cancellationToken) =>
            {
                await service.RevokeEventInvitationAsync(
                    organizationId,
                    eventId,
                    invitationId,
                    cancellationToken);
                return Results.NoContent();
            });

        eventAccess.MapDelete(
            "/{accessId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid accessId,
                EventAccessService service,
                CancellationToken cancellationToken) =>
            {
                await service.RevokeAsync(
                    organizationId,
                    eventId,
                    accessId,
                    cancellationToken);
                return Results.NoContent();
            });
    }

    private static void MapPortalEndpoints(IEndpointRouteBuilder endpoints)
    {
        var portal = endpoints
            .MapGroup("/api/client-portal/events")
            .WithTags("Portal del cliente")
            .RequireAuthorization();

        portal.MapGet(
            "/",
            async (
                PortalEventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetEventsAsync(cancellationToken)));

        portal.MapGet(
            "/{eventId:guid}",
            async (
                Guid eventId,
                PortalEventService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetEventAsync(
                    eventId,
                    cancellationToken)));
    }
}
