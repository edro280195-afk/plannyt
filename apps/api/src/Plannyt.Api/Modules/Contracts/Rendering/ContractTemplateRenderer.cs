using System.Net;
using System.Text.RegularExpressions;

namespace Plannyt.Api.Modules.Contracts.Rendering;

public sealed partial class ContractTemplateRenderer
{
    public static IReadOnlySet<string> AllowedVariables { get; } =
        new HashSet<string>(
        [
            "organization.name",
            "organization.country",
            "organization.currency",
            "client.displayName",
            "client.contactName",
            "client.email",
            "client.phone",
            "event.name",
            "event.type",
            "event.date",
            "event.city",
            "event.country",
            "proposal.number",
            "proposal.version",
            "proposal.subtotal",
            "proposal.discountTotal",
            "proposal.taxTotal",
            "proposal.grandTotal",
            "proposal.currency",
            "contract.number",
            "contract.createdAt",
            "contract.validUntil"
        ],
        StringComparer.Ordinal);

    public ContractRenderResult Render(
        string content,
        IReadOnlyDictionary<string, string?> values)
    {
        var sanitized = Sanitize(content);
        var requested = VariableRegex()
            .Matches(sanitized)
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var unknown = requested
            .Where(variable => !AllowedVariables.Contains(variable))
            .Order(StringComparer.Ordinal)
            .ToList();
        var missing = requested
            .Where(variable =>
                AllowedVariables.Contains(variable)
                && (!values.TryGetValue(variable, out var value)
                    || string.IsNullOrWhiteSpace(value)))
            .Order(StringComparer.Ordinal)
            .ToList();

        var rendered = VariableRegex().Replace(
            sanitized,
            match =>
            {
                var name = match.Groups[1].Value;
                return values.TryGetValue(name, out var value)
                    && !string.IsNullOrWhiteSpace(value)
                        ? WebUtility.HtmlEncode(value)
                        : match.Value;
            });
        return new ContractRenderResult(rendered, unknown, missing);
    }

    public string Sanitize(string content)
    {
        var sanitized = DangerousBlockRegex().Replace(content, string.Empty);
        sanitized = DangerousTagRegex().Replace(sanitized, string.Empty);
        sanitized = EventAttributeRegex().Replace(sanitized, string.Empty);
        sanitized = JavaScriptUriRegex().Replace(
            sanitized,
            "${prefix}=\"#\"");
        return sanitized;
    }

    [GeneratedRegex(
        @"\{\{\s*([A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z][A-Za-z0-9]*)+)\s*\}\}",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex VariableRegex();

    [GeneratedRegex(
        @"<(script|style|iframe|object|embed|svg|math)\b[^>]*>[\s\S]*?</\1\s*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex DangerousBlockRegex();

    [GeneratedRegex(
        @"</?(script|style|iframe|object|embed|svg|math|link|meta|base|form|input|button)\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex DangerousTagRegex();

    [GeneratedRegex(
        @"\s+on[a-z]+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex EventAttributeRegex();

    [GeneratedRegex(
        @"(?<prefix>\b(?:href|src))\s*=\s*(?:""\s*javascript:[^""]*""|'\s*javascript:[^']*'|javascript:[^\s>]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex JavaScriptUriRegex();
}

public sealed record ContractRenderResult(
    string RenderedContent,
    IReadOnlyList<string> UnknownVariables,
    IReadOnlyList<string> MissingVariables)
{
    public bool CanPublish =>
        UnknownVariables.Count == 0 && MissingVariables.Count == 0;
}
