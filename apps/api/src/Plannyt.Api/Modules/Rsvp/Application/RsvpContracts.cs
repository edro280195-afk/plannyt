using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed record RsvpSettingsRequest(
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    string TimeZone,
    bool AllowChangesAfterSubmission,
    DateTimeOffset? ChangesCloseAt,
    bool AllowTentativeResponse,
    bool AllowGroupDecline,
    bool RequireResponseForEveryNamedGuest,
    bool RequireCompanionNames,
    bool AllowContactInformationUpdate,
    bool ShowAttendanceSummaryAfterSubmission,
    string? ConfirmationTitle,
    string? ConfirmationMessage,
    string? DeclineMessage,
    string? ClosedMessage,
    string? PrivacyNotice,
    string? SensitiveDataConsentText);

public sealed record RsvpSettingsResponse(
    Guid Id,
    RsvpSettingsStatus Status,
    DateTimeOffset? OpensAt,
    DateTimeOffset? ClosesAt,
    string TimeZone,
    bool AllowChangesAfterSubmission,
    DateTimeOffset? ChangesCloseAt,
    bool AllowTentativeResponse,
    bool AllowGroupDecline,
    bool RequireResponseForEveryNamedGuest,
    bool RequireCompanionNames,
    bool AllowContactInformationUpdate,
    bool ShowAttendanceSummaryAfterSubmission,
    string? ConfirmationTitle,
    string? ConfirmationMessage,
    string? DeclineMessage,
    string? ClosedMessage,
    string? PrivacyNotice,
    string? SensitiveDataConsentText,
    DateTimeOffset UpdatedAt);

public sealed record RsvpFormResponse(
    Guid Id,
    RsvpFormStatus Status,
    int CurrentDraftVersion,
    Guid? ActivePublishedVersionId,
    DateTimeOffset UpdatedAt);

public sealed record RsvpFormVersionResponse(
    Guid Id,
    Guid RsvpFormId,
    int VersionNumber,
    string SettingsSnapshot,
    string QuestionsSnapshot,
    string MenuSnapshot,
    string TransportSnapshot,
    string AccommodationSnapshot,
    DateTimeOffset CreatedAt,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? PublishedAt);

public sealed record RsvpQuestionRequest(
    string Id,
    RsvpQuestionType QuestionType,
    RsvpQuestionScope Scope,
    RsvpQuestionCategory Category,
    string Label,
    string? HelpText,
    bool IsRequired,
    bool IsActive,
    int SortOrder,
    List<string> Options,
    VisibilityRule? VisibilityRule,
    ValidationRules? ValidationRules);

public sealed record RsvpSubmissionRequest(
    int ExpectedRevision,
    RsvpOverallStatus OverallStatus,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    List<RsvpSubmissionGuestRequest> Guests,
    List<RsvpSubmissionAnswerRequest> Answers,
    string? ConsentSnapshot);

public sealed record RsvpSubmissionGuestRequest(
    Guid? EventGuestId,
    string DisplayName,
    string AgeCategory,
    GuestAttendanceStatus AttendanceStatus,
    string MenuSelectionsJson,
    string TransportSelectionJson,
    string AccommodationSelectionJson,
    string DietaryJson,
    bool IsUnnamedCompanion);

public sealed record RsvpSubmissionAnswerRequest(
    string QuestionId,
    Guid? GuestId,
    string AnswerValue,
    string? DisplayValue);

public sealed record RsvpSubmissionResponse(
    Guid Id,
    Guid InvitationGroupId,
    int RevisionNumber,
    RsvpSubmissionSource Source,
    RsvpOverallStatus OverallStatus,
    DateTimeOffset SubmittedAt,
    string? ContactNameSnapshot,
    string? ContactEmailSnapshot,
    string? ContactPhoneSnapshot,
    string? ConfirmationCode,
    List<RsvpSubmissionGuestResponse> Guests,
    List<RsvpSubmissionAnswerResponse> Answers);

public sealed record RsvpSubmissionGuestResponse(
    Guid? EventGuestId,
    string DisplayName,
    string AgeCategory,
    GuestAttendanceStatus AttendanceStatus,
    string MenuSelectionsJson,
    string TransportSelectionJson,
    string AccommodationSelectionJson,
    string DietaryJson,
    bool IsUnnamedCompanion);

public sealed record RsvpSubmissionAnswerResponse(
    string QuestionId,
    Guid? GuestId,
    string AnswerValue,
    string? DisplayValue);

public sealed record RsvpDashboardResponse(
    int TotalGroups,
    int TotalGuestsGranted,
    int GuestsConfirmed,
    int GuestsNotAttending,
    int GuestsTentative,
    int GuestsPending,
    int PartialResponses,
    int ChangedAfterSubmission,
    DateTimeOffset? ClosesAt,
    List<RsvpGroupSummaryResponse> Groups);

public sealed record RsvpGroupSummaryResponse(
    Guid GroupId,
    string GroupName,
    RsvpOverallStatus? Status,
    int ConfirmedCount,
    int DeclinedCount,
    int PendingCount,
    bool HasMenuSelection,
    bool HasTransport,
    bool HasAccommodation,
    bool HasSensitiveData,
    DateTimeOffset? LastResponseAt);

public sealed record ManualRsvpRequest(
    RsvpSubmissionSource Source,
    string Reason,
    RsvpSubmissionRequest Submission);

public sealed record ReminderTemplateRequest(
    string Name,
    ReminderChannel Channel,
    string SegmentType,
    string MessageTemplate);

public sealed record ReminderTemplateResponse(
    Guid Id,
    string Name,
    ReminderChannel Channel,
    string SegmentType,
    string MessageTemplate,
    bool IsActive,
    DateTimeOffset UpdatedAt);

public sealed record MarkReminderRequest(string? Note);

public sealed record RsvpExportFilters(
    string? Status,
    Guid? TagId,
    string? GuestType,
    bool? HasMenu,
    bool? HasTransport,
    bool? HasAccommodation,
    bool? HasAllergies,
    bool? HasAccessibility,
    DateTimeOffset? ActivitySince);

public sealed record GuestRsvpStateResponse(
    Guid GroupId,
    string GroupName,
    int AllowedGuestCount,
    int MaxUnnamedCompanions,
    bool AllowUnnamedCompanions,
    bool CanRespond,
    bool CanModify,
    string? ClosedMessage,
    RsvpSettingsResponse? Settings,
    RsvpFormVersionResponse? ActiveForm,
    RsvpSubmissionResponse? CurrentResponse,
    int RevisionVersion,
    List<GuestRsvpInviteeResponse> Guests);

public sealed record GuestRsvpInviteeResponse(
    Guid EventGuestId,
    string DisplayName,
    string AgeCategory);

public sealed record EventMenuRequest(
    string Name,
    string? Description,
    MenuCategory MenuCategory,
    bool SelectionRequired,
    int MinimumSelections,
    int MaximumSelections,
    int SortOrder);

public sealed record EventMenuResponse(
    Guid Id,
    string Name,
    string? Description,
    MenuCategory MenuCategory,
    bool IsActive,
    bool SelectionRequired,
    int MinimumSelections,
    int MaximumSelections,
    int SortOrder,
    List<EventMenuOptionResponse> Options,
    DateTimeOffset UpdatedAt);

public sealed record EventMenuOptionRequest(
    string Name,
    string? Description,
    string DietaryTags,
    int? Capacity,
    int SortOrder);

public sealed record EventMenuOptionResponse(
    Guid Id,
    string Name,
    string? Description,
    string DietaryTags,
    bool IsActive,
    int? Capacity,
    int SelectionCount,
    int SortOrder);

public sealed record EventTransportOptionRequest(
    string Name,
    string? Description,
    TransportDirection Direction,
    string? PickupPoint,
    DateTimeOffset? DepartureAt,
    DateTimeOffset? ReturnAt,
    int? Capacity,
    bool AllowWaitlist,
    int SortOrder);

public sealed record EventTransportOptionResponse(
    Guid Id,
    string Name,
    string? Description,
    TransportDirection Direction,
    string? PickupPoint,
    DateTimeOffset? DepartureAt,
    DateTimeOffset? ReturnAt,
    int? Capacity,
    bool AllowWaitlist,
    bool IsActive,
    int SortOrder,
    int ConfirmedCount,
    int WaitlistCount);

public sealed record EventAccommodationOptionRequest(
    string Name,
    string? Description,
    string? Address,
    string? BookingUrl,
    string? BookingCode,
    DateTimeOffset? BookingDeadline,
    string? ContactInformation,
    int SortOrder);

public sealed record EventAccommodationOptionResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Address,
    string? BookingUrl,
    string? BookingCode,
    DateTimeOffset? BookingDeadline,
    string? ContactInformation,
    bool IsActive,
    int SortOrder,
    int InterestedCount);

public sealed record SensitiveGuestDataResponse(
    Guid EventGuestId,
    string DisplayName,
    string? Allergies,
    string? DietaryRestrictions,
    string? AccessibilityRequirements,
    string? AdditionalNotes,
    DateTimeOffset? ConsentGrantedAt,
    DateTimeOffset UpdatedAt);

public sealed record RsvpProjectionIssueResponse(
    string Code,
    string Projection,
    Guid? InvitationGroupId,
    Guid? EventGuestId,
    Guid? LatestSubmissionId,
    bool Repairable,
    string Description);

public sealed record RsvpProjectionReconciliationResponse(
    bool RepairMode,
    int GroupsEvaluated,
    int IssuesDetected,
    int IssuesRepaired,
    IReadOnlyList<RsvpProjectionIssueResponse> Issues);
