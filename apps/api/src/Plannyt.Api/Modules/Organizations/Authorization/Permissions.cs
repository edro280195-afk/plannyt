namespace Plannyt.Api.Modules.Organizations.Authorization;

public static class Permissions
{
    public const string OrganizationView = "organization.view";
    public const string OrganizationUpdate = "organization.update";
    public const string OrganizationMembersView = "organization.members.view";
    public const string OrganizationMembersInvite = "organization.members.invite";
    public const string OrganizationMembersUpdate = "organization.members.update";
    public const string OrganizationMembersRevoke = "organization.members.revoke";

    public const string ClientsView = "clients.view";
    public const string ClientsCreate = "clients.create";
    public const string ClientsUpdate = "clients.update";
    public const string ClientsArchive = "clients.archive";
    public const string ClientsPrivateNotesView = "clients.private-notes.view";
    public const string ClientsPrivateNotesManage = "clients.private-notes.manage";

    public const string ProspectsView = "prospects.view";
    public const string ProspectsCreate = "prospects.create";
    public const string ProspectsUpdate = "prospects.update";
    public const string ProspectsAssign = "prospects.assign";
    public const string ProspectsChangeStatus = "prospects.change-status";
    public const string ProspectsArchive = "prospects.archive";
    public const string ProspectsPrivateNotesView = "prospects.private-notes.view";
    public const string ProspectsPrivateNotesManage = "prospects.private-notes.manage";

    public const string CatalogView = "catalog.view";
    public const string CatalogManage = "catalog.manage";
    public const string PackagesView = "packages.view";
    public const string PackagesManage = "packages.manage";
    public const string CouponsView = "coupons.view";
    public const string CouponsManage = "coupons.manage";

    public const string ProposalsView = "proposals.view";
    public const string ProposalsCreate = "proposals.create";
    public const string ProposalsUpdateDraft = "proposals.update-draft";
    public const string ProposalsPublish = "proposals.publish";
    public const string ProposalsSend = "proposals.send";
    public const string ProposalsCancel = "proposals.cancel";
    public const string ProposalsViewInternal = "proposals.view-internal";
    public const string ProposalsManageComments = "proposals.manage-comments";
    public const string ProposalsConvertClient = "proposals.convert-client";

    public const string EventsView = "events.view";
    public const string EventsCreate = "events.create";
    public const string EventsUpdate = "events.update";
    public const string EventsArchive = "events.archive";
    public const string EventsMembersView = "events.members.view";
    public const string EventsMembersInvite = "events.members.invite";
    public const string EventsMembersUpdate = "events.members.update";
    public const string EventsMembersRevoke = "events.members.revoke";
    public const string EventsInternalDataView = "events.internal-data.view";
    public const string EventsSharedDataView = "events.shared-data.view";

    public const string ParticipantsView = "participants.view";
    public const string ParticipantsManage = "participants.manage";

    public const string DocumentsViewShared = "documents.view-shared";
    public const string DocumentsUploadShared = "documents.upload-shared";
    public const string DocumentsViewInternal = "documents.view-internal";
    public const string DocumentsUploadInternal = "documents.upload-internal";
    public const string DocumentsDelete = "documents.delete";

    public const string AuditView = "audit.view";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(
    [
        OrganizationView,
        OrganizationUpdate,
        OrganizationMembersView,
        OrganizationMembersInvite,
        OrganizationMembersUpdate,
        OrganizationMembersRevoke,
        ClientsView,
        ClientsCreate,
        ClientsUpdate,
        ClientsArchive,
        ClientsPrivateNotesView,
        ClientsPrivateNotesManage,
        ProspectsView,
        ProspectsCreate,
        ProspectsUpdate,
        ProspectsAssign,
        ProspectsChangeStatus,
        ProspectsArchive,
        ProspectsPrivateNotesView,
        ProspectsPrivateNotesManage,
        CatalogView,
        CatalogManage,
        PackagesView,
        PackagesManage,
        CouponsView,
        CouponsManage,
        ProposalsView,
        ProposalsCreate,
        ProposalsUpdateDraft,
        ProposalsPublish,
        ProposalsSend,
        ProposalsCancel,
        ProposalsViewInternal,
        ProposalsManageComments,
        ProposalsConvertClient,
        EventsView,
        EventsCreate,
        EventsUpdate,
        EventsArchive,
        EventsMembersView,
        EventsMembersInvite,
        EventsMembersUpdate,
        EventsMembersRevoke,
        EventsInternalDataView,
        EventsSharedDataView,
        ParticipantsView,
        ParticipantsManage,
        DocumentsViewShared,
        DocumentsUploadShared,
        DocumentsViewInternal,
        DocumentsUploadInternal,
        DocumentsDelete,
        AuditView
    ], StringComparer.Ordinal);
}
