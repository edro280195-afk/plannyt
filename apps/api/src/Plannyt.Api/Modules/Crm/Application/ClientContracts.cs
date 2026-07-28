using Plannyt.Api.Modules.Crm.Domain;

namespace Plannyt.Api.Modules.Crm.Application;

public sealed record PersonProfileRequest(
    string FirstName,
    string LastName,
    string? ContactEmail,
    string? ContactPhone,
    string PreferredLanguage,
    string TimeZone);

public sealed record CreateClientRequest(
    ClientType ClientType,
    string DisplayName,
    string? CompanyName,
    string? Source,
    PersonProfileRequest? Person);

public sealed record UpdateClientRequest(
    string DisplayName,
    string? CompanyName,
    string? Source,
    PersonProfileRequest? Person);

public sealed record UpsertClientContactRequest(
    string FirstName,
    string LastName,
    string? ContactEmail,
    string? ContactPhone,
    string PreferredLanguage,
    string TimeZone,
    string ContactRole,
    bool IsPrimary);

public sealed record PersonProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    string? ContactEmail,
    string? ContactPhone,
    string PreferredLanguage,
    string TimeZone);

public sealed record ClientContactResponse(
    Guid Id,
    Guid PersonId,
    string DisplayName,
    string? ContactEmail,
    string? ContactPhone,
    string ContactRole,
    bool IsPrimary);

public sealed record ClientListItemResponse(
    Guid Id,
    ClientType ClientType,
    string DisplayName,
    string? CompanyName,
    ClientStatus Status,
    string? Source,
    DateTimeOffset UpdatedAt);

public sealed record ClientResponse(
    Guid Id,
    ClientType ClientType,
    string DisplayName,
    string? CompanyName,
    ClientStatus Status,
    string? Source,
    PersonProfileResponse? Person,
    IReadOnlyList<ClientContactResponse> Contacts,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ArchivedAt);
