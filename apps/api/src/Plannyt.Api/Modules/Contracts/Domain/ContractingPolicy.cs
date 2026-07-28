using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Contracts.Domain;

public sealed class OrganizationContractingPolicy : ITenantEntity
{
    private OrganizationContractingPolicy()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public bool RequireAcceptedProposal { get; private set; }

    public bool RequireCompletedContract { get; private set; }

    public DepositRequirementType DepositRequirementType { get; private set; }

    public decimal DepositRequirementValue { get; private set; }

    public ConfirmationMode ConfirmationMode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static OrganizationContractingPolicy CreateDefault(
        Guid organizationId,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RequireAcceptedProposal = true,
            RequireCompletedContract = true,
            DepositRequirementType = DepositRequirementType.PercentageOfContract,
            DepositRequirementValue = 20m,
            ConfirmationMode = ConfirmationMode.ManualAfterRequirements,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        bool requireAcceptedProposal,
        bool requireCompletedContract,
        DepositRequirementType depositRequirementType,
        decimal depositRequirementValue,
        ConfirmationMode confirmationMode,
        DateTimeOffset now)
    {
        ValidateDeposit(depositRequirementType, depositRequirementValue);
        RequireAcceptedProposal = requireAcceptedProposal;
        RequireCompletedContract = requireCompletedContract;
        DepositRequirementType = depositRequirementType;
        DepositRequirementValue = depositRequirementValue;
        ConfirmationMode = confirmationMode;
        UpdatedAt = now;
    }

    public static void ValidateDeposit(
        DepositRequirementType type,
        decimal value)
    {
        if (value < 0m
            || (type == DepositRequirementType.PercentageOfContract
                && value > 100m)
            || (type == DepositRequirementType.None && value != 0m))
        {
            throw new DomainRuleException(
                "La configuración del anticipo no es válida.");
        }
    }
}

public sealed class ContractingRequirementSnapshot : ITenantEntity
{
    private ContractingRequirementSnapshot()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ContractId { get; private set; }

    public bool RequireAcceptedProposal { get; private set; }

    public bool RequireCompletedContract { get; private set; }

    public DepositRequirementType DepositRequirementType { get; private set; }

    public decimal DepositRequirementValue { get; private set; }

    public decimal RequiredDepositAmount { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public ConfirmationMode ConfirmationMode { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ContractingRequirementSnapshot Create(
        Guid organizationId,
        Guid contractId,
        OrganizationContractingPolicy policy,
        decimal contractGrandTotal,
        string currencyCode,
        DateTimeOffset now)
    {
        var requiredAmount = policy.DepositRequirementType switch
        {
            DepositRequirementType.None => 0m,
            DepositRequirementType.FixedAmount =>
                decimal.Round(
                    policy.DepositRequirementValue,
                    2,
                    MidpointRounding.AwayFromZero),
            DepositRequirementType.PercentageOfContract =>
                decimal.Round(
                    contractGrandTotal * policy.DepositRequirementValue / 100m,
                    2,
                    MidpointRounding.AwayFromZero),
            _ => throw new DomainRuleException(
                "El tipo de requisito de anticipo no es válido.")
        };

        return new ContractingRequirementSnapshot
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ContractId = contractId,
            RequireAcceptedProposal = policy.RequireAcceptedProposal,
            RequireCompletedContract = policy.RequireCompletedContract,
            DepositRequirementType = policy.DepositRequirementType,
            DepositRequirementValue = policy.DepositRequirementValue,
            RequiredDepositAmount = requiredAmount,
            CurrencyCode = currencyCode,
            ConfirmationMode = policy.ConfirmationMode,
            CreatedAt = now
        };
    }
}
