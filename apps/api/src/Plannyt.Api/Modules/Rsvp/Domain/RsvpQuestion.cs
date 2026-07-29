namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class RsvpQuestion
{
    public string Id { get; init; } = string.Empty;
    public RsvpQuestionType QuestionType { get; init; }
    public RsvpQuestionScope Scope { get; init; }
    public RsvpQuestionCategory Category { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? HelpText { get; init; }
    public bool IsRequired { get; init; }
    public bool IsSensitive { get; init; }
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
    public List<RsvpQuestionOption> Options { get; init; } = [];
    public VisibilityRule VisibilityRule { get; init; } =
        VisibilityRule.Always();
    public ValidationRules ValidationRules { get; init; } = new();
}

public sealed class RsvpQuestionOption
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
}

public sealed class VisibilityRule
{
    public RsvpVisibilityConditionType ConditionType { get; init; }
    public string? ReferenceQuestionId { get; init; }
    public string? ExpectedValue { get; init; }
    public List<VisibilityRule> Conditions { get; init; } = [];

    public static VisibilityRule Always() =>
        new() { ConditionType = RsvpVisibilityConditionType.Always };
}

public sealed class ValidationRules
{
    public bool? Required { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public int? MinimumSelections { get; init; }
    public int? MaximumSelections { get; init; }
    public decimal? Minimum { get; init; }
    public decimal? Maximum { get; init; }
    public bool? IntegerOnly { get; init; }
    public DateOnly? MinimumDate { get; init; }
    public DateOnly? MaximumDate { get; init; }
}
