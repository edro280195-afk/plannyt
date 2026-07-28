using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Crm.Domain;

public sealed class Prospect : ITenantEntity
{
    private Prospect()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? CompanyName { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? Source { get; private set; }

    public string? EventTypeInterest { get; private set; }

    public DateOnly? EstimatedEventDate { get; private set; }

    public int? EstimatedGuestCount { get; private set; }

    public decimal? EstimatedBudget { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public string? City { get; private set; }

    public string? Notes { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public ProspectStatus Status { get; private set; }

    public string? LostReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? ConvertedClientId { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Prospect Create(
        Guid organizationId,
        string displayName,
        string? firstName,
        string? lastName,
        string? companyName,
        string? email,
        string? phone,
        string? source,
        string? eventTypeInterest,
        DateOnly? estimatedEventDate,
        int? estimatedGuestCount,
        decimal? estimatedBudget,
        string currencyCode,
        string? city,
        string? notes,
        Guid? assignedUserId,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DisplayName = displayName,
            FirstName = firstName,
            LastName = lastName,
            CompanyName = companyName,
            Email = email,
            Phone = phone,
            Source = source,
            EventTypeInterest = eventTypeInterest,
            EstimatedEventDate = estimatedEventDate,
            EstimatedGuestCount = estimatedGuestCount,
            EstimatedBudget = estimatedBudget,
            CurrencyCode = currencyCode,
            City = city,
            Notes = notes,
            AssignedUserId = assignedUserId,
            Status = ProspectStatus.New,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        string displayName,
        string? firstName,
        string? lastName,
        string? companyName,
        string? email,
        string? phone,
        string? source,
        string? eventTypeInterest,
        DateOnly? estimatedEventDate,
        int? estimatedGuestCount,
        decimal? estimatedBudget,
        string currencyCode,
        string? city,
        string? notes,
        DateTimeOffset now)
    {
        EnsureEditable();
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        CompanyName = companyName;
        Email = email;
        Phone = phone;
        Source = source;
        EventTypeInterest = eventTypeInterest;
        EstimatedEventDate = estimatedEventDate;
        EstimatedGuestCount = estimatedGuestCount;
        EstimatedBudget = estimatedBudget;
        CurrencyCode = currencyCode;
        City = city;
        Notes = notes;
        UpdatedAt = now;
    }

    public void Assign(Guid? assignedUserId, DateTimeOffset now)
    {
        EnsureEditable();
        AssignedUserId = assignedUserId;
        UpdatedAt = now;
    }

    public void MarkConverted(Guid clientId, DateTimeOffset now)
    {
        if (ConvertedClientId is not null && ConvertedClientId != clientId)
        {
            throw new DomainRuleException(
                "El prospecto ya fue convertido a otro cliente.");
        }

        ConvertedClientId = clientId;
        UpdatedAt = now;
    }

    internal void ApplyStatus(
        ProspectStatus status,
        string? lostReason,
        DateTimeOffset now)
    {
        Status = status;
        LostReason = status == ProspectStatus.Lost ? lostReason : null;
        ArchivedAt = status == ProspectStatus.Archived ? now : null;
        UpdatedAt = now;
    }

    private void EnsureEditable()
    {
        if (Status == ProspectStatus.Archived)
        {
            throw new DomainRuleException(
                "Un prospecto archivado no admite cambios.");
        }
    }
}
