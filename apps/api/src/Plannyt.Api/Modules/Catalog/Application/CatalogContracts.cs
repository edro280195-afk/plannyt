using Plannyt.Api.Modules.Catalog.Domain;

namespace Plannyt.Api.Modules.Catalog.Application;

public sealed record ServiceCatalogItemRequest(
    string Name,
    string? Description,
    string Category,
    PricingType PricingType,
    decimal BasePrice,
    string CurrencyCode,
    TaxBehavior TaxBehavior,
    bool IsNegotiable,
    bool IsActive,
    int SortOrder);

public sealed record ServiceCatalogItemResponse(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    PricingType PricingType,
    decimal BasePrice,
    string CurrencyCode,
    TaxBehavior TaxBehavior,
    bool IsNegotiable,
    bool IsActive,
    int SortOrder,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record PackageItemRequest(
    Guid ServiceCatalogItemId,
    decimal Quantity,
    bool IsOptional,
    decimal? IncludedPrice,
    int SortOrder);

public sealed record PackageRequest(
    string Name,
    string? Description,
    decimal BasePrice,
    string CurrencyCode,
    bool IsNegotiable,
    bool IsActive,
    IReadOnlyList<PackageItemRequest> Items);

public sealed record PackageItemResponse(
    Guid Id,
    Guid ServiceCatalogItemId,
    string ServiceName,
    decimal Quantity,
    bool IsOptional,
    decimal? IncludedPrice,
    int SortOrder);

public sealed record PackageResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal BasePrice,
    string CurrencyCode,
    bool IsNegotiable,
    bool IsActive,
    IReadOnlyList<PackageItemResponse> Items,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record CouponRequest(
    string Code,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MaximumUses,
    bool IsActive);

public sealed record CouponResponse(
    Guid Id,
    string Code,
    string? Description,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MaximumUses,
    int CurrentUses,
    bool IsActive);
