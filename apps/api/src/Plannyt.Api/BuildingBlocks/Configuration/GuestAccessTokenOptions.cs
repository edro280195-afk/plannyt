using System.ComponentModel.DataAnnotations;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class GuestAccessTokenOptions
{
    public const string SectionName = "GuestAccessTokens";

    [Required]
    [MinLength(64)]
    public string DerivationKey { get; init; } = string.Empty;
}
