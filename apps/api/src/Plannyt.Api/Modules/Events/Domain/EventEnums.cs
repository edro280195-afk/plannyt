namespace Plannyt.Api.Modules.Events.Domain;

public enum EventStatus
{
    Preliminary,
    Confirmed,
    Planning,
    Suspended,
    Cancelled,
    Closed,
    Archived
}

public enum EventClientRelationshipType
{
    ContractingClient,
    PrimaryClient,
    Payer,
    Approver,
    Other
}
