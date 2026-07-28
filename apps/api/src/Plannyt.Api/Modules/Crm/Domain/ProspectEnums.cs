namespace Plannyt.Api.Modules.Crm.Domain;

public enum ProspectStatus
{
    New,
    Contacted,
    Qualified,
    Opportunity,
    ProposalDraft,
    ProposalSent,
    Negotiation,
    Won,
    Lost,
    Archived
}

public enum ProspectActivityType
{
    Note,
    Call,
    WhatsApp,
    Email,
    Meeting,
    FollowUp,
    StatusChange,
    ProposalSent,
    ClientComment
}

public enum CommercialVisibility
{
    Internal,
    ClientShared
}
