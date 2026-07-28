using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Invitations.Domain;

public sealed class InvitationTemplate : ITenantEntity
{
    private InvitationTemplate()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public bool IsGlobal { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ThemeJson { get; private set; } = "{}";
    public string ContentJson { get; private set; } = "[]";
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static InvitationTemplate CreateGlobal(
        Guid id,
        string name,
        string description,
        string themeJson,
        string contentJson,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            OrganizationId = Guid.Empty,
            IsGlobal = true,
            Name = name,
            Description = description,
            ThemeJson = themeJson,
            ContentJson = contentJson,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public static InvitationTemplate CreateForOrganization(
        Guid organizationId,
        string name,
        string description,
        string themeJson,
        string contentJson,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            ThemeJson = themeJson,
            ContentJson = contentJson,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        string name,
        string description,
        string themeJson,
        string contentJson,
        DateTimeOffset now)
    {
        Name = name;
        Description = description;
        ThemeJson = themeJson;
        ContentJson = contentJson;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}
