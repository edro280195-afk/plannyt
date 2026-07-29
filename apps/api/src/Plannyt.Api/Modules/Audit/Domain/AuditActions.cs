namespace Plannyt.Api.Modules.Audit.Domain;

public static class AuditActions
{
    public static readonly AuditAction GuestLinkGenerated =
        AuditAction.Define("guest_link.generated");
    public static readonly AuditAction GuestLinkRegenerated =
        AuditAction.Define("guest_link.regenerated");
    public static readonly AuditAction GuestLinkRevoked =
        AuditAction.Define("guest_link.revoked");
    public static readonly AuditAction GuestLinkMarkedShared =
        AuditAction.Define("guest_link.marked_shared");
    public static readonly AuditAction PortalGuestLinkMarkedShared =
        AuditAction.Define("portal.guest_link.marked_shared");

    public static readonly AuditAction RsvpSubmitted =
        AuditAction.Define("rsvp.submitted");
    public static readonly AuditAction RsvpManualCapture =
        AuditAction.Define("rsvp.manual_capture");
    public static readonly AuditAction RsvpSupportCorrected =
        AuditAction.Define("rsvp.support_corrected");
    public static readonly AuditAction RsvpGroupExceptionOpened =
        AuditAction.Define("rsvp.group_exception.opened");
    public static readonly AuditAction RsvpGroupExceptionClosed =
        AuditAction.Define("rsvp.group_exception.closed");
    public static readonly AuditAction RsvpSettingsUpdated =
        AuditAction.Define("rsvp_settings.updated");
    public static readonly AuditAction RsvpSettingsPublished =
        AuditAction.Define("rsvp_settings.published");
    public static readonly AuditAction RsvpSettingsOpened =
        AuditAction.Define("rsvp_settings.opened");
    public static readonly AuditAction RsvpSettingsClosed =
        AuditAction.Define("rsvp_settings.closed");
    public static readonly AuditAction RsvpFormCreated =
        AuditAction.Define("rsvp_form.created");
    public static readonly AuditAction RsvpFormVersionCreated =
        AuditAction.Define("rsvp_form.version_created");
    public static readonly AuditAction RsvpFormDraftCreated =
        AuditAction.Define("rsvp_form.draft_created");
    public static readonly AuditAction RsvpFormSubmittedReview =
        AuditAction.Define("rsvp_form.submitted_review");
    public static readonly AuditAction RsvpFormApproved =
        AuditAction.Define("rsvp_form.approved");
    public static readonly AuditAction RsvpFormPublished =
        AuditAction.Define("rsvp_form.published");

    public static readonly AuditAction GuestSensitiveDataViewed =
        AuditAction.Define("guest_sensitive_data.viewed");
    public static readonly AuditAction GuestSensitiveDataExported =
        AuditAction.Define("guest_sensitive_data.exported");
    public static readonly AuditAction GuestSensitiveDataUpdated =
        AuditAction.Define("guest_sensitive_data.updated");

    public static readonly AuditAction TransportSelectionConfirmed =
        AuditAction.Define("transport.selection.confirmed");
    public static readonly AuditAction TransportSelectionWaitlisted =
        AuditAction.Define("transport.selection.waitlisted");
    public static readonly AuditAction TransportSelectionCancelled =
        AuditAction.Define("transport.selection.cancelled");
    public static readonly AuditAction TransportWaitlistPromoted =
        AuditAction.Define("transport.waitlist.promoted");

    public static readonly AuditAction RsvpProjectionDiagnosed =
        AuditAction.Define("rsvp.projection.diagnosed");
    public static readonly AuditAction RsvpProjectionRepaired =
        AuditAction.Define("rsvp.projection.repaired");

    // Alias histórico conservado para consultas sobre registros previos a Sprint 2B.2.
    public const string LegacyRsvpGroupExceptionOpened =
        "rsvp.group_exception_opened";
}
