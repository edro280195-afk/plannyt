namespace Plannyt.Api.Modules.Catalog.Domain;

public enum PricingType
{
    Fixed,
    StartingAt,
    PerUnit,
    Custom
}

public enum TaxBehavior
{
    Exclusive,
    Inclusive,
    Exempt
}

public enum DiscountType
{
    None,
    FixedAmount,
    Percentage
}
