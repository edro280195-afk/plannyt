using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Access.Domain;

public sealed class EventAccess : ITenantEntity
{
    private EventAccess()
    {
    }

    private EventAccess(
        Guid id,
        Guid organizationId,
        Guid eventId,
        Guid userAccountId,
        EventAccessRole baseRole,
        DateTimeOffset startsAt,
        DateTimeOffset? expiresAt,
        Guid invitedBy,
        DateTimeOffset acceptedAt,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        EventId = eventId;
        UserAccountId = userAccountId;
        BaseRole = baseRole;
        Status = EventAccessStatus.Active;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
        InvitedBy = invitedBy;
        AcceptedAt = acceptedAt;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid EventId { get; private set; }

    public Guid UserAccountId { get; private set; }

    public EventAccessRole BaseRole { get; private set; }

    public EventAccessStatus Status { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public Guid InvitedBy { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static EventAccess CreateAccepted(
        Guid organizationId,
        Guid eventId,
        Guid userAccountId,
        EventAccessRole baseRole,
        DateTimeOffset startsAt,
        DateTimeOffset? expiresAt,
        Guid invitedBy,
        DateTimeOffset acceptedAt,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            organizationId,
            eventId,
            userAccountId,
            baseRole,
            startsAt,
            expiresAt,
            invitedBy,
            acceptedAt,
            now);

    public bool IsActiveAt(DateTimeOffset now) =>
        Status == EventAccessStatus.Active
        && StartsAt <= now
        && (ExpiresAt is null || ExpiresAt > now)
        && RevokedAt is null;

    public void Revoke(DateTimeOffset now)
    {
        Status = EventAccessStatus.Revoked;
        RevokedAt = now;
        UpdatedAt = now;
    }
}
