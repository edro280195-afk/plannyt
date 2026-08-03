using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Guests.Application;

public sealed class GuestCsvImportService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    PortalAccessService portalAccessService,
    GuestPlanLimitService planLimitService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    private const int MaximumRows = 5000;
    private const long MaximumBytes = 5 * 1024 * 1024;
    private static readonly string[] SupportedColumns =
    [
        "GroupName",
        "GroupType",
        "AllowedGuestCount",
        "ContactName",
        "ContactPhone",
        "ContactEmail",
        "GuestFirstName",
        "GuestLastName",
        "AgeCategory",
        "IsPrimaryContact",
        "IsVip",
        "Tags"
    ];

    public async Task<GuestImportAnalysisResponse> AnalyzeAsync(
        Guid organizationId,
        Guid eventId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var access = await RequireAsync(
            organizationId,
            eventId,
            Permissions.GuestsImport,
            cancellationToken);
        return await AnalyzeAuthorizedAsync(
            organizationId,
            eventId,
            access.UserAccountId,
            file,
            cancellationToken);
    }

    public async Task<GuestImportAnalysisResponse> AnalyzePortalAsync(
        Guid eventId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsImport,
            cancellationToken);
        return await AnalyzeAuthorizedAsync(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            file,
            cancellationToken);
    }

    private async Task<GuestImportAnalysisResponse> AnalyzeAuthorizedAsync(
        Guid organizationId,
        Guid eventId,
        Guid actorUserId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        var isXlsx = string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase);
        var isCsv = string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase);
        if (!isXlsx && !isCsv)
        {
            throw new UnsupportedMediaTypeException(
                "Solo se permiten archivos CSV o Excel (.xlsx).");
        }

        if (file.Length is <= 0 or > MaximumBytes)
        {
            throw new PayloadTooLargeException(
                "El archivo debe contener datos y pesar como máximo 5 MB.");
        }

        string content;
        if (isXlsx)
        {
            await using var xlsxStream = file.OpenReadStream();
            content = ConvertXlsxToCsv(xlsxStream);
        }
        else
        {
            try
            {
                await using var stream = file.OpenReadStream();
                using var reader = new StreamReader(
                    stream,
                    new UTF8Encoding(false, true),
                    detectEncodingFromByteOrderMarks: true);
                content = await reader.ReadToEndAsync(cancellationToken);
            }
            catch (DecoderFallbackException)
            {
                throw new RequestValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["file"] = ["El CSV debe estar codificado en UTF-8."]
                    });
            }
        }

        var table = ParseCsv(content);
        var mapping = BuildDefaultMapping(table.Headers);
        var parsed = Analyze(table, mapping);
        var response = BuildAnalysis(Guid.Empty, mapping, table.Headers, parsed);
        var batch = GuestImportBatch.Create(
            organizationId,
            eventId,
            Path.GetFileName(file.FileName),
            content,
            JsonSerializer.Serialize(mapping),
            JsonSerializer.Serialize(parsed),
            parsed.Count,
            parsed.Count(row => row.Errors.Count == 0),
            parsed.Count(row => row.Errors.Count > 0),
            actorUserId,
            timeProvider.GetUtcNow());
        dbContext.GuestImportBatches.Add(batch);
        auditService.Add(
            organizationId,
            eventId,
            actorUserId,
            "guest_import.analyzed",
            nameof(GuestImportBatch),
            batch.Id,
            new Dictionary<string, object?>
            {
                ["fileName"] = batch.FileName,
                ["rows"] = batch.TotalRows,
                ["errors"] = batch.ErrorRows
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return response with { ImportId = batch.Id };
    }

    public async Task<GuestImportAnalysisResponse> UpdateMappingAsync(
        Guid organizationId,
        Guid eventId,
        Guid importId,
        UpdateGuestImportMappingRequest request,
        CancellationToken cancellationToken)
    {
        await RequireAsync(
            organizationId,
            eventId,
            Permissions.GuestsImport,
            cancellationToken);
        return await UpdateMappingAuthorizedAsync(
            organizationId,
            eventId,
            importId,
            request,
            cancellationToken);
    }

    public async Task<GuestImportAnalysisResponse> UpdatePortalMappingAsync(
        Guid eventId,
        Guid importId,
        UpdateGuestImportMappingRequest request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsImport,
            cancellationToken);
        return await UpdateMappingAuthorizedAsync(
            access.OrganizationId,
            eventId,
            importId,
            request,
            cancellationToken);
    }

    private async Task<GuestImportAnalysisResponse> UpdateMappingAuthorizedAsync(
        Guid organizationId,
        Guid eventId,
        Guid importId,
        UpdateGuestImportMappingRequest request,
        CancellationToken cancellationToken)
    {
        var batch = await FindBatchAsync(
            organizationId,
            eventId,
            importId,
            cancellationToken);
        var table = ParseCsv(batch.CsvContent);
        ValidateMapping(request.Mapping, table.Headers);
        var mapping = request.Mapping.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        var parsed = Analyze(table, mapping);
        batch.ReplaceAnalysis(
            JsonSerializer.Serialize(mapping),
            JsonSerializer.Serialize(parsed),
            parsed.Count,
            parsed.Count(row => row.Errors.Count == 0),
            parsed.Count(row => row.Errors.Count > 0));
        await dbContext.SaveChangesAsync(cancellationToken);
        return BuildAnalysis(importId, mapping, table.Headers, parsed);
    }

    public async Task<GuestImportResultResponse> ConfirmAsync(
        Guid organizationId,
        Guid eventId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var access = await RequireAsync(
            organizationId,
            eventId,
            Permissions.GuestsImport,
            cancellationToken);
        return await ConfirmAuthorizedAsync(
            organizationId,
            eventId,
            importId,
            access.UserAccountId,
            cancellationToken);
    }

    public async Task<GuestImportResultResponse> ConfirmPortalAsync(
        Guid eventId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsImport,
            cancellationToken);
        return await ConfirmAuthorizedAsync(
            access.OrganizationId,
            eventId,
            importId,
            access.UserAccountId,
            cancellationToken);
    }

    private async Task<GuestImportResultResponse> ConfirmAuthorizedAsync(
        Guid organizationId,
        Guid eventId,
        Guid importId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var batch = await FindBatchAsync(
            organizationId,
            eventId,
            importId,
            cancellationToken);
        if (batch.Status == GuestImportStatus.Completed)
        {
            return JsonSerializer.Deserialize<GuestImportResultResponse>(
                batch.ResultJson
                ?? throw new InvalidOperationException("Falta el resultado de importación."))
                ?? throw new InvalidOperationException("El resultado de importación es inválido.");
        }

        var rows = JsonSerializer.Deserialize<List<ParsedGuestImportRow>>(
            batch.AnalysisJson) ?? [];
        var invalid = rows.Where(row => row.Errors.Count > 0).ToList();
        if (invalid.Count > 0)
        {
            throw new ConflictException(
                "Corrige todos los errores del archivo antes de confirmar la importación.");
        }

        await planLimitService.EnsureCapacityAsync(
            organizationId,
            eventId,
            rows.Count,
            cancellationToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var existingTags = await dbContext.GuestTags.Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null)
            .ToListAsync(cancellationToken);
        var tagsByName = existingTags.ToDictionary(
            tag => tag.Name,
            StringComparer.OrdinalIgnoreCase);
        var groupsByName = new Dictionary<string, InvitationGroup>(
            StringComparer.OrdinalIgnoreCase);
        var createdGroups = 0;
        var createdGuests = 0;
        var reusedGroups = 0;
        foreach (var row in rows)
        {
            if (!groupsByName.TryGetValue(row.GroupName, out var group))
            {
                group = InvitationGroup.Create(
                    organizationId,
                    eventId,
                    row.GroupType,
                    row.GroupName,
                    row.ContactName,
                    GuestRequestValidator.NormalizePhone(row.ContactPhone),
                    GuestRequestValidator.NormalizeEmail(row.ContactEmail),
                    row.AllowedGuestCount,
                    false,
                    0,
                    "CSV",
                    null,
                    actorUserId,
                    now);
                dbContext.InvitationGroups.Add(group);
                groupsByName.Add(row.GroupName, group);
                createdGroups++;
                foreach (var tagName in row.Tags)
                {
                    if (!tagsByName.TryGetValue(tagName, out var tag))
                    {
                        tag = GuestTag.Create(
                            organizationId,
                            eventId,
                            tagName,
                            "sky",
                            now);
                        dbContext.GuestTags.Add(tag);
                        tagsByName.Add(tagName, tag);
                    }

                    dbContext.InvitationGroupTags.Add(
                        InvitationGroupTag.Create(
                            organizationId,
                            eventId,
                            group.Id,
                            tag.Id));
                }
            }
            else
            {
                reusedGroups++;
            }

            var guest = EventGuest.Create(
                organizationId,
                eventId,
                group.Id,
                null,
                row.GuestFirstName,
                row.GuestLastName,
                null,
                null,
                GuestType.Other,
                row.AgeCategory,
                row.IsPrimaryContact,
                true,
                false,
                row.IsVip,
                createdGuests,
                null,
                actorUserId,
                now);
            dbContext.EventGuests.Add(guest);
            createdGuests++;
        }

        var result = new GuestImportResultResponse(
            importId,
            GuestImportStatus.Completed,
            createdGroups,
            createdGuests,
            reusedGroups,
            0,
            [],
            now);
        batch.Complete(JsonSerializer.Serialize(result), now);
        auditService.Add(
            organizationId,
            eventId,
            actorUserId,
            "guest_import.completed",
            nameof(GuestImportBatch),
            batch.Id,
            new Dictionary<string, object?>
            {
                ["createdGroups"] = createdGroups,
                ["createdGuests"] = createdGuests
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<GuestImportResultResponse> GetResultAsync(
        Guid organizationId,
        Guid eventId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        await RequireAsync(
            organizationId,
            eventId,
            Permissions.GuestsImport,
            cancellationToken);
        return await GetResultAuthorizedAsync(
            organizationId,
            eventId,
            importId,
            cancellationToken);
    }

    public async Task<GuestImportResultResponse> GetPortalResultAsync(
        Guid eventId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsImport,
            cancellationToken);
        return await GetResultAuthorizedAsync(
            access.OrganizationId,
            eventId,
            importId,
            cancellationToken);
    }

    private async Task<GuestImportResultResponse> GetResultAuthorizedAsync(
        Guid organizationId,
        Guid eventId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var batch = await FindBatchAsync(
            organizationId,
            eventId,
            importId,
            cancellationToken);
        if (batch.Status != GuestImportStatus.Completed || batch.ResultJson is null)
        {
            throw new ConflictException("La importación todavía no ha finalizado.");
        }

        return JsonSerializer.Deserialize<GuestImportResultResponse>(batch.ResultJson)
            ?? throw new InvalidOperationException("El reporte almacenado es inválido.");
    }

    public async Task<byte[]> ExportAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await RequireAsync(
            organizationId,
            eventId,
            Permissions.GuestsExport,
            cancellationToken);
        var rows = await dbContext.EventGuests.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null)
            .Join(
                dbContext.InvitationGroups.AsNoTracking(),
                guest => new
                {
                    guest.OrganizationId,
                    guest.EventId,
                    GroupId = guest.InvitationGroupId
                },
                group => new
                {
                    group.OrganizationId,
                    group.EventId,
                    GroupId = (Guid?)group.Id
                },
                (guest, group) => new { Guest = guest, Group = group })
            .OrderBy(item => item.Group.DisplayName)
            .ThenBy(item => item.Guest.SortOrder)
            .ToListAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine(
            "GroupName,GroupType,AllowedGuestCount,ContactName,ContactPhone,ContactEmail,GuestFirstName,GuestLastName,AgeCategory,IsPrimaryContact,IsVip,Tags");
        foreach (var item in rows)
        {
            var tags = await dbContext.InvitationGroupTags.AsNoTracking()
                .Where(relation =>
                    relation.OrganizationId == organizationId
                    && relation.EventId == eventId
                    && relation.InvitationGroupId == item.Group.Id)
                .Join(
                    dbContext.GuestTags.AsNoTracking(),
                    relation => relation.GuestTagId,
                    tag => tag.Id,
                    (_, tag) => tag.Name)
                .ToListAsync(cancellationToken);
            builder.AppendLine(string.Join(
                ',',
                CsvCell(item.Group.DisplayName),
                CsvCell(item.Group.GroupType.ToString()),
                item.Group.AllowedGuestCount.ToString(CultureInfo.InvariantCulture),
                CsvCell(item.Group.ContactName),
                CsvCell(item.Group.ContactPhone),
                CsvCell(item.Group.ContactEmail),
                CsvCell(item.Guest.FirstName),
                CsvCell(item.Guest.LastName),
                CsvCell(item.Guest.AgeCategory.ToString()),
                item.Guest.IsPrimaryContact ? "true" : "false",
                item.Guest.IsVip ? "true" : "false",
                CsvCell(string.Join('|', tags))));
        }

        return new UTF8Encoding(true).GetBytes(builder.ToString());
    }

    public static GuestImportTemplateFile GetTemplate(string? format, string? language)
    {
        var normalizedFormat = (format ?? "csv").Trim().ToLowerInvariant();
        var normalizedLanguage = (language ?? "es").Trim().ToLowerInvariant();
        if (!GuestImportLocalization.SupportedFormats.Contains(normalizedFormat))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["format"] = ["El formato debe ser csv o xlsx."]
                });
        }

        if (!GuestImportLocalization.SupportedLanguages.Contains(normalizedLanguage))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["language"] = ["El idioma debe ser es o en."]
                });
        }

        return normalizedFormat == "xlsx"
            ? BuildXlsxTemplate(normalizedLanguage)
            : BuildCsvTemplate(normalizedLanguage);
    }

    private static GuestImportTemplateFile BuildCsvTemplate(string language)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(
            ',',
            GuestImportLocalization.ColumnOrder.Select(
                column => GuestImportLocalization.ColumnLabel(column, language))));
        foreach (var row in BuildExampleRows(language))
        {
            builder.AppendLine(string.Join(',', row));
        }

        var fileName = language == "en" ? "guest-import-template.csv" : "plantilla-invitados.csv";
        return new GuestImportTemplateFile(
            new UTF8Encoding(true).GetBytes(builder.ToString()),
            "text/csv; charset=utf-8",
            fileName);
    }

    private static GuestImportTemplateFile BuildXlsxTemplate(string language)
    {
        using var workbook = new XLWorkbook();
        var columns = GuestImportLocalization.ColumnOrder;

        var data = workbook.Worksheets.Add(language == "en" ? "Guests" : "Invitados");
        for (var index = 0; index < columns.Count; index++)
        {
            data.Cell(1, index + 1).Value = GuestImportLocalization.ColumnLabel(columns[index], language);
        }

        var exampleRow = 2;
        foreach (var row in BuildExampleRows(language))
        {
            for (var index = 0; index < row.Length; index++)
            {
                data.Cell(exampleRow, index + 1).Value = row[index];
            }

            exampleRow++;
        }

        data.Row(1).Style.Font.Bold = true;
        data.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8ECFB");
        data.SheetView.FreezeRows(1);
        data.Columns().AdjustToContents();

        var guide = workbook.Worksheets.Add(language == "en" ? "Instructions" : "Instrucciones");
        var guideHeaders = language == "en"
            ? new[] { "Column", "Required", "Description", "Valid values" }
            : new[] { "Columna", "Obligatorio", "Descripción", "Valores válidos" };
        for (var index = 0; index < guideHeaders.Length; index++)
        {
            guide.Cell(1, index + 1).Value = guideHeaders[index];
        }

        var guideRow = 2;
        foreach (var field in GuestImportLocalization.FieldGuide(language))
        {
            guide.Cell(guideRow, 1).Value = field.Label;
            guide.Cell(guideRow, 2).Value = GuestImportLocalization.BooleanLabel(field.Required, language);
            guide.Cell(guideRow, 3).Value = field.Description;
            guide.Cell(guideRow, 4).Value = field.ValidValues;
            guideRow++;
        }

        guide.Row(1).Style.Font.Bold = true;
        guide.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8ECFB");
        guide.Column(3).Width = 48;
        guide.Column(4).Width = 48;
        guide.Column(3).Style.Alignment.WrapText = true;
        guide.Column(4).Style.Alignment.WrapText = true;
        guide.Column(1).AdjustToContents();
        guide.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = language == "en" ? "guest-import-template.xlsx" : "plantilla-invitados.xlsx";
        return new GuestImportTemplateFile(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static IReadOnlyList<string[]> BuildExampleRows(string language)
    {
        var groupName = language == "en" ? "García Family" : "Familia García";
        var groupType = GuestImportLocalization.GroupTypeLabel(InvitationGroupType.Family, language);
        var age = GuestImportLocalization.AgeCategoryLabel(AgeCategory.Adult, language);
        var yes = GuestImportLocalization.BooleanLabel(true, language);
        var no = GuestImportLocalization.BooleanLabel(false, language);
        var tags = language == "en" ? "Family|VIP" : "Familia|VIP";
        return
        [
            [
                groupName, groupType, "4", "Ana García", "8991234567", "ana@example.com",
                "Ana", "García", age, yes, yes, tags
            ],
            [
                groupName, groupType, "4", "Ana García", "8991234567", "ana@example.com",
                "Luis", "García", age, no, no, tags
            ]
        ];
    }

    private async Task<Organizations.Authorization.TenantAccess> RequireAsync(
        Guid organizationId,
        Guid eventId,
        string permission,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            permission,
            eventId,
            cancellationToken);
        if (!await dbContext.Events.AsNoTracking().AnyAsync(entity =>
                entity.OrganizationId == organizationId && entity.Id == eventId,
                cancellationToken))
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        return access;
    }

    private async Task<GuestImportBatch> FindBatchAsync(
        Guid organizationId,
        Guid eventId,
        Guid importId,
        CancellationToken cancellationToken) =>
        await dbContext.GuestImportBatches.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == importId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la importación.");

    private static GuestImportAnalysisResponse BuildAnalysis(
        Guid importId,
        IReadOnlyDictionary<string, string> mapping,
        IReadOnlyList<string> headers,
        IReadOnlyList<ParsedGuestImportRow> rows) =>
        new(
            importId,
            GuestImportStatus.Analyzed,
            headers,
            mapping,
            rows.Count,
            rows.Count(row => row.Errors.Count == 0),
            rows.Count(row => row.Errors.Count > 0),
            rows.Take(50).Select(row => new GuestImportRowPreview(
                row.RowNumber,
                row.GroupName,
                $"{row.GuestFirstName} {row.GuestLastName}".Trim(),
                row.Errors.Count == 0,
                row.Errors)).ToList());

    private static IReadOnlyList<ParsedGuestImportRow> Analyze(
        CsvTable table,
        IReadOnlyDictionary<string, string> mapping)
    {
        var result = new List<ParsedGuestImportRow>(table.Rows.Count);
        foreach (var source in table.Rows)
        {
            var errors = new List<string>();
            var groupName = Get(source, mapping, "GroupName").Trim();
            var firstName = Get(source, mapping, "GuestFirstName").Trim();
            var lastName = Get(source, mapping, "GuestLastName").Trim();
            if (groupName.Length == 0)
            {
                errors.Add($"Falta {FieldName("GroupName")}.");
            }

            if (firstName.Length == 0 && lastName.Length == 0)
            {
                errors.Add("Falta el nombre del invitado.");
            }

            var groupTypeText = Get(source, mapping, "GroupType");
            if (!GuestImportLocalization.TryParseGroupType(groupTypeText, out var groupType))
            {
                groupType = InvitationGroupType.Other;
                errors.Add($"{FieldName("GroupType")} no es válido.");
            }

            if (!int.TryParse(
                    Get(source, mapping, "AllowedGuestCount"),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var capacity)
                || capacity < 1)
            {
                capacity = 1;
                errors.Add($"{FieldName("AllowedGuestCount")} debe ser un entero mayor a cero.");
            }

            var ageText = Get(source, mapping, "AgeCategory");
            if (!GuestImportLocalization.TryParseAgeCategory(ageText, out var age))
            {
                age = AgeCategory.Adult;
                errors.Add($"{FieldName("AgeCategory")} no es válido.");
            }

            var primary = ParseBoolean(
                Get(source, mapping, "IsPrimaryContact"),
                "IsPrimaryContact",
                errors);
            var vip = ParseBoolean(Get(source, mapping, "IsVip"), "IsVip", errors);
            var email = EmptyToNull(Get(source, mapping, "ContactEmail"));
            if (email is not null
                && !GuestRequestValidator.IsValidEmail(email))
            {
                errors.Add($"{FieldName("ContactEmail")} no es válido.");
            }

            result.Add(new ParsedGuestImportRow(
                source.RowNumber,
                groupName,
                groupType,
                capacity,
                EmptyToNull(Get(source, mapping, "ContactName")),
                EmptyToNull(Get(source, mapping, "ContactPhone")),
                email,
                firstName,
                lastName,
                age,
                primary,
                vip,
                Get(source, mapping, "Tags")
                    .Split('|', StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                errors));
        }

        foreach (var group in result.GroupBy(row => row.GroupName, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > group.First().AllowedGuestCount)
            {
                foreach (var row in group)
                {
                    ((List<string>)row.Errors).Add(
                        $"Las filas del grupo exceden {FieldName("AllowedGuestCount")}.");
                }
            }

            if (group.Count(row => row.IsPrimaryContact) > 1)
            {
                foreach (var row in group.Where(row => row.IsPrimaryContact))
                {
                    ((List<string>)row.Errors).Add(
                        "El grupo tiene más de un contacto principal.");
                }
            }

            if (group.Any(row =>
                    row.GroupType != group.First().GroupType
                    || row.AllowedGuestCount != group.First().AllowedGuestCount))
            {
                foreach (var row in group)
                {
                    ((List<string>)row.Errors).Add(
                        "Las filas del grupo no coinciden en tipo o capacidad.");
                }
            }
        }

        return result;
    }

    private static bool ParseBoolean(
        string value,
        string column,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (GuestImportLocalization.TryParseBoolean(value, out var result))
        {
            return result;
        }

        errors.Add($"{FieldName(column)} debe ser Sí o No.");
        return false;
    }

    private static string FieldName(string column) =>
        GuestImportLocalization.ColumnLabel(column, "es");

    private static Dictionary<string, string> BuildDefaultMapping(
        IReadOnlyList<string> headers)
    {
        var resolved = headers
            .Select(header => (
                Header: header,
                Resolved: GuestImportLocalization.TryResolveColumn(header, out var column)
                    ? column
                    : null))
            .Where(item => item.Resolved is not null)
            .ToList();
        return SupportedColumns.ToDictionary(
            column => column,
            column => resolved.FirstOrDefault(item =>
                    string.Equals(item.Resolved, column, StringComparison.OrdinalIgnoreCase))
                .Header ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateMapping(
        IReadOnlyDictionary<string, string> mapping,
        IReadOnlyList<string> headers)
    {
        var unknownKeys = mapping.Keys.Except(
            SupportedColumns,
            StringComparer.OrdinalIgnoreCase).ToList();
        var unknownHeaders = mapping.Values
            .Where(value => value.Length > 0)
            .Except(headers, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unknownKeys.Count > 0 || unknownHeaders.Count > 0)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["mapping"] = ["El mapeo contiene columnas no compatibles."]
                });
        }
    }

    private static string Get(
        CsvRow row,
        IReadOnlyDictionary<string, string> mapping,
        string logicalName)
    {
        if (!mapping.TryGetValue(logicalName, out var header)
            || string.IsNullOrWhiteSpace(header))
        {
            return string.Empty;
        }

        return row.Values.GetValueOrDefault(header) ?? string.Empty;
    }

    /// <summary>
    /// Lee la primera hoja de un archivo subido en Excel y la convierte a texto CSV
    /// equivalente, para que el resto del importador (almacenamiento del lote,
    /// remapeo posterior, análisis) siga operando exactamente igual que con un CSV.
    /// </summary>
    private static string ConvertXlsxToCsv(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var usedRange = worksheet.RangeUsed();
            if (usedRange is null)
            {
                throw new RequestValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["file"] = ["El archivo no contiene datos."]
                    });
            }

            var builder = new StringBuilder();
            var firstColumn = usedRange.FirstColumn().ColumnNumber();
            var lastColumn = usedRange.LastColumn().ColumnNumber();
            foreach (var row in usedRange.Rows())
            {
                builder.AppendLine(string.Join(
                    ',',
                    row.Cells(firstColumn, lastColumn).Select(cell => CsvCell(cell.GetString()))));
            }

            return builder.ToString();
        }
        catch (Exception exception) when (
            exception is not ApiException and not OperationCanceledException)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["No se pudo leer el archivo de Excel."]
                });
        }
    }

    private static CsvTable ParseCsv(string content)
    {
        var records = new List<List<string>>();
        var currentRecord = new List<string>();
        var currentField = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < content.Length
                    && content[index + 1] == '"')
                {
                    currentField.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
                else
                {
                    currentField.Append(character);
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == ',')
            {
                currentRecord.Add(currentField.ToString());
                currentField.Clear();
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < content.Length
                    && content[index + 1] == '\n')
                {
                    index++;
                }

                currentRecord.Add(currentField.ToString());
                currentField.Clear();
                if (currentRecord.Any(value => value.Length > 0))
                {
                    records.Add(currentRecord);
                }

                currentRecord = [];
            }
            else
            {
                currentField.Append(character);
            }
        }

        if (quoted)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["El CSV contiene comillas sin cerrar."]
                });
        }

        currentRecord.Add(currentField.ToString());
        if (currentRecord.Any(value => value.Length > 0))
        {
            records.Add(currentRecord);
        }

        if (records.Count < 2)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["El CSV requiere encabezados y al menos una fila."]
                });
        }

        var headers = records[0].Select(value => value.Trim().TrimStart('\uFEFF')).ToList();
        if (headers.Count != headers.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            || headers.Any(string.IsNullOrWhiteSpace))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["Los encabezados deben ser únicos y no pueden estar vacíos."]
                });
        }

        if (records.Count - 1 > MaximumRows)
        {
            throw new PayloadTooLargeException(
                $"El CSV admite como máximo {MaximumRows} filas.");
        }

        var rows = records.Skip(1).Select((record, index) =>
        {
            var values = headers.Select((header, column) =>
                    new KeyValuePair<string, string>(
                        header,
                        column < record.Count ? record[column] : string.Empty))
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            return new CsvRow(index + 2, values);
        }).ToList();
        return new CsvTable(headers, rows);
    }

    private static string CsvCell(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && "=+-@\t\r".Contains(safe[0]))
        {
            safe = $"'{safe}";
        }

        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private static string? EmptyToNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CsvTable(
        IReadOnlyList<string> Headers,
        IReadOnlyList<CsvRow> Rows);

    private sealed record CsvRow(
        int RowNumber,
        IReadOnlyDictionary<string, string> Values);
}
