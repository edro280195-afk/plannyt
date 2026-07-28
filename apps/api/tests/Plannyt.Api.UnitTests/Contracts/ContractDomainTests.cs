using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Contracts.Domain;

namespace Plannyt.Api.UnitTests.Contracts;

public sealed class ContractDomainTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PublishedVersion_CannotBeEdited()
    {
        var version = ContractVersion.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            null,
            null,
            "<p>Contrato</p>",
            "Acepto utilizar medios electrónicos.",
            Now.AddDays(7),
            Guid.NewGuid(),
            Now);
        version.Publish(
            "2026/07/document.pdf",
            "contrato.pdf",
            128,
            new string('A', 64),
            Now);

        var exception = Assert.Throws<DomainRuleException>(() =>
            version.UpdateDraft(
                null,
                "<p>Cambio silencioso</p>",
                "Otro consentimiento",
                Now.AddDays(8)));

        Assert.Contains("inmutable", exception.Message);
    }

    [Theory]
    [InlineData(DepositRequirementType.None, 0, 10000, 0)]
    [InlineData(DepositRequirementType.FixedAmount, 2500, 10000, 2500)]
    [InlineData(DepositRequirementType.PercentageOfContract, 20, 12345, 2469)]
    public void RequirementSnapshot_CalculatesDeposit(
        DepositRequirementType type,
        decimal value,
        decimal total,
        decimal expected)
    {
        var policy = OrganizationContractingPolicy.CreateDefault(
            Guid.NewGuid(),
            Now);
        policy.Update(
            true,
            true,
            type,
            value,
            ConfirmationMode.ManualAfterRequirements,
            Now);

        var snapshot = ContractingRequirementSnapshot.Create(
            policy.OrganizationId,
            Guid.NewGuid(),
            policy,
            total,
            "MXN",
            Now);

        Assert.Equal(expected, snapshot.RequiredDepositAmount);
    }

    [Fact]
    public void CancelledContract_CannotBeSigned()
    {
        var contract = Contract.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "C-20260728-ABC123",
            "Contrato",
            ContractSourceType.Manual,
            10000,
            "MXN",
            Guid.NewGuid(),
            Now);
        contract.Cancel("El cliente desistió.", Now);

        Assert.Throws<DomainRuleException>(() =>
            contract.MarkPartiallySigned(Now));
    }

    [Fact]
    public void GeneratedContract_RequiresProposalAndExactVersion()
    {
        Assert.Throws<DomainRuleException>(() =>
            Contract.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                "C-20260728-ABC124",
                "Contrato",
                ContractSourceType.GeneratedFromProposal,
                10000m,
                "MXN",
                Guid.NewGuid(),
                Now));
    }

    [Fact]
    public void CompletedContract_CannotReturnToDraft()
    {
        var contract = Contract.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "C-20260728-ABC125",
            "Contrato externo",
            ContractSourceType.ExternalUpload,
            10000m,
            "MXN",
            Guid.NewGuid(),
            Now);
        contract.MarkPublished(Now);
        contract.Complete(Now);

        Assert.Throws<DomainRuleException>(() =>
            contract.RenameDraft("Cambio", Now.AddMinutes(1)));
        Assert.Throws<DomainRuleException>(() =>
            contract.RecordVersion(2, Now.AddMinutes(1)));
    }

    [Fact]
    public void PercentageSnapshot_RoundsAwayFromZero()
    {
        var policy = OrganizationContractingPolicy.CreateDefault(
            Guid.NewGuid(),
            Now);
        policy.Update(
            true,
            true,
            DepositRequirementType.PercentageOfContract,
            33.333m,
            ConfirmationMode.Automatic,
            Now);

        var snapshot = ContractingRequirementSnapshot.Create(
            policy.OrganizationId,
            Guid.NewGuid(),
            policy,
            100m,
            "MXN",
            Now);

        Assert.Equal(33.33m, snapshot.RequiredDepositAmount);
        Assert.Equal(ConfirmationMode.Automatic, snapshot.ConfirmationMode);
    }
}
