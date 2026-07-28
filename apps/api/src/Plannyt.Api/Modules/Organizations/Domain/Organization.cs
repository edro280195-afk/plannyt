namespace Plannyt.Api.Modules.Organizations.Domain;

public sealed class Organization
{
    private Organization()
    {
    }

    private Organization(
        Guid id,
        string name,
        string slug,
        OrganizationType organizationType,
        string timeZone,
        string countryCode,
        string currencyCode,
        DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Slug = slug;
        OrganizationType = organizationType;
        TimeZone = timeZone;
        CountryCode = countryCode;
        CurrencyCode = currencyCode;
        Status = OrganizationStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public OrganizationType OrganizationType { get; private set; }

    public string TimeZone { get; private set; } = string.Empty;

    public string CountryCode { get; private set; } = string.Empty;

    public string CurrencyCode { get; private set; } = string.Empty;

    public OrganizationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Organization Create(
        string name,
        string slug,
        OrganizationType organizationType,
        string timeZone,
        string countryCode,
        string currencyCode,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            name,
            slug,
            organizationType,
            timeZone,
            countryCode,
            currencyCode,
            now);
}
