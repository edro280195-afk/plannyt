using System.ComponentModel.DataAnnotations;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    [Required]
    public string RootPath { get; init; } = "storage";
}
