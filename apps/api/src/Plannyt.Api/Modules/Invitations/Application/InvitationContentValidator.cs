using System.Text.Json;
using System.Text.RegularExpressions;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Invitations.Domain;

namespace Plannyt.Api.Modules.Invitations.Application;

public static partial class InvitationContentValidator
{
    private static readonly IReadOnlySet<string> Fonts =
        new HashSet<string>(
            ["inter", "source-serif", "playfair", "montserrat", "nunito", "lora"],
            StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<InvitationBlockType, IReadOnlySet<string>>
        ContentProperties =
            new Dictionary<InvitationBlockType, IReadOnlySet<string>>
            {
                [InvitationBlockType.Cover] = Set("eyebrow", "title", "subtitle", "imageUrl"),
                [InvitationBlockType.Greeting] = Set("title", "body"),
                [InvitationBlockType.Participants] = Set("heading", "format"),
                [InvitationBlockType.EventDate] = Set(
                    "heading",
                    "dateFormat",
                    "showTimeZone"),
                [InvitationBlockType.Countdown] = Set("heading", "completedText"),
                [InvitationBlockType.Story] = Set("heading", "body"),
                [InvitationBlockType.Image] = Set("url", "alt", "caption"),
                [InvitationBlockType.GalleryPreview] = Set("heading", "itemCount"),
                [InvitationBlockType.Text] = Set("body"),
                [InvitationBlockType.Divider] = Set("style"),
                [InvitationBlockType.DressCode] = Set("heading", "value", "details"),
                [InvitationBlockType.Contact] = Set(
                    "heading",
                    "name",
                    "phone",
                    "email"),
                [InvitationBlockType.CustomButton] = Set("label", "url"),
                [InvitationBlockType.Footer] = Set("text")
            };

    private static readonly IReadOnlySet<string> PresentationProperties =
        Set("backgroundToken", "textAlign", "emphasis", "width");

    private static readonly IReadOnlySet<string> Variables =
        Set(
            "group.displayName",
            "group.contactName",
            "event.name",
            "event.date",
            "participants.names");

    public static InvitationValidationResult Validate(
        string name,
        InvitationThemeRequest theme,
        IReadOnlyList<InvitationBlockRequest> blocks)
    {
        var errors = new Dictionary<string, List<string>>();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            Add(errors, "name", "El nombre es obligatorio y admite 120 caracteres.");
        }

        ValidateTheme(theme, errors);
        if (blocks.Count is < 1 or > 40)
        {
            Add(errors, "blocks", "El diseño debe tener entre 1 y 40 bloques.");
        }

        if (blocks.Select(block => block.Id).Distinct().Count() != blocks.Count)
        {
            Add(errors, "blocks", "Cada bloque requiere un identificador único.");
        }

        if (blocks.Select(block => block.SortOrder).Distinct().Count() != blocks.Count)
        {
            Add(errors, "blocks", "El orden de los bloques no puede repetirse.");
        }

        foreach (var block in blocks)
        {
            ValidateObject(
                block.Content,
                ContentProperties[block.Type],
                $"blocks.{block.Id}.content",
                errors,
                block.Type);
            ValidateObject(
                block.Presentation,
                PresentationProperties,
                $"blocks.{block.Id}.presentation",
                errors,
                null);
            if (block.Visibility != BlockVisibility.Everyone
                && string.IsNullOrWhiteSpace(block.VisibilityValue)
                && block.Visibility is not (BlockVisibility.VipOnly))
            {
                Add(
                    errors,
                    $"blocks.{block.Id}.visibilityValue",
                    "La regla de visibilidad requiere un valor.");
            }
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(
                errors.ToDictionary(
                    item => item.Key,
                    item => item.Value.ToArray()));
        }

        return new InvitationValidationResult(GetAccessibilityWarnings(theme, blocks));
    }

    public static string SerializeTheme(InvitationThemeRequest theme) =>
        JsonSerializer.Serialize(theme);

    public static string SerializeBlocks(IReadOnlyList<InvitationBlockRequest> blocks) =>
        JsonSerializer.Serialize(blocks.OrderBy(block => block.SortOrder));

    public static InvitationThemeRequest DeserializeTheme(string json) =>
        JsonSerializer.Deserialize<InvitationThemeRequest>(json)
        ?? throw new InvalidOperationException("El tema almacenado es inválido.");

    public static IReadOnlyList<InvitationBlockRequest> DeserializeBlocks(string json) =>
        JsonSerializer.Deserialize<List<InvitationBlockRequest>>(json)
        ?? throw new InvalidOperationException("Los bloques almacenados son inválidos.");

    public static IReadOnlyList<string> GetAccessibilityWarnings(
        InvitationThemeRequest theme,
        IReadOnlyList<InvitationBlockRequest> blocks)
    {
        var warnings = new List<string>();
        if (Contrast(theme.TextColor, theme.BackgroundColor) < 4.5)
        {
            warnings.Add(
                "El contraste entre texto y fondo es menor a 4.5:1.");
        }

        if (Contrast(theme.AccentColor, theme.BackgroundColor) < 3)
        {
            warnings.Add(
                "El contraste del acento es menor a 3:1.");
        }

        if (blocks.Any(block =>
                block.Type == InvitationBlockType.Image
                && (!block.Content.TryGetProperty("alt", out var alt)
                    || string.IsNullOrWhiteSpace(alt.GetString()))))
        {
            warnings.Add("Todas las imágenes visibles deben tener texto alternativo.");
        }

        return warnings;
    }

    private static void ValidateTheme(
        InvitationThemeRequest theme,
        IDictionary<string, List<string>> errors)
    {
        ValidateColor(theme.BackgroundColor, "theme.backgroundColor", errors);
        ValidateColor(theme.SurfaceColor, "theme.surfaceColor", errors);
        ValidateColor(theme.TextColor, "theme.textColor", errors);
        ValidateColor(theme.AccentColor, "theme.accentColor", errors);
        if (!Fonts.Contains(theme.HeadingFont) || !Fonts.Contains(theme.BodyFont))
        {
            Add(errors, "theme.font", "Selecciona tipografías del catálogo permitido.");
        }

        if (!Set("none", "sm", "md", "lg", "pill").Contains(theme.RadiusToken))
        {
            Add(errors, "theme.radiusToken", "El radio visual no es válido.");
        }

        if (!Set("compact", "comfortable", "airy").Contains(theme.SpacingToken))
        {
            Add(errors, "theme.spacingToken", "El espaciado no es válido.");
        }

        if (!Set("plain", "card", "full-bleed").Contains(theme.CoverStyle))
        {
            Add(errors, "theme.coverStyle", "El estilo de portada no es válido.");
        }

        if (!Set("solid", "outline", "soft").Contains(theme.ButtonStyle))
        {
            Add(errors, "theme.buttonStyle", "El estilo de botón no es válido.");
        }
    }

    private static void ValidateObject(
        JsonElement value,
        IReadOnlySet<string> allowed,
        string path,
        IDictionary<string, List<string>> errors,
        InvitationBlockType? blockType)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            Add(errors, path, "El contenido debe ser un objeto.");
            return;
        }

        foreach (var property in value.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                Add(errors, path, $"La propiedad '{property.Name}' no está permitida.");
                continue;
            }

            ValidateValue(property.Value, $"{path}.{property.Name}", errors);
            if (property.Name is "url" or "imageUrl"
                && property.Value.ValueKind == JsonValueKind.String)
            {
                ValidateUrl(
                    property.Value.GetString(),
                    blockType is InvitationBlockType.Image or InvitationBlockType.Cover,
                    $"{path}.{property.Name}",
                    errors);
            }
        }
    }

    private static void ValidateValue(
        JsonElement value,
        string path,
        IDictionary<string, List<string>> errors)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString() ?? string.Empty;
            if (text.Length > 4000)
            {
                Add(errors, path, "El texto admite hasta 4,000 caracteres.");
            }

            if (text.Contains('<')
                || text.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
                || text.Contains("data:", StringComparison.OrdinalIgnoreCase))
            {
                Add(errors, path, "No se permite HTML, scripts ni URLs de datos.");
            }

            foreach (Match match in VariableRegex().Matches(text))
            {
                if (!Variables.Contains(match.Groups[1].Value))
                {
                    Add(errors, path, $"La variable '{match.Value}' no está permitida.");
                }
            }
        }
        else if (value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
        {
            Add(errors, path, "No se admiten estructuras anidadas en el bloque.");
        }
    }

    private static void ValidateUrl(
        string? value,
        bool requireInternal,
        string path,
        IDictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (requireInternal && !value.StartsWith("/", StringComparison.Ordinal))
        {
            Add(errors, path, "Las imágenes deben usar una ruta interna de Plannyt.");
            return;
        }

        if (!requireInternal
            && !value.StartsWith("/", StringComparison.Ordinal)
            && (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps))
        {
            Add(errors, path, "El enlace debe usar HTTPS o una ruta interna.");
        }
    }

    private static void ValidateColor(
        string value,
        string path,
        IDictionary<string, List<string>> errors)
    {
        if (!ColorRegex().IsMatch(value))
        {
            Add(errors, path, "El color debe usar formato hexadecimal #RRGGBB.");
        }
    }

    private static double Contrast(string first, string second)
    {
        if (!ColorRegex().IsMatch(first) || !ColorRegex().IsMatch(second))
        {
            return 0;
        }

        static double Luminance(string color)
        {
            var channels = new[]
            {
                Convert.ToInt32(color.Substring(1, 2), 16) / 255d,
                Convert.ToInt32(color.Substring(3, 2), 16) / 255d,
                Convert.ToInt32(color.Substring(5, 2), 16) / 255d
            };
            return channels.Select(channel =>
                    channel <= 0.03928
                        ? channel / 12.92
                        : Math.Pow((channel + 0.055) / 1.055, 2.4))
                .Zip(new[] { 0.2126, 0.7152, 0.0722 })
                .Sum(pair => pair.First * pair.Second);
        }

        var firstLuminance = Luminance(first);
        var secondLuminance = Luminance(second);
        var light = Math.Max(firstLuminance, secondLuminance);
        var dark = Math.Min(firstLuminance, secondLuminance);
        return (light + 0.05) / (dark + 0.05);
    }

    private static void Add(
        IDictionary<string, List<string>> errors,
        string key,
        string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9.]+)\s*\}\}")]
    private static partial Regex VariableRegex();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex ColorRegex();
}

public sealed record InvitationValidationResult(
    IReadOnlyList<string> AccessibilityWarnings);
