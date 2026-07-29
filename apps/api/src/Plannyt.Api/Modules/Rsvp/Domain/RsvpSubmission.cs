using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class RsvpSubmission : ITenantEntity
{
    private RsvpSubmission() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid InvitationGroupId { get; private set; }
    public Guid RsvpFormVersionId { get; private set; }
    public Guid? GuestAccessLinkId { get; private set; }
    public int RevisionNumber { get; private set; }
    public RsvpSubmissionSource Source { get; private set; }
    public RsvpOverallStatus OverallStatus { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public string? ContactNameSnapshot { get; private set; }
    public string? ContactEmailSnapshot { get; private set; }
    public string? ContactPhoneSnapshot { get; private set; }
    public string? UserAgentCategory { get; private set; }
    public string? IpAddress { get; private set; }
    public string? ConsentSnapshot { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestFingerprint { get; private set; } = string.Empty;
    public Guid? PreviousSubmissionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static RsvpSubmission Create(
        Guid organizationId,
        Guid eventId,
        Guid invitationGroupId,
        Guid rsvpFormVersionId,
        Guid? guestAccessLinkId,
        int revisionNumber,
        RsvpSubmissionSource source,
        RsvpOverallStatus overallStatus,
        Guid? submittedByUserId,
        string? contactNameSnapshot,
        string? contactEmailSnapshot,
        string? contactPhoneSnapshot,
        string? userAgentCategory,
        string? ipAddress,
        string? consentSnapshot,
        string idempotencyKey,
        Guid? previousSubmissionId,
        DateTimeOffset now,
        string? requestFingerprint = null)
    {
        if (source == RsvpSubmissionSource.GuestPrivateLink && overallStatus == RsvpOverallStatus.Incomplete)
        {
            throw new DomainRuleException("Un envío público no puede quedar incompleto.");
        }

        return new RsvpSubmission
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            InvitationGroupId = invitationGroupId,
            RsvpFormVersionId = rsvpFormVersionId,
            GuestAccessLinkId = guestAccessLinkId,
            RevisionNumber = revisionNumber,
            Source = source,
            OverallStatus = overallStatus,
            SubmittedAt = now,
            SubmittedByUserId = submittedByUserId,
            ContactNameSnapshot = contactNameSnapshot,
            ContactEmailSnapshot = contactEmailSnapshot,
            ContactPhoneSnapshot = contactPhoneSnapshot,
            UserAgentCategory = userAgentCategory,
            IpAddress = ipAddress,
            ConsentSnapshot = consentSnapshot,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = requestFingerprint
                ?? Convert.ToHexString(
                    System.Security.Cryptography.RandomNumberGenerator
                        .GetBytes(32)),
            PreviousSubmissionId = previousSubmissionId,
            CreatedAt = now
        };
    }
}
