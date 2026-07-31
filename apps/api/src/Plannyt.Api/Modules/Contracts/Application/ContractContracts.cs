using Plannyt.Api.Modules.Contracts.Domain;

namespace Plannyt.Api.Modules.Contracts.Application;

public sealed record UpsertContractTemplateRequest(
    string Name,
    string? Description,
    string Content,
    bool IsDefault,
    bool IsActive = true);

public sealed record ContractTemplateResponse(
    Guid Id,
    string Name,
    string? Description,
    string Content,
    ContractContentFormat ContentFormat,
    bool IsDefault,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record PreviewContractTemplateRequest(
    string Content,
    Guid? EventId,
    Guid? ClientId,
    Guid? ProposalVersionId,
    Guid? ContractId,
    DateTimeOffset? ValidUntil);

public sealed record ContractTemplatePreviewResponse(
    string RenderedContent,
    IReadOnlyList<string> UnknownVariables,
    IReadOnlyList<string> MissingVariables,
    bool CanPublish);

public sealed record UpdateContractingPolicyRequest(
    bool RequireAcceptedProposal,
    bool RequireCompletedContract,
    DepositRequirementType DepositRequirementType,
    decimal DepositRequirementValue,
    ConfirmationMode ConfirmationMode);

public sealed record ContractingPolicyResponse(
    bool RequireAcceptedProposal,
    bool RequireCompletedContract,
    DepositRequirementType DepositRequirementType,
    decimal DepositRequirementValue,
    ConfirmationMode ConfirmationMode,
    DateTimeOffset UpdatedAt);

public sealed record CreateContractFromProposalRequest(
    Guid ProposalId,
    string Name,
    Guid? TemplateId,
    string? Content,
    string ConsentText,
    DateTimeOffset? ValidUntil);

public sealed record CreateManualContractRequest(
    Guid EventId,
    Guid ClientId,
    string Name,
    decimal ContractGrandTotal,
    string CurrencyCode,
    Guid? TemplateId,
    string? Content,
    string ConsentText,
    DateTimeOffset? ValidUntil);

public sealed class CreateExternalContractRequest
{
    public required Guid EventId { get; init; }

    public required Guid ClientId { get; init; }

    public required string Name { get; init; }

    public required decimal ContractGrandTotal { get; init; }

    public required string CurrencyCode { get; init; }

    public DateTimeOffset? ValidUntil { get; init; }

    public required IFormFile File { get; init; }
}

public sealed record UpdateContractDraftRequest(
    string Name,
    Guid? TemplateId,
    string Content,
    string ConsentText,
    DateTimeOffset? ValidUntil);

public sealed record CancelContractRequest(string Reason);

public sealed record CreateContractPartyRequest(
    ContractPartyType PartyType,
    Guid? ClientId,
    string DisplayName,
    string? LegalName,
    string? TaxId,
    string? Address,
    int SortOrder);

public sealed record ContractPartyResponse(
    Guid Id,
    ContractPartyType PartyType,
    Guid? ClientId,
    Guid? OrganizationPartyId,
    string DisplayName,
    string? LegalName,
    string? TaxId,
    string? Address,
    int SortOrder);

public sealed record UpsertContractSignerRequest(
    Guid ContractPartyId,
    Guid? PersonId,
    Guid? UserAccountId,
    string Name,
    string Email,
    string SignerRole,
    int SigningOrder,
    bool IsRequired);

public sealed record ContractSignerResponse(
    Guid Id,
    Guid ContractPartyId,
    Guid? PersonId,
    Guid? UserAccountId,
    string Name,
    string Email,
    string SignerRole,
    int SigningOrder,
    bool IsRequired,
    ContractSignerStatus Status,
    DateTimeOffset? SignedAt,
    DateTimeOffset? DeclinedAt,
    Guid? ActiveSignatureRequestId);

public sealed record ContractVersionResponse(
    Guid Id,
    int VersionNumber,
    Guid? TemplateId,
    Guid? SourceProposalVersionId,
    string RenderedContent,
    string? DocumentFileName,
    long? DocumentSizeBytes,
    string? DocumentSha256,
    string ConsentText,
    DateTimeOffset? ValidUntil,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? SupersededAt);

public sealed record ContractRequirementSnapshotResponse(
    bool RequireAcceptedProposal,
    bool RequireCompletedContract,
    DepositRequirementType DepositRequirementType,
    decimal DepositRequirementValue,
    decimal RequiredDepositAmount,
    string CurrencyCode,
    ConfirmationMode ConfirmationMode,
    DateTimeOffset CreatedAt);

public sealed record ContractListItemResponse(
    Guid Id,
    Guid EventId,
    Guid ClientId,
    string ContractNumber,
    string Name,
    ContractSourceType SourceType,
    ContractStatus Status,
    int CurrentVersionNumber,
    decimal ContractGrandTotal,
    string CurrencyCode,
    DateTimeOffset UpdatedAt);

public sealed record ContractResponse(
    Guid Id,
    Guid OrganizationId,
    Guid EventId,
    Guid ClientId,
    Guid? AcceptedProposalId,
    Guid? AcceptedProposalVersionId,
    string ContractNumber,
    string Name,
    ContractSourceType SourceType,
    ContractStatus Status,
    int CurrentVersionNumber,
    decimal ContractGrandTotal,
    string CurrencyCode,
    IReadOnlyList<ContractVersionResponse> Versions,
    IReadOnlyList<ContractPartyResponse> Parties,
    IReadOnlyList<ContractSignerResponse> Signers,
    ContractRequirementSnapshotResponse Requirements,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason);

public sealed record CreateSignatureRequest(
    DateTimeOffset? ExpiresAt);

public sealed record SignatureRequestLinkResponse(
    Guid Id,
    Guid ContractVersionId,
    Guid ContractSignerId,
    DateTimeOffset ExpiresAt,
    string SigningUrl);

public sealed record SubmitSignatureRequest(
    SigningMethod SigningMethod,
    string DeclaredSignerName,
    bool AcceptElectronicMeans,
    bool ConfirmDisplayedVersion,
    string? SignatureDataUrl);

public sealed record DeclineSignatureRequest(string? Reason);

public sealed record PublicContractSignerStatusResponse(
    string SignerRole,
    ContractSignerStatus Status,
    DateTimeOffset? SignedAt);

public sealed record PublicContractSignatureResponse(
    Guid ContractId,
    Guid ContractVersionId,
    Guid ContractSignerId,
    string ContractNumber,
    string Name,
    int VersionNumber,
    string OrganizationName,
    string SignerName,
    string SignerEmail,
    IReadOnlyList<string> Parties,
    string RenderedContent,
    DateTimeOffset? ValidUntil,
    string ConsentText,
    string DocumentSha256,
    IReadOnlyList<PublicContractSignerStatusResponse> Signers,
    bool CanSign);

public sealed record SignatureEvidenceSummaryResponse(
    Guid Id,
    Guid ContractVersionId,
    Guid ContractSignerId,
    SigningMethod SigningMethod,
    string DeclaredSignerName,
    string DeclaredSignerEmail,
    string DocumentSha256,
    DateTimeOffset SignedAt);

public sealed record ValidateExternalContractRequest(
    DateTimeOffset SignedAt);

public sealed record ContractFileDownload(
    Stream Content,
    string MimeType,
    string FileName);

public sealed record ContractPdfModel(
    string OrganizationName,
    string ContractNumber,
    string Name,
    int VersionNumber,
    string RenderedContent,
    string ConsentText,
    DateTimeOffset? ValidUntil,
    string DocumentSha256,
    IReadOnlyList<ContractPartyResponse> Parties);

public sealed record PortalContractListItemResponse(
    Guid Id,
    Guid EventId,
    string ContractNumber,
    string Name,
    ContractStatus Status,
    int CurrentVersionNumber,
    bool HasPendingSignature,
    bool HasFinalDocument);

public sealed record PortalContractResponse(
    Guid Id,
    Guid EventId,
    string ContractNumber,
    string Name,
    ContractStatus Status,
    ContractVersionResponse Version,
    IReadOnlyList<ContractPartyResponse> Parties,
    IReadOnlyList<PublicContractSignerStatusResponse> Signers,
    Guid? PendingSignerId,
    string? PendingSignerName,
    bool HasFinalDocument);

public sealed record ContractingReadinessResponse(
    bool ProposalAccepted,
    bool ContractCompleted,
    decimal RequiredDepositAmount,
    decimal ApprovedDepositAmount,
    bool DepositSatisfied,
    int MissingRequiredSigners,
    IReadOnlyList<string> MissingRequirements,
    bool ReadyForConfirmation,
    ConfirmationMode ConfirmationMode,
    string EventStatus);
