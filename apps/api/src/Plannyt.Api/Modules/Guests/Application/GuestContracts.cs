using Plannyt.Api.Modules.Guests.Domain;

namespace Plannyt.Api.Modules.Guests.Application;

public sealed record InvitationGroupRequest(
    InvitationGroupType GroupType,
    string DisplayName,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    int AllowedGuestCount,
    bool AllowUnnamedCompanions,
    int MaxUnnamedCompanions,
    string? InternalNotes,
    IReadOnlyList<Guid> TagIds,
    bool ApplyCapacityOverride = false);

public sealed record EventGuestRequest(
    Guid? InvitationGroupId,
    Guid? PersonId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    GuestType GuestType,
    AgeCategory AgeCategory,
    bool IsPrimaryContact,
    bool IsNamed,
    bool IsPlusOne,
    bool IsVip,
    int SortOrder,
    string? InternalNotes);

public sealed record GuestTagRequest(string Name, string ColorToken);

public sealed record GuestTagResponse(
    Guid Id,
    string Name,
    string ColorToken);

public sealed record InvitationGroupResponse(
    Guid Id,
    InvitationGroupType GroupType,
    string DisplayName,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    int AllowedGuestCount,
    int NamedGuestCount,
    int AvailableGuestCount,
    bool AllowUnnamedCompanions,
    int MaxUnnamedCompanions,
    InvitationGroupStatus Status,
    string Source,
    string? InternalNotes,
    bool CapacityOverrideApplied,
    IReadOnlyList<GuestTagResponse> Tags,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record EventGuestResponse(
    Guid Id,
    Guid? InvitationGroupId,
    Guid? PersonId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    GuestType GuestType,
    AgeCategory AgeCategory,
    bool IsPrimaryContact,
    bool IsNamed,
    bool IsPlusOne,
    bool IsVip,
    bool IsActive,
    int SortOrder,
    string? InternalNotes,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record GuestDashboardResponse(
    int ActiveGuestCount,
    int GroupCount,
    int LinkCount,
    int OpenedLinkCount,
    GuestPlanUsageResponse Plan,
    IReadOnlyList<InvitationGroupResponse> Groups,
    IReadOnlyList<EventGuestResponse> Guests);

public sealed record GuestPlanUsageResponse(
    GuestPlanTier Tier,
    int ActiveGuests,
    int Limit,
    int Percentage,
    bool Warning80,
    bool Warning90,
    bool IsAtLimit);

public sealed record GuestDuplicateSuggestionResponse(
    string Kind,
    string Reason,
    IReadOnlyList<Guid> GuestIds,
    string SuggestedAction);

public sealed record UpdateGuestImportMappingRequest(
    IReadOnlyDictionary<string, string> Mapping);

public sealed record GuestImportRowPreview(
    int RowNumber,
    string? GroupName,
    string? GuestName,
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed record GuestImportAnalysisResponse(
    Guid ImportId,
    GuestImportStatus Status,
    IReadOnlyList<string> Headers,
    IReadOnlyDictionary<string, string> Mapping,
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    IReadOnlyList<GuestImportRowPreview> Preview);

public sealed record GuestImportResultResponse(
    Guid ImportId,
    GuestImportStatus Status,
    int CreatedGroups,
    int CreatedGuests,
    int ReusedGroups,
    int SkippedRows,
    IReadOnlyList<GuestImportRowPreview> Errors,
    DateTimeOffset CompletedAt);

internal sealed record ParsedGuestImportRow(
    int RowNumber,
    string GroupName,
    InvitationGroupType GroupType,
    int AllowedGuestCount,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    string GuestFirstName,
    string GuestLastName,
    AgeCategory AgeCategory,
    bool IsPrimaryContact,
    bool IsVip,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Errors);
