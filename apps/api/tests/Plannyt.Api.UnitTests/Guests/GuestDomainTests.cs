using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Guests.Application;
using Plannyt.Api.Modules.Guests.Domain;

namespace Plannyt.Api.UnitTests.Guests;

public sealed class GuestDomainTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 28, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateGroup_WithZeroCapacity_RejectsRequest()
    {
        Assert.Throws<DomainRuleException>(() => CreateGroup(0));
    }

    [Fact]
    public void UpdateGroup_WhenNamedGuestsExceedCapacity_RequiresOverride()
    {
        var group = CreateGroup(4);

        Assert.Throws<DomainRuleException>(() => group.Update(
            InvitationGroupType.Family,
            "Familia",
            null,
            null,
            null,
            2,
            false,
            0,
            null,
            3,
            false,
            Now.AddMinutes(1)));
    }

    [Fact]
    public void UpdateGroup_WithExplicitOverride_RecordsOverride()
    {
        var group = CreateGroup(4);

        group.Update(
            InvitationGroupType.Family,
            "Familia",
            null,
            null,
            null,
            2,
            false,
            0,
            null,
            3,
            true,
            Now.AddMinutes(1));

        Assert.True(group.CapacityOverrideApplied);
        Assert.Equal(2, group.AllowedGuestCount);
    }

    [Fact]
    public void ArchiveGuest_DisablesAndRemovesPrimaryFlag()
    {
        var guest = EventGuest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Ana",
            "García",
            "ana@example.com",
            "8991234567",
            GuestType.Family,
            AgeCategory.Adult,
            true,
            true,
            false,
            true,
            0,
            null,
            Guid.NewGuid(),
            Now);

        guest.Archive(Now.AddMinutes(1));

        Assert.False(guest.IsActive);
        Assert.False(guest.IsPrimaryContact);
        Assert.NotNull(guest.ArchivedAt);
    }

    [Fact]
    public void ArchiveGroup_ChangesStatusWithoutHardDelete()
    {
        var group = CreateGroup(4);

        group.Archive(Now);

        Assert.Equal(InvitationGroupStatus.Archived, group.Status);
        Assert.Equal(Now, group.ArchivedAt);
    }

    [Theory]
    [InlineData(GuestPlanTier.Community, 100)]
    [InlineData(GuestPlanTier.EventComplete, 300)]
    [InlineData(GuestPlanTier.PlannerPro, 500)]
    public void PlanCatalog_UsesDocumentedGuestLimits(
        GuestPlanTier tier,
        int expectedLimit)
    {
        Assert.Equal(expectedLimit, GuestPlanLimitService.GetLimit(tier));
    }

    private static InvitationGroup CreateGroup(int capacity) =>
        InvitationGroup.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InvitationGroupType.Family,
            "Familia García",
            null,
            null,
            null,
            capacity,
            false,
            0,
            "Manual",
            null,
            Guid.NewGuid(),
            Now);
}
