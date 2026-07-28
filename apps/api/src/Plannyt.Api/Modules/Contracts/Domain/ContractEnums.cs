namespace Plannyt.Api.Modules.Contracts.Domain;

public enum ContractContentFormat
{
    SanitizedHtml
}

public enum ContractSourceType
{
    GeneratedFromProposal,
    Manual,
    ExternalUpload
}

public enum ContractStatus
{
    Draft,
    Ready,
    Sent,
    Viewed,
    PartiallySigned,
    FullySigned,
    Completed,
    Declined,
    Expired,
    Cancelled
}

public enum ContractPartyType
{
    PlannerOrganization,
    Client,
    Other
}

public enum ContractSignerStatus
{
    Pending,
    Invited,
    Viewed,
    Signed,
    Declined,
    Expired,
    Revoked
}

public enum SigningMethod
{
    Drawn,
    Typed,
    AuthenticatedConfirmation,
    External
}

public enum DepositRequirementType
{
    None,
    FixedAmount,
    PercentageOfContract
}

public enum ConfirmationMode
{
    Automatic,
    ManualAfterRequirements
}
