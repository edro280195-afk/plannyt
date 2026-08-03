using System.Text;
using ClosedXML.Excel;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Guests.Application;
using Plannyt.Api.Modules.Guests.Domain;

namespace Plannyt.Api.UnitTests.Guests;

public sealed class GuestImportLocalizationTests
{
    [Theory]
    [InlineData("GroupName", "GroupName")]
    [InlineData("Nombre del grupo", "GroupName")]
    [InlineData("Group name", "GroupName")]
    [InlineData("Tipo de grupo", "GroupType")]
    [InlineData("Allowed guests", "AllowedGuestCount")]
    public void TryResolveColumn_AcceptsTechnicalSpanishAndEnglishHeaders(
        string header,
        string expectedColumn)
    {
        var resolved = GuestImportLocalization.TryResolveColumn(header, out var column);

        Assert.True(resolved);
        Assert.Equal(expectedColumn, column);
    }

    [Theory]
    [InlineData("Familia", InvitationGroupType.Family)]
    [InlineData("Corporate table", InvitationGroupType.CorporateTable)]
    public void TryParseGroupType_AcceptsHumanReadableValues(
        string text,
        InvitationGroupType expected)
    {
        Assert.True(GuestImportLocalization.TryParseGroupType(text, out var groupType));
        Assert.Equal(expected, groupType);
    }

    [Theory]
    [InlineData("Adulto", AgeCategory.Adult)]
    [InlineData("Infant", AgeCategory.Infant)]
    public void TryParseAgeCategory_AcceptsHumanReadableValues(
        string text,
        AgeCategory expected)
    {
        Assert.True(GuestImportLocalization.TryParseAgeCategory(text, out var ageCategory));
        Assert.Equal(expected, ageCategory);
    }

    [Theory]
    [InlineData("S\u00ed", true)]
    [InlineData("si", true)]
    [InlineData("yes", true)]
    [InlineData("No", false)]
    public void TryParseBoolean_AcceptsSpanishAndEnglishValues(string text, bool expected)
    {
        var parsed = GuestImportLocalization.TryParseBoolean(text, out var value);

        Assert.True(parsed);
        Assert.Equal(expected, value);
    }

    [Fact]
    public void GetTemplate_CsvEnglish_UsesReadableHeadersAndFileMetadata()
    {
        var template = GuestCsvImportService.GetTemplate("csv", "en");

        Assert.Equal("text/csv; charset=utf-8", template.ContentType);
        Assert.Equal("guest-import-template.csv", template.FileName);
        var csv = Encoding.UTF8.GetString(template.Content);
        Assert.StartsWith("Group name,Group type,Allowed guests", csv, StringComparison.Ordinal);
        Assert.Contains("Garc\u00eda Family,Family,4", csv, StringComparison.Ordinal);
    }

    [Fact]
    public void GetTemplate_XlsxSpanish_IncludesInstructionsSheet()
    {
        var template = GuestCsvImportService.GetTemplate("xlsx", "es");

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            template.ContentType);
        Assert.Equal("plantilla-invitados.xlsx", template.FileName);
        using var stream = new MemoryStream(template.Content);
        using var workbook = new XLWorkbook(stream);

        var data = workbook.Worksheet("Invitados");
        Assert.Equal("Nombre del grupo", data.Cell(1, 1).GetString());
        Assert.Equal("Familia Garc\u00eda", data.Cell(2, 1).GetString());
        Assert.Equal("S\u00ed", data.Cell(2, 10).GetString());

        var instructions = workbook.Worksheet("Instrucciones");
        Assert.Equal("Columna", instructions.Cell(1, 1).GetString());
        Assert.Equal("Valores v\u00e1lidos", instructions.Cell(1, 4).GetString());
        Assert.Equal("Nombre del grupo", instructions.Cell(2, 1).GetString());
    }

    [Theory]
    [InlineData("pdf", "es", "format")]
    [InlineData("csv", "fr", "language")]
    public void GetTemplate_RejectsUnsupportedFormatOrLanguage(
        string format,
        string language,
        string errorKey)
    {
        var exception = Assert.Throws<RequestValidationException>(
            () => GuestCsvImportService.GetTemplate(format, language));

        Assert.Contains(errorKey, exception.Errors.Keys);
    }
}
