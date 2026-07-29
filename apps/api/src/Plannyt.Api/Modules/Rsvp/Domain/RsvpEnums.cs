namespace Plannyt.Api.Modules.Rsvp.Domain;

public enum GuestAttendanceStatus
{
    Pending,
    Attending,
    NotAttending,
    Tentative,
    CancelledAfterConfirmation
}

public enum RsvpSettingsStatus
{
    Draft,
    Ready,
    Open,
    Closed,
    Suspended,
    Archived
}

public enum RsvpFormStatus
{
    Draft,
    InReview,
    ChangesRequested,
    Approved,
    Published,
    Archived
}

public enum RsvpQuestionType
{
    ShortText,
    LongText,
    YesNo,
    SingleChoice,
    MultipleChoice,
    Number,
    Date,
    InformationalConsent
}

public enum RsvpQuestionScope
{
    InvitationGroup,
    IndividualGuest,
    PrimaryContact
}

public enum RsvpQuestionCategory
{
    General,
    Dietary,
    Transportation,
    Accommodation,
    Accessibility,
    Consent,
    Other
}

public enum RsvpSubmissionSource
{
    GuestPrivateLink,
    PlannerManual,
    ClientPortal,
    Imported,
    SupportCorrection
}

public enum RsvpOverallStatus
{
    Confirmed,
    Declined,
    Mixed,
    Tentative,
    Incomplete
}

public enum MenuCategory
{
    AdultMeal,
    ChildMeal,
    TeenMeal,
    Beverage,
    Dessert,
    LateSnack,
    Other
}

public enum TransportDirection
{
    ToCeremony,
    ToReception,
    Return,
    RoundTrip,
    Other
}

public enum TransportSelectionStatus
{
    Requested,
    Confirmed,
    Waitlisted,
    NotNeeded,
    Cancelled
}

public enum AccommodationSelectionStatus
{
    NotNeeded,
    Interested,
    PlanningToBook,
    Booked,
    NeedAssistance
}

public enum ReminderChannel
{
    WhatsAppManual,
    EmailCopy,
    GeneralCopy
}

public enum RsvpGroupExceptionStatus
{
    Active,
    Expired,
    Closed
}
