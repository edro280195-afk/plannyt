using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Crm.Domain;

public sealed class Client : ITenantEntity
{
    private Client()
    {
    }

    private Client(
        Guid id,
        Guid organizationId,
        ClientType clientType,
        Guid? personId,
        string? companyName,
        string displayName,
        string? source,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        ClientType = clientType;
        PersonId = personId;
        CompanyName = companyName;
        DisplayName = displayName;
        Status = ClientStatus.Active;
        Source = source;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public ClientType ClientType { get; private set; }

    public Guid? PersonId { get; private set; }

    public string? CompanyName { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public ClientStatus Status { get; private set; }

    public string? Source { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Client CreatePerson(
        Guid organizationId,
        Guid personId,
        string displayName,
        string? source,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            organizationId,
            ClientType.Person,
            personId,
            null,
            displayName,
            source,
            now);

    public static Client CreateCompany(
        Guid organizationId,
        string companyName,
        string displayName,
        string? source,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new DomainRuleException("El nombre de la empresa es obligatorio.");
        }

        return new Client(
            Guid.NewGuid(),
            organizationId,
            ClientType.Company,
            null,
            companyName,
            displayName,
            source,
            now);
    }

    public void Update(
        string displayName,
        string? companyName,
        string? source,
        DateTimeOffset now)
    {
        if (ClientType == ClientType.Company && string.IsNullOrWhiteSpace(companyName))
        {
            throw new DomainRuleException("El nombre de la empresa es obligatorio.");
        }

        DisplayName = displayName;
        CompanyName = ClientType == ClientType.Company ? companyName : null;
        Source = source;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = ClientStatus.Archived;
        ArchivedAt = now;
        UpdatedAt = now;
    }
}
