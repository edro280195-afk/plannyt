using System.Text.Json;
using Plannyt.Api.Modules.Invitations.Domain;

namespace Plannyt.Api.Modules.Invitations.Application;

public sealed record GuestExperienceRequest(
    string Language,
    string PublicTitle,
    string CelebrantDisplayName,
    string? WelcomeMessage,
    string? ClosingMessage,
    bool ShowEventName,
    bool ShowEventDate,
    bool ShowParticipantNames,
    bool ShowCity,
    bool PrivateAccessOnly);

public sealed record InvitationThemeRequest(
    string BackgroundColor,
    string SurfaceColor,
    string TextColor,
    string AccentColor,
    string HeadingFont,
    string BodyFont,
    string RadiusToken,
    string SpacingToken,
    string CoverStyle,
    string ButtonStyle,
    InvitationAnimationLevel Animation);

public sealed record InvitationBlockRequest(
    Guid Id,
    InvitationBlockType Type,
    bool Visible,
    BlockVisibility Visibility,
    string? VisibilityValue,
    int SortOrder,
    JsonElement Content,
    JsonElement Presentation);

public sealed record CreateInvitationDesignRequest(
    string Name,
    Guid? TemplateId);

public sealed record UpdateInvitationDesignRequest(
    string Name,
    InvitationThemeRequest Theme,
    IReadOnlyList<InvitationBlockRequest> Blocks);

public sealed record InvitationTemplateRequest(
    string Name,
    string Description,
    InvitationThemeRequest Theme,
    IReadOnlyList<InvitationBlockRequest> Blocks);

public sealed record InvitationTemplateResponse(
    Guid Id,
    bool IsGlobal,
    string Name,
    string Description,
    InvitationThemeRequest Theme,
    IReadOnlyList<InvitationBlockRequest> Blocks);

public sealed record InvitationDesignResponse(
    Guid Id,
    Guid EventId,
    string Name,
    InvitationDesignStatus Status,
    InvitationThemeRequest Theme,
    IReadOnlyList<InvitationBlockRequest> Blocks,
    int NextVersionNumber,
    Guid? ApprovedVersionId,
    IReadOnlyList<InvitationVersionResponse> Versions,
    IReadOnlyList<InvitationCommentResponse> Comments,
    IReadOnlyList<string> AccessibilityWarnings,
    DateTimeOffset UpdatedAt);

public sealed record InvitationVersionResponse(
    Guid Id,
    int VersionNumber,
    InvitationThemeRequest Theme,
    IReadOnlyList<InvitationBlockRequest> Blocks,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? PublishedAt);

public sealed record InvitationCommentRequest(string Message);

public sealed record PublishInvitationDesignRequest(
    bool BypassApprovalForTesting = false);

public sealed record InvitationCommentResponse(
    Guid Id,
    Guid VersionId,
    InvitationReviewDecision Decision,
    string Message,
    DateTimeOffset CreatedAt);

public sealed record GuestExperienceResponse(
    Guid Id,
    Guid EventId,
    GuestExperienceStatus Status,
    string Language,
    string PublicTitle,
    string CelebrantDisplayName,
    string? WelcomeMessage,
    string? ClosingMessage,
    bool ShowEventName,
    bool ShowEventDate,
    bool ShowParticipantNames,
    bool ShowCity,
    bool PrivateAccessOnly,
    Guid? ActiveInvitationDesignId,
    Guid? ActiveVersionId,
    DateTimeOffset UpdatedAt);

public sealed record GenerateGuestLinkRequest(DateTimeOffset? ExpiresAt);

public sealed record GuestAccessLinkResponse(
    Guid Id,
    Guid InvitationGroupId,
    GuestAccessLinkStatus Status,
    string? PublicUrl,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? FirstOpenedAt,
    DateTimeOffset? LastOpenedAt,
    int OpenCount,
    DateTimeOffset? SharedManuallyAt,
    DateTimeOffset CreatedAt);

public sealed record PublicInvitationResponse(
    string Status,
    string Language,
    string PublicTitle,
    string CelebrantDisplayName,
    string? WelcomeMessage,
    string? EventName,
    DateTimeOffset? EventStartsAt,
    string EventTimeZone,
    string? City,
    string? CountryCode,
    string GroupDisplayName,
    int AllowedGuestCount,
    IReadOnlyList<PublicGuestResponse> Participants,
    InvitationThemeRequest Theme,
    IReadOnlyList<InvitationBlockRequest> Blocks,
    string? ClosingMessage);

public sealed record PublicGuestResponse(
    string FirstName,
    string LastName,
    GuestTypeProjection GuestType,
    AgeCategoryProjection AgeCategory,
    bool IsPrimaryContact,
    bool IsVip);

public enum GuestTypeProjection
{
    Standard,
    Family,
    Friend,
    Colleague,
    Vendor,
    WeddingParty,
    SponsorOrGodparent,
    StaffGuest,
    VendorGuest,
    Vip,
    Other
}

public enum AgeCategoryProjection
{
    Adult,
    Teen,
    Child,
    Infant,
    Unknown
}

public sealed record PortalGuestWorkspaceResponse(
    Guid EventId,
    IReadOnlyList<PortalInvitationGroupResponse> Groups,
    IReadOnlyList<PortalGuestResponse> Guests,
    InvitationDesignResponse? Design);

public sealed record PortalInvitationGroupRequest(
    InvitationGroupTypeProjection GroupType,
    string DisplayName,
    int AllowedGuestCount,
    bool AllowUnnamedCompanions,
    int MaxUnnamedCompanions);

public enum InvitationGroupTypeProjection
{
    Individual,
    Couple,
    Family,
    Group,
    Company,
    CorporateTable,
    Other
}

public sealed record PortalGuestRequest(
    Guid? InvitationGroupId,
    string FirstName,
    string LastName,
    GuestTypeProjection GuestType,
    AgeCategoryProjection AgeCategory,
    bool IsPrimaryContact,
    bool IsVip,
    int SortOrder);

public sealed record PortalInvitationGroupResponse(
    Guid Id,
    InvitationGroupTypeProjection GroupType,
    string DisplayName,
    int AllowedGuestCount,
    int NamedGuestCount,
    bool AllowUnnamedCompanions,
    int MaxUnnamedCompanions);

public sealed record PortalGuestResponse(
    Guid Id,
    Guid? InvitationGroupId,
    string FirstName,
    string LastName,
    GuestTypeProjection GuestType,
    AgeCategoryProjection AgeCategory,
    bool IsPrimaryContact,
    bool IsVip);
