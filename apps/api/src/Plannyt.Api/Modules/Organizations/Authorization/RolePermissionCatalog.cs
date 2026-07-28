using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Organizations.Authorization;

public static class RolePermissionCatalog
{
    private static readonly IReadOnlyDictionary<OrganizationRole, IReadOnlySet<string>>
        OrganizationPermissions =
            new Dictionary<OrganizationRole, IReadOnlySet<string>>
            {
                [OrganizationRole.Owner] = Copy(Permissions.All),
                [OrganizationRole.OrganizationAdmin] = Copy(Permissions.All),
                [OrganizationRole.Planner] = Set(
                    Permissions.OrganizationView,
                    Permissions.OrganizationMembersView,
                    Permissions.ClientsView,
                    Permissions.ClientsCreate,
                    Permissions.ClientsUpdate,
                    Permissions.ClientsArchive,
                    Permissions.ClientsPrivateNotesView,
                    Permissions.ClientsPrivateNotesManage,
                    Permissions.EventsView,
                    Permissions.EventsCreate,
                    Permissions.EventsUpdate,
                    Permissions.EventsArchive,
                    Permissions.EventsMembersView,
                    Permissions.EventsMembersInvite,
                    Permissions.EventsMembersUpdate,
                    Permissions.EventsMembersRevoke,
                    Permissions.EventsInternalDataView,
                    Permissions.EventsSharedDataView,
                    Permissions.ParticipantsView,
                    Permissions.ParticipantsManage,
                    Permissions.DocumentsViewShared,
                    Permissions.DocumentsUploadShared,
                    Permissions.DocumentsViewInternal,
                    Permissions.DocumentsUploadInternal,
                    Permissions.DocumentsDelete,
                    Permissions.AuditView),
                [OrganizationRole.Coordinator] = Set(
                    Permissions.OrganizationView,
                    Permissions.OrganizationMembersView,
                    Permissions.ClientsView,
                    Permissions.ClientsUpdate,
                    Permissions.ClientsPrivateNotesView,
                    Permissions.ClientsPrivateNotesManage,
                    Permissions.EventsView,
                    Permissions.EventsUpdate,
                    Permissions.EventsMembersView,
                    Permissions.EventsMembersInvite,
                    Permissions.EventsInternalDataView,
                    Permissions.EventsSharedDataView,
                    Permissions.ParticipantsView,
                    Permissions.ParticipantsManage,
                    Permissions.DocumentsViewShared,
                    Permissions.DocumentsUploadShared,
                    Permissions.DocumentsViewInternal,
                    Permissions.DocumentsUploadInternal,
                    Permissions.DocumentsDelete),
                [OrganizationRole.Assistant] = Set(
                    Permissions.OrganizationView,
                    Permissions.OrganizationMembersView,
                    Permissions.ClientsView,
                    Permissions.EventsView,
                    Permissions.EventsUpdate,
                    Permissions.EventsMembersView,
                    Permissions.EventsInternalDataView,
                    Permissions.EventsSharedDataView,
                    Permissions.ParticipantsView,
                    Permissions.ParticipantsManage,
                    Permissions.DocumentsViewShared,
                    Permissions.DocumentsUploadShared,
                    Permissions.DocumentsViewInternal,
                    Permissions.DocumentsUploadInternal),
                [OrganizationRole.Commercial] = Set(
                    Permissions.OrganizationView,
                    Permissions.ClientsView,
                    Permissions.ClientsCreate,
                    Permissions.ClientsUpdate,
                    Permissions.ClientsArchive,
                    Permissions.ClientsPrivateNotesView,
                    Permissions.ClientsPrivateNotesManage,
                    Permissions.EventsView,
                    Permissions.EventsCreate,
                    Permissions.EventsSharedDataView,
                    Permissions.ParticipantsView,
                    Permissions.DocumentsViewShared,
                    Permissions.DocumentsUploadShared),
                [OrganizationRole.Finance] = Set(
                    Permissions.OrganizationView,
                    Permissions.ClientsView,
                    Permissions.EventsView,
                    Permissions.EventsInternalDataView,
                    Permissions.EventsSharedDataView,
                    Permissions.ParticipantsView,
                    Permissions.DocumentsViewShared,
                    Permissions.DocumentsViewInternal,
                    Permissions.AuditView)
            };

    private static readonly IReadOnlySet<string> ClientPortalPermissions = Set(
        Permissions.EventsView,
        Permissions.EventsSharedDataView,
        Permissions.ParticipantsView,
        Permissions.DocumentsViewShared);

    public static IReadOnlySet<string> GetFor(OrganizationRole role) =>
        OrganizationPermissions[role];

    public static IReadOnlySet<string> GetFor(EventAccessRole role)
    {
        _ = role;
        return ClientPortalPermissions;
    }

    private static IReadOnlySet<string> Set(params string[] permissions) =>
        new HashSet<string>(permissions, StringComparer.Ordinal);

    private static IReadOnlySet<string> Copy(IEnumerable<string> permissions) =>
        new HashSet<string>(permissions, StringComparer.Ordinal);
}
