using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Catalog.Domain;

public sealed class Package : ITenantEntity
{
    private Package()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal BasePrice { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public bool IsNegotiable { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Package Create(
        Guid organizationId,
        string name,
        string? description,
        decimal basePrice,
        string currencyCode,
        bool isNegotiable,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            BasePrice = basePrice,
            CurrencyCode = currencyCode,
            IsNegotiable = isNegotiable,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        string name,
        string? description,
        decimal basePrice,
        string currencyCode,
        bool isNegotiable,
        bool isActive,
        DateTimeOffset now)
    {
        if (ArchivedAt is not null)
        {
            throw new DomainRuleException(
                "Un paquete archivado no admite cambios.");
        }

        Name = name;
        Description = description;
        BasePrice = basePrice;
        CurrencyCode = currencyCode;
        IsNegotiable = isNegotiable;
        IsActive = isActive;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        IsActive = false;
        ArchivedAt = now;
        UpdatedAt = now;
    }
}
