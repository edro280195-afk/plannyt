using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Catalog.Domain;

namespace Plannyt.Api.UnitTests.Catalog;

public sealed class CouponTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegisterUse_WithinValidity_IncrementsCounter()
    {
        var coupon = CreateCoupon(maximumUses: 2);

        coupon.RegisterUse(Now);

        Assert.Equal(1, coupon.CurrentUses);
        Assert.True(coupon.IsAvailable(Now));
    }

    [Fact]
    public void RegisterUse_WhenMaximumReached_IsRejected()
    {
        var coupon = CreateCoupon(maximumUses: 1);
        coupon.RegisterUse(Now);

        Assert.Throws<DomainRuleException>(() => coupon.RegisterUse(Now));
        Assert.False(coupon.IsAvailable(Now));
    }

    [Fact]
    public void IsAvailable_OutsideValidity_ReturnsFalse()
    {
        var coupon = CreateCoupon(maximumUses: null);

        Assert.False(coupon.IsAvailable(Now.AddDays(-2)));
        Assert.False(coupon.IsAvailable(Now.AddDays(2)));
    }

    [Fact]
    public void RegisterSnapshotUse_AfterCatalogChange_PreservesPublishedBenefit()
    {
        var coupon = CreateCoupon(maximumUses: 1);
        coupon.Update(
            "Promoción cerrada",
            DiscountType.FixedAmount,
            1m,
            Now.AddDays(-10),
            Now.AddDays(-5),
            0,
            false,
            Now);

        coupon.RegisterSnapshotUse(Now);

        Assert.Equal(1, coupon.CurrentUses);
        Assert.False(coupon.IsAvailable(Now));
    }

    private static Coupon CreateCoupon(int? maximumUses) =>
        Coupon.Create(
            Guid.NewGuid(),
            "VERANO",
            "Promoción de verano",
            DiscountType.Percentage,
            10m,
            Now.AddDays(-1),
            Now.AddDays(1),
            maximumUses,
            Now);
}
