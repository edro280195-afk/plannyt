using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Payments.Domain;

namespace Plannyt.Api.UnitTests.Payments;

public sealed class PaymentDomainTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ActivatePlan_RequiresExactInstallmentTotal()
    {
        var plan = PaymentPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "MXN",
            10000m,
            Guid.NewGuid(),
            Now);

        Assert.Throws<DomainRuleException>(() =>
            plan.Activate(9999.99m, Now));
    }

    [Fact]
    public void PaidInstallment_CannotBeEdited()
    {
        var installment = PaymentInstallment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Anticipo",
            new DateOnly(2026, 8, 1),
            2000m,
            InstallmentType.Deposit,
            Now);
        installment.RefreshStatus(
            2000m,
            new DateOnly(2026, 7, 28),
            Now);

        Assert.Throws<DomainRuleException>(() =>
            installment.UpdateDraft(
                1,
                "Anticipo cambiado",
                new DateOnly(2026, 8, 2),
                2100m,
                InstallmentType.Deposit,
                Now));
    }

    [Fact]
    public void ClientSubmittedPayment_CannotApproveItself()
    {
        var userId = Guid.NewGuid();
        var payment = PaymentRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 7, 28),
            2000m,
            "MXN",
            PaymentMethod.BankTransfer,
            "ABC-123",
            null,
            null,
            userId,
            true,
            Now);

        Assert.Throws<DomainRuleException>(() =>
            payment.Approve(userId, Now));
    }

    [Fact]
    public void ActivatePlan_FreezesTotalAndPreventsLaterChanges()
    {
        var plan = PaymentPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "MXN",
            10000.005m,
            Guid.NewGuid(),
            Now);

        plan.Activate(10000.01m, Now);

        Assert.Equal(PaymentPlanStatus.Active, plan.Status);
        Assert.Equal(10000.01m, plan.ActivatedTotalAmount);
        Assert.Throws<DomainRuleException>(() =>
            plan.UpdateDraft(11000m, Now.AddMinutes(1)));
    }

    [Theory]
    [InlineData(0, "PartiallyPaid")]
    [InlineData(2000, "Paid")]
    public void Installment_ReflectsApprovedPartialPayments(
        decimal approvedAmount,
        string expectedStatus)
    {
        var installment = PaymentInstallment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Anticipo",
            new DateOnly(2026, 8, 1),
            2000m,
            InstallmentType.Deposit,
            Now);
        var amount = approvedAmount == 0m ? 500m : approvedAmount;

        installment.RefreshStatus(
            amount,
            new DateOnly(2026, 7, 28),
            Now);

        Assert.Equal(expectedStatus, installment.Status.ToString());
    }

    [Fact]
    public void UnpaidPastInstallment_BecomesOverdue()
    {
        var installment = PaymentInstallment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Anticipo",
            new DateOnly(2026, 7, 1),
            2000m,
            InstallmentType.Deposit,
            Now);

        installment.RefreshStatus(
            0m,
            new DateOnly(2026, 7, 28),
            Now);

        Assert.Equal(PaymentInstallmentStatus.Overdue, installment.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Allocation_RequiresPositiveAmount(decimal amount)
    {
        Assert.Throws<DomainRuleException>(() =>
            PaymentAllocation.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                amount,
                Now));
    }

    [Fact]
    public void RejectedPayment_CannotBeApproved()
    {
        var payment = PaymentRecord.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            new DateOnly(2026, 7, 28),
            2000m,
            "MXN",
            PaymentMethod.BankTransfer,
            null,
            null,
            null,
            Guid.NewGuid(),
            false,
            Now);
        payment.Reject(Guid.NewGuid(), "Referencia no localizada.", Now);

        Assert.Throws<DomainRuleException>(() =>
            payment.Approve(Guid.NewGuid(), Now.AddMinutes(1)));
    }
}
