namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool MigrateOnStartup { get; init; }
}
