using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Access.Domain;

public sealed class AccessInvitation : ITenantEntity
{
    private AccessInvitation()
    {
    }

    private AccessInvitation(
        Guid id,
        InvitationType invitationType,
        Guid organizationId,
        Guid? eventId,
        OrganizationRole? intendedOrganizationRole,
        EventAccessRole? intendedEventRole,
        string targetEmail,
        string normalizedTargetEmail,
        string tokenHash,
        DateTimeOffset expiresAt,
        Guid invitedBy,
        DateTimeOffset createdAt)
    {
        Id = id;
        InvitationType = invitationType;
        OrganizationId = organizationId;
        EventId = eventId;
        IntendedOrganizationRole = intendedOrganizationRole;
        IntendedEventRole = intendedEventRole;
        TargetEmail = targetEmail;
        NormalizedTargetEmail = normalizedTargetEmail;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        InvitedBy = invitedBy;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public InvitationType InvitationType { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid? EventId { get; private set; }

    public OrganizationRole? IntendedOrganizationRole { get; private set; }

    public EventAccessRole? IntendedEventRole { get; private set; }

    public string TargetEmail { get; private set; } = string.Empty;

    public string NormalizedTargetEmail { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid InvitedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AccessInvitation CreateOrganizationMembership(
        Guid organizationId,
        OrganizationRole intendedRole,
        string targetEmail,
        string normalizedTargetEmail,
        string tokenHash,
        DateTimeOffset expiresAt,
        Guid invitedBy,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            InvitationType.OrganizationMembership,
            organizationId,
            null,
            intendedRole,
            null,
            targetEmail,
            normalizedTargetEmail,
            tokenHash,
            expiresAt,
            invitedBy,
            now);

    public static AccessInvitation CreateEventAccess(
        Guid organizationId,
        Guid eventId,
        EventAccessRole intendedRole,
        string targetEmail,
        string normalizedTargetEmail,
        string tokenHash,
        DateTimeOffset expiresAt,
        Guid invitedBy,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            InvitationType.EventAccess,
            organizationId,
            eventId,
            null,
            intendedRole,
            targetEmail,
            normalizedTargetEmail,
            tokenHash,
            expiresAt,
            invitedBy,
            now);

    public bool CanAcceptAt(DateTimeOffset now) =>
        AcceptedAt is null
        && RevokedAt is null
        && ExpiresAt > now;

    public void Accept(DateTimeOffset now)
    {
        if (!CanAcceptAt(now))
        {
            throw new DomainRuleException("La invitación no está vigente.");
        }

        AcceptedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (AcceptedAt is not null)
        {
            throw new DomainRuleException(
                "Una invitación aceptada no puede revocarse.");
        }

        RevokedAt = now;
    }
}
