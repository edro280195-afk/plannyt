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
