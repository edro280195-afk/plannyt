using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.Modules.Proposals.Application;

public sealed record ProposalDraftLineRequest(
    string Description,
    Guid? ServiceCatalogItemId,
    Guid? PackageId,
    decimal Quantity,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal TaxRate,
    bool IsOptional,
    int SortOrder);

public sealed record ProposalDraftRequest(
    Guid? ProspectId,
    Guid? ClientId,
    Guid? EventId,
    string CurrencyCode,
    DateTimeOffset ValidUntil,
    string? SharedIntroduction,
    string? SharedTerms,
    string? InternalNotes,
    DiscountType GeneralDiscountType,
    decimal GeneralDiscountValue,
    Guid? CouponId,
    IReadOnlyList<ProposalDraftLineRequest> Lines);

public sealed record ProposalListItemResponse(
    Guid Id,
    string ProposalNumber,
    Guid? ProspectId,
    Guid? ClientId,
    Guid? EventId,
    string TargetDisplayName,
    ProposalStatus Status,
    int CurrentVersionNumber,
    string CurrencyCode,
    DateTimeOffset ValidUntil,
    decimal? GrandTotal,
    DateTimeOffset UpdatedAt);

public sealed record ProposalDraftLineResponse(
    Guid Id,
    string Description,
    Guid? ServiceCatalogItemId,
    Guid? PackageId,
    decimal Quantity,
    decimal UnitPrice,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal TaxRate,
    decimal LineSubtotal,
    decimal LineDiscount,
    decimal LineTax,
    decimal LineTotal,
    bool IsOptional,
    int SortOrder);

public sealed record ProposalTotalsResponse(
    decimal Subtotal,
    decimal DiscountTotal,
    decimal GeneralDiscountTotal,
    decimal CouponDiscountTotal,
    decimal TaxTotal,
    decimal GrandTotal);

public sealed record ProposalVersionSummaryResponse(
    Guid Id,
    int VersionNumber,
    decimal GrandTotal,
    string CurrencyCode,
    DateTimeOffset ValidUntil,
    DateTimeOffset? PublishedAt);

public sealed record ProposalVersionResponse(
    Guid Id,
    int VersionNumber,
    ProposalTotalsResponse Totals,
    string CurrencyCode,
    DateTimeOffset ValidUntil,
    string? SharedIntroduction,
    string? SharedTerms,
    string? CouponCode,
    IReadOnlyList<ProposalDraftLineResponse> Lines,
    DateTimeOffset? PublishedAt);

public sealed record ProposalCommentResponse(
    Guid Id,
    Guid ProposalVersionId,
    Guid? ProposalLineId,
    Guid? AuthorUserId,
    string AuthorDisplayName,
    string Content,
    ProposalCommentVisibility Visibility,
    ProposalCommentStatus Status,
    Guid? ParentCommentId,
    DateTimeOffset CreatedAt);

public sealed record ProposalResponse(
    Guid Id,
    string ProposalNumber,
    Guid? ProspectId,
    Guid? ClientId,
    Guid? EventId,
    ProposalStatus Status,
    int CurrentVersionNumber,
    string CurrencyCode,
    DateTimeOffset ValidUntil,
    string? SharedIntroduction,
    string? SharedTerms,
    string? InternalNotes,
    DiscountType GeneralDiscountType,
    decimal GeneralDiscountValue,
    Guid? CouponId,
    ProposalTotalsResponse DraftTotals,
    IReadOnlyList<ProposalDraftLineResponse> DraftLines,
    IReadOnlyList<ProposalVersionSummaryResponse> Versions,
    IReadOnlyList<ProposalCommentResponse> Comments,
    Guid? AcceptedVersionId,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RejectedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SendProposalRequest(DateTimeOffset? ExpiresAt);

public sealed record ProposalShareLinkResponse(
    Guid Id,
    Guid ProposalVersionId,
    DateTimeOffset ExpiresAt,
    string ShareUrl);

public sealed record CreateProposalCommentRequest(
    Guid ProposalVersionId,
    Guid? ProposalLineId,
    string AuthorDisplayName,
    string Content,
    ProposalCommentVisibility Visibility,
    Guid? ParentCommentId);

public sealed record ProposalPublicLineResponse(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineDiscount,
    decimal LineTax,
    decimal LineTotal,
    bool IsOptional,
    int SortOrder);

public sealed record ProposalPublicCommentResponse(
    Guid Id,
    Guid? ProposalLineId,
    string AuthorDisplayName,
    string Content,
    ProposalCommentStatus Status,
    Guid? ParentCommentId,
    DateTimeOffset CreatedAt);

public sealed record ProposalPublicResponse(
    Guid ProposalId,
    Guid VersionId,
    string ProposalNumber,
    int VersionNumber,
    string OrganizationName,
    string RecipientName,
    string? EventSummary,
    ProposalStatus Status,
    string CurrencyCode,
    DateTimeOffset ValidUntil,
    string? SharedIntroduction,
    string? SharedTerms,
    ProposalTotalsResponse Totals,
    IReadOnlyList<ProposalPublicLineResponse> Lines,
    IReadOnlyList<ProposalPublicCommentResponse> Comments);

public sealed record ProposalPublicCommentRequest(
    string AuthorDisplayName,
    string Content,
    Guid? ProposalLineId,
    Guid? ParentCommentId);

public sealed record ProposalDecisionRequest(
    string? AuthorDisplayName,
    string? Reason);

public sealed record LinkProposalEventRequest(
    Guid? ExistingEventId,
    string? Name,
    string? EventType,
    DateTimeOffset? StartDateTime,
    string? TimeZone,
    string? City,
    string? CountryCode,
    int? EstimatedGuestCount);
