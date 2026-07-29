using System.Globalization;
using System.Text.Json;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed record RsvpQuestionGuestContext(
    Guid ResponseGuestId,
    Guid? EventGuestId,
    string DisplayName,
    AgeCategory AgeCategory,
    GuestType GuestType,
    bool IsUnnamedCompanion,
    bool IsPrimaryContact,
    GuestAttendanceStatus AttendanceStatus);

public sealed record RsvpQuestionEvaluationContext(
    IReadOnlyList<RsvpQuestionGuestContext> Guests,
    IReadOnlySet<string> GroupTags)
{
    public RsvpQuestionGuestContext? PrimaryContact =>
        Guests.SingleOrDefault(guest => guest.IsPrimaryContact);
}

public sealed record NormalizedRsvpAnswer(
    string QuestionId,
    Guid? GuestId,
    string AnswerValue,
    string? DisplayValue,
    string QuestionLabelSnapshot,
    RsvpQuestionType QuestionTypeSnapshot,
    string OptionLabelsSnapshot,
    string? GuestDisplayNameSnapshot,
    bool IsSensitive);

public sealed record RsvpQuestionValidationResult(
    IReadOnlyList<NormalizedRsvpAnswer> Answers,
    bool ContainsSensitiveAnswers);

public static class RsvpQuestionEngine
{
    public static RsvpQuestionValidationResult ValidateAndNormalize(
        IReadOnlyList<RsvpQuestion> questions,
        RsvpQuestionEvaluationContext context,
        IReadOnlyList<RsvpSubmissionAnswerRequest> submittedAnswers,
        string? consentSnapshot)
    {
        var errors = new List<RsvpValidationError>();
        var questionsById = questions.ToDictionary(
            question => question.Id,
            StringComparer.Ordinal);
        var submittedByTarget =
            new Dictionary<(string QuestionId, Guid? GuestId),
                RsvpSubmissionAnswerRequest>();

        foreach (var answer in submittedAnswers)
        {
            var questionId = answer.QuestionId?.Trim() ?? string.Empty;
            if (!questionsById.TryGetValue(questionId, out var question))
            {
                errors.Add(Error(
                    questionId,
                    answer.GuestId,
                    "unknown_question",
                    "La pregunta no pertenece a la versión presentada."));
                continue;
            }

            var key = (question.Id, answer.GuestId);
            if (!submittedByTarget.TryAdd(key, answer))
            {
                errors.Add(Error(
                    question.Id,
                    answer.GuestId,
                    "duplicate_answer",
                    "La pregunta contiene más de una respuesta para el mismo destinatario."));
            }
        }

        var normalized =
            new Dictionary<(string QuestionId, Guid? GuestId),
                NormalizedRsvpAnswer>();
        foreach (var question in questions
                     .OrderBy(item => item.SortOrder)
                     .ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            ValidateUnexpectedTargets(
                question,
                context,
                submittedByTarget,
                errors);
            var targets = TargetsFor(question, context);
            if (question.Scope == RsvpQuestionScope.PrimaryContact
                && targets.Count == 0
                && question.IsRequired
                && question.IsActive
                && EvaluateVisibility(
                    question.VisibilityRule,
                    target: null,
                    context,
                    questionsById,
                    normalized))
            {
                errors.Add(Error(
                    question.Id,
                    null,
                    "required_answer_missing",
                    "La pregunta requiere incluir y responder al contacto principal."));
            }

            foreach (var target in targets)
            {
                var key = (question.Id, target?.ResponseGuestId);
                submittedByTarget.TryGetValue(key, out var submitted);
                var visible = question.IsActive
                              && EvaluateVisibility(
                                  question.VisibilityRule,
                                  target,
                                  context,
                                  questionsById,
                                  normalized);
                if (!visible)
                {
                    if (submitted is not null && HasSubmittedValue(submitted))
                    {
                        errors.Add(Error(
                            question.Id,
                            target?.ResponseGuestId,
                            "hidden_question_answered",
                            "No se admite una respuesta para una pregunta oculta."));
                    }

                    continue;
                }

                if (submitted is null || !HasSubmittedValue(submitted))
                {
                    if (question.IsRequired)
                    {
                        errors.Add(Error(
                            question.Id,
                            target?.ResponseGuestId,
                            question.QuestionType
                            == RsvpQuestionType.InformationalConsent
                                ? "consent_required"
                                : "required_answer_missing",
                            question.QuestionType
                            == RsvpQuestionType.InformationalConsent
                                ? "Debes confirmar explícitamente el consentimiento."
                                : "La pregunta requiere una respuesta."));
                    }

                    continue;
                }

                var normalizedAnswer = NormalizeAnswer(
                    question,
                    target,
                    submitted,
                    errors);
                if (normalizedAnswer is not null)
                {
                    normalized[key] = normalizedAnswer;
                }
            }
        }

        var sensitiveAnswers = normalized.Values
            .Where(answer => answer.IsSensitive)
            .ToList();
        var sensitiveContentAnswers = sensitiveAnswers
            .Where(answer =>
                answer.QuestionTypeSnapshot
                != RsvpQuestionType.InformationalConsent)
            .ToList();
        if (sensitiveContentAnswers.Count > 0
            && !HasExplicitConsent(normalized.Values, consentSnapshot))
        {
            var first = sensitiveContentAnswers[0];
            errors.Add(Error(
                first.QuestionId,
                first.GuestId,
                "consent_required",
                "Debes otorgar consentimiento antes de enviar datos sensibles."));
        }

        if (errors.Count > 0)
        {
            throw new RsvpValidationException(
                errors
                    .Distinct()
                    .OrderBy(error => error.QuestionId, StringComparer.Ordinal)
                    .ThenBy(error => error.GuestId)
                    .ThenBy(error => error.Code, StringComparer.Ordinal)
                    .ToList());
        }

        return new RsvpQuestionValidationResult(
            normalized.Values
                .OrderBy(answer =>
                    questionsById[answer.QuestionId].SortOrder)
                .ThenBy(answer => answer.GuestId)
                .ToList(),
            sensitiveAnswers.Count > 0);
    }

    public static bool IsVisible(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        RsvpQuestionEvaluationContext context,
        IReadOnlyList<RsvpQuestion> questions,
        IReadOnlyList<NormalizedRsvpAnswer> previousAnswers)
    {
        var definitions = questions.ToDictionary(
            item => item.Id,
            StringComparer.Ordinal);
        var answers = previousAnswers.ToDictionary(
            answer => (answer.QuestionId, answer.GuestId));
        return question.IsActive
               && EvaluateVisibility(
                   question.VisibilityRule,
                   target,
                   context,
                   definitions,
                   answers);
    }

    private static void ValidateUnexpectedTargets(
        RsvpQuestion question,
        RsvpQuestionEvaluationContext context,
        IReadOnlyDictionary<(string QuestionId, Guid? GuestId),
            RsvpSubmissionAnswerRequest> submitted,
        ICollection<RsvpValidationError> errors)
    {
        foreach (var entry in submitted.Where(entry =>
                     entry.Key.QuestionId == question.Id))
        {
            var guestId = entry.Key.GuestId;
            switch (question.Scope)
            {
                case RsvpQuestionScope.InvitationGroup when guestId.HasValue:
                    errors.Add(Error(
                        question.Id,
                        guestId,
                        "invalid_scope",
                        "Las respuestas del grupo no admiten GuestId."));
                    break;
                case RsvpQuestionScope.PrimaryContact
                    when context.PrimaryContact is null
                         || guestId
                         != context.PrimaryContact.ResponseGuestId:
                    errors.Add(Error(
                        question.Id,
                        guestId,
                        "invalid_scope",
                        "La respuesta debe corresponder al contacto principal vigente."));
                    break;
                case RsvpQuestionScope.IndividualGuest
                    when !guestId.HasValue
                         || context.Guests.All(guest =>
                             guest.ResponseGuestId != guestId.Value):
                    errors.Add(Error(
                        question.Id,
                        guestId,
                        "guest_not_in_group",
                        "El invitado no pertenece al grupo o no está incluido en la entrega."));
                    break;
            }
        }
    }

    private static IReadOnlyList<RsvpQuestionGuestContext?> TargetsFor(
        RsvpQuestion question,
        RsvpQuestionEvaluationContext context) =>
        question.Scope switch
        {
            RsvpQuestionScope.InvitationGroup => [null],
            RsvpQuestionScope.PrimaryContact =>
                context.PrimaryContact is null
                    ? []
                    : [context.PrimaryContact],
            RsvpQuestionScope.IndividualGuest =>
                context.Guests
                    .Cast<RsvpQuestionGuestContext?>()
                    .ToList(),
            _ => []
        };

    private static NormalizedRsvpAnswer? NormalizeAnswer(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        RsvpSubmissionAnswerRequest submitted,
        ICollection<RsvpValidationError> errors)
    {
        var guestId = target?.ResponseGuestId;
        return question.QuestionType switch
        {
            RsvpQuestionType.ShortText => NormalizeText(
                question,
                target,
                submitted,
                RsvpQuestionDefinitionParser.MaximumShortTextLength,
                errors),
            RsvpQuestionType.LongText => NormalizeText(
                question,
                target,
                submitted,
                RsvpQuestionDefinitionParser.MaximumLongTextLength,
                errors),
            RsvpQuestionType.YesNo => NormalizeBoolean(
                question,
                target,
                submitted,
                consent: false,
                errors),
            RsvpQuestionType.InformationalConsent => NormalizeBoolean(
                question,
                target,
                submitted,
                consent: true,
                errors),
            RsvpQuestionType.SingleChoice => NormalizeSingleChoice(
                question,
                target,
                submitted,
                errors),
            RsvpQuestionType.MultipleChoice => NormalizeMultipleChoice(
                question,
                target,
                submitted,
                errors),
            RsvpQuestionType.Number => NormalizeNumber(
                question,
                target,
                submitted,
                errors),
            RsvpQuestionType.Date => NormalizeDate(
                question,
                target,
                submitted,
                errors),
            _ => Invalid(
                question,
                guestId,
                "invalid_value_type",
                "El tipo de respuesta no está soportado.",
                errors)
        };
    }

    private static NormalizedRsvpAnswer? NormalizeText(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        RsvpSubmissionAnswerRequest submitted,
        int absoluteMaximum,
        ICollection<RsvpValidationError> errors)
    {
        if (!TryReadString(submitted.AnswerValue, out var value))
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_value_type",
                "La respuesta debe ser texto.",
                errors);
        }

        var normalized = value.Trim().Normalize();
        if (normalized.Length == 0)
        {
            return question.IsRequired
                ? Invalid(
                    question,
                    target?.ResponseGuestId,
                    "required_answer_missing",
                    "La pregunta requiere una respuesta.",
                    errors)
                : null;
        }

        var minimum = question.ValidationRules.MinLength ?? 0;
        var maximum = question.ValidationRules.MaxLength
                      ?? absoluteMaximum;
        if (normalized.Length < minimum)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "value_too_short",
                $"La respuesta requiere al menos {minimum} caracteres.",
                errors);
        }

        if (normalized.Length > maximum
            || normalized.Length > absoluteMaximum)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "value_too_long",
                $"La respuesta admite hasta {maximum} caracteres.",
                errors);
        }

        return CreateNormalized(
            question,
            target,
            JsonSerializer.Serialize(normalized),
            normalized,
            []);
    }

    private static NormalizedRsvpAnswer? NormalizeBoolean(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        RsvpSubmissionAnswerRequest submitted,
        bool consent,
        ICollection<RsvpValidationError> errors)
    {
        if (!TryReadBoolean(submitted.AnswerValue, out var value))
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_value_type",
                "La respuesta debe ser un booleano true o false.",
                errors);
        }

        if (consent && question.IsRequired && !value)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "consent_required",
                "Debes confirmar explícitamente el consentimiento.",
                errors);
        }

        return CreateNormalized(
            question,
            target,
            value ? "true" : "false",
            consent
                ? value ? "Aceptado" : "No aceptado"
                : value ? "Sí" : "No",
            []);
    }

    private static NormalizedRsvpAnswer? NormalizeSingleChoice(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        RsvpSubmissionAnswerRequest submitted,
        ICollection<RsvpValidationError> errors)
    {
        if (!TryReadString(submitted.AnswerValue, out var key))
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_value_type",
                "La respuesta debe contener una clave de opción.",
                errors);
        }

        var option = question.Options.SingleOrDefault(item =>
            item.IsActive && item.Key == key);
        if (option is null)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_option",
                "La opción seleccionada no está disponible.",
                errors);
        }

        return CreateNormalized(
            question,
            target,
            JsonSerializer.Serialize(option.Key),
            option.Label,
            [new OptionSnapshot(option.Key, option.Label)]);
    }

    private static NormalizedRsvpAnswer? NormalizeMultipleChoice(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        RsvpSubmissionAnswerRequest submitted,
        ICollection<RsvpValidationError> errors)
    {
        if (!TryReadStringArray(submitted.AnswerValue, out var keys))
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_value_type",
                "La respuesta debe ser un arreglo de claves de opción.",
                errors);
        }

        if (keys.Count != keys.Distinct(StringComparer.Ordinal).Count())
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_option",
                "La respuesta no admite opciones repetidas.",
                errors);
        }

        var options = question.Options
            .Where(option =>
                option.IsActive && keys.Contains(
                    option.Key,
                    StringComparer.Ordinal))
            .OrderBy(option => option.SortOrder)
            .ToList();
        if (options.Count != keys.Count)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_option",
                "Una o más opciones seleccionadas no están disponibles.",
                errors);
        }

        if (options.Count == 0 && !question.IsRequired)
        {
            return null;
        }

        var minimum = question.ValidationRules.MinimumSelections
                      ?? (question.IsRequired ? 1 : 0);
        var maximum = question.ValidationRules.MaximumSelections
                      ?? question.Options.Count(option => option.IsActive);
        if (options.Count < minimum)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "too_few_selections",
                $"Selecciona al menos {minimum} opciones.",
                errors);
        }

        if (options.Count > maximum)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "too_many_selections",
                $"Selecciona como máximo {maximum} opciones.",
                errors);
        }

        var stableKeys = options
            .Select(option => option.Key)
            .Order(StringComparer.Ordinal)
            .ToList();
        return CreateNormalized(
            question,
            target,
            JsonSerializer.Serialize(stableKeys),
            string.Join(", ", options.Select(option => option.Label)),
            options
                .Select(option =>
                    new OptionSnapshot(option.Key, option.Label))
                .ToList());
    }

    private static NormalizedRsvpAnswer? NormalizeNumber(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        RsvpSubmissionAnswerRequest submitted,
        ICollection<RsvpValidationError> errors)
    {
        if (!decimal.TryParse(
                submitted.AnswerValue?.Trim(),
                NumberStyles.Number | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture,
                out var value))
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_value_type",
                "La respuesta debe ser un número en formato invariable.",
                errors);
        }

        if (question.ValidationRules.IntegerOnly == true
            && decimal.Truncate(value) != value)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_value_type",
                "La respuesta debe ser un número entero.",
                errors);
        }

        if (question.ValidationRules.Minimum is { } minimum
            && value < minimum)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "value_below_minimum",
                $"La respuesta no puede ser menor que {minimum.ToString(CultureInfo.InvariantCulture)}.",
                errors);
        }

        if (question.ValidationRules.Maximum is { } maximum
            && value > maximum)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "value_above_maximum",
                $"La respuesta no puede ser mayor que {maximum.ToString(CultureInfo.InvariantCulture)}.",
                errors);
        }

        var canonical = value.ToString(CultureInfo.InvariantCulture);
        return CreateNormalized(
            question,
            target,
            canonical,
            canonical,
            []);
    }

    private static NormalizedRsvpAnswer? NormalizeDate(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        RsvpSubmissionAnswerRequest submitted,
        ICollection<RsvpValidationError> errors)
    {
        if (!TryReadString(submitted.AnswerValue, out var raw)
            || !DateOnly.TryParseExact(
                raw,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var value))
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "invalid_date",
                "La fecha debe usar el formato YYYY-MM-DD.",
                errors);
        }

        if (question.ValidationRules.MinimumDate is { } minimum
            && value < minimum)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "value_below_minimum",
                $"La fecha no puede ser anterior a {minimum:yyyy-MM-dd}.",
                errors);
        }

        if (question.ValidationRules.MaximumDate is { } maximum
            && value > maximum)
        {
            return Invalid(
                question,
                target?.ResponseGuestId,
                "value_above_maximum",
                $"La fecha no puede ser posterior a {maximum:yyyy-MM-dd}.",
                errors);
        }

        var canonical = value.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);
        return CreateNormalized(
            question,
            target,
            JsonSerializer.Serialize(canonical),
            canonical,
            []);
    }

    private static NormalizedRsvpAnswer CreateNormalized(
        RsvpQuestion question,
        RsvpQuestionGuestContext? target,
        string answerValue,
        string? displayValue,
        IReadOnlyList<OptionSnapshot> optionSnapshots) =>
        new(
            question.Id,
            target?.ResponseGuestId,
            answerValue,
            displayValue,
            question.Label,
            question.QuestionType,
            JsonSerializer.Serialize(optionSnapshots),
            target?.DisplayName,
            question.IsSensitive);

    private static NormalizedRsvpAnswer? Invalid(
        RsvpQuestion question,
        Guid? guestId,
        string code,
        string message,
        ICollection<RsvpValidationError> errors)
    {
        errors.Add(Error(question.Id, guestId, code, message));
        return null;
    }

    private static bool EvaluateVisibility(
        VisibilityRule rule,
        RsvpQuestionGuestContext? target,
        RsvpQuestionEvaluationContext context,
        IReadOnlyDictionary<string, RsvpQuestion> questions,
        IReadOnlyDictionary<(string QuestionId, Guid? GuestId),
            NormalizedRsvpAnswer> answers) =>
        rule.ConditionType switch
        {
            RsvpVisibilityConditionType.Always => true,
            RsvpVisibilityConditionType.All => rule.Conditions.All(child =>
                EvaluateVisibility(
                    child,
                    target,
                    context,
                    questions,
                    answers)),
            RsvpVisibilityConditionType.Any => rule.Conditions.Any(child =>
                EvaluateVisibility(
                    child,
                    target,
                    context,
                    questions,
                    answers)),
            RsvpVisibilityConditionType.AttendanceStatusEquals =>
                ResolveGuests(target, context).Any(guest =>
                    guest.AttendanceStatus.ToString()
                    == rule.ExpectedValue),
            RsvpVisibilityConditionType.GuestAgeCategoryEquals =>
                ResolveGuests(target, context).Any(guest =>
                    guest.AgeCategory.ToString() == rule.ExpectedValue),
            RsvpVisibilityConditionType.GuestTypeEquals =>
                ResolveGuests(target, context).Any(guest =>
                    guest.GuestType.ToString() == rule.ExpectedValue),
            RsvpVisibilityConditionType.GroupHasTag =>
                rule.ExpectedValue is not null
                && context.GroupTags.Contains(rule.ExpectedValue),
            RsvpVisibilityConditionType.IsUnnamedCompanion =>
                ResolveGuests(target, context).Any(guest =>
                    guest.IsUnnamedCompanion
                    == bool.Parse(rule.ExpectedValue!)),
            RsvpVisibilityConditionType.IsPrimaryContact =>
                ResolveGuests(target, context).Any(guest =>
                    guest.IsPrimaryContact
                    == bool.Parse(rule.ExpectedValue!)),
            RsvpVisibilityConditionType.PreviousAnswerEquals =>
                TryResolvePreviousAnswer(
                    rule,
                    target,
                    context,
                    questions,
                    answers,
                    out var equalsAnswer)
                && EqualsExpected(
                    equalsAnswer.AnswerValue,
                    rule.ExpectedValue),
            RsvpVisibilityConditionType.PreviousAnswerContains =>
                TryResolvePreviousAnswer(
                    rule,
                    target,
                    context,
                    questions,
                    answers,
                    out var containsAnswer)
                && ContainsExpected(
                    containsAnswer.AnswerValue,
                    rule.ExpectedValue),
            _ => false
        };

    private static IReadOnlyList<RsvpQuestionGuestContext> ResolveGuests(
        RsvpQuestionGuestContext? target,
        RsvpQuestionEvaluationContext context) =>
        target is null ? context.Guests : [target];

    private static bool TryResolvePreviousAnswer(
        VisibilityRule rule,
        RsvpQuestionGuestContext? target,
        RsvpQuestionEvaluationContext context,
        IReadOnlyDictionary<string, RsvpQuestion> questions,
        IReadOnlyDictionary<(string QuestionId, Guid? GuestId),
            NormalizedRsvpAnswer> answers,
        out NormalizedRsvpAnswer answer)
    {
        answer = default!;
        if (rule.ReferenceQuestionId is null
            || !questions.TryGetValue(
                rule.ReferenceQuestionId,
                out var referenced))
        {
            return false;
        }

        var guestId = referenced.Scope switch
        {
            RsvpQuestionScope.InvitationGroup => null,
            RsvpQuestionScope.PrimaryContact =>
                context.PrimaryContact?.ResponseGuestId,
            RsvpQuestionScope.IndividualGuest =>
                target?.ResponseGuestId,
            _ => null
        };
        return answers.TryGetValue(
            (referenced.Id, guestId),
            out answer!);
    }

    private static bool EqualsExpected(
        string normalizedJson,
        string? expected)
    {
        var values = ReadComparableValues(normalizedJson);
        return values.Count == 1 && values[0] == expected;
    }

    private static bool ContainsExpected(
        string normalizedJson,
        string? expected) =>
        expected is not null
        && ReadComparableValues(normalizedJson).Contains(
            expected,
            StringComparer.Ordinal);

    private static IReadOnlyList<string> ReadComparableValues(
        string normalizedJson)
    {
        using var document = JsonDocument.Parse(normalizedJson);
        return document.RootElement.ValueKind switch
        {
            JsonValueKind.Array => document.RootElement
                .EnumerateArray()
                .Select(ReadComparableValue)
                .ToList(),
            _ => [ReadComparableValue(document.RootElement)]
        };
    }

    private static string ReadComparableValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => element.GetRawText(),
            _ => element.GetRawText()
        };

    private static bool HasSubmittedValue(
        RsvpSubmissionAnswerRequest answer) =>
        !string.IsNullOrWhiteSpace(answer.AnswerValue);

    private static bool HasExplicitConsent(
        IEnumerable<NormalizedRsvpAnswer> answers,
        string? consentSnapshot)
    {
        if (answers.Any(answer =>
                answer.QuestionTypeSnapshot
                == RsvpQuestionType.InformationalConsent
                && answer.AnswerValue == "true"))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(consentSnapshot))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(consentSnapshot);
            return document.RootElement.ValueKind == JsonValueKind.Object
                   && document.RootElement
                       .EnumerateObject()
                       .Any(property =>
                           property.Name.Equals(
                               "consentGranted",
                               StringComparison.OrdinalIgnoreCase)
                           && property.Value.ValueKind
                           == JsonValueKind.True);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadString(string? raw, out string value)
    {
        value = string.Empty;
        if (raw is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = document.RootElement.GetString() ?? string.Empty;
            return true;
        }
        catch (JsonException)
        {
            value = raw;
            return true;
        }
    }

    private static bool TryReadBoolean(string? raw, out bool value)
    {
        value = false;
        if (raw is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind is not (
                    JsonValueKind.True or JsonValueKind.False))
            {
                return false;
            }

            value = document.RootElement.GetBoolean();
            return true;
        }
        catch (JsonException)
        {
            return bool.TryParse(raw, out value)
                   && raw is "true" or "false";
        }
    }

    private static bool TryReadStringArray(
        string? raw,
        out List<string> values)
    {
        values = [];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    return false;
                }

                values.Add(item.GetString() ?? string.Empty);
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static RsvpValidationError Error(
        string? questionId,
        Guid? guestId,
        string code,
        string message) =>
        new(questionId, guestId, code, message);

    private sealed record OptionSnapshot(string Key, string Label);
}
