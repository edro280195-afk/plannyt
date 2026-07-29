using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed record RsvpQuestionDefinitionSet(
    IReadOnlyList<RsvpQuestion> Questions,
    string NormalizedSnapshot);

public static partial class RsvpQuestionDefinitionParser
{
    public const int MaximumQuestions = 100;
    public const int MaximumQuestionLabelLength = 200;
    public const int MaximumHelpTextLength = 1000;
    public const int MaximumOptionLabelLength = 200;
    public const int MaximumShortTextLength = 500;
    public const int MaximumLongTextLength = 5000;
    public const int MaximumVisibilityDepth = 5;
    public const int MaximumVisibilityConditions = 32;
    public const int MaximumSnapshotBytes = 131_072;

    private static readonly JsonSerializerOptions SerializerOptions = new(
        JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    static RsvpQuestionDefinitionParser()
    {
        SerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false));
    }

    public static RsvpQuestionDefinitionSet ParseAndValidate(
        string? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot)
            || System.Text.Encoding.UTF8.GetByteCount(snapshot)
            > MaximumSnapshotBytes)
        {
            throw DefinitionError(
                "questionsJson",
                $"El snapshot de preguntas es obligatorio y admite hasta {MaximumSnapshotBytes} bytes.");
        }

        List<RsvpQuestion> parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<RsvpQuestion>>(
                         snapshot,
                         SerializerOptions)
                     ?? [];
        }
        catch (JsonException)
        {
            throw DefinitionError(
                "questionsJson",
                "El snapshot contiene JSON, propiedades o valores de catálogo no válidos.");
        }

        var errors = new Dictionary<string, List<string>>(
            StringComparer.Ordinal);
        if (parsed.Count > MaximumQuestions)
        {
            AddError(
                errors,
                "questionsJson",
                $"El formulario admite hasta {MaximumQuestions} preguntas.");
        }

        var ids = new Dictionary<string, RsvpQuestion>(
            StringComparer.Ordinal);
        var sortOrders = new HashSet<int>();
        for (var index = 0; index < parsed.Count; index++)
        {
            ValidateQuestion(
                parsed[index],
                index,
                ids,
                sortOrders,
                errors);
        }

        ValidateVisibilityGraph(parsed, errors);
        if (errors.Count > 0)
        {
            throw new RequestValidationException(
                errors.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.ToArray(),
                    StringComparer.Ordinal));
        }

        var normalized = parsed
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id, StringComparer.Ordinal)
            .Select(NormalizeQuestion)
            .ToList();
        return new RsvpQuestionDefinitionSet(
            normalized,
            JsonSerializer.Serialize(normalized, SerializerOptions));
    }

    public static RsvpQuestionCatalogResponse GetCatalog() =>
        new(
            Enum.GetNames<RsvpQuestionType>(),
            Enum.GetNames<RsvpQuestionScope>(),
            Enum.GetNames<RsvpQuestionCategory>(),
            Enum.GetNames<RsvpVisibilityConditionType>(),
            new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.Ordinal)
            {
                [nameof(RsvpQuestionType.ShortText)] =
                [
                    "required",
                    "minLength",
                    "maxLength"
                ],
                [nameof(RsvpQuestionType.LongText)] =
                [
                    "required",
                    "minLength",
                    "maxLength"
                ],
                [nameof(RsvpQuestionType.YesNo)] = ["required"],
                [nameof(RsvpQuestionType.SingleChoice)] = ["required"],
                [nameof(RsvpQuestionType.MultipleChoice)] =
                [
                    "required",
                    "minimumSelections",
                    "maximumSelections"
                ],
                [nameof(RsvpQuestionType.Number)] =
                [
                    "required",
                    "minimum",
                    "maximum",
                    "integerOnly"
                ],
                [nameof(RsvpQuestionType.Date)] =
                [
                    "required",
                    "minimumDate",
                    "maximumDate"
                ],
                [nameof(RsvpQuestionType.InformationalConsent)] =
                [
                    "required"
                ]
            },
            MaximumQuestions,
            MaximumQuestionLabelLength,
            MaximumHelpTextLength,
            MaximumOptionLabelLength,
            MaximumShortTextLength,
            MaximumLongTextLength,
            MaximumVisibilityDepth,
            MaximumVisibilityConditions);

    private static void ValidateQuestion(
        RsvpQuestion question,
        int index,
        Dictionary<string, RsvpQuestion> ids,
        ISet<int> sortOrders,
        IDictionary<string, List<string>> errors)
    {
        var path = $"questions[{index}]";
        if (string.IsNullOrWhiteSpace(question.Id)
            || !QuestionIdPattern().IsMatch(question.Id))
        {
            AddError(
                errors,
                $"{path}.id",
                "El ID debe tener de 1 a 64 caracteres alfanuméricos, punto, guion o guion bajo.");
        }
        else if (!ids.TryAdd(question.Id, question))
        {
            AddError(
                errors,
                $"{path}.id",
                "El ID de pregunta está repetido.");
        }

        if (question.SortOrder < 0 || !sortOrders.Add(question.SortOrder))
        {
            AddError(
                errors,
                $"{path}.sortOrder",
                "El orden debe ser no negativo y único.");
        }

        ValidateVisibleText(
            question.Label,
            MaximumQuestionLabelLength,
            $"{path}.label",
            required: true,
            errors);
        ValidateVisibleText(
            question.HelpText,
            MaximumHelpTextLength,
            $"{path}.helpText",
            required: false,
            errors);
        ValidateOptions(question, path, errors);
        ValidateRules(question, path, errors);

        var conditionCount = 0;
        ValidateVisibilityRule(
            question,
            question.VisibilityRule,
            path,
            depth: 1,
            ref conditionCount,
            ids,
            errors);

        if (RequiresSensitiveHandling(question) && !question.IsSensitive)
        {
            AddError(
                errors,
                $"{path}.isSensitive",
                "Las preguntas de texto dietético o de accesibilidad y los consentimientos deben marcarse como sensibles.");
        }
    }

    private static void ValidateOptions(
        RsvpQuestion question,
        string path,
        IDictionary<string, List<string>> errors)
    {
        if (question.Options is null)
        {
            AddError(
                errors,
                $"{path}.options",
                "Options debe ser un arreglo.");
            return;
        }

        var choice = question.QuestionType is
            RsvpQuestionType.SingleChoice
            or RsvpQuestionType.MultipleChoice;
        if (!choice && question.Options.Count > 0)
        {
            AddError(
                errors,
                $"{path}.options",
                "Este tipo de pregunta no admite opciones.");
            return;
        }

        if (!choice)
        {
            return;
        }

        var active = question.Options.Count(option => option.IsActive);
        var minimumActive = question.QuestionType
                            == RsvpQuestionType.SingleChoice
            ? 2
            : 1;
        if (active < minimumActive)
        {
            AddError(
                errors,
                $"{path}.options",
                $"La pregunta requiere al menos {minimumActive} opciones activas.");
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        var orders = new HashSet<int>();
        for (var index = 0; index < question.Options.Count; index++)
        {
            var option = question.Options[index];
            var optionPath = $"{path}.options[{index}]";
            if (option is null)
            {
                AddError(
                    errors,
                    optionPath,
                    "La opción no puede ser null.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(option.Key)
                || !QuestionIdPattern().IsMatch(option.Key)
                || !keys.Add(option.Key))
            {
                AddError(
                    errors,
                    $"{optionPath}.key",
                    "La clave es obligatoria, controlada y única en la pregunta.");
            }

            ValidateVisibleText(
                option.Label,
                MaximumOptionLabelLength,
                $"{optionPath}.label",
                required: true,
                errors);
            if (option.SortOrder < 0 || !orders.Add(option.SortOrder))
            {
                AddError(
                    errors,
                    $"{optionPath}.sortOrder",
                    "El orden de opción debe ser no negativo y único.");
            }
        }
    }

    private static void ValidateRules(
        RsvpQuestion question,
        string path,
        IDictionary<string, List<string>> errors)
    {
        var rules = question.ValidationRules;
        if (rules is null)
        {
            AddError(
                errors,
                $"{path}.validationRules",
                "ValidationRules debe ser un objeto.");
            return;
        }

        if (rules.Required.HasValue
            && rules.Required.Value != question.IsRequired)
        {
            AddError(
                errors,
                $"{path}.validationRules.required",
                "Required debe coincidir con IsRequired.");
        }

        var hasLength = rules.MinLength.HasValue || rules.MaxLength.HasValue;
        var hasSelections = rules.MinimumSelections.HasValue
                            || rules.MaximumSelections.HasValue;
        var hasNumber = rules.Minimum.HasValue
                        || rules.Maximum.HasValue
                        || rules.IntegerOnly.HasValue;
        var hasDate = rules.MinimumDate.HasValue
                      || rules.MaximumDate.HasValue;
        switch (question.QuestionType)
        {
            case RsvpQuestionType.ShortText:
            case RsvpQuestionType.LongText:
                RejectUnsupported(
                    hasSelections || hasNumber || hasDate,
                    path,
                    errors);
                ValidateLengthRules(question, path, errors);
                break;
            case RsvpQuestionType.MultipleChoice:
                RejectUnsupported(
                    hasLength || hasNumber || hasDate,
                    path,
                    errors);
                ValidateSelectionRules(rules, path, errors);
                break;
            case RsvpQuestionType.Number:
                RejectUnsupported(
                    hasLength || hasSelections || hasDate,
                    path,
                    errors);
                if (rules.Minimum.HasValue
                    && rules.Maximum.HasValue
                    && rules.Minimum > rules.Maximum)
                {
                    AddError(
                        errors,
                        $"{path}.validationRules",
                        "Minimum no puede ser mayor que Maximum.");
                }

                break;
            case RsvpQuestionType.Date:
                RejectUnsupported(
                    hasLength || hasSelections || hasNumber,
                    path,
                    errors);
                if (rules.MinimumDate.HasValue
                    && rules.MaximumDate.HasValue
                    && rules.MinimumDate > rules.MaximumDate)
                {
                    AddError(
                        errors,
                        $"{path}.validationRules",
                        "MinimumDate no puede ser posterior a MaximumDate.");
                }

                break;
            default:
                RejectUnsupported(
                    hasLength || hasSelections || hasNumber || hasDate,
                    path,
                    errors);
                break;
        }
    }

    private static void ValidateLengthRules(
        RsvpQuestion question,
        string path,
        IDictionary<string, List<string>> errors)
    {
        var rules = question.ValidationRules;
        var absoluteMaximum = question.QuestionType
                              == RsvpQuestionType.ShortText
            ? MaximumShortTextLength
            : MaximumLongTextLength;
        if (rules.MinLength is < 0
            || rules.MaxLength is <= 0
            || rules.MaxLength > absoluteMaximum
            || (rules.MinLength.HasValue
                && rules.MaxLength.HasValue
                && rules.MinLength > rules.MaxLength))
        {
            AddError(
                errors,
                $"{path}.validationRules",
                $"Los límites de texto deben ser coherentes y no exceder {absoluteMaximum} caracteres.");
        }
    }

    private static void ValidateSelectionRules(
        ValidationRules rules,
        string path,
        IDictionary<string, List<string>> errors)
    {
        if (rules.MinimumSelections is < 0
            || rules.MaximumSelections is <= 0
            || (rules.MinimumSelections.HasValue
                && rules.MaximumSelections.HasValue
                && rules.MinimumSelections > rules.MaximumSelections))
        {
            AddError(
                errors,
                $"{path}.validationRules",
                "Los límites de selección deben ser coherentes y no negativos.");
        }
    }

    private static void RejectUnsupported(
        bool unsupported,
        string path,
        IDictionary<string, List<string>> errors)
    {
        if (unsupported)
        {
            AddError(
                errors,
                $"{path}.validationRules",
                "La definición contiene reglas incompatibles con el tipo de pregunta.");
        }
    }

    private static void ValidateVisibilityRule(
        RsvpQuestion question,
        VisibilityRule? rule,
        string path,
        int depth,
        ref int conditionCount,
        IReadOnlyDictionary<string, RsvpQuestion> knownQuestions,
        IDictionary<string, List<string>> errors)
    {
        if (rule is null)
        {
            AddError(
                errors,
                $"{path}.visibilityRule",
                "VisibilityRule debe ser un objeto.");
            return;
        }

        conditionCount++;
        if (depth > MaximumVisibilityDepth
            || conditionCount > MaximumVisibilityConditions)
        {
            AddError(
                errors,
                $"{path}.visibilityRule",
                "La regla excede la profundidad o cantidad máxima de condiciones.");
            return;
        }

        var rulePath = $"{path}.visibilityRule";
        switch (rule.ConditionType)
        {
            case RsvpVisibilityConditionType.All:
            case RsvpVisibilityConditionType.Any:
                if (rule.Conditions is null
                    || rule.Conditions.Count == 0
                    || rule.ReferenceQuestionId is not null
                    || rule.ExpectedValue is not null)
                {
                    AddError(
                        errors,
                        rulePath,
                        "All y Any requieren condiciones hijas y no admiten valores directos.");
                }

                foreach (var child in rule.Conditions ?? [])
                {
                    ValidateVisibilityRule(
                        question,
                        child,
                        path,
                        depth + 1,
                        ref conditionCount,
                        knownQuestions,
                        errors);
                }

                return;
            case RsvpVisibilityConditionType.Always:
                if (rule.Conditions is null
                    || rule.Conditions.Count > 0
                    || rule.ReferenceQuestionId is not null
                    || rule.ExpectedValue is not null)
                {
                    AddError(
                        errors,
                        rulePath,
                        "Always no admite condiciones ni valores.");
                }

                return;
            case RsvpVisibilityConditionType.PreviousAnswerEquals:
            case RsvpVisibilityConditionType.PreviousAnswerContains:
                ValidatePreviousAnswerReference(
                    question,
                    rule,
                    rulePath,
                    knownQuestions,
                    errors);
                break;
            case RsvpVisibilityConditionType.AttendanceStatusEquals:
                ValidateEnumExpected<GuestAttendanceStatus>(
                    rule,
                    rulePath,
                    errors);
                break;
            case RsvpVisibilityConditionType.GuestAgeCategoryEquals:
                ValidateEnumExpected<AgeCategory>(
                    rule,
                    rulePath,
                    errors);
                break;
            case RsvpVisibilityConditionType.GuestTypeEquals:
                ValidateEnumExpected<GuestType>(
                    rule,
                    rulePath,
                    errors);
                break;
            case RsvpVisibilityConditionType.IsUnnamedCompanion:
            case RsvpVisibilityConditionType.IsPrimaryContact:
                if (!bool.TryParse(rule.ExpectedValue, out _))
                {
                    AddError(
                        errors,
                        rulePath,
                        "La condición requiere ExpectedValue con true o false.");
                }

                break;
            case RsvpVisibilityConditionType.GroupHasTag:
                if (string.IsNullOrWhiteSpace(rule.ExpectedValue)
                    || rule.ExpectedValue.Length > 100)
                {
                    AddError(
                        errors,
                        rulePath,
                        "GroupHasTag requiere un nombre de etiqueta válido.");
                }

                break;
        }

        if (rule.Conditions is null)
        {
            AddError(
                errors,
                rulePath,
                "Conditions debe ser un arreglo.");
        }
        else if (rule.Conditions.Count > 0)
        {
            AddError(
                errors,
                rulePath,
                "Las condiciones simples no admiten condiciones hijas.");
        }
    }

    private static void ValidatePreviousAnswerReference(
        RsvpQuestion question,
        VisibilityRule rule,
        string path,
        IReadOnlyDictionary<string, RsvpQuestion> knownQuestions,
        IDictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(rule.ReferenceQuestionId)
            || !knownQuestions.TryGetValue(
                rule.ReferenceQuestionId,
                out var referenced))
        {
            AddError(
                errors,
                path,
                "La regla referencia una pregunta inexistente.");
            return;
        }

        if (referenced.SortOrder >= question.SortOrder)
        {
            AddError(
                errors,
                path,
                "La regla solo puede referenciar preguntas anteriores.");
        }

        if (rule.ExpectedValue is null
            || rule.ExpectedValue.Length > MaximumLongTextLength)
        {
            AddError(
                errors,
                path,
                "La condición requiere un ExpectedValue limitado.");
        }
    }

    private static void ValidateEnumExpected<TEnum>(
        VisibilityRule rule,
        string path,
        IDictionary<string, List<string>> errors)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(
                rule.ExpectedValue,
                ignoreCase: false,
                out _))
        {
            AddError(
                errors,
                path,
                $"ExpectedValue debe pertenecer al catálogo {typeof(TEnum).Name}.");
        }
    }

    private static void ValidateVisibilityGraph(
        IReadOnlyList<RsvpQuestion> questions,
        IDictionary<string, List<string>> errors)
    {
        var graph = questions
            .Where(question =>
                !string.IsNullOrWhiteSpace(question.Id))
            .GroupBy(question => question.Id, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => GetReferences(
                        group.Single().VisibilityRule)
                    .ToList(),
                StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var question in questions)
        {
            if (HasCycle(question.Id, graph, visiting, visited))
            {
                AddError(
                    errors,
                    $"questions[{question.Id}].visibilityRule",
                    "Las reglas de visibilidad contienen un ciclo.");
            }
        }
    }

    private static bool HasCycle(
        string id,
        IReadOnlyDictionary<string, List<string>> graph,
        ISet<string> visiting,
        ISet<string> visited)
    {
        if (visited.Contains(id))
        {
            return false;
        }

        if (!visiting.Add(id))
        {
            return true;
        }

        if (graph.TryGetValue(id, out var references))
        {
            foreach (var reference in references.Where(graph.ContainsKey))
            {
                if (HasCycle(reference, graph, visiting, visited))
                {
                    return true;
                }
            }
        }

        visiting.Remove(id);
        visited.Add(id);
        return false;
    }

    private static IEnumerable<string> GetReferences(VisibilityRule? rule)
    {
        if (rule is null)
        {
            yield break;
        }

        if (rule.ConditionType is
            RsvpVisibilityConditionType.PreviousAnswerEquals
            or RsvpVisibilityConditionType.PreviousAnswerContains
            && rule.ReferenceQuestionId is not null)
        {
            yield return rule.ReferenceQuestionId;
        }

        foreach (var child in rule.Conditions ?? [])
        {
            foreach (var reference in GetReferences(child))
            {
                yield return reference;
            }
        }
    }

    private static RsvpQuestion NormalizeQuestion(RsvpQuestion question) =>
        new()
        {
            Id = question.Id.Trim().Normalize(),
            QuestionType = question.QuestionType,
            Scope = question.Scope,
            Category = question.Category,
            Label = question.Label.Trim().Normalize(),
            HelpText = NormalizeOptional(question.HelpText),
            IsRequired = question.IsRequired,
            IsSensitive = question.IsSensitive,
            IsActive = question.IsActive,
            SortOrder = question.SortOrder,
            Options = question.Options
                .OrderBy(option => option.SortOrder)
                .Select(option => new RsvpQuestionOption
                {
                    Key = option.Key.Trim().Normalize(),
                    Label = option.Label.Trim().Normalize(),
                    IsActive = option.IsActive,
                    SortOrder = option.SortOrder
                })
                .ToList(),
            VisibilityRule = NormalizeVisibilityRule(question.VisibilityRule),
            ValidationRules = new ValidationRules
            {
                Required = question.IsRequired,
                MinLength = question.ValidationRules.MinLength,
                MaxLength = question.ValidationRules.MaxLength,
                MinimumSelections =
                    question.ValidationRules.MinimumSelections,
                MaximumSelections =
                    question.ValidationRules.MaximumSelections,
                Minimum = question.ValidationRules.Minimum,
                Maximum = question.ValidationRules.Maximum,
                IntegerOnly = question.ValidationRules.IntegerOnly,
                MinimumDate = question.ValidationRules.MinimumDate,
                MaximumDate = question.ValidationRules.MaximumDate
            }
        };

    private static VisibilityRule NormalizeVisibilityRule(
        VisibilityRule rule) =>
        new()
        {
            ConditionType = rule.ConditionType,
            ReferenceQuestionId =
                NormalizeOptional(rule.ReferenceQuestionId),
            ExpectedValue = NormalizeOptional(rule.ExpectedValue),
            Conditions = rule.Conditions
                .Select(NormalizeVisibilityRule)
                .ToList()
        };

    private static bool RequiresSensitiveHandling(RsvpQuestion question) =>
        question.QuestionType == RsvpQuestionType.InformationalConsent
        || (question.Category is
                RsvpQuestionCategory.Dietary
                or RsvpQuestionCategory.Accessibility
            && question.QuestionType is
                RsvpQuestionType.ShortText
                or RsvpQuestionType.LongText);

    private static void ValidateVisibleText(
        string? value,
        int maximumLength,
        string path,
        bool required,
        IDictionary<string, List<string>> errors)
    {
        var normalized = value?.Trim();
        if ((required && string.IsNullOrEmpty(normalized))
            || normalized?.Length > maximumLength)
        {
            AddError(
                errors,
                path,
                required
                    ? $"El texto es obligatorio y admite hasta {maximumLength} caracteres."
                    : $"El texto admite hasta {maximumLength} caracteres.");
        }

        if (!string.IsNullOrEmpty(normalized)
            && UnsafeMarkupPattern().IsMatch(normalized))
        {
            AddError(
                errors,
                path,
                "No se permite HTML, scripts ni manejadores de eventos.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().Normalize();

    private static RequestValidationException DefinitionError(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var messages))
        {
            messages = [];
            errors[field] = messages;
        }

        if (!messages.Contains(message, StringComparer.Ordinal))
        {
            messages.Add(message);
        }
    }

    [GeneratedRegex(
        "^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex QuestionIdPattern();

    [GeneratedRegex(
        @"<\s*/?\s*[a-zA-Z!]|javascript\s*:|on[a-zA-Z]+\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnsafeMarkupPattern();
}
