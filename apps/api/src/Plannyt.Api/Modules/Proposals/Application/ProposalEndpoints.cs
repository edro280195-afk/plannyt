using Plannyt.Api.BuildingBlocks.Http;

namespace Plannyt.Api.Modules.Proposals.Application;

public static class ProposalEndpoints
{
    public static IEndpointRouteBuilder MapProposalEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        MapAdministrationEndpoints(endpoints);
        MapPublicEndpoints(endpoints);
        MapPortalEndpoints(endpoints);
        return endpoints;
    }

    private static void MapAdministrationEndpoints(
        IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/proposals")
            .WithTags("Propuestas")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid organizationId,
            ProposalService service,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            string? status = null,
            CancellationToken cancellationToken = default) =>
            Results.Ok(await service.GetPageAsync(
                organizationId,
                page,
                pageSize,
                search,
                status,
                cancellationToken)));

        group.MapPost("/", async (
            Guid organizationId,
            ProposalDraftRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
        {
            var proposal = await service.CreateAsync(
                organizationId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/proposals/{proposal.Id}",
                proposal);
        });

        group.MapGet("/{proposalId:guid}", async (
            Guid organizationId,
            Guid proposalId,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(
                organizationId,
                proposalId,
                cancellationToken)));

        group.MapPut("/{proposalId:guid}/draft", async (
            Guid organizationId,
            Guid proposalId,
            ProposalDraftRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateDraftAsync(
                organizationId,
                proposalId,
                request,
                cancellationToken)));

        group.MapPost("/{proposalId:guid}/publish", async (
            Guid organizationId,
            Guid proposalId,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.PublishAsync(
                organizationId,
                proposalId,
                cancellationToken)));

        group.MapPost("/{proposalId:guid}/send", async (
            Guid organizationId,
            Guid proposalId,
            SendProposalRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.SendAsync(
                organizationId,
                proposalId,
                request,
                cancellationToken)));

        group.MapPost("/{proposalId:guid}/cancel", async (
            Guid organizationId,
            Guid proposalId,
            ProposalService service,
            CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(
                organizationId,
                proposalId,
                cancellationToken);
            return Results.NoContent();
        });

        group.MapPost("/{proposalId:guid}/duplicate", async (
            Guid organizationId,
            Guid proposalId,
            ProposalService service,
            CancellationToken cancellationToken) =>
        {
            var proposal = await service.DuplicateAsync(
                organizationId,
                proposalId,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/proposals/{proposal.Id}",
                proposal);
        });

        group.MapPost("/{proposalId:guid}/comments", async (
            Guid organizationId,
            Guid proposalId,
            CreateProposalCommentRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
        {
            var comment = await service.AddAdminCommentAsync(
                organizationId,
                proposalId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/proposals/{proposalId}/comments/{comment.Id}",
                comment);
        });

        group.MapPost("/{proposalId:guid}/comments/{commentId:guid}/resolve", async (
            Guid organizationId,
            Guid proposalId,
            Guid commentId,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ResolveCommentAsync(
                organizationId,
                proposalId,
                commentId,
                cancellationToken)));

        group.MapGet("/{proposalId:guid}/versions/{versionId:guid}/pdf", async (
            Guid organizationId,
            Guid proposalId,
            Guid versionId,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.File(
                await service.GetAdminVersionPdfAsync(
                    organizationId,
                    proposalId,
                    versionId,
                    cancellationToken),
                "application/pdf",
                $"propuesta-{proposalId:N}.pdf"));

        group.MapPost("/{proposalId:guid}/preliminary-event", async (
            Guid organizationId,
            Guid proposalId,
            LinkProposalEventRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(new
            {
                eventId = await service.LinkPreliminaryEventAsync(
                    organizationId,
                    proposalId,
                    request,
                    cancellationToken)
            }));
    }

    private static void MapPublicEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/public/proposals/{token}")
            .WithTags("Propuesta pública")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);

        group.MapGet("/", async (
            string token,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPublicAsync(
                token,
                cancellationToken)));

        group.MapGet("/pdf", async (
            string token,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.File(
                await service.GetPublicPdfAsync(token, cancellationToken),
                "application/pdf",
                "propuesta.pdf"));

        group.MapPost("/comments", async (
            string token,
            ProposalPublicCommentRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
        {
            var comment = await service.AddPublicCommentAsync(
                token,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/public/proposals/{token}/comments/{comment.Id}",
                comment);
        });

        group.MapPost("/request-changes", async (
            string token,
            ProposalDecisionRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.RequestChangesAsync(
                token,
                request,
                cancellationToken)));

        group.MapPost("/accept", async (
            string token,
            ProposalDecisionRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.AcceptAsync(
                token,
                request,
                cancellationToken)));

        group.MapPost("/reject", async (
            string token,
            ProposalDecisionRequest request,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.RejectAsync(
                token,
                request,
                cancellationToken)));
    }

    private static void MapPortalEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/client-portal/proposals")
            .WithTags("Portal del cliente")
            .RequireAuthorization();

        group.MapGet("/", async (
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPortalProposalsAsync(
                cancellationToken)));

        group.MapGet("/{proposalId:guid}", async (
            Guid proposalId,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPortalProposalAsync(
                proposalId,
                cancellationToken)));

        group.MapGet("/{proposalId:guid}/pdf", async (
            Guid proposalId,
            ProposalService service,
            CancellationToken cancellationToken) =>
            Results.File(
                await service.GetPortalProposalPdfAsync(
                    proposalId,
                    cancellationToken),
                "application/pdf",
                $"propuesta-{proposalId:N}.pdf"));
    }
}
