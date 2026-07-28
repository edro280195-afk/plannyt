using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Contracts.Domain;

public sealed class ContractTemplate : ITenantEntity
{
    private ContractTemplate()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public ContractContentFormat ContentFormat { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static ContractTemplate Create(
        Guid organizationId,
        string name,
        string? description,
        string content,
        bool isDefault,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            Content = content,
            ContentFormat = ContractContentFormat.SanitizedHtml,
            IsDefault = isDefault,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        string name,
        string? description,
        string content,
        bool isDefault,
        bool isActive,
        DateTimeOffset now)
    {
        if (ArchivedAt is not null)
        {
            throw new DomainRuleException("Una plantilla archivada no admite cambios.");
        }

        Name = name;
        Description = description;
        Content = content;
        IsDefault = isDefault;
        IsActive = isActive;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        ArchivedAt ??= now;
        IsActive = false;
        IsDefault = false;
        UpdatedAt = now;
    }
}
