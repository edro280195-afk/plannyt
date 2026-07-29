namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class RsvpSubmissionAnswer
{
    private RsvpSubmissionAnswer() { }

    public Guid Id { get; private set; }
    public Guid RsvpSubmissionId { get; private set; }
    public string QuestionId { get; private set; } = string.Empty;
    public Guid? GuestId { get; private set; }
    public string AnswerValue { get; private set; } = string.Empty;
    public string? DisplayValueSnapshot { get; private set; }
    public string QuestionLabelSnapshot { get; private set; } = string.Empty;
    public RsvpQuestionType QuestionTypeSnapshot { get; private set; }
    public string OptionLabelsSnapshot { get; private set; } = "[]";
    public string? GuestDisplayNameSnapshot { get; private set; }
    public bool IsSensitive { get; private set; }

    public static RsvpSubmissionAnswer Create(
        Guid rsvpSubmissionId,
        string questionId,
        Guid? guestId,
        string answerValue,
        string? displayValueSnapshot,
        string questionLabelSnapshot,
        RsvpQuestionType questionTypeSnapshot,
        string optionLabelsSnapshot,
        string? guestDisplayNameSnapshot,
        bool isSensitive)
    {
        return new RsvpSubmissionAnswer
        {
            Id = Guid.NewGuid(),
            RsvpSubmissionId = rsvpSubmissionId,
            QuestionId = questionId,
            GuestId = guestId,
            AnswerValue = answerValue,
            DisplayValueSnapshot = displayValueSnapshot,
            QuestionLabelSnapshot = questionLabelSnapshot,
            QuestionTypeSnapshot = questionTypeSnapshot,
            OptionLabelsSnapshot = optionLabelsSnapshot,
            GuestDisplayNameSnapshot = guestDisplayNameSnapshot,
            IsSensitive = isSensitive
        };
    }
}
