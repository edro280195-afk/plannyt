using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class GuestDietaryAndAccessibility : ITenantEntity
{
    private GuestDietaryAndAccessibility() { }

    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid EventGuestId { get; private set; }
    public string? Allergies { get; private set; }
    public string? DietaryRestrictions { get; private set; }
    public string? AccessibilityRequirements { get; private set; }
    public string? AdditionalNotes { get; private set; }
    public DateTimeOffset? ConsentGrantedAt { get; private set; }
    public Guid? LastSubmissionId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static GuestDietaryAndAccessibility Create(
        Guid organizationId,
        Guid eventId,
        Guid eventGuestId,
        DateTimeOffset now)
    {
        return new GuestDietaryAndAccessibility
        {
            OrganizationId = organizationId,
            EventId = eventId,
            EventGuestId = eventGuestId,
            UpdatedAt = now
        };
    }

    public void Update(
        string? allergies,
        string? dietaryRestrictions,
        string? accessibilityRequirements,
        string? additionalNotes,
        Guid lastSubmissionId,
        DateTimeOffset now)
    {
        Allergies = allergies;
        DietaryRestrictions = dietaryRestrictions;
        AccessibilityRequirements = accessibilityRequirements;
        AdditionalNotes = additionalNotes;
        LastSubmissionId = lastSubmissionId;
        UpdatedAt = now;
    }

    public void SetConsent(bool granted, DateTimeOffset now)
    {
        ConsentGrantedAt = granted ? now : null;
        UpdatedAt = now;
    }

    public void GrantConsent(DateTimeOffset now) => SetConsent(true, now);
}
