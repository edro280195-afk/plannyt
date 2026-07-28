using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Crm.Domain;

public sealed class ClientContact : ITenantEntity
{
    private ClientContact()
    {
    }

    private ClientContact(
        Guid id,
        Guid organizationId,
        Guid clientId,
        Guid personId,
        string contactRole,
        bool isPrimary,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        ClientId = clientId;
        PersonId = personId;
        ContactRole = contactRole;
        IsPrimary = isPrimary;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ClientId { get; private set; }

    public Guid PersonId { get; private set; }

    public string ContactRole { get; private set; } = string.Empty;

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ClientContact Create(
        Guid organizationId,
        Guid clientId,
        Guid personId,
        string contactRole,
        bool isPrimary,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            organizationId,
            clientId,
            personId,
            contactRole,
            isPrimary,
            now);

    public void Update(string contactRole, bool isPrimary, DateTimeOffset now)
    {
        ContactRole = contactRole;
        IsPrimary = isPrimary;
        UpdatedAt = now;
    }
}
