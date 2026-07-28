using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Events.Domain;

namespace Plannyt.Api.UnitTests.Events;

public sealed class EventStatusTransitionServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ChangeStatus_FromPreliminaryToConfirmed_UpdatesEventAndReturnsHistory()
    {
        var eventEntity = CreateEvent();
        var service = new EventStatusTransitionService();

        var history = service.ChangeStatus(
            eventEntity,
            EventStatus.Confirmed,
            ActorId,
            Now,
            "Fecha confirmada");

        Assert.Equal(EventStatus.Confirmed, eventEntity.Status);
        Assert.Equal(EventStatus.Preliminary, history.PreviousStatus);
        Assert.Equal(EventStatus.Confirmed, history.NewStatus);
        Assert.Equal(ActorId, history.ChangedBy);
        Assert.Equal(Now, history.ChangedAt);
    }

    [Fact]
    public void ChangeStatus_FromPreliminaryToPlanning_RejectsInvalidTransition()
    {
        var eventEntity = CreateEvent();
        var service = new EventStatusTransitionService();

        var exception = Assert.Throws<DomainRuleException>(() =>
            service.ChangeStatus(
                eventEntity,
                EventStatus.Planning,
                ActorId,
                Now));

        Assert.Contains("Preliminary", exception.Message, StringComparison.Ordinal);
        Assert.Equal(EventStatus.Preliminary, eventEntity.Status);
    }

    [Fact]
    public void ChangeStatus_WhenSuspended_CanReturnToPreviousActiveStatus()
    {
        var eventEntity = CreateEvent();
        var service = new EventStatusTransitionService();
        service.ChangeStatus(eventEntity, EventStatus.Confirmed, ActorId, Now);
        service.ChangeStatus(
            eventEntity,
            EventStatus.Suspended,
            ActorId,
            Now.AddMinutes(1));

        service.ChangeStatus(
            eventEntity,
            EventStatus.Confirmed,
            ActorId,
            Now.AddMinutes(2));

        Assert.Equal(EventStatus.Confirmed, eventEntity.Status);
        Assert.Null(eventEntity.StatusBeforeSuspension);
    }

    [Fact]
    public void ChangeStatus_ToArchived_SetsArchivedAt()
    {
        var eventEntity = CreateEvent();
        var service = new EventStatusTransitionService();

        service.ChangeStatus(
            eventEntity,
            EventStatus.Archived,
            ActorId,
            Now);

        Assert.Equal(EventStatus.Archived, eventEntity.Status);
        Assert.Equal(Now, eventEntity.ArchivedAt);
    }

    [Fact]
    public void ChangeStatus_FromClosedToPlanning_RequiresExceptionalAuthorization()
    {
        var eventEntity = CreateEvent();
        var service = new EventStatusTransitionService();
        service.ChangeStatus(eventEntity, EventStatus.Confirmed, ActorId, Now);
        service.ChangeStatus(eventEntity, EventStatus.Planning, ActorId, Now.AddMinutes(1));
        service.ChangeStatus(eventEntity, EventStatus.Closed, ActorId, Now.AddMinutes(2));

        Assert.Throws<DomainRuleException>(() =>
            service.ChangeStatus(
                eventEntity,
                EventStatus.Planning,
                ActorId,
                Now.AddMinutes(3)));

        service.ChangeStatus(
            eventEntity,
            EventStatus.Planning,
            ActorId,
            Now.AddMinutes(4),
            "Reapertura aprobada",
            allowExceptionalTransition: true);

        Assert.Equal(EventStatus.Planning, eventEntity.Status);
    }

    [Fact]
    public void ChangeStatus_FromArchived_RejectsNormalChanges()
    {
        var eventEntity = CreateEvent();
        var service = new EventStatusTransitionService();
        service.ChangeStatus(eventEntity, EventStatus.Archived, ActorId, Now);

        Assert.Throws<DomainRuleException>(() =>
            service.ChangeStatus(
                eventEntity,
                EventStatus.Preliminary,
                ActorId,
                Now.AddMinutes(1),
                allowExceptionalTransition: true));
    }

    private static Event CreateEvent() =>
        Event.Create(
            OrganizationId,
            "Evento de prueba",
            "Wedding",
            Now.AddMonths(3),
            Now.AddMonths(3).AddHours(8),
            "America/Matamoros",
            "Matamoros",
            "MX",
            "Información compartida",
            120,
            ActorId,
            Now);
}
