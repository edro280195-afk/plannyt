using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Catalog.Domain;

public sealed class ServiceCatalogItem : ITenantEntity
{
    private ServiceCatalogItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string Category { get; private set; } = string.Empty;

    public PricingType PricingType { get; private set; }

    public decimal BasePrice { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public TaxBehavior TaxBehavior { get; private set; }

    public bool IsNegotiable { get; private set; }

    public bool IsActive { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static ServiceCatalogItem Create(
        Guid organizationId,
        string name,
        string? description,
        string category,
        PricingType pricingType,
        decimal basePrice,
        string currencyCode,
        TaxBehavior taxBehavior,
        bool isNegotiable,
        int sortOrder,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            Category = category,
            PricingType = pricingType,
            BasePrice = basePrice,
            CurrencyCode = currencyCode,
            TaxBehavior = taxBehavior,
            IsNegotiable = isNegotiable,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        string name,
        string? description,
        string category,
        PricingType pricingType,
        decimal basePrice,
        string currencyCode,
        TaxBehavior taxBehavior,
        bool isNegotiable,
        bool isActive,
        int sortOrder,
        DateTimeOffset now)
    {
        if (ArchivedAt is not null)
        {
            throw new DomainRuleException(
                "Un servicio archivado no admite cambios.");
        }

        Name = name;
        Description = description;
        Category = category;
        PricingType = pricingType;
        BasePrice = basePrice;
        CurrencyCode = currencyCode;
        TaxBehavior = taxBehavior;
        IsNegotiable = isNegotiable;
        IsActive = isActive;
        SortOrder = sortOrder;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        IsActive = false;
        ArchivedAt = now;
        UpdatedAt = now;
    }
}
