using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Events.Domain;

public sealed class Event : ITenantEntity
{
    private Event()
    {
    }

    private Event(
        Guid id,
        Guid organizationId,
        string name,
        string eventType,
        DateTimeOffset startDateTime,
        DateTimeOffset? endDateTime,
        string timeZone,
        string city,
        string countryCode,
        string? sharedDescription,
        int? estimatedGuestCount,
        Guid createdBy,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        EventType = eventType;
        Status = EventStatus.Preliminary;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        TimeZone = timeZone;
        City = city;
        CountryCode = countryCode;
        SharedDescription = sharedDescription;
        EstimatedGuestCount = estimatedGuestCount;
        CreatedBy = createdBy;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    public EventStatus Status { get; private set; }

    public EventStatus? StatusBeforeSuspension { get; private set; }

    public DateTimeOffset StartDateTime { get; private set; }

    public DateTimeOffset? EndDateTime { get; private set; }

    public string TimeZone { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string CountryCode { get; private set; } = string.Empty;

    public int? EstimatedGuestCount { get; private set; }

    public string? SharedDescription { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? ArchivedAt { get; private set; }

    public static Event Create(
        Guid organizationId,
        string name,
        string eventType,
        DateTimeOffset startDateTime,
        DateTimeOffset? endDateTime,
        string timeZone,
        string city,
        string countryCode,
        string? sharedDescription,
        int? estimatedGuestCount,
        Guid createdBy,
        DateTimeOffset now)
    {
        ValidateDates(startDateTime, endDateTime);

        if (estimatedGuestCount < 0)
        {
            throw new DomainRuleException(
                "La cantidad estimada de invitados no puede ser negativa.");
        }

        return new Event(
            Guid.NewGuid(),
            organizationId,
            name,
            eventType,
            startDateTime,
            endDateTime,
            timeZone,
            city,
            countryCode,
            sharedDescription,
            estimatedGuestCount,
            createdBy,
            now);
    }

    public void UpdateDetails(
        string name,
        string eventType,
        DateTimeOffset startDateTime,
        DateTimeOffset? endDateTime,
        string timeZone,
        string city,
        string countryCode,
        string? sharedDescription,
        int? estimatedGuestCount,
        DateTimeOffset now)
    {
        if (Status == EventStatus.Archived)
        {
            throw new DomainRuleException("Un evento archivado no admite cambios normales.");
        }

        ValidateDates(startDateTime, endDateTime);

        if (estimatedGuestCount < 0)
        {
            throw new DomainRuleException(
                "La cantidad estimada de invitados no puede ser negativa.");
        }

        Name = name;
        EventType = eventType;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        TimeZone = timeZone;
        City = city;
        CountryCode = countryCode;
        SharedDescription = sharedDescription;
        EstimatedGuestCount = estimatedGuestCount;
        UpdatedAt = now;
    }

    internal void ApplyStatus(EventStatus newStatus, DateTimeOffset now)
    {
        if (newStatus == EventStatus.Suspended)
        {
            StatusBeforeSuspension = Status;
        }
        else if (Status == EventStatus.Suspended)
        {
            StatusBeforeSuspension = null;
        }

        Status = newStatus;
        ArchivedAt = newStatus == EventStatus.Archived ? now : null;
        UpdatedAt = now;
    }

    private static void ValidateDates(
        DateTimeOffset startDateTime,
        DateTimeOffset? endDateTime)
    {
        if (endDateTime < startDateTime)
        {
            throw new DomainRuleException(
                "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }
    }
}
