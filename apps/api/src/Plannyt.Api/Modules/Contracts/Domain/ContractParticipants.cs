using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Contracts.Domain;

public sealed class ContractParty : ITenantEntity
{
    private ContractParty()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ContractId { get; private set; }

    public ContractPartyType PartyType { get; private set; }

    public Guid? ClientId { get; private set; }

    public Guid? OrganizationPartyId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string? LegalName { get; private set; }

    public string? TaxId { get; private set; }

    public string? Address { get; private set; }

    public int SortOrder { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ContractParty Create(
        Guid organizationId,
        Guid contractId,
        ContractPartyType partyType,
        Guid? clientId,
        Guid? organizationPartyId,
        string displayName,
        string? legalName,
        string? taxId,
        string? address,
        int sortOrder,
        DateTimeOffset now)
    {
        if (partyType == ContractPartyType.Client && clientId is null
            || partyType == ContractPartyType.PlannerOrganization
            && organizationPartyId is null)
        {
            throw new DomainRuleException(
                "La parte contractual no corresponde con su tipo.");
        }

        return new ContractParty
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ContractId = contractId,
            PartyType = partyType,
            ClientId = clientId,
            OrganizationPartyId = organizationPartyId,
            DisplayName = displayName,
            LegalName = legalName,
            TaxId = taxId,
            Address = address,
            SortOrder = sortOrder,
            CreatedAt = now
        };
    }
}

public sealed class ContractSigner : ITenantEntity
{
    private ContractSigner()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ContractId { get; private set; }

    public Guid ContractPartyId { get; private set; }

    public Guid? PersonId { get; private set; }

    public Guid? UserAccountId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string SignerRole { get; private set; } = string.Empty;

    public int SigningOrder { get; private set; }

    public bool IsRequired { get; private set; }

    public ContractSignerStatus Status { get; private set; }

    public DateTimeOffset? SignedAt { get; private set; }

    public DateTimeOffset? DeclinedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ContractSigner Create(
        Guid organizationId,
        Guid contractId,
        Guid contractPartyId,
        Guid? personId,
        Guid? userAccountId,
        string name,
        string email,
        string signerRole,
        int signingOrder,
        bool isRequired,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ContractId = contractId,
            ContractPartyId = contractPartyId,
            PersonId = personId,
            UserAccountId = userAccountId,
            Name = name,
            Email = email,
            SignerRole = signerRole,
            SigningOrder = signingOrder,
            IsRequired = isRequired,
            Status = ContractSignerStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        Guid contractPartyId,
        Guid? personId,
        Guid? userAccountId,
        string name,
        string email,
        string signerRole,
        int signingOrder,
        bool isRequired,
        DateTimeOffset now)
    {
        EnsureUnsigned();
        ContractPartyId = contractPartyId;
        PersonId = personId;
        UserAccountId = userAccountId;
        Name = name;
        Email = email;
        SignerRole = signerRole;
        SigningOrder = signingOrder;
        IsRequired = isRequired;
        UpdatedAt = now;
    }

    public void MarkInvited(DateTimeOffset now)
    {
        EnsureUnsigned();
        Status = ContractSignerStatus.Invited;
        UpdatedAt = now;
    }

    public void MarkViewed(DateTimeOffset now)
    {
        EnsureUnsigned();
        Status = ContractSignerStatus.Viewed;
        UpdatedAt = now;
    }

    public void Sign(DateTimeOffset now)
    {
        EnsureUnsigned();
        Status = ContractSignerStatus.Signed;
        SignedAt = now;
        UpdatedAt = now;
    }

    public void Decline(DateTimeOffset now)
    {
        EnsureUnsigned();
        Status = ContractSignerStatus.Declined;
        DeclinedAt = now;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        EnsureUnsigned();
        Status = ContractSignerStatus.Revoked;
        UpdatedAt = now;
    }

    public void ResetForNewVersion(DateTimeOffset now)
    {
        Status = ContractSignerStatus.Pending;
        SignedAt = null;
        DeclinedAt = null;
        UpdatedAt = now;
    }

    public void EnsureUnsigned()
    {
        if (Status is ContractSignerStatus.Signed or ContractSignerStatus.Declined)
        {
            throw new DomainRuleException(
                "Un firmante que ya respondió no admite cambios.");
        }
    }
}
