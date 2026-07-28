using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Plannyt.Api.Modules.Contracts.Application;

namespace Plannyt.Api.Modules.Contracts.Pdf;

public sealed partial class ContractPdfGenerator : IContractPdfGenerator
{
    private const int LinesPerPage = 40;

    public byte[] GeneratePublished(ContractPdfModel model)
    {
        var lines = BuildContractLines(model);
        return SimplePdfWriter.Build(lines.Chunk(LinesPerPage).ToList());
    }

    public byte[] GenerateFinal(
        ContractPdfModel model,
        IReadOnlyList<SignatureEvidenceSummaryResponse> evidence)
    {
        var pages = BuildContractLines(model)
            .Chunk(LinesPerPage)
            .ToList();
        var evidenceLines = new List<string>
        {
            "ANEXO DE EVIDENCIA DE FIRMA ELECTRÓNICA SIMPLE",
            string.Empty,
            $"Contrato: {model.ContractNumber}",
            $"Versión: {model.VersionNumber}",
            $"Hash SHA-256 del documento presentado: {model.DocumentSha256}",
            string.Empty,
            "Plannyt conserva el documento original publicado por separado.",
            "Este anexo no acredita una firma electrónica avanzada ni una",
            "verificación oficial de identidad.",
            string.Empty
        };
        foreach (var item in evidence.OrderBy(item => item.SignedAt))
        {
            evidenceLines.Add($"Firmante: {item.DeclaredSignerName}");
            evidenceLines.Add($"Método: {item.SigningMethod}");
            evidenceLines.Add($"Fecha UTC: {item.SignedAt:O}");
            evidenceLines.Add($"Evidencia: {item.Id}");
            evidenceLines.Add(string.Empty);
        }

        pages.AddRange(evidenceLines.Chunk(LinesPerPage));
        return SimplePdfWriter.Build(pages);
    }

    private static IReadOnlyList<string> BuildContractLines(ContractPdfModel model)
    {
        var lines = new List<string>
        {
            model.OrganizationName,
            $"CONTRATO {model.ContractNumber}",
            model.Name,
            $"Versión {model.VersionNumber}",
            model.ValidUntil is null
                ? "Sin fecha de vencimiento"
                : $"Vigente hasta {model.ValidUntil:dd/MM/yyyy HH:mm} UTC",
            string.Empty,
            "PARTES"
        };
        lines.AddRange(model.Parties.Select(party => $"- {party.DisplayName}"));
        lines.Add(string.Empty);
        lines.AddRange(ToPlainTextLines(model.RenderedContent));
        lines.Add(string.Empty);
        lines.Add("CONSENTIMIENTO PARA MEDIOS ELECTRÓNICOS");
        lines.AddRange(Wrap(model.ConsentText, 88));
        return lines;
    }

    private static IEnumerable<string> ToPlainTextLines(string html)
    {
        var withBreaks = BlockEndRegex().Replace(html, "\n");
        withBreaks = BreakRegex().Replace(withBreaks, "\n");
        var plain = WebUtility.HtmlDecode(TagRegex().Replace(withBreaks, string.Empty));
        foreach (var paragraph in plain.Split(
                     ['\r', '\n'],
                     StringSplitOptions.RemoveEmptyEntries
                         | StringSplitOptions.TrimEntries))
        {
            foreach (var line in Wrap(paragraph, 88))
            {
                yield return line;
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

    [GeneratedRegex(
        @"</(?:p|div|section|article|h[1-6]|li|tr)>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockEndRegex();

    [GeneratedRegex(
        @"<br\s*/?>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BreakRegex();

    [GeneratedRegex(
        @"<[^>]+>",
        RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();

    private static class SimplePdfWriter
    {
        public static byte[] Build(IReadOnlyList<string[]> pages)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding(
                1252,
                EncoderFallback.ReplacementFallback,
                DecoderFallback.ReplacementFallback);
            var normalizedPages = pages.Count == 0
                ? [Array.Empty<string>()]
                : pages;
            var objects = new List<byte[]>();
            var pageObjectIds = Enumerable
                .Range(0, normalizedPages.Count)
                .Select(index => 4 + index * 2)
                .ToList();
            objects.Add(encoding.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
            objects.Add(encoding.GetBytes(
                $"<< /Type /Pages /Count {normalizedPages.Count} /Kids "
                + $"[{string.Join(' ', pageObjectIds.Select(id => $"{id} 0 R"))}] >>"));
            objects.Add(encoding.GetBytes(
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica "
                + "/Encoding /WinAnsiEncoding >>"));

            for (var index = 0; index < normalizedPages.Count; index++)
            {
                var pageId = pageObjectIds[index];
                var contentId = pageId + 1;
                objects.Add(encoding.GetBytes(
                    "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                    + "/Resources << /Font << /F1 3 0 R >> >> "
                    + $"/Contents {contentId} 0 R >>"));
                var stream = BuildPage(
                    normalizedPages[index],
                    index + 1,
                    normalizedPages.Count,
                    encoding);
                var prefix = encoding.GetBytes(
                    $"<< /Length {stream.Length} >>\nstream\n");
                var suffix = encoding.GetBytes("\nendstream");
                objects.Add([.. prefix, .. stream, .. suffix]);
            }

            using var output = new MemoryStream();
            Write(output, encoding.GetBytes("%PDF-1.4\n"));
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
                Write(
                    output,
                    encoding.GetBytes($"{offset:0000000000} 00000 n \n"));
            }

            Write(output, encoding.GetBytes(
                $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\n"
                + $"startxref\n{xrefOffset}\n%%EOF"));
            return output.ToArray();
        }

        private static byte[] BuildPage(
            IReadOnlyList<string> lines,
            int page,
            int count,
            Encoding encoding)
        {
            var builder = new StringBuilder();
            builder.AppendLine("0.19 0.17 0.21 rg 0 724 612 68 re f");
            builder.AppendLine("0.78 0.32 0.27 rg 0 716 612 8 re f");
            AppendText(builder, 42, 750, 20, "Plannyt", true);
            var y = 686;
            foreach (var line in lines)
            {
                if (line.Length == 0)
                {
                    y -= 8;
                    continue;
                }

                AppendText(builder, 48, y, 10, line, false);
                y -= 16;
            }

            AppendText(builder, 48, 28, 8, $"Página {page} de {count}", false);
            return encoding.GetBytes(builder.ToString());
        }

        private static void AppendText(
            StringBuilder builder,
            int x,
            int y,
            int size,
            string value,
            bool white)
        {
            builder.AppendLine(white ? "1 1 1 rg" : "0.19 0.17 0.21 rg");
            builder.Append("BT /F1 ")
                .Append(size)
                .Append(" Tf ")
                .Append(x)
                .Append(' ')
                .Append(y)
                .Append(" Td (")
                .Append(Escape(value))
                .AppendLine(") Tj ET");
        }

        private static string Escape(string value) =>
            value.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);

        private static void Write(Stream stream, byte[] value) =>
            stream.Write(value, 0, value.Length);
    }
}
