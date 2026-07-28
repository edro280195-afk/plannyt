using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Events.Domain;

public sealed class EventStatusTransitionService
{
    public EventStatusHistory ChangeStatus(
        Event eventEntity,
        EventStatus newStatus,
        Guid changedBy,
        DateTimeOffset changedAt,
        string? reason = null,
        bool allowExceptionalTransition = false)
    {
        var previousStatus = eventEntity.Status;

        if (!IsAllowed(
                previousStatus,
                newStatus,
                eventEntity.StatusBeforeSuspension,
                allowExceptionalTransition))
        {
            throw new DomainRuleException(
                $"La transición de {previousStatus} a {newStatus} no está permitida.");
        }

        eventEntity.ApplyStatus(newStatus, changedAt);

        return EventStatusHistory.Create(
            eventEntity.OrganizationId,
            eventEntity.Id,
            previousStatus,
            newStatus,
            reason,
            changedBy,
            changedAt);
    }

    private static bool IsAllowed(
        EventStatus currentStatus,
        EventStatus newStatus,
        EventStatus? statusBeforeSuspension,
        bool allowExceptionalTransition) =>
        currentStatus switch
        {
            EventStatus.Preliminary =>
                newStatus is EventStatus.Confirmed
                    or EventStatus.Cancelled
                    or EventStatus.Archived,
            EventStatus.Confirmed =>
                newStatus is EventStatus.Planning
                    or EventStatus.Suspended
                    or EventStatus.Cancelled,
            EventStatus.Planning =>
                newStatus is EventStatus.Suspended
                    or EventStatus.Closed
                    or EventStatus.Cancelled,
            EventStatus.Suspended =>
                newStatus == statusBeforeSuspension
                || newStatus is EventStatus.Cancelled or EventStatus.Archived,
            EventStatus.Closed =>
                newStatus == EventStatus.Archived
                || (allowExceptionalTransition && newStatus == EventStatus.Planning),
            EventStatus.Cancelled =>
                newStatus == EventStatus.Archived
                || (allowExceptionalTransition && newStatus == EventStatus.Preliminary),
            EventStatus.Archived => false,
            _ => false
        };
}
