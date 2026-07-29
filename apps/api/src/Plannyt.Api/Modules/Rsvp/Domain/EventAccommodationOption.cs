using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class EventAccommodationOption : ITenantEntity
{
    private EventAccommodationOption() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Address { get; private set; }
    public string? BookingUrl { get; private set; }
    public string? BookingCode { get; private set; }
    public DateTimeOffset? BookingDeadline { get; private set; }
    public string? ContactInformation { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static EventAccommodationOption Create(
        Guid organizationId,
        Guid eventId,
        string name,
        string? description,
        string? address,
        string? bookingUrl,
        string? bookingCode,
        DateTimeOffset? bookingDeadline,
        string? contactInformation,
        int sortOrder,
        DateTimeOffset now)
    {
        return new EventAccommodationOption
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            Name = name,
            Description = description,
            Address = address,
            BookingUrl = bookingUrl,
            BookingCode = bookingCode,
            BookingDeadline = bookingDeadline,
            ContactInformation = contactInformation,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
