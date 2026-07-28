using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Catalog.Domain;

namespace Plannyt.Api.Modules.Proposals.Domain;

public sealed class ProposalDraftLine : ITenantEntity
{
    private ProposalDraftLine()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProposalId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid? ServiceCatalogItemId { get; private set; }

    public Guid? PackageId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public DiscountType DiscountType { get; private set; }

    public decimal DiscountValue { get; private set; }

    public decimal TaxRate { get; private set; }

    public bool IsOptional { get; private set; }

    public int SortOrder { get; private set; }

    public static ProposalDraftLine Create(
        Guid organizationId,
        Guid proposalId,
        string description,
        Guid? serviceCatalogItemId,
        Guid? packageId,
        decimal quantity,
        decimal unitPrice,
        DiscountType discountType,
        decimal discountValue,
        decimal taxRate,
        bool isOptional,
        int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProposalId = proposalId,
            Description = description,
            ServiceCatalogItemId = serviceCatalogItemId,
            PackageId = packageId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountType = discountType,
            DiscountValue = discountValue,
            TaxRate = taxRate,
            IsOptional = isOptional,
            SortOrder = sortOrder
        };
}
