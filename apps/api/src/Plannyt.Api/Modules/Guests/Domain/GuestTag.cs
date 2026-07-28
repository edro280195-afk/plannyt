using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Guests.Domain;

public sealed class GuestTag : ITenantEntity
{
    private GuestTag()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ColorToken { get; private set; } = "slate";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public static GuestTag Create(
        Guid organizationId,
        Guid eventId,
        string name,
        string colorToken,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            Name = name,
            ColorToken = colorToken,
            CreatedAt = now
        };

    public void Archive(DateTimeOffset now) => ArchivedAt = now;

    public void Update(string name, string colorToken)
    {
        Name = name;
        ColorToken = colorToken;
    }
}

public sealed class InvitationGroupTag : ITenantEntity
{
    private InvitationGroupTag()
    {
    }

    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid InvitationGroupId { get; private set; }
    public Guid GuestTagId { get; private set; }

    public static InvitationGroupTag Create(
        Guid organizationId,
        Guid eventId,
        Guid invitationGroupId,
        Guid guestTagId) =>
        new()
        {
            OrganizationId = organizationId,
            EventId = eventId,
            InvitationGroupId = invitationGroupId,
            GuestTagId = guestTagId
        };
}
