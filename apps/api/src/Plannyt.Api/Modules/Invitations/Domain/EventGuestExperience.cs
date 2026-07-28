using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Invitations.Domain;

public sealed class EventGuestExperience : ITenantEntity
{
    private EventGuestExperience()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public GuestExperienceStatus Status { get; private set; }
    public string Language { get; private set; } = "es";
    public string PublicTitle { get; private set; } = string.Empty;
    public string CelebrantDisplayName { get; private set; } = string.Empty;
    public string? WelcomeMessage { get; private set; }
    public string? ClosingMessage { get; private set; }
    public bool ShowEventName { get; private set; }
    public bool ShowEventDate { get; private set; }
    public bool ShowParticipantNames { get; private set; }
    public bool ShowCity { get; private set; }
    public bool PrivateAccessOnly { get; private set; }
    public Guid? ActiveInvitationDesignId { get; private set; }
    public Guid? ActiveVersionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }

    public static EventGuestExperience Create(
        Guid organizationId,
        Guid eventId,
        string publicTitle,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            Status = GuestExperienceStatus.Draft,
            PublicTitle = publicTitle,
            CelebrantDisplayName = publicTitle,
            ShowEventName = true,
            ShowEventDate = true,
            ShowParticipantNames = true,
            ShowCity = true,
            PrivateAccessOnly = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void UpdateSettings(
        string language,
        string publicTitle,
        string celebrantDisplayName,
        string? welcomeMessage,
        string? closingMessage,
        bool showEventName,
        bool showEventDate,
        bool showParticipantNames,
        bool showCity,
        bool privateAccessOnly,
        DateTimeOffset now)
    {
        if (Status == GuestExperienceStatus.Archived)
        {
            throw new DomainRuleException("La experiencia archivada no admite cambios.");
        }

        Language = language;
        PublicTitle = publicTitle;
        CelebrantDisplayName = celebrantDisplayName;
        WelcomeMessage = welcomeMessage;
        ClosingMessage = closingMessage;
        ShowEventName = showEventName;
        ShowEventDate = showEventDate;
        ShowParticipantNames = showParticipantNames;
        ShowCity = showCity;
        PrivateAccessOnly = privateAccessOnly;
        UpdatedAt = now;
    }

    public void MarkReady(DateTimeOffset now)
    {
        if (Status == GuestExperienceStatus.Draft)
        {
            Status = GuestExperienceStatus.Ready;
            UpdatedAt = now;
        }
    }

    public void Publish(Guid designId, Guid versionId, DateTimeOffset now)
    {
        Status = GuestExperienceStatus.Published;
        ActiveInvitationDesignId = designId;
        ActiveVersionId = versionId;
        PublishedAt = now;
        SuspendedAt = null;
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        if (Status != GuestExperienceStatus.Published)
        {
            throw new DomainRuleException("Solo una experiencia publicada puede suspenderse.");
        }

        Status = GuestExperienceStatus.Suspended;
        SuspendedAt = now;
        UpdatedAt = now;
    }

    public void Resume(DateTimeOffset now)
    {
        if (Status != GuestExperienceStatus.Suspended || ActiveVersionId is null)
        {
            throw new DomainRuleException("La experiencia no puede reanudarse.");
        }

        Status = GuestExperienceStatus.Published;
        SuspendedAt = null;
        UpdatedAt = now;
    }
}
