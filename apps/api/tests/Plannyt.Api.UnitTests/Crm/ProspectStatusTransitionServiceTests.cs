using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Crm.Domain;

namespace Plannyt.Api.UnitTests.Crm;

public sealed class ProspectStatusTransitionServiceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ChangeStatus_ThroughCommercialFlow_PreservesHistoryData()
    {
        var prospect = CreateProspect();
        var service = new ProspectStatusTransitionService();

        var contacted = service.ChangeStatus(
            prospect,
            ProspectStatus.Contacted,
            ActorId,
            Now,
            "Primera llamada");
        var qualified = service.ChangeStatus(
            prospect,
            ProspectStatus.Qualified,
            ActorId,
            Now.AddMinutes(1));

        Assert.Equal(ProspectStatus.Qualified, prospect.Status);
        Assert.Equal(ProspectStatus.New, contacted.PreviousStatus);
        Assert.Equal(ProspectStatus.Contacted, contacted.NewStatus);
        Assert.Equal("Primera llamada", contacted.Reason);
        Assert.Equal(ProspectStatus.Contacted, qualified.PreviousStatus);
        Assert.Equal(ActorId, qualified.ChangedBy);
    }

    [Fact]
    public void ChangeStatus_ToLostWithoutReason_IsRejected()
    {
        var prospect = CreateProspect();
        var service = new ProspectStatusTransitionService();

        var exception = Assert.Throws<DomainRuleException>(() =>
            service.ChangeStatus(
                prospect,
                ProspectStatus.Lost,
                ActorId,
                Now));

        Assert.Contains("motivo", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ProspectStatus.New, prospect.Status);
    }

    [Fact]
    public void ChangeStatus_FromNewDirectlyToWon_IsRejected()
    {
        var prospect = CreateProspect();
        var service = new ProspectStatusTransitionService();

        Assert.Throws<DomainRuleException>(() =>
            service.ChangeStatus(
                prospect,
                ProspectStatus.Won,
                ActorId,
                Now));
    }

    [Fact]
    public void ChangeStatus_FromLostToContacted_ClearsLostReason()
    {
        var prospect = CreateProspect();
        var service = new ProspectStatusTransitionService();
        service.ChangeStatus(
            prospect,
            ProspectStatus.Lost,
            ActorId,
            Now,
            "Pospuso el evento");

        service.ChangeStatus(
            prospect,
            ProspectStatus.Contacted,
            ActorId,
            Now.AddDays(10));

        Assert.Equal(ProspectStatus.Contacted, prospect.Status);
        Assert.Null(prospect.LostReason);
    }

    private static Prospect CreateProspect() =>
        Prospect.Create(
            OrganizationId,
            "María Hernández",
            "María",
            "Hernández",
            null,
            "maria@example.invalid",
            "+528991234567",
            "Instagram",
            "Wedding",
            new DateOnly(2027, 2, 14),
            120,
            150000m,
            "MXN",
            "Matamoros",
            "Prefiere contacto por WhatsApp",
            ActorId,
            Now);
}
