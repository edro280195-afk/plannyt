using Plannyt.Api.BuildingBlocks.Http;

namespace Plannyt.Api.Modules.Invitations.Application;

public static class InvitationDesignEndpoints
{
    public static IEndpointRouteBuilder MapInvitationDesignEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/events/{eventId:guid}/invitations")
            .WithTags("Experiencia de invitados")
            .RequireAuthorization();

        group.MapGet(
            "/experience",
            async (
                Guid organizationId,
                Guid eventId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetExperienceAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapPut(
            "/experience",
            async (
                Guid organizationId,
                Guid eventId,
                GuestExperienceRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateExperienceAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/experience/suspend",
            async (
                Guid organizationId,
                Guid eventId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.SuspendExperienceAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapPost(
            "/experience/resume",
            async (
                Guid organizationId,
                Guid eventId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ResumeExperienceAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapGet(
            "/templates",
            async (
                Guid organizationId,
                Guid eventId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetTemplatesAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapPost(
            "/templates",
            async (
                Guid organizationId,
                Guid eventId,
                InvitationTemplateRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateTemplateAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/invitations/templates/{result.Id}",
                    result);
            });

        group.MapPut(
            "/templates/{templateId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid templateId,
                InvitationTemplateRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateTemplateAsync(
                    organizationId,
                    eventId,
                    templateId,
                    request,
                    cancellationToken)));

        group.MapDelete(
            "/templates/{templateId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid templateId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
            {
                await service.ArchiveTemplateAsync(
                    organizationId,
                    eventId,
                    templateId,
                    cancellationToken);
                return Results.NoContent();
            });

        group.MapGet(
            "/designs",
            async (
                Guid organizationId,
                Guid eventId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetDesignsAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapGet(
            "/designs/{designId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid designId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetDesignAsync(
                    organizationId,
                    eventId,
                    designId,
                    cancellationToken)));

        group.MapPost(
            "/designs",
            async (
                Guid organizationId,
                Guid eventId,
                CreateInvitationDesignRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.CreateDesignAsync(
                    organizationId,
                    eventId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/invitations/designs/{result.Id}",
                    result);
            });

        group.MapPut(
            "/designs/{designId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid designId,
                UpdateInvitationDesignRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateDesignAsync(
                    organizationId,
                    eventId,
                    designId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/designs/{designId:guid}/submit-review",
            async (
                Guid organizationId,
                Guid eventId,
                Guid designId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.SubmitReviewAsync(
                    organizationId,
                    eventId,
                    designId,
                    cancellationToken)));

        group.MapPost(
            "/designs/{designId:guid}/versions/{versionId:guid}/comments",
            async (
                Guid organizationId,
                Guid eventId,
                Guid designId,
                Guid versionId,
                InvitationCommentRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.AddCommentAsync(
                    organizationId,
                    eventId,
                    designId,
                    versionId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/designs/{designId:guid}/versions/{versionId:guid}/approve",
            async (
                Guid organizationId,
                Guid eventId,
                Guid designId,
                Guid versionId,
                InvitationCommentRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.ApproveAsync(
                    organizationId,
                    eventId,
                    designId,
                    versionId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/designs/{designId:guid}/versions/{versionId:guid}/request-changes",
            async (
                Guid organizationId,
                Guid eventId,
                Guid designId,
                Guid versionId,
                InvitationCommentRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.RequestChangesAsync(
                    organizationId,
                    eventId,
                    designId,
                    versionId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/designs/{designId:guid}/publish",
            async (
                Guid organizationId,
                Guid eventId,
                Guid designId,
                PublishInvitationDesignRequest request,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.PublishAsync(
                    organizationId,
                    eventId,
                    designId,
                    request,
                    cancellationToken)));

        group.MapDelete(
            "/designs/{designId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid designId,
                InvitationDesignService service,
                CancellationToken cancellationToken) =>
            {
                await service.ArchiveDesignAsync(
                    organizationId,
                    eventId,
                    designId,
                    cancellationToken);
                return Results.NoContent();
            });

        group.MapGet(
            "/links",
            async (
                Guid organizationId,
                Guid eventId,
                GuestLinkService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetLinksAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        group.MapPost(
            "/groups/{groupId:guid}/links",
            async (
                Guid organizationId,
                Guid eventId,
                Guid groupId,
                GenerateGuestLinkRequest request,
                GuestLinkService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GenerateAsync(
                    organizationId,
                    eventId,
                    groupId,
                    request,
                    cancellationToken);
                return Results.Created(
                    $"/api/organizations/{organizationId}/events/{eventId}/invitations/links/{result.Id}",
                    result);
            });

        group.MapPost(
            "/links/{linkId:guid}/regenerate",
            async (
                Guid organizationId,
                Guid eventId,
                Guid linkId,
                GenerateGuestLinkRequest request,
                GuestLinkService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.RegenerateAsync(
                    organizationId,
                    eventId,
                    linkId,
                    request,
                    cancellationToken)));

        group.MapPost(
            "/links/{linkId:guid}/mark-shared",
            async (
                Guid organizationId,
                Guid eventId,
                Guid linkId,
                GuestLinkService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.MarkSharedAsync(
                    organizationId,
                    eventId,
                    linkId,
                    cancellationToken)));

        group.MapDelete(
            "/links/{linkId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid linkId,
                GuestLinkService service,
                CancellationToken cancellationToken) =>
            {
                await service.RevokeAsync(
                    organizationId,
                    eventId,
                    linkId,
                    cancellationToken);
                return Results.NoContent();
            });

        endpoints.MapGet(
                "/api/public/invitations/{token}",
                async (
                    string token,
                    PublicInvitationService service,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetAsync(token, cancellationToken)))
            .WithTags("Invitaciones públicas")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);

        return endpoints;
    }
}
