using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class RsvpFormVersion : ITenantEntity
{
    private RsvpFormVersion() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid RsvpFormId { get; private set; }
    public int VersionNumber { get; private set; }
    public string SettingsSnapshot { get; private set; } = string.Empty;
    public string QuestionsSnapshot { get; private set; } = string.Empty;
    public string MenuSnapshot { get; private set; } = string.Empty;
    public string TransportSnapshot { get; private set; } = string.Empty;
    public string AccommodationSnapshot { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public static RsvpFormVersion Create(
        Guid organizationId,
        Guid rsvpFormId,
        int versionNumber,
        string settingsSnapshot,
        string questionsSnapshot,
        string menuSnapshot,
        string transportSnapshot,
        string accommodationSnapshot,
        Guid createdBy,
        DateTimeOffset now)
    {
        return new RsvpFormVersion
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            RsvpFormId = rsvpFormId,
            VersionNumber = versionNumber,
            SettingsSnapshot = settingsSnapshot,
            QuestionsSnapshot = questionsSnapshot,
            MenuSnapshot = menuSnapshot,
            TransportSnapshot = transportSnapshot,
            AccommodationSnapshot = accommodationSnapshot,
            CreatedBy = createdBy,
            CreatedAt = now
        };
    }

    public void Approve(Guid approvedBy, DateTimeOffset now)
    {
        ApprovedBy = approvedBy;
        ApprovedAt = now;
    }

    public void Publish(DateTimeOffset now)
    {
        if (ApprovedAt is null)
        {
            throw new DomainRuleException("La versión debe estar aprobada antes de publicarse.");
        }

        PublishedAt = now;
    }

    public bool IsPublished => PublishedAt is not null;
}
