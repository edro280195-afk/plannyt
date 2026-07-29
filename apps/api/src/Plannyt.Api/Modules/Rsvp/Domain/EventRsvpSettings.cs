using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class EventRsvpSettings : ITenantEntity
{
    private EventRsvpSettings() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public RsvpSettingsStatus Status { get; private set; }
    public DateTimeOffset? OpensAt { get; private set; }
    public DateTimeOffset? ClosesAt { get; private set; }
    public string TimeZone { get; private set; } = string.Empty;
    public bool AllowChangesAfterSubmission { get; private set; }
    public DateTimeOffset? ChangesCloseAt { get; private set; }
    public bool AllowTentativeResponse { get; private set; }
    public bool AllowGroupDecline { get; private set; }
    public bool RequireResponseForEveryNamedGuest { get; private set; }
    public bool RequireCompanionNames { get; private set; }
    public bool AllowContactInformationUpdate { get; private set; }
    public bool ShowAttendanceSummaryAfterSubmission { get; private set; }
    public string? ConfirmationTitle { get; private set; }
    public string? ConfirmationMessage { get; private set; }
    public string? DeclineMessage { get; private set; }
    public string? ClosedMessage { get; private set; }
    public string? PrivacyNotice { get; private set; }
    public string? SensitiveDataConsentText { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static EventRsvpSettings Create(
        Guid organizationId,
        Guid eventId,
        string timeZone,
        DateTimeOffset now)
    {
        return new EventRsvpSettings
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            Status = RsvpSettingsStatus.Draft,
            TimeZone = timeZone,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(
        DateTimeOffset? opensAt,
        DateTimeOffset? closesAt,
        string timeZone,
        bool allowChangesAfterSubmission,
        DateTimeOffset? changesCloseAt,
        bool allowTentativeResponse,
        bool allowGroupDecline,
        bool requireResponseForEveryNamedGuest,
        bool requireCompanionNames,
        bool allowContactInformationUpdate,
        bool showAttendanceSummaryAfterSubmission,
        string? confirmationTitle,
        string? confirmationMessage,
        string? declineMessage,
        string? closedMessage,
        string? privacyNotice,
        string? sensitiveDataConsentText,
        DateTimeOffset now)
    {
        if (Status != RsvpSettingsStatus.Draft && Status != RsvpSettingsStatus.Ready)
        {
            throw new DomainRuleException("Solo se puede editar en estado Draft o Ready.");
        }

        if (closesAt.HasValue && opensAt.HasValue && closesAt.Value <= opensAt.Value)
        {
            throw new DomainRuleException("La fecha de cierre debe ser posterior a la de apertura.");
        }

        if (changesCloseAt.HasValue && closesAt.HasValue && changesCloseAt.Value > closesAt.Value)
        {
            throw new DomainRuleException("La fecha límite de cambios no puede exceder la de cierre.");
        }

        OpensAt = opensAt;
        ClosesAt = closesAt;
        TimeZone = timeZone;
        AllowChangesAfterSubmission = allowChangesAfterSubmission;
        ChangesCloseAt = changesCloseAt;
        AllowTentativeResponse = allowTentativeResponse;
        AllowGroupDecline = allowGroupDecline;
        RequireResponseForEveryNamedGuest = requireResponseForEveryNamedGuest;
        RequireCompanionNames = requireCompanionNames;
        AllowContactInformationUpdate = allowContactInformationUpdate;
        ShowAttendanceSummaryAfterSubmission = showAttendanceSummaryAfterSubmission;
        ConfirmationTitle = confirmationTitle;
        ConfirmationMessage = confirmationMessage;
        DeclineMessage = declineMessage;
        ClosedMessage = closedMessage;
        PrivacyNotice = privacyNotice;
        SensitiveDataConsentText = sensitiveDataConsentText;
        UpdatedAt = now;
    }

    public void MarkReady(DateTimeOffset now)
    {
        if (Status != RsvpSettingsStatus.Draft)
        {
            throw new DomainRuleException("Solo un borrador puede marcarse como listo.");
        }

        Status = RsvpSettingsStatus.Ready;
        UpdatedAt = now;
    }

    public void Open(DateTimeOffset now)
    {
        if (Status is not (RsvpSettingsStatus.Ready or RsvpSettingsStatus.Closed))
        {
            throw new DomainRuleException("Solo configuración Ready o Closed puede abrirse.");
        }

        Status = RsvpSettingsStatus.Open;
        UpdatedAt = now;
    }

    public void Close(DateTimeOffset now)
    {
        if (Status != RsvpSettingsStatus.Open)
        {
            throw new DomainRuleException("Solo configuración abierta puede cerrarse.");
        }

        Status = RsvpSettingsStatus.Closed;
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        if (Status is not (RsvpSettingsStatus.Open or RsvpSettingsStatus.Ready))
        {
            throw new DomainRuleException("Solo configuración Open o Ready puede suspenderse.");
        }

        Status = RsvpSettingsStatus.Suspended;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        if (Status is RsvpSettingsStatus.Open)
        {
            throw new DomainRuleException("No se puede archivar una configuración abierta.");
        }

        Status = RsvpSettingsStatus.Archived;
        UpdatedAt = now;
    }

    public bool IsAcceptingResponses(DateTimeOffset now)
    {
        if (Status != RsvpSettingsStatus.Open) return false;
        if (OpensAt.HasValue && now < OpensAt.Value) return false;
        if (ClosesAt.HasValue && now > ClosesAt.Value) return false;
        return true;
    }

    public bool CanGuestModifyResponse(DateTimeOffset now)
    {
        if (!AllowChangesAfterSubmission) return false;
        if (ChangesCloseAt.HasValue && now > ChangesCloseAt.Value) return false;
        return true;
    }

    public bool CanGuestSubmitTentative()
    {
        return AllowTentativeResponse;
    }

    public bool CanGuestDecline()
    {
        return AllowGroupDecline;
    }
}
