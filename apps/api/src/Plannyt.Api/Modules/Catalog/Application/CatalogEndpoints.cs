namespace Plannyt.Api.Modules.Catalog.Application;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/catalog")
            .WithTags("Catálogo comercial")
            .RequireAuthorization();

        group.MapGet("/services", async (
            Guid organizationId,
            CatalogService service,
            bool includeArchived = false,
            CancellationToken cancellationToken = default) =>
            Results.Ok(await service.GetServicesAsync(
                organizationId,
                includeArchived,
                cancellationToken)));
        group.MapPost("/services", async (
            Guid organizationId,
            ServiceCatalogItemRequest request,
            CatalogService service,
            CancellationToken cancellationToken) =>
            Results.Created(
                $"/api/organizations/{organizationId}/catalog/services",
                await service.CreateServiceAsync(
                    organizationId,
                    request,
                    cancellationToken)));
        group.MapPut("/services/{serviceId:guid}", async (
            Guid organizationId,
            Guid serviceId,
            ServiceCatalogItemRequest request,
            CatalogService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateServiceAsync(
                organizationId,
                serviceId,
                request,
                cancellationToken)));
        group.MapPost("/services/{serviceId:guid}/archive", async (
            Guid organizationId,
            Guid serviceId,
            CatalogService service,
            CancellationToken cancellationToken) =>
        {
            await service.ArchiveServiceAsync(
                organizationId,
                serviceId,
                cancellationToken);
            return Results.NoContent();
        });

        group.MapGet("/packages", async (
            Guid organizationId,
            CatalogService service,
            bool includeArchived = false,
            CancellationToken cancellationToken = default) =>
            Results.Ok(await service.GetPackagesAsync(
                organizationId,
                includeArchived,
                cancellationToken)));
        group.MapPost("/packages", async (
            Guid organizationId,
            PackageRequest request,
            CatalogService service,
            CancellationToken cancellationToken) =>
            Results.Created(
                $"/api/organizations/{organizationId}/catalog/packages",
                await service.CreatePackageAsync(
                    organizationId,
                    request,
                    cancellationToken)));
        group.MapPut("/packages/{packageId:guid}", async (
            Guid organizationId,
            Guid packageId,
            PackageRequest request,
            CatalogService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdatePackageAsync(
                organizationId,
                packageId,
                request,
                cancellationToken)));
        group.MapPost("/packages/{packageId:guid}/archive", async (
            Guid organizationId,
            Guid packageId,
            CatalogService service,
            CancellationToken cancellationToken) =>
        {
            await service.ArchivePackageAsync(
                organizationId,
                packageId,
                cancellationToken);
            return Results.NoContent();
        });

        group.MapGet("/coupons", async (
            Guid organizationId,
            CatalogService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetCouponsAsync(
                organizationId,
                cancellationToken)));
        group.MapPost("/coupons", async (
            Guid organizationId,
            CouponRequest request,
            CatalogService service,
            CancellationToken cancellationToken) =>
            Results.Created(
                $"/api/organizations/{organizationId}/catalog/coupons",
                await service.CreateCouponAsync(
                    organizationId,
                    request,
                    cancellationToken)));
        group.MapPut("/coupons/{couponId:guid}", async (
            Guid organizationId,
            Guid couponId,
            CouponRequest request,
            CatalogService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateCouponAsync(
                organizationId,
                couponId,
                request,
                cancellationToken)));

        return endpoints;
    }
}
