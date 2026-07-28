using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Access.Application;

public sealed record CreateOrganizationInvitationRequest(
    string TargetEmail,
    OrganizationRole IntendedOrganizationRole);

public sealed record CreateEventInvitationRequest(
    string TargetEmail,
    EventAccessRole IntendedEventRole);

public sealed record InvitationCreatedResponse(
    Guid Id,
    InvitationType InvitationType,
    string TargetEmail,
    DateTimeOffset ExpiresAt,
    string InvitationUrl);

public sealed record InvitationPublicResponse(
    InvitationType InvitationType,
    string OrganizationName,
    string? EventName,
    string TargetEmail,
    string IntendedRole,
    DateTimeOffset ExpiresAt,
    InvitationPublicStatus Status);

public enum InvitationPublicStatus
{
    Pending,
    Expired,
    Accepted,
    Revoked
}

public sealed record AcceptInvitationRequest(
    string? FirstName,
    string? LastName,
    string? ContactPhone,
    string? PreferredLanguage,
    string? TimeZone);

public sealed record RegisterAndAcceptInvitationRequest(
    string Password,
    string FirstName,
    string LastName,
    string? ContactPhone,
    string PreferredLanguage,
    string TimeZone);

public sealed record InvitationAcceptanceResponse(
    InvitationType InvitationType,
    Guid? OrganizationId,
    Guid? EventId);

public sealed record EventAccessResponse(
    Guid Id,
    Guid UserAccountId,
    string Email,
    EventAccessRole Role,
    EventAccessStatus Status,
    DateTimeOffset StartsAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt);

public sealed record PortalEventListItemResponse(
    Guid Id,
    string Name,
    string EventType,
    DateTimeOffset StartDateTime,
    DateTimeOffset? EndDateTime,
    string TimeZone,
    string City,
    string CountryCode,
    string? SharedDescription,
    int? EstimatedGuestCount);

public sealed record PortalParticipantResponse(
    Guid Id,
    string DisplayName,
    string ParticipantType,
    int DisplayOrder,
    string? SharedDescription);

public sealed record PortalEventResponse(
    Guid Id,
    string Name,
    string EventType,
    DateTimeOffset StartDateTime,
    DateTimeOffset? EndDateTime,
    string TimeZone,
    string City,
    string CountryCode,
    string? SharedDescription,
    int? EstimatedGuestCount,
    IReadOnlyList<PortalParticipantResponse> Participants);
