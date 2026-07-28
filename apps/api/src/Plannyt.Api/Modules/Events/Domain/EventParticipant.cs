using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Events.Domain;

public sealed class EventParticipant : ITenantEntity
{
    private EventParticipant()
    {
    }

    private EventParticipant(
        Guid id,
        Guid organizationId,
        Guid eventId,
        Guid personId,
        string participantType,
        int displayOrder,
        bool isVisibleToClient,
        string? sharedDescription,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        EventId = eventId;
        PersonId = personId;
        ParticipantType = participantType;
        DisplayOrder = displayOrder;
        IsVisibleToClient = isVisibleToClient;
        SharedDescription = sharedDescription;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid EventId { get; private set; }

    public Guid PersonId { get; private set; }

    public string ParticipantType { get; private set; } = string.Empty;

    public int DisplayOrder { get; private set; }

    public bool IsVisibleToClient { get; private set; }

    public string? SharedDescription { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static EventParticipant Create(
        Guid organizationId,
        Guid eventId,
        Guid personId,
        string participantType,
        int displayOrder,
        bool isVisibleToClient,
        string? sharedDescription,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            organizationId,
            eventId,
            personId,
            participantType,
            displayOrder,
            isVisibleToClient,
            sharedDescription,
            now);
}
