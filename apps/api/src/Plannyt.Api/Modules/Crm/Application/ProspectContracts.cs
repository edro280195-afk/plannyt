using Plannyt.Api.Modules.Crm.Domain;

namespace Plannyt.Api.Modules.Crm.Application;

public sealed record ProspectDetailsRequest(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Source,
    string? EventTypeInterest,
    DateOnly? EstimatedEventDate,
    int? EstimatedGuestCount,
    decimal? EstimatedBudget,
    string CurrencyCode,
    string? City,
    string? Notes,
    Guid? AssignedUserId);

public sealed record ChangeProspectStatusRequest(
    ProspectStatus NewStatus,
    string? Reason);

public sealed record CreateProspectActivityRequest(
    ProspectActivityType ActivityType,
    string Subject,
    string? Description,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? CompletedAt,
    Guid? AssignedUserId,
    CommercialVisibility Visibility);

public sealed record ProspectListItemResponse(
    Guid Id,
    string DisplayName,
    string? Email,
    string? Phone,
    string? EventTypeInterest,
    DateOnly? EstimatedEventDate,
    decimal? EstimatedBudget,
    string CurrencyCode,
    Guid? AssignedUserId,
    ProspectStatus Status,
    DateTimeOffset UpdatedAt);

public sealed record ProspectActivityResponse(
    Guid Id,
    ProspectActivityType ActivityType,
    string Subject,
    string? Description,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? CompletedAt,
    Guid? AssignedUserId,
    CommercialVisibility Visibility,
    Guid CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record ProspectStatusHistoryResponse(
    Guid Id,
    ProspectStatus PreviousStatus,
    ProspectStatus NewStatus,
    string? Reason,
    Guid ChangedBy,
    DateTimeOffset ChangedAt);

public sealed record ProspectResponse(
    Guid Id,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Source,
    string? EventTypeInterest,
    DateOnly? EstimatedEventDate,
    int? EstimatedGuestCount,
    decimal? EstimatedBudget,
    string CurrencyCode,
    string? City,
    string? Notes,
    Guid? AssignedUserId,
    ProspectStatus Status,
    string? LostReason,
    Guid? ConvertedClientId,
    IReadOnlyList<ProspectActivityResponse> Activities,
    IReadOnlyList<ProspectStatusHistoryResponse> StatusHistory,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record ClientMatchSuggestionResponse(
    Guid ClientId,
    string DisplayName,
    string MatchField,
    string MatchValue);

public sealed record ConvertProspectRequest(
    Guid? ExistingClientId,
    ClientType? NewClientType,
    bool ConfirmCreateDespiteMatches);

public sealed record ConvertProspectResponse(
    Guid ProspectId,
    Guid ClientId,
    bool CreatedNewClient);

public sealed record LinkPreliminaryEventRequest(
    Guid? ExistingEventId,
    string? Name,
    string? EventType,
    DateTimeOffset? StartDateTime,
    string? TimeZone,
    string? City,
    string? CountryCode,
    int? EstimatedGuestCount);

public sealed record LinkPreliminaryEventResponse(
    Guid ProspectId,
    Guid EventId,
    bool CreatedNewEvent);
