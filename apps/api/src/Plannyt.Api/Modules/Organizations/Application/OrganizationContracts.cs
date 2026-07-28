using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Organizations.Application;

public sealed record UpdateOrganizationRequest(
    string Name,
    OrganizationType OrganizationType,
    string TimeZone,
    string CountryCode,
    string CurrencyCode);

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    OrganizationType OrganizationType,
    string TimeZone,
    string CountryCode,
    string CurrencyCode,
    OrganizationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrganizationMemberResponse(
    Guid MembershipId,
    Guid UserAccountId,
    Guid PersonId,
    string DisplayName,
    string Email,
    OrganizationRole Role,
    MembershipStatus Status,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ExpiresAt);
