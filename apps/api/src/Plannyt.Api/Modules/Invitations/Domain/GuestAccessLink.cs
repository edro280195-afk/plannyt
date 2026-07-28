using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Invitations.Domain;

public sealed class GuestAccessLink : ITenantEntity
{
    private GuestAccessLink()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid InvitationGroupId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public GuestAccessLinkStatus Status { get; private set; }
    public Guid? ReplacedByLinkId { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? FirstOpenedAt { get; private set; }
    public DateTimeOffset? LastOpenedAt { get; private set; }
    public int OpenCount { get; private set; }
    public DateTimeOffset? SharedManuallyAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static GuestAccessLink Create(
        Guid id,
        Guid organizationId,
        Guid eventId,
        Guid invitationGroupId,
        string tokenHash,
        DateTimeOffset? expiresAt,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = id,
            OrganizationId = organizationId,
            EventId = eventId,
            InvitationGroupId = invitationGroupId,
            TokenHash = tokenHash,
            Status = GuestAccessLinkStatus.Active,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy,
            CreatedAt = now
        };

    public void RegisterOpen(DateTimeOffset now)
    {
        FirstOpenedAt ??= now;
        LastOpenedAt = now;
        OpenCount++;
    }

    public void MarkShared(DateTimeOffset now) => SharedManuallyAt = now;

    public void ReplaceWith(Guid linkId, DateTimeOffset now)
    {
        Status = GuestAccessLinkStatus.Replaced;
        ReplacedByLinkId = linkId;
        RevokedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        Status = GuestAccessLinkStatus.Revoked;
        RevokedAt = now;
    }

    public bool IsExpired(DateTimeOffset now) => ExpiresAt is not null && ExpiresAt <= now;
}
