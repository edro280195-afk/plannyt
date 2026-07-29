using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.UnitTests.Organizations;

public sealed class ClientPortalRolePermissionTests
{
    [Theory]
    [InlineData(EventAccessRole.ClientAuthority)]
    [InlineData(EventAccessRole.ClientPrimary)]
    [InlineData(EventAccessRole.ClientCollaborator)]
    [InlineData(EventAccessRole.ClientGuestManager)]
    [InlineData(EventAccessRole.ClientPayer)]
    [InlineData(EventAccessRole.ClientApprover)]
    [InlineData(EventAccessRole.ClientViewer)]
    public void EveryClientRole_ReceivesSharedReadPermissions(
        EventAccessRole role)
    {
        var permissions = RolePermissionCatalog.GetFor(role);

        Assert.Contains(Permissions.EventsView, permissions);
        Assert.Contains(Permissions.EventsSharedDataView, permissions);
        Assert.Contains(Permissions.DocumentsViewShared, permissions);
        Assert.Contains(Permissions.ContractsView, permissions);
        Assert.Contains(Permissions.PaymentsView, permissions);
        Assert.DoesNotContain(
            Permissions.GuestSensitiveDataView,
            permissions);
    }

    [Fact]
    public void Viewer_ReceivesNoPortalMutationPermission()
    {
        var permissions = RolePermissionCatalog.GetFor(
            EventAccessRole.ClientViewer);

        Assert.Equal(13, permissions.Count);
        Assert.DoesNotContain(Permissions.GuestsCreate, permissions);
        Assert.DoesNotContain(
            Permissions.InvitationDesignsApprove,
            permissions);
        Assert.DoesNotContain(Permissions.PaymentsCreate, permissions);
        Assert.DoesNotContain(
            Permissions.RsvpResponsesCorrect,
            permissions);
    }

    [Fact]
    public void GuestManager_CanOperateGuestsButCannotApproveOrPay()
    {
        var permissions = RolePermissionCatalog.GetFor(
            EventAccessRole.ClientGuestManager);

        Assert.Equal(25, permissions.Count);
        Assert.Contains(Permissions.GuestsCreate, permissions);
        Assert.Contains(Permissions.InvitationGroupsCreate, permissions);
        Assert.Contains(Permissions.RsvpResponsesCorrect, permissions);
        Assert.DoesNotContain(
            Permissions.InvitationDesignsUpdateDraft,
            permissions);
        Assert.DoesNotContain(
            Permissions.InvitationDesignsApprove,
            permissions);
        Assert.DoesNotContain(Permissions.PaymentsCreate, permissions);
    }

    [Fact]
    public void Collaborator_CanEditInvitationButCannotApproveOrPay()
    {
        var permissions = RolePermissionCatalog.GetFor(
            EventAccessRole.ClientCollaborator);

        Assert.Equal(27, permissions.Count);
        Assert.Contains(
            Permissions.InvitationDesignsUpdateDraft,
            permissions);
        Assert.Contains(
            Permissions.InvitationDesignsSubmitReview,
            permissions);
        Assert.DoesNotContain(
            Permissions.InvitationDesignsApprove,
            permissions);
        Assert.DoesNotContain(Permissions.PaymentsCreate, permissions);
    }

    [Fact]
    public void Payer_OnlyAddsPaymentMutationToSharedReadPermissions()
    {
        var permissions = RolePermissionCatalog.GetFor(
            EventAccessRole.ClientPayer);

        Assert.Equal(14, permissions.Count);
        Assert.Contains(Permissions.PaymentsCreate, permissions);
        Assert.DoesNotContain(Permissions.GuestsCreate, permissions);
        Assert.DoesNotContain(
            Permissions.InvitationDesignsApprove,
            permissions);
    }

    [Fact]
    public void Approver_OnlyAddsApprovalToSharedReadPermissions()
    {
        var permissions = RolePermissionCatalog.GetFor(
            EventAccessRole.ClientApprover);

        Assert.Equal(14, permissions.Count);
        Assert.Contains(
            Permissions.InvitationDesignsApprove,
            permissions);
        Assert.DoesNotContain(Permissions.GuestsCreate, permissions);
        Assert.DoesNotContain(Permissions.PaymentsCreate, permissions);
    }

    [Theory]
    [InlineData(EventAccessRole.ClientAuthority)]
    [InlineData(EventAccessRole.ClientPrimary)]
    public void AuthorityAndPrimary_RetainTheCompletePortalWorkflow(
        EventAccessRole role)
    {
        var permissions = RolePermissionCatalog.GetFor(role);

        Assert.Equal(29, permissions.Count);
        Assert.Contains(Permissions.GuestsCreate, permissions);
        Assert.Contains(
            Permissions.InvitationDesignsUpdateDraft,
            permissions);
        Assert.Contains(
            Permissions.InvitationDesignsApprove,
            permissions);
        Assert.Contains(Permissions.PaymentsCreate, permissions);
    }
}
