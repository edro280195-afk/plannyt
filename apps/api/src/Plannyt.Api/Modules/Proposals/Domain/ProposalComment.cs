using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Proposals.Domain;

public sealed class ProposalComment : ITenantEntity
{
    private ProposalComment()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProposalId { get; private set; }

    public Guid ProposalVersionId { get; private set; }

    public Guid? ProposalLineId { get; private set; }

    public Guid? AuthorUserId { get; private set; }

    public string AuthorDisplayName { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public ProposalCommentVisibility Visibility { get; private set; }

    public ProposalCommentStatus Status { get; private set; }

    public Guid? ParentCommentId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ProposalComment Create(
        Guid organizationId,
        Guid proposalId,
        Guid proposalVersionId,
        Guid? proposalLineId,
        Guid? authorUserId,
        string authorDisplayName,
        string content,
        ProposalCommentVisibility visibility,
        Guid? parentCommentId,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProposalId = proposalId,
            ProposalVersionId = proposalVersionId,
            ProposalLineId = proposalLineId,
            AuthorUserId = authorUserId,
            AuthorDisplayName = authorDisplayName,
            Content = content,
            Visibility = visibility,
            Status = ProposalCommentStatus.Pending,
            ParentCommentId = parentCommentId,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Resolve(DateTimeOffset now)
    {
        Status = ProposalCommentStatus.Resolved;
        UpdatedAt = now;
    }
}
