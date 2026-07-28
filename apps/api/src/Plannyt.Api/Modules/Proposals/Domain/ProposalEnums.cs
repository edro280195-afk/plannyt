namespace Plannyt.Api.Modules.Proposals.Domain;

public enum ProposalStatus
{
    Draft,
    Ready,
    Sent,
    Viewed,
    ChangesRequested,
    Negotiation,
    Accepted,
    Rejected,
    Expired,
    Cancelled
}

public enum ProposalCommentVisibility
{
    Internal,
    ClientShared
}

public enum ProposalCommentStatus
{
    Pending,
    Resolved
}
