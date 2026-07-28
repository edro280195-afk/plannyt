using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Catalog.Application;

public sealed class CatalogService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ServiceCatalogItemResponse>> GetServicesAsync(
        Guid organizationId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.CatalogView,
            null,
            cancellationToken);
        var query = dbContext.ServiceCatalogItems
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);
        if (!includeArchived)
        {
            query = query.Where(entity => entity.ArchivedAt == null);
        }

        return await query
            .OrderBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.Name)
            .Select(entity => new ServiceCatalogItemResponse(
                entity.Id,
                entity.Name,
                entity.Description,
                entity.Category,
                entity.PricingType,
                entity.BasePrice,
                entity.CurrencyCode,
                entity.TaxBehavior,
                entity.IsNegotiable,
                entity.IsActive,
                entity.SortOrder,
                entity.UpdatedAt,
                entity.ArchivedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceCatalogItemResponse> CreateServiceAsync(
        Guid organizationId,
        ServiceCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.CatalogManage,
            null,
            cancellationToken);
        CatalogRequestValidator.Validate(request);
        var now = timeProvider.GetUtcNow();
        var item = ServiceCatalogItem.Create(
            organizationId,
            request.Name.Trim(),
            Normalize(request.Description),
            request.Category.Trim(),
            request.PricingType,
            request.BasePrice,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.TaxBehavior,
            request.IsNegotiable,
            request.SortOrder,
            now);
        if (!request.IsActive)
        {
            item.Update(
                item.Name,
                item.Description,
                item.Category,
                item.PricingType,
                item.BasePrice,
                item.CurrencyCode,
                item.TaxBehavior,
                item.IsNegotiable,
                false,
                item.SortOrder,
                now);
        }

        dbContext.ServiceCatalogItems.Add(item);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "catalog.service_created",
            nameof(ServiceCatalogItem),
            item.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToServiceResponse(item);
    }

    public async Task<ServiceCatalogItemResponse> UpdateServiceAsync(
        Guid organizationId,
        Guid serviceId,
        ServiceCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.CatalogManage,
            null,
            cancellationToken);
        CatalogRequestValidator.Validate(request);
        var item = await FindServiceAsync(
            organizationId,
            serviceId,
            cancellationToken);
        item.Update(
            request.Name.Trim(),
            Normalize(request.Description),
            request.Category.Trim(),
            request.PricingType,
            request.BasePrice,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.TaxBehavior,
            request.IsNegotiable,
            request.IsActive,
            request.SortOrder,
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "catalog.service_updated",
            nameof(ServiceCatalogItem),
            item.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToServiceResponse(item);
    }

    public async Task ArchiveServiceAsync(
        Guid organizationId,
        Guid serviceId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.CatalogManage,
            null,
            cancellationToken);
        var item = await FindServiceAsync(
            organizationId,
            serviceId,
            cancellationToken);
        item.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "catalog.service_archived",
            nameof(ServiceCatalogItem),
            item.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PackageResponse>> GetPackagesAsync(
        Guid organizationId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PackagesView,
            null,
            cancellationToken);
        var query = dbContext.Packages
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);
        if (!includeArchived)
        {
            query = query.Where(entity => entity.ArchivedAt == null);
        }

        var packages = await query
            .OrderBy(entity => entity.Name)
            .ToListAsync(cancellationToken);
        return await BuildPackageResponsesAsync(
            packages,
            cancellationToken);
    }

    public async Task<PackageResponse> CreatePackageAsync(
        Guid organizationId,
        PackageRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PackagesManage,
            null,
            cancellationToken);
        CatalogRequestValidator.Validate(request);
        await EnsureServicesExistAsync(
            organizationId,
            request.Items,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var package = Package.Create(
            organizationId,
            request.Name.Trim(),
            Normalize(request.Description),
            request.BasePrice,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.IsNegotiable,
            now);
        if (!request.IsActive)
        {
            package.Update(
                package.Name,
                package.Description,
                package.BasePrice,
                package.CurrencyCode,
                package.IsNegotiable,
                false,
                now);
        }

        dbContext.Packages.Add(package);
        AddPackageItems(package, request.Items);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "catalog.package_created",
            nameof(Package),
            package.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildPackageResponsesAsync(
            [package],
            cancellationToken)).Single();
    }

    public async Task<PackageResponse> UpdatePackageAsync(
        Guid organizationId,
        Guid packageId,
        PackageRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PackagesManage,
            null,
            cancellationToken);
        CatalogRequestValidator.Validate(request);
        await EnsureServicesExistAsync(
            organizationId,
            request.Items,
            cancellationToken);
        var package = await FindPackageAsync(
            organizationId,
            packageId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        package.Update(
            request.Name.Trim(),
            Normalize(request.Description),
            request.BasePrice,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.IsNegotiable,
            request.IsActive,
            now);
        var oldItems = await dbContext.PackageItems
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.PackageId == packageId)
            .ToListAsync(cancellationToken);
        dbContext.PackageItems.RemoveRange(oldItems);
        AddPackageItems(package, request.Items);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "catalog.package_updated",
            nameof(Package),
            package.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildPackageResponsesAsync(
            [package],
            cancellationToken)).Single();
    }

    public async Task ArchivePackageAsync(
        Guid organizationId,
        Guid packageId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.PackagesManage,
            null,
            cancellationToken);
        var package = await FindPackageAsync(
            organizationId,
            packageId,
            cancellationToken);
        package.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "catalog.package_archived",
            nameof(Package),
            package.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CouponResponse>> GetCouponsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.CouponsView,
            null,
            cancellationToken);
        return await dbContext.Coupons
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId)
            .OrderBy(entity => entity.Code)
            .Select(entity => new CouponResponse(
                entity.Id,
                entity.Code,
                entity.Description,
                entity.DiscountType,
                entity.DiscountValue,
                entity.StartsAt,
                entity.EndsAt,
                entity.MaximumUses,
                entity.CurrentUses,
                entity.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CouponResponse> CreateCouponAsync(
        Guid organizationId,
        CouponRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.CouponsManage,
            null,
            cancellationToken);
        CatalogRequestValidator.Validate(request, true);
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Coupons.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Code == code,
                cancellationToken))
        {
            throw new ConflictException("Ya existe un cupón con ese código.");
        }

        var now = timeProvider.GetUtcNow();
        var coupon = Coupon.Create(
            organizationId,
            code,
            Normalize(request.Description),
            request.DiscountType,
            request.DiscountValue,
            request.StartsAt,
            request.EndsAt,
            request.MaximumUses,
            now);
        if (!request.IsActive)
        {
            coupon.Update(
                coupon.Description ?? string.Empty,
                coupon.DiscountType,
                coupon.DiscountValue,
                coupon.StartsAt,
                coupon.EndsAt,
                coupon.MaximumUses,
                false,
                now);
        }

        dbContext.Coupons.Add(coupon);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "catalog.coupon_created",
            nameof(Coupon),
            coupon.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCouponResponse(coupon);
    }

    public async Task<CouponResponse> UpdateCouponAsync(
        Guid organizationId,
        Guid couponId,
        CouponRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.CouponsManage,
            null,
            cancellationToken);
        CatalogRequestValidator.Validate(request, false);
        var coupon = await dbContext.Coupons.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == couponId,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró el cupón.");
        coupon.Update(
            Normalize(request.Description) ?? string.Empty,
            request.DiscountType,
            request.DiscountValue,
            request.StartsAt,
            request.EndsAt,
            request.MaximumUses,
            request.IsActive,
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "catalog.coupon_updated",
            nameof(Coupon),
            coupon.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToCouponResponse(coupon);
    }

    private async Task<ServiceCatalogItem> FindServiceAsync(
        Guid organizationId,
        Guid serviceId,
        CancellationToken cancellationToken) =>
        await dbContext.ServiceCatalogItems.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == serviceId,
            cancellationToken)
        ?? throw new NotFoundException("No se encontró el servicio.");

    private async Task<Package> FindPackageAsync(
        Guid organizationId,
        Guid packageId,
        CancellationToken cancellationToken) =>
        await dbContext.Packages.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == packageId,
            cancellationToken)
        ?? throw new NotFoundException("No se encontró el paquete.");

    private async Task EnsureServicesExistAsync(
        Guid organizationId,
        IReadOnlyList<PackageItemRequest> items,
        CancellationToken cancellationToken)
    {
        var ids = items.Select(item => item.ServiceCatalogItemId).Distinct().ToList();
        var found = await dbContext.ServiceCatalogItems.CountAsync(
            entity =>
                entity.OrganizationId == organizationId
                && ids.Contains(entity.Id)
                && entity.ArchivedAt == null,
            cancellationToken);
        if (found != ids.Count)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["items"] =
                    [
                        "Todos los servicios deben existir y estar activos en la organización."
                    ]
                });
        }
    }

    private void AddPackageItems(
        Package package,
        IReadOnlyList<PackageItemRequest> items)
    {
        dbContext.PackageItems.AddRange(items.Select(item =>
            PackageItem.Create(
                package.OrganizationId,
                package.Id,
                item.ServiceCatalogItemId,
                item.Quantity,
                item.IsOptional,
                item.IncludedPrice,
                item.SortOrder)));
    }

    private async Task<IReadOnlyList<PackageResponse>> BuildPackageResponsesAsync(
        IReadOnlyCollection<Package> packages,
        CancellationToken cancellationToken)
    {
        var packageIds = packages.Select(package => package.Id).ToList();
        var organizationIds = packages
            .Select(package => package.OrganizationId)
            .Distinct()
            .ToList();
        var items = await dbContext.PackageItems
            .AsNoTracking()
            .Where(entity =>
                packageIds.Contains(entity.PackageId)
                && organizationIds.Contains(entity.OrganizationId))
            .Join(
                dbContext.ServiceCatalogItems.AsNoTracking(),
                item => new
                {
                    item.OrganizationId,
                    Id = item.ServiceCatalogItemId
                },
                service => new { service.OrganizationId, service.Id },
                (item, service) => new
                {
                    item.PackageId,
                    Response = new PackageItemResponse(
                        item.Id,
                        item.ServiceCatalogItemId,
                        service.Name,
                        item.Quantity,
                        item.IsOptional,
                        item.IncludedPrice,
                        item.SortOrder)
                })
            .ToListAsync(cancellationToken);
        return packages.Select(package => new PackageResponse(
            package.Id,
            package.Name,
            package.Description,
            package.BasePrice,
            package.CurrencyCode,
            package.IsNegotiable,
            package.IsActive,
            items
                .Where(item => item.PackageId == package.Id)
                .OrderBy(item => item.Response.SortOrder)
                .Select(item => item.Response)
                .ToList(),
            package.UpdatedAt,
            package.ArchivedAt)).ToList();
    }

    private static ServiceCatalogItemResponse ToServiceResponse(
        ServiceCatalogItem item) =>
        new(
            item.Id,
            item.Name,
            item.Description,
            item.Category,
            item.PricingType,
            item.BasePrice,
            item.CurrencyCode,
            item.TaxBehavior,
            item.IsNegotiable,
            item.IsActive,
            item.SortOrder,
            item.UpdatedAt,
            item.ArchivedAt);

    private static CouponResponse ToCouponResponse(Coupon coupon) =>
        new(
            coupon.Id,
            coupon.Code,
            coupon.Description,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.StartsAt,
            coupon.EndsAt,
            coupon.MaximumUses,
            coupon.CurrentUses,
            coupon.IsActive);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
