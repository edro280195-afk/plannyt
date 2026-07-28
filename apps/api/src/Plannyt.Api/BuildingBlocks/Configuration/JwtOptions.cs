using System.ComponentModel.DataAnnotations;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    [MinLength(64)]
    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenMinutes { get; init; } = 10;

    public int RefreshTokenDays { get; init; } = 30;
}
