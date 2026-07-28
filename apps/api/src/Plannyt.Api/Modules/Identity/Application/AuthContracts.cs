using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Identity.Application;

public sealed record RegisterPlannerRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string OrganizationName,
    OrganizationType OrganizationType,
    string TimeZone,
    string CountryCode,
    string CurrencyCode);

public sealed record LoginRequest(
    string Email,
    string Password,
    bool IsPersistent = true);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    Guid UserAccountId,
    string Email,
    Guid? OrganizationId);

public sealed record AuthSessionResult(
    AuthResponse Response,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool IsPersistent);

public sealed record MeResponse(
    Guid UserAccountId,
    string Email,
    IReadOnlyList<MeOrganizationResponse> Organizations,
    IReadOnlyList<MeEventAccessResponse> EventAccesses);

public sealed record MeOrganizationResponse(
    Guid OrganizationId,
    string OrganizationName,
    Guid MembershipId,
    OrganizationRole Role,
    IReadOnlySet<string> Permissions);

public sealed record MeEventAccessResponse(
    Guid OrganizationId,
    Guid EventId,
    string EventName,
    string Role);
