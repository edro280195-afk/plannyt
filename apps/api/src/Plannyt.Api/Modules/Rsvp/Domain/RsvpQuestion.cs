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
    public bool IsActive { get; init; } = true;
    public int SortOrder { get; init; }
    public List<string> Options { get; init; } = [];
    public VisibilityRule? VisibilityRule { get; init; }
    public ValidationRules? ValidationRules { get; init; }
}

public sealed class VisibilityRule
{
    public string DependsOnQuestionId { get; init; } = string.Empty;
    public string ExpectedValue { get; init; } = string.Empty;
}

public sealed class ValidationRules
{
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public decimal? Minimum { get; init; }
    public decimal? Maximum { get; init; }
    public bool Required { get; init; }
    public List<string>? AllowedOptions { get; init; }
}
