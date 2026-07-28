using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.UnitTests.Proposals;

public sealed class ProposalTotalsCalculatorTests
{
    private readonly ProposalTotalsCalculator _calculator = new();

    [Fact]
    public void Calculate_WithLineGeneralAndCouponDiscounts_CalculatesTaxAfterDiscounts()
    {
        var calculation = _calculator.Calculate(
            [
                Line("Producción", 2m, 1000m, DiscountType.Percentage, 10m, 16m),
                Line("Coordinación", 1m, 500m, DiscountType.FixedAmount, 100m, 16m)
            ],
            DiscountType.Percentage,
            10m,
            DiscountType.FixedAmount,
            200m);

        Assert.Equal(2500m, calculation.Subtotal);
        Assert.Equal(720m, calculation.DiscountTotal);
        Assert.Equal(220m, calculation.GeneralDiscountTotal);
        Assert.Equal(200m, calculation.CouponDiscountTotal);
        Assert.Equal(284.80m, calculation.TaxTotal);
        Assert.Equal(2064.80m, calculation.GrandTotal);
    }

    [Fact]
    public void Calculate_OptionalLine_DoesNotIncreaseProposalTotals()
    {
        var calculation = _calculator.Calculate(
            [
                Line("Incluido", 1m, 1000m, DiscountType.None, 0m, 16m),
                Line("Opcional", 1m, 5000m, DiscountType.None, 0m, 16m, true)
            ],
            DiscountType.None,
            0m,
            DiscountType.None,
            0m);

        Assert.Equal(1000m, calculation.Subtotal);
        Assert.Equal(1160m, calculation.GrandTotal);
        Assert.Equal(5800m, calculation.Lines.Single(line => line.Source.IsOptional).LineTotal);
    }

    [Fact]
    public void Calculate_FixedDiscountGreaterThanBase_StopsAtZero()
    {
        var calculation = _calculator.Calculate(
            [Line("Servicio", 1m, 100m, DiscountType.FixedAmount, 1000m, 16m)],
            DiscountType.FixedAmount,
            1000m,
            DiscountType.FixedAmount,
            1000m);

        Assert.Equal(0m, calculation.GrandTotal);
        Assert.Equal(100m, calculation.DiscountTotal);
        Assert.Equal(0m, calculation.TaxTotal);
    }

    [Fact]
    public void Calculate_PercentageAboveOneHundred_IsRejected()
    {
        Assert.Throws<DomainRuleException>(() =>
            _calculator.Calculate(
                [Line("Servicio", 1m, 100m, DiscountType.Percentage, 101m, 16m)],
                DiscountType.None,
                0m,
                DiscountType.None,
                0m));
    }

    [Fact]
    public void Calculate_WithOnlyOptionalLines_IsRejected()
    {
        Assert.Throws<DomainRuleException>(() =>
            _calculator.Calculate(
                [Line("Opcional", 1m, 100m, DiscountType.None, 0m, 16m, true)],
                DiscountType.None,
                0m,
                DiscountType.None,
                0m));
    }

    [Fact]
    public void Calculate_DistributesSharedDiscountWithoutRoundingDrift()
    {
        var calculation = _calculator.Calculate(
            [
                Line("A", 1m, 33.33m, DiscountType.None, 0m, 16m),
                Line("B", 1m, 33.33m, DiscountType.None, 0m, 16m),
                Line("C", 1m, 33.34m, DiscountType.None, 0m, 16m)
            ],
            DiscountType.FixedAmount,
            10m,
            DiscountType.None,
            0m);

        Assert.Equal(10m, calculation.GeneralDiscountTotal);
        Assert.Equal(10m, calculation.Lines.Sum(line => line.LineDiscount));
        Assert.Equal(calculation.GrandTotal, calculation.Lines.Sum(line => line.LineTotal));
    }

    private static ProposalCalculationLine Line(
        string description,
        decimal quantity,
        decimal unitPrice,
        DiscountType discountType,
        decimal discountValue,
        decimal taxRate,
        bool isOptional = false) =>
        new(
            Guid.NewGuid(),
            description,
            null,
            null,
            quantity,
            unitPrice,
            discountType,
            discountValue,
            taxRate,
            isOptional,
            0);
}
