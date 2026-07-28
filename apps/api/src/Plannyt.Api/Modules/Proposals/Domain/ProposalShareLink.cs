using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Proposals.Domain;

public sealed class ProposalShareLink : ITenantEntity
{
    private ProposalShareLink()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProposalId { get; private set; }

    public Guid ProposalVersionId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public DateTimeOffset? FirstViewedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static ProposalShareLink Create(
        Guid organizationId,
        Guid proposalId,
        Guid proposalVersionId,
        string tokenHash,
        DateTimeOffset expiresAt,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProposalId = proposalId,
            ProposalVersionId = proposalVersionId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy,
            CreatedAt = now
        };

    public void MarkViewed(DateTimeOffset now) => FirstViewedAt ??= now;

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;

    public bool IsAvailable(DateTimeOffset now) =>
        RevokedAt is null && ExpiresAt >= now;
}
