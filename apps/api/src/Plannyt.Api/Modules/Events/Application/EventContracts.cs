using Plannyt.Api.Modules.Events.Domain;

namespace Plannyt.Api.Modules.Events.Application;

public sealed record CreateEventRequest(
    string Name,
    string EventType,
    DateTimeOffset StartDateTime,
    DateTimeOffset? EndDateTime,
    string TimeZone,
    string City,
    string CountryCode,
    string? SharedDescription,
    int? EstimatedGuestCount);

public sealed record UpdateEventRequest(
    string Name,
    string EventType,
    DateTimeOffset StartDateTime,
    DateTimeOffset? EndDateTime,
    string TimeZone,
    string City,
    string CountryCode,
    string? SharedDescription,
    int? EstimatedGuestCount);

public sealed record ChangeEventStatusRequest(
    EventStatus NewStatus,
    string? Reason);

public sealed record CreateEventClientRequest(
    Guid ClientId,
    EventClientRelationshipType RelationshipType,
    bool IsPrimary,
    bool HasTransferAuthority);

public sealed record UpsertEventParticipantRequest(
    string FirstName,
    string LastName,
    string? ContactEmail,
    string? ContactPhone,
    string PreferredLanguage,
    string TimeZone,
    string ParticipantType,
    int DisplayOrder,
    bool IsVisibleToClient,
    string? SharedDescription);

public sealed record EventListItemResponse(
    Guid Id,
    string Name,
    string EventType,
    EventStatus Status,
    DateTimeOffset StartDateTime,
    DateTimeOffset? EndDateTime,
    string TimeZone,
    string City,
    int? EstimatedGuestCount,
    DateTimeOffset UpdatedAt);

public sealed record EventStatusHistoryResponse(
    Guid Id,
    EventStatus PreviousStatus,
    EventStatus NewStatus,
    string? Reason,
    Guid ChangedBy,
    DateTimeOffset ChangedAt);

public sealed record EventResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string EventType,
    EventStatus Status,
    DateTimeOffset StartDateTime,
    DateTimeOffset? EndDateTime,
    string TimeZone,
    string City,
    string CountryCode,
    string? SharedDescription,
    int? EstimatedGuestCount,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt,
    IReadOnlyList<EventStatusHistoryResponse> StatusHistory);

public sealed record EventClientResponse(
    Guid Id,
    Guid ClientId,
    string ClientDisplayName,
    EventClientRelationshipType RelationshipType,
    bool IsPrimary,
    bool HasTransferAuthority);

public sealed record EventParticipantResponse(
    Guid Id,
    Guid PersonId,
    string DisplayName,
    string? ContactEmail,
    string? ContactPhone,
    string ParticipantType,
    int DisplayOrder,
    bool IsVisibleToClient,
    string? SharedDescription);
