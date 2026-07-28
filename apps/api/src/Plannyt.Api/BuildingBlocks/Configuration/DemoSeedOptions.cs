namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class DemoSeedOptions
{
    public const string SectionName = "DemoSeed";

    public bool Enabled { get; init; }

    public string PlannerEmail { get; init; } = string.Empty;

    public string PlannerPassword { get; init; } = string.Empty;
}
