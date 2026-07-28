using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Catalog.Domain;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.UnitTests.Proposals;

public sealed class ProposalLifecycleTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PublishAndSend_AdvancesVersionAndStatus()
    {
        var proposal = CreateProposal();

        proposal.RecordPublishedVersion(1, Now.AddMinutes(1));
        proposal.MarkSent(Now.AddMinutes(2));
        proposal.MarkViewed(Now.AddMinutes(3));

        Assert.Equal(1, proposal.CurrentVersionNumber);
        Assert.Equal(ProposalStatus.Viewed, proposal.Status);
    }

    [Fact]
    public void AcceptedProposal_CannotBeEditedOrCancelled()
    {
        var proposal = CreateProposal();
        var versionId = Guid.NewGuid();
        proposal.RecordPublishedVersion(1, Now);
        proposal.MarkSent(Now);
        proposal.Accept(versionId, Now.AddHours(1));

        Assert.Equal(versionId, proposal.AcceptedVersionId);
        Assert.Throws<DomainRuleException>(() => proposal.EnsureDraftEditable());
        Assert.Throws<DomainRuleException>(() => proposal.Cancel(Now.AddHours(2)));
    }

    [Fact]
    public void ExpiredProposal_CannotBeAccepted()
    {
        var proposal = CreateProposal(validUntil: Now.AddMinutes(5));
        proposal.RecordPublishedVersion(1, Now);
        proposal.MarkSent(Now);

        Assert.Throws<DomainRuleException>(() =>
            proposal.Accept(Guid.NewGuid(), Now.AddMinutes(6)));
        Assert.Equal(ProposalStatus.Expired, proposal.Status);
    }

    [Fact]
    public void RequestedChanges_StartsNewRevisionWithoutChangingVersionNumber()
    {
        var proposal = CreateProposal();
        proposal.RecordPublishedVersion(1, Now);
        proposal.MarkSent(Now);
        proposal.RequestChanges(Now.AddMinutes(1));

        proposal.StartRevision(Now.AddMinutes(2));

        Assert.Equal(ProposalStatus.Negotiation, proposal.Status);
        Assert.Equal(1, proposal.CurrentVersionNumber);
    }

    [Fact]
    public void ProposalVersion_HoldsCommercialSnapshot()
    {
        var couponId = Guid.NewGuid();
        var version = ProposalVersion.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            3,
            1000m,
            250m,
            120m,
            870m,
            "MXN",
            Now.AddDays(10),
            "Introducción",
            "Términos",
            DiscountType.Percentage,
            10m,
            100m,
            "VERANO",
            couponId,
            150m,
            Guid.NewGuid(),
            Now);

        Assert.Equal(3, version.VersionNumber);
        Assert.Equal("VERANO", version.CouponCode);
        Assert.Equal(couponId, version.CouponId);
        Assert.Equal(870m, version.GrandTotal);
    }

    private static Proposal CreateProposal(DateTimeOffset? validUntil = null) =>
        Proposal.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "P-20260728-ABC123",
            "MXN",
            validUntil ?? Now.AddDays(14),
            "Introducción",
            "Términos",
            "Nota interna",
            DiscountType.None,
            0m,
            null,
            Guid.NewGuid(),
            Now);
}
