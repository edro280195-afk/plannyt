using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Organizations.Domain;

public sealed class Person : ITenantEntity
{
    private Person()
    {
    }

    private Person(
        Guid id,
        Guid organizationId,
        Guid? linkedUserAccountId,
        string firstName,
        string lastName,
        string displayName,
        string? contactEmail,
        string? contactPhone,
        string preferredLanguage,
        string timeZone,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        LinkedUserAccountId = linkedUserAccountId;
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid? LinkedUserAccountId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? ContactEmail { get; private set; }

    public string? ContactPhone { get; private set; }

    public string PreferredLanguage { get; private set; } = "es";

    public string TimeZone { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Person Create(
        Guid organizationId,
        Guid? linkedUserAccountId,
        string firstName,
        string lastName,
        string displayName,
        string? contactEmail,
        string? contactPhone,
        string preferredLanguage,
        string timeZone,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            organizationId,
            linkedUserAccountId,
            firstName,
            lastName,
            displayName,
            contactEmail,
            contactPhone,
            preferredLanguage,
            timeZone,
            now);

    public void UpdateProfile(
        string firstName,
        string lastName,
        string displayName,
        string? contactEmail,
        string? contactPhone,
        string preferredLanguage,
        string timeZone,
        DateTimeOffset now)
    {
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        PreferredLanguage = preferredLanguage;
        TimeZone = timeZone;
        UpdatedAt = now;
    }
}
