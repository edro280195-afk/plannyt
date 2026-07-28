namespace Plannyt.Api.Modules.Crm.Application;

public static class ProspectEndpoints
{
    public static IEndpointRouteBuilder MapProspectEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/prospects")
            .WithTags("Prospectos")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid organizationId,
            ProspectService service,
            int page = 1,
            int pageSize = 50,
            string? search = null,
            string? status = null,
            Guid? assignedUserId = null,
            string? eventType = null,
            DateOnly? dateFrom = null,
            DateOnly? dateTo = null,
            CancellationToken cancellationToken = default) =>
            Results.Ok(await service.GetPageAsync(
                organizationId,
                page,
                pageSize,
                search,
                status,
                assignedUserId,
                eventType,
                dateFrom,
                dateTo,
                cancellationToken)));

        group.MapPost("/", async (
            Guid organizationId,
            ProspectDetailsRequest request,
            ProspectService service,
            CancellationToken cancellationToken) =>
        {
            var prospect = await service.CreateAsync(
                organizationId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/prospects/{prospect.Id}",
                prospect);
        });

        group.MapGet("/{prospectId:guid}", async (
            Guid organizationId,
            Guid prospectId,
            ProspectService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(
                organizationId,
                prospectId,
                cancellationToken)));

        group.MapPut("/{prospectId:guid}", async (
            Guid organizationId,
            Guid prospectId,
            ProspectDetailsRequest request,
            ProspectService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateAsync(
                organizationId,
                prospectId,
                request,
                cancellationToken)));

        group.MapPost("/{prospectId:guid}/status", async (
            Guid organizationId,
            Guid prospectId,
            ChangeProspectStatusRequest request,
            ProspectService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ChangeStatusAsync(
                organizationId,
                prospectId,
                request,
                cancellationToken)));

        group.MapPost("/{prospectId:guid}/archive", async (
            Guid organizationId,
            Guid prospectId,
            ProspectService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ArchiveAsync(
                organizationId,
                prospectId,
                cancellationToken)));

        group.MapPost("/{prospectId:guid}/activities", async (
            Guid organizationId,
            Guid prospectId,
            CreateProspectActivityRequest request,
            ProspectService service,
            CancellationToken cancellationToken) =>
        {
            var activity = await service.AddActivityAsync(
                organizationId,
                prospectId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/prospects/{prospectId}/activities/{activity.Id}",
                activity);
        });

        group.MapPost("/{prospectId:guid}/activities/{activityId:guid}/complete", async (
            Guid organizationId,
            Guid prospectId,
            Guid activityId,
            ProspectService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.CompleteActivityAsync(
                organizationId,
                prospectId,
                activityId,
                cancellationToken)));

        group.MapGet("/{prospectId:guid}/client-matches", async (
            Guid organizationId,
            Guid prospectId,
            ProspectService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetClientMatchesAsync(
                organizationId,
                prospectId,
                cancellationToken)));

        group.MapPost("/{prospectId:guid}/convert", async (
            Guid organizationId,
            Guid prospectId,
            ConvertProspectRequest request,
            ProspectService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ConvertAsync(
                organizationId,
                prospectId,
                request,
                cancellationToken)));

        group.MapPost("/{prospectId:guid}/preliminary-event", async (
            Guid organizationId,
            Guid prospectId,
            LinkPreliminaryEventRequest request,
            ProspectService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.LinkPreliminaryEventAsync(
                organizationId,
                prospectId,
                request,
                cancellationToken)));

        return endpoints;
    }
}
