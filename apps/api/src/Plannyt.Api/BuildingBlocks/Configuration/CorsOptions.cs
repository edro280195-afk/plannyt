using System.ComponentModel.DataAnnotations;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    [Required]
    [Url]
    public string AllowedOrigin { get; init; } = string.Empty;
}
