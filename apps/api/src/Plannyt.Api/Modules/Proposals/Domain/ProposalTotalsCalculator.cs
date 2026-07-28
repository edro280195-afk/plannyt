using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Catalog.Domain;

namespace Plannyt.Api.Modules.Proposals.Domain;

public sealed record ProposalCalculationLine(
    Guid DraftLineId,
    string Description,
    Guid? ServiceCatalogItemId,
    Guid? PackageId,
    decimal Quantity,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal TaxRate,
    bool IsOptional,
    int SortOrder);

public sealed record CalculatedProposalLine(
    ProposalCalculationLine Source,
    decimal LineSubtotal,
    decimal LineDiscount,
    decimal LineTax,
    decimal LineTotal);

public sealed record ProposalCalculation(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal GeneralDiscountTotal,
    decimal CouponDiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal,
    IReadOnlyList<CalculatedProposalLine> Lines);

public sealed class ProposalTotalsCalculator
{
    public ProposalCalculation Calculate(
        IReadOnlyCollection<ProposalCalculationLine> sourceLines,
        DiscountType generalDiscountType,
        decimal generalDiscountValue,
        DiscountType couponDiscountType,
        decimal couponDiscountValue)
    {
        if (sourceLines.Count == 0)
        {
            throw new DomainRuleException(
                "La propuesta requiere al menos un concepto.");
        }

        var working = sourceLines
            .OrderBy(line => line.SortOrder)
            .Select(CreateWorkingLine)
            .ToList();
        var included = working.Where(line => !line.Source.IsOptional).ToList();
        if (included.Count == 0)
        {
            throw new DomainRuleException(
                "La propuesta requiere al menos un concepto no opcional.");
        }

        var subtotal = Money(included.Sum(line => line.Subtotal));
        var postLineDiscount = Money(included.Sum(line => line.NetAfterLineDiscount));
        var generalDiscountTotal = CalculateDiscount(
            postLineDiscount,
            generalDiscountType,
            generalDiscountValue);
        var afterGeneral = Money(postLineDiscount - generalDiscountTotal);
        var couponDiscountTotal = CalculateDiscount(
            afterGeneral,
            couponDiscountType,
            couponDiscountValue);
        var sharedDiscount = Money(generalDiscountTotal + couponDiscountTotal);

        AllocateSharedDiscount(included, sharedDiscount);

        var results = working
            .Select(line =>
            {
                var taxable = Money(
                    line.NetAfterLineDiscount - line.AllocatedSharedDiscount);
                var tax = Money(taxable * line.Source.TaxRate / 100m);
                var total = Money(taxable + tax);
                return new CalculatedProposalLine(
                    line.Source,
                    line.Subtotal,
                    Money(line.LineDiscount + line.AllocatedSharedDiscount),
                    tax,
                    total);
            })
            .ToList();
        var includedResults = results.Where(line => !line.Source.IsOptional).ToList();
        var lineDiscountTotal = Money(
            working
                .Where(line => !line.Source.IsOptional)
                .Sum(line => line.LineDiscount));
        var discountTotal = Money(lineDiscountTotal + sharedDiscount);
        var taxTotal = Money(includedResults.Sum(line => line.LineTax));
        var grandTotal = Money(includedResults.Sum(line => line.LineTotal));

        if (grandTotal < 0)
        {
            throw new DomainRuleException(
                "Los descuentos no pueden producir un total negativo.");
        }

        return new ProposalCalculation(
            subtotal,
            discountTotal,
            generalDiscountTotal,
            couponDiscountTotal,
            taxTotal,
            grandTotal,
            results);
    }

    private static WorkingLine CreateWorkingLine(ProposalCalculationLine line)
    {
        if (line.Quantity <= 0 || line.UnitPrice < 0)
        {
            throw new DomainRuleException(
                "Cantidad y precio deben ser valores válidos.");
        }

        if (line.TaxRate is < 0 or > 100)
        {
            throw new DomainRuleException(
                "La tasa de impuesto debe estar entre 0 y 100.");
        }

        var subtotal = Money(line.Quantity * line.UnitPrice);
        var discount = CalculateDiscount(
            subtotal,
            line.DiscountType,
            line.DiscountValue);
        return new WorkingLine(line, subtotal, discount);
    }

    private static decimal CalculateDiscount(
        decimal baseAmount,
        DiscountType discountType,
        decimal discountValue)
    {
        if (discountValue < 0)
        {
            throw new DomainRuleException(
                "El descuento no puede ser negativo.");
        }

        var result = discountType switch
        {
            DiscountType.None => 0m,
            DiscountType.FixedAmount => discountValue,
            DiscountType.Percentage when discountValue <= 100m =>
                baseAmount * discountValue / 100m,
            DiscountType.Percentage =>
                throw new DomainRuleException(
                    "El porcentaje de descuento no puede exceder 100."),
            _ => throw new DomainRuleException("El tipo de descuento no es válido.")
        };

        return Money(Math.Min(baseAmount, result));
    }

    private static void AllocateSharedDiscount(
        IReadOnlyList<WorkingLine> included,
        decimal sharedDiscount)
    {
        if (sharedDiscount == 0)
        {
            return;
        }

        var baseAmount = included.Sum(line => line.NetAfterLineDiscount);
        var allocated = 0m;
        for (var index = 0; index < included.Count; index++)
        {
            var line = included[index];
            var amount = index == included.Count - 1
                ? Money(sharedDiscount - allocated)
                : Money(sharedDiscount * line.NetAfterLineDiscount / baseAmount);
            line.AllocatedSharedDiscount = amount;
            allocated += amount;
        }
    }

    private static decimal Money(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed class WorkingLine(
        ProposalCalculationLine source,
        decimal subtotal,
        decimal lineDiscount)
    {
        public ProposalCalculationLine Source { get; } = source;

        public decimal Subtotal { get; } = subtotal;

        public decimal LineDiscount { get; } = lineDiscount;

        public decimal NetAfterLineDiscount => Subtotal - LineDiscount;

        public decimal AllocatedSharedDiscount { get; set; }
    }
}
