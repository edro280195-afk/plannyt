using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Catalog.Domain;

public sealed class PackageItem : ITenantEntity
{
    private PackageItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid PackageId { get; private set; }

    public Guid ServiceCatalogItemId { get; private set; }

    public decimal Quantity { get; private set; }

    public bool IsOptional { get; private set; }

    public decimal? IncludedPrice { get; private set; }

    public int SortOrder { get; private set; }

    public static PackageItem Create(
        Guid organizationId,
        Guid packageId,
        Guid serviceCatalogItemId,
        decimal quantity,
        bool isOptional,
        decimal? includedPrice,
        int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PackageId = packageId,
            ServiceCatalogItemId = serviceCatalogItemId,
            Quantity = quantity,
            IsOptional = isOptional,
            IncludedPrice = includedPrice,
            SortOrder = sortOrder
        };
}
