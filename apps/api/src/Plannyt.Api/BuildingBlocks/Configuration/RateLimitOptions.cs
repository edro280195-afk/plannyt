using System.ComponentModel.DataAnnotations;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    [Range(1, 10_000)]
    public int SensitivePermitLimit { get; init; } = 10;

    [Range(1, 10_000)]
    public int SessionPermitLimit { get; init; } = 60;
}
