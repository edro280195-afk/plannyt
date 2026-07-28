namespace Plannyt.Api.Modules.Organizations.Application;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}")
            .WithTags("Organizaciones")
            .RequireAuthorization();

        group.MapGet(
            "/",
            async (
                Guid organizationId,
                OrganizationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAsync(
                    organizationId,
                    cancellationToken)));

        group.MapPut(
            "/",
            async (
                Guid organizationId,
                UpdateOrganizationRequest request,
                OrganizationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateAsync(
                    organizationId,
                    request,
                    cancellationToken)));

        group.MapGet(
            "/members",
            async (
                Guid organizationId,
                OrganizationService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetMembersAsync(
                    organizationId,
                    cancellationToken)));

        group.MapDelete(
            "/members/{membershipId:guid}",
            async (
                Guid organizationId,
                Guid membershipId,
                OrganizationService service,
                CancellationToken cancellationToken) =>
            {
                await service.RevokeMemberAsync(
                    organizationId,
                    membershipId,
                    cancellationToken);
                return Results.NoContent();
            });

        return endpoints;
    }
}
