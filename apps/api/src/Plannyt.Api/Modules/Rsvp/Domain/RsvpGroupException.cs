using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class RsvpGroupException : ITenantEntity
{
    private RsvpGroupException() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid InvitationGroupId { get; private set; }
    public RsvpGroupExceptionStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? ClosedBy { get; private set; }

    public static RsvpGroupException Create(
        Guid organizationId,
        Guid eventId,
        Guid invitationGroupId,
        DateTimeOffset expiresAt,
        string reason,
        Guid createdBy,
        DateTimeOffset now)
    {
        if (expiresAt <= now)
        {
            throw new DomainRuleException("La expiración debe ser futura.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException(
                "El motivo de la excepción es obligatorio.");
        }

        return new RsvpGroupException
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            InvitationGroupId = invitationGroupId,
            Status = RsvpGroupExceptionStatus.Active,
            ExpiresAt = expiresAt,
            Reason = reason.Trim(),
            CreatedBy = createdBy,
            CreatedAt = now
        };
    }

    public void Close(DateTimeOffset now)
    {
        Close(null, now);
    }

    public void Close(Guid? closedBy, DateTimeOffset now)
    {
        if (Status != RsvpGroupExceptionStatus.Active)
        {
            throw new DomainRuleException(
                "Solo una excepción activa puede cerrarse.");
        }

        Status = RsvpGroupExceptionStatus.Closed;
        ClosedAt = now;
        ClosedBy = closedBy;
    }

    public bool IsValid(DateTimeOffset now) =>
        Status == RsvpGroupExceptionStatus.Active
        && now <= ExpiresAt;
}
