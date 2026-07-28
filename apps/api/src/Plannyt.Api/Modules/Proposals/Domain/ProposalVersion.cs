using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Catalog.Domain;

namespace Plannyt.Api.Modules.Proposals.Domain;

public sealed class ProposalVersion : ITenantEntity
{
    private ProposalVersion()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProposalId { get; private set; }

    public int VersionNumber { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal DiscountTotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal GrandTotal { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public DateTimeOffset ValidUntil { get; private set; }

    public string? SharedIntroduction { get; private set; }

    public string? SharedTerms { get; private set; }

    public DiscountType GeneralDiscountType { get; private set; }

    public decimal GeneralDiscountValue { get; private set; }

    public decimal GeneralDiscountTotal { get; private set; }

    public string? CouponCode { get; private set; }

    public Guid? CouponId { get; private set; }

    public decimal CouponDiscountTotal { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public static ProposalVersion Create(
        Guid organizationId,
        Guid proposalId,
        int versionNumber,
        decimal subtotal,
        decimal discountTotal,
        decimal taxTotal,
        decimal grandTotal,
        string currencyCode,
        DateTimeOffset validUntil,
        string? sharedIntroduction,
        string? sharedTerms,
        DiscountType generalDiscountType,
        decimal generalDiscountValue,
        decimal generalDiscountTotal,
        string? couponCode,
        Guid? couponId,
        decimal couponDiscountTotal,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProposalId = proposalId,
            VersionNumber = versionNumber,
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            TaxTotal = taxTotal,
            GrandTotal = grandTotal,
            CurrencyCode = currencyCode,
            ValidUntil = validUntil,
            SharedIntroduction = sharedIntroduction,
            SharedTerms = sharedTerms,
            GeneralDiscountType = generalDiscountType,
            GeneralDiscountValue = generalDiscountValue,
            GeneralDiscountTotal = generalDiscountTotal,
            CouponCode = couponCode,
            CouponId = couponId,
            CouponDiscountTotal = couponDiscountTotal,
            CreatedBy = createdBy,
            CreatedAt = now,
            PublishedAt = now
        };
}
