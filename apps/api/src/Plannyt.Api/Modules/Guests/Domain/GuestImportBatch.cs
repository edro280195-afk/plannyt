using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Guests.Domain;

public sealed class GuestImportBatch : ITenantEntity
{
    private GuestImportBatch()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string CsvContent { get; private set; } = string.Empty;
    public string MappingJson { get; private set; } = "{}";
    public string AnalysisJson { get; private set; } = "{}";
    public string? ResultJson { get; private set; }
    public GuestImportStatus Status { get; private set; }
    public int TotalRows { get; private set; }
    public int ValidRows { get; private set; }
    public int ErrorRows { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static GuestImportBatch Create(
        Guid organizationId,
        Guid eventId,
        string fileName,
        string csvContent,
        string mappingJson,
        string analysisJson,
        int totalRows,
        int validRows,
        int errorRows,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            FileName = fileName,
            CsvContent = csvContent,
            MappingJson = mappingJson,
            AnalysisJson = analysisJson,
            Status = GuestImportStatus.Analyzed,
            TotalRows = totalRows,
            ValidRows = validRows,
            ErrorRows = errorRows,
            CreatedBy = createdBy,
            CreatedAt = now
        };

    public void ReplaceAnalysis(
        string mappingJson,
        string analysisJson,
        int totalRows,
        int validRows,
        int errorRows)
    {
        if (Status != GuestImportStatus.Analyzed)
        {
            throw new DomainRuleException("Una importación finalizada ya no puede remapearse.");
        }

        MappingJson = mappingJson;
        AnalysisJson = analysisJson;
        TotalRows = totalRows;
        ValidRows = validRows;
        ErrorRows = errorRows;
    }

    public void Complete(string resultJson, DateTimeOffset now)
    {
        Status = GuestImportStatus.Completed;
        ResultJson = resultJson;
        CompletedAt = now;
        CsvContent = string.Empty;
    }
}
