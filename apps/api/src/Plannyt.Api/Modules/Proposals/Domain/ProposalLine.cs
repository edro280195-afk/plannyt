using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Proposals.Domain;

public sealed class ProposalLine : ITenantEntity
{
    private ProposalLine()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProposalVersionId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid? ServiceCatalogItemId { get; private set; }

    public Guid? PackageId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public string DiscountType { get; private set; } = string.Empty;

    public decimal DiscountValue { get; private set; }

    public decimal TaxRate { get; private set; }

    public decimal LineSubtotal { get; private set; }

    public decimal LineDiscount { get; private set; }

    public decimal LineTax { get; private set; }

    public decimal LineTotal { get; private set; }

    public bool IsOptional { get; private set; }

    public int SortOrder { get; private set; }

    public static ProposalLine Create(
        Guid organizationId,
        Guid proposalVersionId,
        string description,
        Guid? serviceCatalogItemId,
        Guid? packageId,
        decimal quantity,
        decimal unitPrice,
        string discountType,
        decimal discountValue,
        decimal taxRate,
        decimal lineSubtotal,
        decimal lineDiscount,
        decimal lineTax,
        decimal lineTotal,
        bool isOptional,
        int sortOrder) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProposalVersionId = proposalVersionId,
            Description = description,
            ServiceCatalogItemId = serviceCatalogItemId,
            PackageId = packageId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountType = discountType,
            DiscountValue = discountValue,
            TaxRate = taxRate,
            LineSubtotal = lineSubtotal,
            LineDiscount = lineDiscount,
            LineTax = lineTax,
            LineTotal = lineTotal,
            IsOptional = isOptional,
            SortOrder = sortOrder
        };
}
