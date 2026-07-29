using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed record RsvpAvailability(
    bool CanRespond,
    bool CanModify,
    bool UsesGroupException);

public static class RsvpAvailabilityEvaluator
{
    public static RsvpAvailability Evaluate(
        EventRsvpSettings? settings,
        RsvpGroupException? groupException,
        bool hasCurrentSubmission,
        DateTimeOffset now)
    {
        if (settings is null)
        {
            return new RsvpAvailability(false, false, false);
        }

        var globalOpen = settings.IsAcceptingResponses(now);
        var exceptionOpen = groupException?.IsValid(now) == true;
        var windowOpen = globalOpen || exceptionOpen;
        if (!windowOpen)
        {
            return new RsvpAvailability(false, false, false);
        }

        if (!hasCurrentSubmission)
        {
            return new RsvpAvailability(
                true,
                settings.CanGuestModifyResponse(now),
                exceptionOpen);
        }

        var canModify = settings.CanGuestModifyResponse(now);
        return new RsvpAvailability(
            canModify,
            canModify,
            exceptionOpen);
    }
}
