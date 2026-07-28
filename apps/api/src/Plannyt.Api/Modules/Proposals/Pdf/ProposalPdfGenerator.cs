using System.Globalization;
using System.Text;
using Plannyt.Api.Modules.Proposals.Application;

namespace Plannyt.Api.Modules.Proposals.Pdf;

public sealed class ProposalPdfGenerator(TimeProvider timeProvider)
    : IProposalPdfGenerator
{
    private const int MaxBodyLines = 38;
    private static readonly CultureInfo MoneyCulture =
        CultureInfo.GetCultureInfo("es-MX");

    public byte[] Generate(ProposalPublicResponse proposal)
    {
        var pages = BuildPages(proposal);
        return BuildPdf(pages);
    }

    private IReadOnlyList<IReadOnlyList<PdfTextLine>> BuildPages(
        ProposalPublicResponse proposal)
    {
        var content = new List<PdfTextLine>
        {
            new($"Propuesta {proposal.ProposalNumber}", 18, true),
            new($"Versión {proposal.VersionNumber}", 11, false),
            new(string.Empty, 8, false),
            new($"Para: {proposal.RecipientName}", 12, true)
        };
        if (!string.IsNullOrWhiteSpace(proposal.EventSummary))
        {
            AddWrapped(content, $"Evento: {proposal.EventSummary}", 11);
        }

        content.Add(new(
            $"Vigencia: {proposal.ValidUntil:dd/MM/yyyy}",
            11,
            false));
        content.Add(new(string.Empty, 8, false));
        AddWrapped(content, proposal.SharedIntroduction, 11);
        content.Add(new(string.Empty, 8, false));
        content.Add(new("Conceptos", 13, true));

        foreach (var line in proposal.Lines.OrderBy(line => line.SortOrder))
        {
            var optional = line.IsOptional ? " (opcional)" : string.Empty;
            AddWrapped(
                content,
                $"{line.Description}{optional}",
                11,
                true);
            content.Add(new(
                $"{line.Quantity:N2} x {Money(line.UnitPrice, proposal.CurrencyCode)}"
                + $"    Total: {Money(line.LineTotal, proposal.CurrencyCode)}",
                10,
                false));
            if (line.LineDiscount > 0)
            {
                content.Add(new(
                    $"Descuento: {Money(line.LineDiscount, proposal.CurrencyCode)}",
                    9,
                    false));
            }
        }

        content.Add(new(string.Empty, 8, false));
        content.Add(new("Resumen", 13, true));
        content.Add(new(
            $"Subtotal: {Money(proposal.Totals.Subtotal, proposal.CurrencyCode)}",
            11,
            false));
        content.Add(new(
            $"Descuentos: -{Money(proposal.Totals.DiscountTotal, proposal.CurrencyCode)}",
            11,
            false));
        content.Add(new(
            $"Impuestos: {Money(proposal.Totals.TaxTotal, proposal.CurrencyCode)}",
            11,
            false));
        content.Add(new(
            $"Total: {Money(proposal.Totals.GrandTotal, proposal.CurrencyCode)}",
            15,
            true));
        content.Add(new(string.Empty, 8, false));
        if (!string.IsNullOrWhiteSpace(proposal.SharedTerms))
        {
            content.Add(new("Términos", 13, true));
            AddWrapped(content, proposal.SharedTerms, 10);
        }

        content.Add(new(string.Empty, 8, false));
        content.Add(new(
            $"Generado el {timeProvider.GetUtcNow():dd/MM/yyyy HH:mm} UTC",
            8,
            false));

        return content
            .Chunk(MaxBodyLines)
            .Select(chunk => (IReadOnlyList<PdfTextLine>)chunk.ToList())
            .ToList();
    }

    private static byte[] BuildPdf(
        IReadOnlyList<IReadOnlyList<PdfTextLine>> pages)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding(
            1252,
            EncoderFallback.ReplacementFallback,
            DecoderFallback.ReplacementFallback);
        var objects = new List<byte[]>();
        var pageObjectIds = Enumerable
            .Range(0, pages.Count)
            .Select(index => 4 + index * 2)
            .ToList();
        objects.Add(encoding.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add(encoding.GetBytes(
            $"<< /Type /Pages /Count {pages.Count} /Kids "
            + $"[{string.Join(' ', pageObjectIds.Select(id => $"{id} 0 R"))}] >>"));
        objects.Add(encoding.GetBytes(
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica "
            + "/Encoding /WinAnsiEncoding >>"));

        for (var index = 0; index < pages.Count; index++)
        {
            var pageId = pageObjectIds[index];
            var contentId = pageId + 1;
            objects.Add(encoding.GetBytes(
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Font << /F1 3 0 R >> >> "
                + $"/Contents {contentId} 0 R >>"));
            var stream = BuildPageStream(
                pages[index],
                index + 1,
                pages.Count,
                encoding);
            var prefix = encoding.GetBytes($"<< /Length {stream.Length} >>\nstream\n");
            var suffix = encoding.GetBytes("\nendstream");
            objects.Add([.. prefix, .. stream, .. suffix]);
        }

        using var output = new MemoryStream();
        Write(output, encoding.GetBytes("%PDF-1.4\n%âãÏÓ\n"));
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            Write(output, encoding.GetBytes($"{index + 1} 0 obj\n"));
            Write(output, objects[index]);
            Write(output, encoding.GetBytes("\nendobj\n"));
        }

        var xrefOffset = output.Position;
        Write(output, encoding.GetBytes($"xref\n0 {objects.Count + 1}\n"));
        Write(output, encoding.GetBytes("0000000000 65535 f \n"));
        foreach (var offset in offsets.Skip(1))
        {
            Write(output, encoding.GetBytes($"{offset:0000000000} 00000 n \n"));
        }

        Write(output, encoding.GetBytes(
            $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\n"
            + $"startxref\n{xrefOffset}\n%%EOF"));
        return output.ToArray();
    }

    private static byte[] BuildPageStream(
        IReadOnlyList<PdfTextLine> lines,
        int pageNumber,
        int pageCount,
        Encoding encoding)
    {
        var builder = new StringBuilder();
        builder.AppendLine("0.19 0.17 0.21 rg 0 724 612 68 re f");
        builder.AppendLine("0.78 0.32 0.27 rg 0 716 612 8 re f");
        AppendText(builder, 42, 750, 20, "Plannyt", true, white: true);
        var y = 686;
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line.Text))
            {
                y -= 8;
                continue;
            }

            AppendText(
                builder,
                48,
                y,
                line.FontSize,
                line.Text,
                line.IsBold,
                false);
            y -= line.FontSize + 6;
        }

        AppendText(
            builder,
            48,
            28,
            8,
            $"Página {pageNumber} de {pageCount}",
            false,
            false);
        return encoding.GetBytes(builder.ToString());
    }

    private static void AppendText(
        StringBuilder builder,
        int x,
        int y,
        int size,
        string text,
        bool isBold,
        bool white)
    {
        builder.AppendLine(white ? "1 1 1 rg" : "0.19 0.17 0.21 rg");
        builder.Append("BT /F1 ")
            .Append(size + (isBold ? 1 : 0))
            .Append(" Tf ")
            .Append(x)
            .Append(' ')
            .Append(y)
            .Append(" Td (")
            .Append(Escape(text))
            .AppendLine(") Tj ET");
    }

    private static void AddWrapped(
        ICollection<PdfTextLine> target,
        string? text,
        int fontSize,
        bool bold = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        foreach (var paragraph in text.Split(
                     ['\r', '\n'],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var line in Wrap(paragraph.Trim(), 88))
            {
                target.Add(new PdfTextLine(line, fontSize, bold));
            }
        }
    }

    private static IEnumerable<string> Wrap(string text, int maxLength)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length > 0
                && current.Length + word.Length + 1 > maxLength)
            {
                yield return current.ToString();
                current.Clear();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    private static string Money(decimal value, string currency) =>
        $"{value.ToString("N2", MoneyCulture)} {currency}";

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static void Write(Stream stream, byte[] bytes) =>
        stream.Write(bytes, 0, bytes.Length);

    private sealed record PdfTextLine(
        string Text,
        int FontSize,
        bool IsBold);
}
