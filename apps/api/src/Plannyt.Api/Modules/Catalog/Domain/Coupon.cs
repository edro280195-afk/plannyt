using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Catalog.Domain;

public sealed class Coupon : ITenantEntity
{
    private Coupon()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DiscountType DiscountType { get; private set; }

    public decimal DiscountValue { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public int? MaximumUses { get; private set; }

    public int CurrentUses { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Coupon Create(
        Guid organizationId,
        string code,
        string? description,
        DiscountType discountType,
        decimal discountValue,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int? maximumUses,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = code,
            Description = description,
            DiscountType = discountType,
            DiscountValue = discountValue,
            StartsAt = startsAt,
            EndsAt = endsAt,
            MaximumUses = maximumUses,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        string description,
        DiscountType discountType,
        decimal discountValue,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int? maximumUses,
        bool isActive,
        DateTimeOffset now)
    {
        Description = description;
        DiscountType = discountType;
        DiscountValue = discountValue;
        StartsAt = startsAt;
        EndsAt = endsAt;
        MaximumUses = maximumUses;
        IsActive = isActive;
        UpdatedAt = now;
    }

    public void RegisterUse(DateTimeOffset now)
    {
        if (!IsAvailable(now))
        {
            throw new DomainRuleException(
                "El cupón no está disponible.");
        }

        CurrentUses++;
        UpdatedAt = now;
    }

    public void RegisterSnapshotUse(DateTimeOffset now)
    {
        CurrentUses++;
        UpdatedAt = now;
    }

    public bool IsAvailable(DateTimeOffset now) =>
        IsActive
        && now >= StartsAt
        && now <= EndsAt
        && (MaximumUses is null || CurrentUses < MaximumUses);
}
