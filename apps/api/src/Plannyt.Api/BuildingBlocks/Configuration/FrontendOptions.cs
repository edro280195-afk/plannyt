using System.ComponentModel.DataAnnotations;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    [Required]
    [Url]
    public string PublicUrl { get; init; } = string.Empty;
}
