using System.Text.Json;
using System.Text.Json.Serialization;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Rsvp.Application;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.UnitTests.Rsvp;

public sealed class RsvpQuestionDefinitionParserTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(
        JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(allowIntegerValues: false)
        }
    };

    [Fact]
    public void ParseAndValidate_Accepts_Every_Supported_Type()
    {
        var questions = new[]
        {
            Question("short", RsvpQuestionType.ShortText, 0),
            Question("long", RsvpQuestionType.LongText, 1),
            Question("yes-no", RsvpQuestionType.YesNo, 2),
            Question(
                "single",
                RsvpQuestionType.SingleChoice,
                3,
                options: Options("a", "b")),
            Question(
                "multiple",
                RsvpQuestionType.MultipleChoice,
                4,
                options: Options("a", "b")),
            Question("number", RsvpQuestionType.Number, 5),
            Question("date", RsvpQuestionType.Date, 6),
            Question(
                "consent",
                RsvpQuestionType.InformationalConsent,
                7,
                category: RsvpQuestionCategory.Consent,
                sensitive: true)
        };

        var parsed = RsvpQuestionDefinitionParser.ParseAndValidate(
            Serialize(questions));

        Assert.Equal(8, parsed.Questions.Count);
        Assert.All(
            Enum.GetValues<RsvpQuestionType>(),
            type => Assert.Contains(
                parsed.Questions,
                question => question.QuestionType == type));
    }

    [Fact]
    public void ParseAndValidate_Rejects_Unknown_Enum_And_Properties()
    {
        var snapshot = Serialize(
                new[] { Question("q1", RsvpQuestionType.ShortText, 0) })
            .Replace(
                "\"questionType\":\"ShortText\"",
                "\"questionType\":\"Script\",\"expression\":\"alert(1)\"",
                StringComparison.Ordinal);

        Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(snapshot));
    }

    [Fact]
    public void ParseAndValidate_Rejects_Null_Objects_Without_Internal_Error()
    {
        const string snapshot =
            """
            [{
              "id": "q1",
              "questionType": "ShortText",
              "scope": "InvitationGroup",
              "category": "General",
              "label": "Pregunta",
              "isRequired": false,
              "isSensitive": false,
              "isActive": true,
              "sortOrder": 0,
              "options": null,
              "visibilityRule": null,
              "validationRules": null
            }]
            """;

        var error = Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(snapshot));

        Assert.Contains("questions[0].options", error.Errors.Keys);
        Assert.Contains("questions[0].visibilityRule", error.Errors.Keys);
        Assert.Contains("questions[0].validationRules", error.Errors.Keys);
    }

    [Fact]
    public void ParseAndValidate_Rejects_Duplicate_Ids_And_Orders()
    {
        var questions = new[]
        {
            Question("duplicate", RsvpQuestionType.ShortText, 0),
            Question("duplicate", RsvpQuestionType.LongText, 0)
        };

        var error = Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(
                Serialize(questions)));

        Assert.Contains(
            error.Errors.Keys,
            key => key.EndsWith(".id", StringComparison.Ordinal));
        Assert.Contains(
            error.Errors.Keys,
            key => key.EndsWith(".sortOrder", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseAndValidate_Rejects_Options_For_Text()
    {
        var question = Question(
            "q1",
            RsvpQuestionType.ShortText,
            0,
            options: Options("a", "b"));

        Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(
                Serialize(new[] { question })));
    }

    [Fact]
    public void ParseAndValidate_Rejects_Invalid_Choice_Options()
    {
        var duplicateOptions = new List<RsvpQuestionOption>
        {
            Option("same", "Primera", 0),
            Option("same", "Segunda", 1)
        };
        var question = Question(
            "q1",
            RsvpQuestionType.SingleChoice,
            0,
            options: duplicateOptions);

        Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(
                Serialize(new[] { question })));
    }

    [Fact]
    public void ParseAndValidate_Rejects_Rules_Incompatible_With_Type()
    {
        var question = Question(
            "q1",
            RsvpQuestionType.YesNo,
            0,
            rules: new ValidationRules { MinLength = 1 });

        Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(
                Serialize(new[] { question })));
    }

    [Fact]
    public void ParseAndValidate_Rejects_Html_And_Excessive_Length()
    {
        var question = Question(
            "q1",
            RsvpQuestionType.ShortText,
            0,
            label: "<script>alert('x')</script>",
            helpText: new string('a', 1001));

        var error = Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(
                Serialize(new[] { question })));

        Assert.True(error.Errors.Count >= 2);
    }

    [Fact]
    public void ParseAndValidate_Rejects_Forward_Reference_And_Cycle()
    {
        var first = Question(
            "q1",
            RsvpQuestionType.YesNo,
            0,
            visibility: PreviousEquals("q2", "true"));
        var second = Question(
            "q2",
            RsvpQuestionType.YesNo,
            1,
            visibility: PreviousEquals("q1", "true"));

        var error = Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(
                Serialize(new[] { first, second })));

        Assert.Contains(
            error.Errors.Values.SelectMany(value => value),
            message => message.Contains(
                "referencia",
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            error.Errors.Values.SelectMany(value => value),
            message => message.Contains(
                "ciclo",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseAndValidate_Rejects_Visibility_Depth_Over_Limit()
    {
        VisibilityRule rule = VisibilityRule.Always();
        for (var index = 0;
             index <= RsvpQuestionDefinitionParser.MaximumVisibilityDepth;
             index++)
        {
            rule = new VisibilityRule
            {
                ConditionType = RsvpVisibilityConditionType.All,
                Conditions = [rule]
            };
        }

        var question = Question(
            "q1",
            RsvpQuestionType.ShortText,
            0,
            visibility: rule);

        Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(
                Serialize(new[] { question })));
    }

    [Fact]
    public void ParseAndValidate_Requires_Sensitive_Mark_For_Free_Text_Needs()
    {
        var question = Question(
            "allergies",
            RsvpQuestionType.LongText,
            0,
            category: RsvpQuestionCategory.Dietary);

        Assert.Throws<RequestValidationException>(() =>
            RsvpQuestionDefinitionParser.ParseAndValidate(
                Serialize(new[] { question })));
    }

    [Fact]
    public void Catalog_Contains_Exactly_The_Controlled_Enums()
    {
        var catalog = RsvpQuestionDefinitionParser.GetCatalog();

        Assert.Equal(
            Enum.GetNames<RsvpQuestionType>(),
            catalog.QuestionTypes);
        Assert.Equal(
            Enum.GetNames<RsvpQuestionScope>(),
            catalog.QuestionScopes);
        Assert.Equal(
            Enum.GetNames<RsvpQuestionCategory>(),
            catalog.QuestionCategories);
        Assert.Equal(
            Enum.GetNames<RsvpVisibilityConditionType>(),
            catalog.VisibilityConditionTypes);
    }

    private static string Serialize(IEnumerable<RsvpQuestion> questions) =>
        JsonSerializer.Serialize(questions, JsonOptions);

    private static RsvpQuestion Question(
        string id,
        RsvpQuestionType type,
        int order,
        string label = "Pregunta",
        string? helpText = null,
        RsvpQuestionCategory category = RsvpQuestionCategory.General,
        bool sensitive = false,
        List<RsvpQuestionOption>? options = null,
        VisibilityRule? visibility = null,
        ValidationRules? rules = null) =>
        new()
        {
            Id = id,
            QuestionType = type,
            Scope = RsvpQuestionScope.InvitationGroup,
            Category = category,
            Label = label,
            HelpText = helpText,
            IsRequired = false,
            IsSensitive = sensitive,
            IsActive = true,
            SortOrder = order,
            Options = options ?? [],
            VisibilityRule = visibility ?? VisibilityRule.Always(),
            ValidationRules = rules ?? new ValidationRules()
        };

    private static List<RsvpQuestionOption> Options(
        params string[] keys) =>
        keys.Select((key, index) =>
                Option(key, $"Opción {key}", index))
            .ToList();

    private static RsvpQuestionOption Option(
        string key,
        string label,
        int order) =>
        new()
        {
            Key = key,
            Label = label,
            IsActive = true,
            SortOrder = order
        };

    private static VisibilityRule PreviousEquals(
        string questionId,
        string expected) =>
        new()
        {
            ConditionType =
                RsvpVisibilityConditionType.PreviousAnswerEquals,
            ReferenceQuestionId = questionId,
            ExpectedValue = expected
        };
}

public sealed class RsvpQuestionEngineTests
{
    private static readonly Guid PrimaryGuestId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111");
    private static readonly Guid CompanionGuestId = Guid.Parse(
        "22222222-2222-2222-2222-222222222222");
    private static readonly Guid ForeignGuestId = Guid.Parse(
        "99999999-9999-9999-9999-999999999999");

    public static TheoryData<RsvpQuestionType, string, string> ValidValues =>
        new()
        {
            { RsvpQuestionType.ShortText, "\"  José  \"", "\"Jos\\u00E9\"" },
            { RsvpQuestionType.LongText, "\"Texto amplio\"", "\"Texto amplio\"" },
            { RsvpQuestionType.YesNo, "true", "true" },
            { RsvpQuestionType.SingleChoice, "\"a\"", "\"a\"" },
            { RsvpQuestionType.MultipleChoice, "[\"b\",\"a\"]", "[\"a\",\"b\"]" },
            { RsvpQuestionType.Number, "001.500", "1.500" },
            { RsvpQuestionType.Date, "\"2026-12-31\"", "\"2026-12-31\"" },
            { RsvpQuestionType.InformationalConsent, "true", "true" }
        };

    public static TheoryData<RsvpQuestionType, string, string> InvalidValues =>
        new()
        {
            { RsvpQuestionType.ShortText, "false", "invalid_value_type" },
            { RsvpQuestionType.LongText, "{}", "invalid_value_type" },
            { RsvpQuestionType.YesNo, "\"true\"", "invalid_value_type" },
            { RsvpQuestionType.SingleChoice, "\"missing\"", "invalid_option" },
            { RsvpQuestionType.MultipleChoice, "[\"a\",\"a\"]", "invalid_option" },
            { RsvpQuestionType.Number, "\"1,5\"", "invalid_value_type" },
            { RsvpQuestionType.Date, "\"31/12/2026\"", "invalid_date" },
            { RsvpQuestionType.InformationalConsent, "\"Acepto\"", "invalid_value_type" }
        };

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void ValidateAndNormalize_Accepts_And_Normalizes_Each_Type(
        RsvpQuestionType type,
        string raw,
        string expected)
    {
        var question = CreateQuestion(
            "q1",
            type,
            required: true,
            sensitive: type == RsvpQuestionType.InformationalConsent);

        var result = Validate(
            [question],
            [Answer("q1", null, raw)],
            consent: type == RsvpQuestionType.InformationalConsent
                ? """{"consentGranted":true}"""
                : null);

        Assert.Equal(expected, Assert.Single(result.Answers).AnswerValue);
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void ValidateAndNormalize_Rejects_Invalid_Value_For_Each_Type(
        RsvpQuestionType type,
        string raw,
        string code)
    {
        var question = CreateQuestion(
            "q1",
            type,
            required: false,
            sensitive: type == RsvpQuestionType.InformationalConsent);

        AssertCode(
            code,
            () => Validate(
                [question],
                [Answer("q1", null, raw)]));
    }

    [Fact]
    public void ValidateAndNormalize_Distinguishes_Required_From_Optional()
    {
        var required = CreateQuestion(
            "required",
            RsvpQuestionType.ShortText,
            required: true,
            order: 0);
        var optional = CreateQuestion(
            "optional",
            RsvpQuestionType.ShortText,
            required: false,
            order: 1);

        var error = Assert.Throws<RsvpValidationException>(() =>
            Validate([required, optional], []));

        var item = Assert.Single(error.Errors);
        Assert.Equal("required", item.QuestionId);
        Assert.Equal("required_answer_missing", item.Code);
    }

    [Fact]
    public void ValidateAndNormalize_Treats_Empty_Text_As_Missing()
    {
        var required = CreateQuestion(
            "required",
            RsvpQuestionType.ShortText,
            required: true);
        AssertCode(
            "required_answer_missing",
            () => Validate(
                [required],
                [Answer("required", null, "\"   \"")]));

        var optionalText = CreateQuestion(
            "optional-text",
            RsvpQuestionType.LongText);
        var optionalChoice = CreateQuestion(
            "optional-choice",
            RsvpQuestionType.MultipleChoice,
            order: 1);
        var result = Validate(
            [optionalText, optionalChoice],
            [
                Answer("optional-text", null, "\"   \""),
                Answer("optional-choice", null, "[]")
            ]);

        Assert.Empty(result.Answers);
    }

    [Fact]
    public void ValidateAndNormalize_Enforces_Text_And_Number_Limits()
    {
        var text = CreateQuestion(
            "text",
            RsvpQuestionType.ShortText,
            rules: new ValidationRules
            {
                MinLength = 3,
                MaxLength = 4
            });
        var number = CreateQuestion(
            "number",
            RsvpQuestionType.Number,
            order: 1,
            rules: new ValidationRules
            {
                Minimum = 1,
                Maximum = 10,
                IntegerOnly = true
            });

        AssertCode(
            "value_too_short",
            () => Validate(
                [text],
                [Answer("text", null, "\"ab\"")]));
        AssertCode(
            "value_too_long",
            () => Validate(
                [text],
                [Answer("text", null, "\"abcde\"")]));
        AssertCode(
            "value_below_minimum",
            () => Validate(
                [number],
                [Answer("number", null, "0")]));
        AssertCode(
            "value_above_maximum",
            () => Validate(
                [number],
                [Answer("number", null, "11")]));
        AssertCode(
            "invalid_value_type",
            () => Validate(
                [number],
                [Answer("number", null, "1.5")]));
    }

    [Fact]
    public void ValidateAndNormalize_Enforces_Selection_Limits()
    {
        var question = CreateQuestion(
            "multiple",
            RsvpQuestionType.MultipleChoice,
            rules: new ValidationRules
            {
                MinimumSelections = 2,
                MaximumSelections = 2
            });

        AssertCode(
            "too_few_selections",
            () => Validate(
                [question],
                [Answer("multiple", null, "[\"a\"]")]));
        AssertCode(
            "too_many_selections",
            () => Validate(
                [question],
                [Answer(
                    "multiple",
                    null,
                    "[\"a\",\"b\",\"c\"]")]));
    }

    [Fact]
    public void ValidateAndNormalize_Rejects_Unknown_And_Duplicate_Answers()
    {
        var question = CreateQuestion(
            "known",
            RsvpQuestionType.ShortText);

        var error = Assert.Throws<RsvpValidationException>(() =>
            Validate(
                [question],
                [
                    Answer("known", null, "\"uno\""),
                    Answer("known", null, "\"dos\""),
                    Answer("unknown", null, "\"tres\"")
                ]));

        Assert.Contains(
            error.Errors,
            item => item.Code == "duplicate_answer");
        Assert.Contains(
            error.Errors,
            item => item.Code == "unknown_question");
    }

    [Fact]
    public void ValidateAndNormalize_Enforces_Group_Scope()
    {
        var question = CreateQuestion(
            "group",
            RsvpQuestionType.ShortText);

        AssertCode(
            "invalid_scope",
            () => Validate(
                [question],
                [Answer("group", PrimaryGuestId, "\"valor\"")]));
    }

    [Fact]
    public void ValidateAndNormalize_Enforces_Primary_Contact_Scope()
    {
        var question = CreateQuestion(
            "primary",
            RsvpQuestionType.ShortText,
            scope: RsvpQuestionScope.PrimaryContact);

        AssertCode(
            "invalid_scope",
            () => Validate(
                [question],
                [Answer("primary", CompanionGuestId, "\"valor\"")]));

        var result = Validate(
            [question],
            [Answer("primary", PrimaryGuestId, "\"valor\"")]);
        Assert.Equal(
            PrimaryGuestId,
            Assert.Single(result.Answers).GuestId);
    }

    [Fact]
    public void ValidateAndNormalize_Does_Not_Bypass_Required_Primary_Contact()
    {
        var question = CreateQuestion(
            "primary",
            RsvpQuestionType.ShortText,
            required: true,
            scope: RsvpQuestionScope.PrimaryContact);
        var contextWithoutPrimary = new RsvpQuestionEvaluationContext(
            [
                new RsvpQuestionGuestContext(
                    CompanionGuestId,
                    null,
                    "Acompañante",
                    AgeCategory.Adult,
                    GuestType.Other,
                    true,
                    false,
                    GuestAttendanceStatus.Attending)
            ],
            new HashSet<string>(StringComparer.Ordinal));

        var error = Assert.Throws<RsvpValidationException>(() =>
            RsvpQuestionEngine.ValidateAndNormalize(
                [question],
                contextWithoutPrimary,
                [],
                null));

        Assert.Contains(
            error.Errors,
            item => item.Code == "required_answer_missing");
    }

    [Fact]
    public void ValidateAndNormalize_Enforces_Individual_Guest_Scope()
    {
        var question = CreateQuestion(
            "individual",
            RsvpQuestionType.ShortText,
            scope: RsvpQuestionScope.IndividualGuest);

        AssertCode(
            "guest_not_in_group",
            () => Validate(
                [question],
                [Answer("individual", ForeignGuestId, "\"valor\"")]));

        var result = Validate(
            [question],
            [
                Answer("individual", PrimaryGuestId, "\"uno\""),
                Answer("individual", CompanionGuestId, "\"dos\"")
            ]);
        Assert.Equal(2, result.Answers.Count);
    }

    [Fact]
    public void ValidateAndNormalize_Hidden_Question_Is_Not_Required()
    {
        var question = CreateQuestion(
            "hidden",
            RsvpQuestionType.ShortText,
            required: true,
            visibility: new VisibilityRule
            {
                ConditionType =
                    RsvpVisibilityConditionType.GroupHasTag,
                ExpectedValue = "VIP"
            });

        var result = Validate([question], []);

        Assert.Empty(result.Answers);
    }

    [Fact]
    public void ValidateAndNormalize_Rejects_Answer_For_Hidden_Question()
    {
        var question = CreateQuestion(
            "hidden",
            RsvpQuestionType.ShortText,
            visibility: new VisibilityRule
            {
                ConditionType =
                    RsvpVisibilityConditionType.GroupHasTag,
                ExpectedValue = "VIP"
            });

        AssertCode(
            "hidden_question_answered",
            () => Validate(
                [question],
                [Answer("hidden", null, "\"valor\"")]));
    }

    [Fact]
    public void ValidateAndNormalize_Evaluates_All_And_Any()
    {
        var all = CreateQuestion(
            "all",
            RsvpQuestionType.ShortText,
            visibility: Composite(
                RsvpVisibilityConditionType.All,
                Attendance("Attending"),
                Tag("Familia")));
        var any = CreateQuestion(
            "any",
            RsvpQuestionType.ShortText,
            order: 1,
            visibility: Composite(
                RsvpVisibilityConditionType.Any,
                Attendance("NotAttending"),
                Tag("Familia")));

        var result = Validate(
            [all, any],
            [
                Answer("all", null, "\"visible\""),
                Answer("any", null, "\"visible\"")
            ],
            groupTags: new HashSet<string>(
                ["Familia"],
                StringComparer.Ordinal));

        Assert.Equal(2, result.Answers.Count);
    }

    [Fact]
    public void ValidateAndNormalize_Evaluates_Previous_Answer()
    {
        var first = CreateQuestion(
            "first",
            RsvpQuestionType.MultipleChoice,
            order: 0);
        var second = CreateQuestion(
            "second",
            RsvpQuestionType.ShortText,
            required: true,
            order: 1,
            visibility: new VisibilityRule
            {
                ConditionType =
                    RsvpVisibilityConditionType.PreviousAnswerContains,
                ReferenceQuestionId = "first",
                ExpectedValue = "b"
            });

        var result = Validate(
            [first, second],
            [
                Answer("first", null, "[\"b\",\"a\"]"),
                Answer("second", null, "\"detalle\"")
            ]);

        Assert.Equal(2, result.Answers.Count);
    }

    [Fact]
    public void ValidateAndNormalize_Requires_Explicit_Consent_For_Sensitive()
    {
        var question = CreateQuestion(
            "allergy",
            RsvpQuestionType.LongText,
            sensitive: true);

        AssertCode(
            "consent_required",
            () => Validate(
                [question],
                [Answer("allergy", null, "\"Nueces\"")]));

        var result = Validate(
            [question],
            [Answer("allergy", null, "\"Nueces\"")],
            consent: """{"consentGranted":true}""");
        Assert.True(result.ContainsSensitiveAnswers);
        Assert.True(Assert.Single(result.Answers).IsSensitive);
    }

    [Fact]
    public void ValidateAndNormalize_Consent_Must_Be_True_When_Required()
    {
        var question = CreateQuestion(
            "consent",
            RsvpQuestionType.InformationalConsent,
            required: true,
            sensitive: true);

        AssertCode(
            "consent_required",
            () => Validate(
                [question],
                [Answer("consent", null, "false")]));
    }

    [Fact]
    public void ValidateAndNormalize_Allows_Optional_Consent_Denial()
    {
        var question = CreateQuestion(
            "consent",
            RsvpQuestionType.InformationalConsent,
            required: false,
            sensitive: true);

        var result = Validate(
            [question],
            [Answer("consent", null, "false")]);

        var answer = Assert.Single(result.Answers);
        Assert.Equal("false", answer.AnswerValue);
        Assert.True(answer.IsSensitive);
        Assert.True(result.ContainsSensitiveAnswers);
    }

    [Fact]
    public void Normalized_Answers_Produce_Semantically_Stable_Fingerprint()
    {
        var formVersionId = Guid.Parse(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var question = CreateQuestion(
            "choices",
            RsvpQuestionType.MultipleChoice);
        var first = Validate(
            [question],
            [Answer("choices", null, "[\"b\",\"a\"]")]);
        var second = Validate(
            [question],
            [Answer("choices", null, "[\"a\",\"b\"]")]);
        var firstRequest = Request(
            formVersionId,
            first.Answers);
        var secondRequest = Request(
            formVersionId,
            second.Answers);

        Assert.Equal(
            RsvpRequestFingerprint.Compute(firstRequest, "public"),
            RsvpRequestFingerprint.Compute(secondRequest, "public"));
    }

    private static RsvpQuestionValidationResult Validate(
        IReadOnlyList<RsvpQuestion> questions,
        IReadOnlyList<RsvpSubmissionAnswerRequest> answers,
        string? consent = null,
        IReadOnlySet<string>? groupTags = null) =>
        RsvpQuestionEngine.ValidateAndNormalize(
            questions,
            new RsvpQuestionEvaluationContext(
                [
                    new RsvpQuestionGuestContext(
                        PrimaryGuestId,
                        PrimaryGuestId,
                        "Principal",
                        AgeCategory.Adult,
                        GuestType.Family,
                        false,
                        true,
                        GuestAttendanceStatus.Attending),
                    new RsvpQuestionGuestContext(
                        CompanionGuestId,
                        null,
                        "Acompañante",
                        AgeCategory.Child,
                        GuestType.Other,
                        true,
                        false,
                        GuestAttendanceStatus.Attending)
                ],
                groupTags ?? new HashSet<string>(StringComparer.Ordinal)),
            answers,
            consent);

    private static void AssertCode(
        string code,
        Action action)
    {
        var error = Assert.Throws<RsvpValidationException>(action);
        Assert.Contains(error.Errors, item => item.Code == code);
    }

    private static RsvpQuestion CreateQuestion(
        string id,
        RsvpQuestionType type,
        bool required = false,
        bool sensitive = false,
        int order = 0,
        RsvpQuestionScope scope = RsvpQuestionScope.InvitationGroup,
        VisibilityRule? visibility = null,
        ValidationRules? rules = null) =>
        new()
        {
            Id = id,
            QuestionType = type,
            Scope = scope,
            Category = sensitive
                ? RsvpQuestionCategory.Dietary
                : RsvpQuestionCategory.General,
            Label = $"Pregunta {id}",
            IsRequired = required,
            IsSensitive = sensitive,
            IsActive = true,
            SortOrder = order,
            Options = type is
                RsvpQuestionType.SingleChoice
                or RsvpQuestionType.MultipleChoice
                    ?
                    [
                        Option("a", "Opción A", 0),
                        Option("b", "Opción B", 1),
                        Option("c", "Opción C", 2)
                    ]
                    : [],
            VisibilityRule = visibility ?? VisibilityRule.Always(),
            ValidationRules = rules ?? new ValidationRules()
        };

    private static RsvpQuestionOption Option(
        string key,
        string label,
        int order) =>
        new()
        {
            Key = key,
            Label = label,
            IsActive = true,
            SortOrder = order
        };

    private static RsvpSubmissionAnswerRequest Answer(
        string questionId,
        Guid? guestId,
        string answerValue) =>
        new(questionId, guestId, answerValue, null);

    private static VisibilityRule Composite(
        RsvpVisibilityConditionType type,
        params VisibilityRule[] conditions) =>
        new()
        {
            ConditionType = type,
            Conditions = conditions.ToList()
        };

    private static VisibilityRule Attendance(string expected) =>
        new()
        {
            ConditionType =
                RsvpVisibilityConditionType.AttendanceStatusEquals,
            ExpectedValue = expected
        };

    private static VisibilityRule Tag(string expected) =>
        new()
        {
            ConditionType =
                RsvpVisibilityConditionType.GroupHasTag,
            ExpectedValue = expected
        };

    private static RsvpSubmissionRequest Request(
        Guid formVersionId,
        IReadOnlyList<NormalizedRsvpAnswer> answers) =>
        new(
            0,
            RsvpOverallStatus.Confirmed,
            "Contacto",
            null,
            null,
            [],
            answers.Select(answer =>
                    new RsvpSubmissionAnswerRequest(
                        answer.QuestionId,
                        answer.GuestId,
                        answer.AnswerValue,
                        answer.DisplayValue))
                .ToList(),
            null,
            formVersionId);
}
