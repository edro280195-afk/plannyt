using Plannyt.Api.BuildingBlocks.Domain;
using Plannyt.Api.Modules.Catalog.Domain;

namespace Plannyt.Api.Modules.Proposals.Domain;

public sealed class Proposal : ITenantEntity
{
    private Proposal()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid? ProspectId { get; private set; }

    public Guid? ClientId { get; private set; }

    public Guid? EventId { get; private set; }

    public string ProposalNumber { get; private set; } = string.Empty;

    public ProposalStatus Status { get; private set; }

    public int CurrentVersionNumber { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public DateTimeOffset ValidUntil { get; private set; }

    public string? SharedIntroduction { get; private set; }

    public string? SharedTerms { get; private set; }

    public string? InternalNotes { get; private set; }

    public DiscountType GeneralDiscountType { get; private set; }

    public decimal GeneralDiscountValue { get; private set; }

    public Guid? CouponId { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public Guid? AcceptedVersionId { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    public static Proposal Create(
        Guid organizationId,
        Guid? prospectId,
        Guid? clientId,
        Guid? eventId,
        string proposalNumber,
        string currencyCode,
        DateTimeOffset validUntil,
        string? sharedIntroduction,
        string? sharedTerms,
        string? internalNotes,
        DiscountType generalDiscountType,
        decimal generalDiscountValue,
        Guid? couponId,
        Guid createdBy,
        DateTimeOffset now)
    {
        EnsureTarget(prospectId, clientId);
        return new Proposal
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProspectId = prospectId,
            ClientId = clientId,
            EventId = eventId,
            ProposalNumber = proposalNumber,
            Status = ProposalStatus.Draft,
            CurrencyCode = currencyCode,
            ValidUntil = validUntil,
            SharedIntroduction = sharedIntroduction,
            SharedTerms = sharedTerms,
            InternalNotes = internalNotes,
            GeneralDiscountType = generalDiscountType,
            GeneralDiscountValue = generalDiscountValue,
            CouponId = couponId,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(
        Guid? prospectId,
        Guid? clientId,
        Guid? eventId,
        string currencyCode,
        DateTimeOffset validUntil,
        string? sharedIntroduction,
        string? sharedTerms,
        string? internalNotes,
        DiscountType generalDiscountType,
        decimal generalDiscountValue,
        Guid? couponId,
        DateTimeOffset now)
    {
        EnsureDraftEditable();
        EnsureTarget(prospectId, clientId);
        ProspectId = prospectId;
        ClientId = clientId;
        EventId = eventId;
        CurrencyCode = currencyCode;
        ValidUntil = validUntil;
        SharedIntroduction = sharedIntroduction;
        SharedTerms = sharedTerms;
        InternalNotes = internalNotes;
        GeneralDiscountType = generalDiscountType;
        GeneralDiscountValue = generalDiscountValue;
        CouponId = couponId;
        Status = CurrentVersionNumber > 0
            ? ProposalStatus.Negotiation
            : ProposalStatus.Draft;
        UpdatedAt = now;
    }

    public void MarkReady(DateTimeOffset now)
    {
        EnsureDraftEditable();
        Status = ProposalStatus.Ready;
        UpdatedAt = now;
    }

    public void RecordPublishedVersion(int versionNumber, DateTimeOffset now)
    {
        if (Status is ProposalStatus.Accepted or ProposalStatus.Cancelled)
        {
            throw new DomainRuleException(
                "La propuesta ya no admite nuevas versiones.");
        }

        CurrentVersionNumber = versionNumber;
        Status = ProposalStatus.Ready;
        UpdatedAt = now;
    }

    public void MarkSent(DateTimeOffset now)
    {
        if (CurrentVersionNumber < 1
            || Status is ProposalStatus.Accepted or ProposalStatus.Cancelled)
        {
            throw new DomainRuleException(
                "La propuesta no tiene una versión publicable.");
        }

        Status = ProposalStatus.Sent;
        UpdatedAt = now;
    }

    public void MarkViewed(DateTimeOffset now)
    {
        if (Status == ProposalStatus.Sent)
        {
            Status = ProposalStatus.Viewed;
            UpdatedAt = now;
        }
    }

    public void RequestChanges(DateTimeOffset now)
    {
        EnsureSharedActionAllowed(now);
        Status = ProposalStatus.ChangesRequested;
        UpdatedAt = now;
    }

    public void StartRevision(DateTimeOffset now)
    {
        if (Status is not (ProposalStatus.ChangesRequested
            or ProposalStatus.Negotiation
            or ProposalStatus.Rejected))
        {
            throw new DomainRuleException(
                "La propuesta no está lista para una nueva revisión.");
        }

        Status = ProposalStatus.Negotiation;
        UpdatedAt = now;
    }

    public void Accept(Guid versionId, DateTimeOffset now)
    {
        EnsureSharedActionAllowed(now);
        Status = ProposalStatus.Accepted;
        AcceptedAt = now;
        AcceptedVersionId = versionId;
        RejectedAt = null;
        UpdatedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        EnsureSharedActionAllowed(now);
        Status = ProposalStatus.Rejected;
        RejectedAt = now;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == ProposalStatus.Accepted)
        {
            throw new DomainRuleException(
                "Una propuesta aceptada no puede cancelarse.");
        }

        Status = ProposalStatus.Cancelled;
        UpdatedAt = now;
    }

    public void LinkClient(Guid clientId, DateTimeOffset now)
    {
        ClientId = clientId;
        UpdatedAt = now;
    }

    public void LinkEvent(Guid eventId, DateTimeOffset now)
    {
        EventId = eventId;
        UpdatedAt = now;
    }

    public void EnsureDraftEditable()
    {
        if (Status is ProposalStatus.Accepted or ProposalStatus.Cancelled)
        {
            throw new DomainRuleException(
                "La propuesta ya no admite edición.");
        }
    }

    private void EnsureSharedActionAllowed(DateTimeOffset now)
    {
        if (Status is ProposalStatus.Accepted
            or ProposalStatus.Cancelled
            or ProposalStatus.Expired)
        {
            throw new DomainRuleException(
                "La propuesta ya no admite esta acción.");
        }

        if (ValidUntil < now)
        {
            Status = ProposalStatus.Expired;
            UpdatedAt = now;
            throw new DomainRuleException("La propuesta está vencida.");
        }
    }

    private static void EnsureTarget(Guid? prospectId, Guid? clientId)
    {
        if (prospectId is null && clientId is null)
        {
            throw new DomainRuleException(
                "La propuesta requiere un prospecto o cliente.");
        }
    }
}
