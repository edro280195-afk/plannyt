using Plannyt.Api.Modules.Guests.Domain;

namespace Plannyt.Api.Modules.Guests.Application;

public static class GuestImportLocalization
{
    public static readonly IReadOnlyList<string> SupportedLanguages = ["es", "en"];
    public static readonly IReadOnlyList<string> SupportedFormats = ["csv", "xlsx"];

    private static readonly IReadOnlyList<(string Column, string Es, string En, bool Required, string DescriptionEs, string DescriptionEn, string ValuesEs, string ValuesEn)>
        Columns =
        [
            (
                "GroupName", "Nombre del grupo", "Group name", true,
                "Identifica al grupo o familia. Las filas con el mismo nombre se agrupan.",
                "Identifies the group or family. Rows sharing the same name are grouped together.",
                "Cualquier texto, por ejemplo \"Familia García\".",
                "Any text, e.g. \"García Family\"."),
            (
                "GroupType", "Tipo de grupo", "Group type", true,
                "Qué clase de grupo es.",
                "What kind of group this is.",
                "Individual, Pareja, Familia, Grupo, Empresa, Mesa corporativa u Otro.",
                "Individual, Couple, Family, Group, Company, Corporate table or Other."),
            (
                "AllowedGuestCount", "Invitados permitidos", "Allowed guests", true,
                "Cuántas personas puede incluir este grupo en total.",
                "How many people this group may include in total.",
                "Un número entero mayor a 0, igual en todas las filas del mismo grupo.",
                "A whole number greater than 0, the same on every row of the group."),
            (
                "ContactName", "Nombre de contacto", "Contact name", false,
                "Nombre de la persona de contacto del grupo.",
                "Name of the group's contact person.",
                "Texto libre, opcional.",
                "Free text, optional."),
            (
                "ContactPhone", "Teléfono de contacto", "Contact phone", false,
                "Teléfono de la persona de contacto.",
                "Phone number of the contact person.",
                "Texto libre, opcional.",
                "Free text, optional."),
            (
                "ContactEmail", "Correo de contacto", "Contact email", false,
                "Correo de la persona de contacto.",
                "Email address of the contact person.",
                "Debe ser un correo válido si se llena; opcional.",
                "Must be a valid email if filled in; optional."),
            (
                "GuestFirstName", "Nombre del invitado", "Guest first name", true,
                "Nombre de pila de esta persona.",
                "This person's first name.",
                "Texto libre. Debe llenarse el nombre o el apellido.",
                "Free text. Either the first or last name must be filled in."),
            (
                "GuestLastName", "Apellido del invitado", "Guest last name", true,
                "Apellido de esta persona.",
                "This person's last name.",
                "Texto libre. Debe llenarse el nombre o el apellido.",
                "Free text. Either the first or last name must be filled in."),
            (
                "AgeCategory", "Categoría de edad", "Age category", true,
                "Rango de edad de esta persona.",
                "This person's age range.",
                "Adulto, Adolescente, Niño, Bebé o Sin especificar.",
                "Adult, Teen, Child, Infant or Unknown."),
            (
                "IsPrimaryContact", "Contacto principal", "Primary contact", false,
                "Si esta persona es el contacto principal del grupo. Solo una por grupo.",
                "Whether this person is the group's primary contact. Only one per group.",
                "Sí o No (vacío se toma como No).",
                "Yes or No (blank is treated as No)."),
            (
                "IsVip", "VIP", "VIP", false,
                "Si esta persona debe marcarse como invitado VIP.",
                "Whether this person should be marked as a VIP guest.",
                "Sí o No (vacío se toma como No).",
                "Yes or No (blank is treated as No)."),
            (
                "Tags", "Etiquetas", "Tags", false,
                "Etiquetas libres para organizar invitados, separadas por \"|\".",
                "Free-form tags to organize guests, separated by \"|\".",
                "Texto libre separado por \"|\", por ejemplo \"Familia|VIP\".",
                "Free text separated by \"|\", e.g. \"Family|VIP\"."),
        ];

    private static readonly IReadOnlyDictionary<InvitationGroupType, (string Es, string En)> GroupTypeLabels =
        new Dictionary<InvitationGroupType, (string, string)>
        {
            [InvitationGroupType.Individual] = ("Individual", "Individual"),
            [InvitationGroupType.Couple] = ("Pareja", "Couple"),
            [InvitationGroupType.Family] = ("Familia", "Family"),
            [InvitationGroupType.Group] = ("Grupo", "Group"),
            [InvitationGroupType.Company] = ("Empresa", "Company"),
            [InvitationGroupType.CorporateTable] = ("Mesa corporativa", "Corporate table"),
            [InvitationGroupType.Other] = ("Otro", "Other"),
        };

    private static readonly IReadOnlyDictionary<AgeCategory, (string Es, string En)> AgeCategoryLabels =
        new Dictionary<AgeCategory, (string, string)>
        {
            [AgeCategory.Adult] = ("Adulto", "Adult"),
            [AgeCategory.Teen] = ("Adolescente", "Teen"),
            [AgeCategory.Child] = ("Niño", "Child"),
            [AgeCategory.Infant] = ("Bebé", "Infant"),
            [AgeCategory.Unknown] = ("Sin especificar", "Unknown"),
        };

    private static readonly IReadOnlyDictionary<string, bool> BooleanAliases =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["true"] = true,
            ["false"] = false,
            ["sí"] = true,
            ["si"] = true,
            ["verdadero"] = true,
            ["no"] = false,
            ["falso"] = false,
            ["yes"] = true,
        };

    private static readonly IReadOnlyDictionary<string, string> ColumnAliasIndex = BuildColumnAliasIndex();

    public static IReadOnlyList<string> ColumnOrder { get; } = Columns.Select(item => item.Column).ToList();

    public static string NormalizeLanguage(string? language) =>
        SupportedLanguages.FirstOrDefault(
            supported => string.Equals(supported, language, StringComparison.OrdinalIgnoreCase))
        ?? "es";

    public static string NormalizeFormat(string? format) =>
        SupportedFormats.FirstOrDefault(
            supported => string.Equals(supported, format, StringComparison.OrdinalIgnoreCase))
        ?? "csv";

    public static string ColumnLabel(string column, string language)
    {
        var entry = Columns.Single(item =>
            string.Equals(item.Column, column, StringComparison.OrdinalIgnoreCase));
        return language == "en" ? entry.En : entry.Es;
    }

    public static IReadOnlyList<(string Column, string Label, bool Required, string Description, string ValidValues)>
        FieldGuide(string language)
    {
        var isEnglish = language == "en";
        return Columns.Select(item => (
            item.Column,
            Label: isEnglish ? item.En : item.Es,
            item.Required,
            Description: isEnglish ? item.DescriptionEn : item.DescriptionEs,
            ValidValues: isEnglish ? item.ValuesEn : item.ValuesEs)).ToList();
    }

    public static string GroupTypeLabel(InvitationGroupType value, string language) =>
        language == "en" ? GroupTypeLabels[value].En : GroupTypeLabels[value].Es;

    public static string AgeCategoryLabel(AgeCategory value, string language) =>
        language == "en" ? AgeCategoryLabels[value].En : AgeCategoryLabels[value].Es;

    public static string BooleanLabel(bool value, string language) =>
        value
            ? (language == "en" ? "Yes" : "Sí")
            : "No";

    /// <summary>
    /// Resuelve un encabezado de archivo (técnico, en español o en inglés) al nombre
    /// técnico de columna que usa el resto del importador. Preserva el camino existente
    /// de encabezados técnicos exactos para no romper plantillas ya descargadas.
    /// </summary>
    public static bool TryResolveColumn(string header, out string column) =>
        ColumnAliasIndex.TryGetValue(header.Trim(), out column!);

    public static bool TryParseGroupType(string text, out InvitationGroupType value)
    {
        if (Enum.TryParse(text, true, out value))
        {
            return true;
        }

        var trimmed = text.Trim();
        foreach (var (candidate, labels) in GroupTypeLabels)
        {
            if (string.Equals(labels.Es, trimmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(labels.En, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static bool TryParseAgeCategory(string text, out AgeCategory value)
    {
        if (Enum.TryParse(text, true, out value))
        {
            return true;
        }

        var trimmed = text.Trim();
        foreach (var (candidate, labels) in AgeCategoryLabels)
        {
            if (string.Equals(labels.Es, trimmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(labels.En, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                value = candidate;
                return true;
            }
        }

        value = default;
        return false;
    }

    public static bool TryParseBoolean(string text, out bool value) =>
        BooleanAliases.TryGetValue(text.Trim(), out value);

    private static Dictionary<string, string> BuildColumnAliasIndex()
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Columns)
        {
            index[entry.Column] = entry.Column;
            index[entry.Es] = entry.Column;
            index[entry.En] = entry.Column;
        }

        return index;
    }
}
