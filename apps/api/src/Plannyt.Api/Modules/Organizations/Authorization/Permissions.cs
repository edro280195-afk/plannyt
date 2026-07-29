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

    public const string ContractTemplatesView = "contract-templates.view";
    public const string ContractTemplatesManage = "contract-templates.manage";

    public const string ContractsView = "contracts.view";
    public const string ContractsCreate = "contracts.create";
    public const string ContractsUpdateDraft = "contracts.update-draft";
    public const string ContractsPublish = "contracts.publish";
    public const string ContractsSend = "contracts.send";
    public const string ContractsCancel = "contracts.cancel";
    public const string ContractsUploadExternal = "contracts.upload-external";
    public const string ContractsValidateExternal = "contracts.validate-external";
    public const string ContractsViewInternal = "contracts.view-internal";

    public const string SignaturesView = "signatures.view";
    public const string SignaturesManageSigners = "signatures.manage-signers";
    public const string SignaturesCreateRequest = "signatures.create-request";
    public const string SignaturesRevokeRequest = "signatures.revoke-request";
    public const string SignaturesCountersign = "signatures.countersign";
    public const string SignaturesViewEvidence = "signatures.view-evidence";

    public const string PaymentPlansView = "payment-plans.view";
    public const string PaymentPlansCreate = "payment-plans.create";
    public const string PaymentPlansUpdateDraft = "payment-plans.update-draft";
    public const string PaymentPlansActivate = "payment-plans.activate";
    public const string PaymentPlansCancel = "payment-plans.cancel";

    public const string PaymentsView = "payments.view";
    public const string PaymentsCreate = "payments.create";
    public const string PaymentsApprove = "payments.approve";
    public const string PaymentsReject = "payments.reject";
    public const string PaymentsCancel = "payments.cancel";
    public const string PaymentsRefund = "payments.refund";
    public const string PaymentsViewInternal = "payments.view-internal";

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
    public const string EventsConfirm = "events.confirm";

    public const string ParticipantsView = "participants.view";
    public const string ParticipantsManage = "participants.manage";

    public const string GuestsView = "guests.view";
    public const string GuestsCreate = "guests.create";
    public const string GuestsUpdate = "guests.update";
    public const string GuestsArchive = "guests.archive";
    public const string GuestsImport = "guests.import";
    public const string GuestsExport = "guests.export";
    public const string GuestsViewPrivate = "guests.view-private";
    public const string GuestsManageTags = "guests.manage-tags";

    public const string InvitationGroupsView = "invitation-groups.view";
    public const string InvitationGroupsCreate = "invitation-groups.create";
    public const string InvitationGroupsUpdate = "invitation-groups.update";
    public const string InvitationGroupsArchive = "invitation-groups.archive";
    public const string InvitationGroupsManageCapacity =
        "invitation-groups.manage-capacity";
    public const string InvitationGroupsViewPrivate =
        "invitation-groups.view-private";

    public const string InvitationDesignsView = "invitation-designs.view";
    public const string InvitationDesignsCreate = "invitation-designs.create";
    public const string InvitationDesignsUpdateDraft =
        "invitation-designs.update-draft";
    public const string InvitationDesignsSubmitReview =
        "invitation-designs.submit-review";
    public const string InvitationDesignsApprove = "invitation-designs.approve";
    public const string InvitationDesignsPublish = "invitation-designs.publish";
    public const string InvitationDesignsPublishTesting =
        "invitation-designs.publish-testing";
    public const string InvitationDesignsArchive = "invitation-designs.archive";
    public const string InvitationDesignsManageTemplates =
        "invitation-designs.manage-templates";

    public const string GuestLinksView = "guest-links.view";
    public const string GuestLinksGenerate = "guest-links.generate";
    public const string GuestLinksRegenerate = "guest-links.regenerate";
    public const string GuestLinksRevoke = "guest-links.revoke";
    public const string GuestLinksMarkShared = "guest-links.mark-shared";

    public const string DocumentsViewShared = "documents.view-shared";
    public const string DocumentsUploadShared = "documents.upload-shared";
    public const string DocumentsViewInternal = "documents.view-internal";
    public const string DocumentsUploadInternal = "documents.upload-internal";
    public const string DocumentsDelete = "documents.delete";

    public const string AuditView = "audit.view";

    // RSVP Settings
    public const string RsvpSettingsView = "rsvp-settings.view";
    public const string RsvpSettingsManage = "rsvp-settings.manage";
    public const string RsvpSettingsPublish = "rsvp-settings.publish";
    public const string RsvpSettingsOpenClose = "rsvp-settings.open-close";

    // RSVP Forms
    public const string RsvpFormsView = "rsvp-forms.view";
    public const string RsvpFormsCreate = "rsvp-forms.create";
    public const string RsvpFormsUpdateDraft = "rsvp-forms.update-draft";
    public const string RsvpFormsSubmitReview = "rsvp-forms.submit-review";
    public const string RsvpFormsApprove = "rsvp-forms.approve";
    public const string RsvpFormsPublish = "rsvp-forms.publish";

    // RSVP Responses
    public const string RsvpResponsesView = "rsvp-responses.view";
    public const string RsvpResponsesCreateManual = "rsvp-responses.create-manual";
    public const string RsvpResponsesCorrect = "rsvp-responses.correct";
    public const string RsvpResponsesReopen = "rsvp-responses.reopen";
    public const string RsvpResponsesExport = "rsvp-responses.export";
    public const string RsvpResponsesViewHistory = "rsvp-responses.view-history";

    // Event Menus
    public const string EventMenusView = "event-menus.view";
    public const string EventMenusManage = "event-menus.manage";
    public const string EventMenusExport = "event-menus.export";

    // Guest Travel
    public const string GuestTravelView = "guest-travel.view";
    public const string GuestTravelManage = "guest-travel.manage";
    public const string GuestTravelExport = "guest-travel.export";

    // Guest Sensitive Data
    public const string GuestSensitiveDataView = "guest-sensitive-data.view";
    public const string GuestSensitiveDataManage = "guest-sensitive-data.manage";
    public const string GuestSensitiveDataExport = "guest-sensitive-data.export";

    // Guest Reminders
    public const string GuestRemindersView = "guest-reminders.view";
    public const string GuestRemindersManage = "guest-reminders.manage";
    public const string GuestRemindersMarkSent = "guest-reminders.mark-sent";

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
        ContractTemplatesView,
        ContractTemplatesManage,
        ContractsView,
        ContractsCreate,
        ContractsUpdateDraft,
        ContractsPublish,
        ContractsSend,
        ContractsCancel,
        ContractsUploadExternal,
        ContractsValidateExternal,
        ContractsViewInternal,
        SignaturesView,
        SignaturesManageSigners,
        SignaturesCreateRequest,
        SignaturesRevokeRequest,
        SignaturesCountersign,
        SignaturesViewEvidence,
        PaymentPlansView,
        PaymentPlansCreate,
        PaymentPlansUpdateDraft,
        PaymentPlansActivate,
        PaymentPlansCancel,
        PaymentsView,
        PaymentsCreate,
        PaymentsApprove,
        PaymentsReject,
        PaymentsCancel,
        PaymentsRefund,
        PaymentsViewInternal,
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
        EventsConfirm,
        ParticipantsView,
        ParticipantsManage,
        GuestsView,
        GuestsCreate,
        GuestsUpdate,
        GuestsArchive,
        GuestsImport,
        GuestsExport,
        GuestsViewPrivate,
        GuestsManageTags,
        InvitationGroupsView,
        InvitationGroupsCreate,
        InvitationGroupsUpdate,
        InvitationGroupsArchive,
        InvitationGroupsManageCapacity,
        InvitationGroupsViewPrivate,
        InvitationDesignsView,
        InvitationDesignsCreate,
        InvitationDesignsUpdateDraft,
        InvitationDesignsSubmitReview,
        InvitationDesignsApprove,
        InvitationDesignsPublish,
        InvitationDesignsPublishTesting,
        InvitationDesignsArchive,
        InvitationDesignsManageTemplates,
        GuestLinksView,
        GuestLinksGenerate,
        GuestLinksRegenerate,
        GuestLinksRevoke,
        GuestLinksMarkShared,
        DocumentsViewShared,
        DocumentsUploadShared,
        DocumentsViewInternal,
        DocumentsUploadInternal,
        DocumentsDelete,
        RsvpSettingsView,
        RsvpSettingsManage,
        RsvpSettingsPublish,
        RsvpSettingsOpenClose,
        RsvpFormsView,
        RsvpFormsCreate,
        RsvpFormsUpdateDraft,
        RsvpFormsSubmitReview,
        RsvpFormsApprove,
        RsvpFormsPublish,
        RsvpResponsesView,
        RsvpResponsesCreateManual,
        RsvpResponsesCorrect,
        RsvpResponsesReopen,
        RsvpResponsesExport,
        RsvpResponsesViewHistory,
        EventMenusView,
        EventMenusManage,
        EventMenusExport,
        GuestTravelView,
        GuestTravelManage,
        GuestTravelExport,
        GuestSensitiveDataView,
        GuestSensitiveDataManage,
        GuestSensitiveDataExport,
        GuestRemindersView,
        GuestRemindersManage,
        GuestRemindersMarkSent,
        AuditView
    ], StringComparer.Ordinal);
}
