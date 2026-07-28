using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Payments.Domain;

public sealed class PaymentRecord : ITenantEntity
{
    private PaymentRecord()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid EventId { get; private set; }

    public Guid ClientId { get; private set; }

    public Guid? PaymentPlanId { get; private set; }

    public DateOnly PaymentDate { get; private set; }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public PaymentMethod Method { get; private set; }

    public string? Reference { get; private set; }

    public PaymentRecordStatus Status { get; private set; }

    public string? NotesShared { get; private set; }

    public string? InternalNotes { get; private set; }

    public Guid RecordedBy { get; private set; }

    public bool SubmittedByClient { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? ApprovedBy { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public Guid? RejectedBy { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public static PaymentRecord Create(
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        Guid? paymentPlanId,
        DateOnly paymentDate,
        decimal amount,
        string currencyCode,
        PaymentMethod method,
        string? reference,
        string? notesShared,
        string? internalNotes,
        Guid recordedBy,
        bool submittedByClient,
        DateTimeOffset now)
    {
        if (amount <= 0m)
        {
            throw new DomainRuleException(
                "El importe del pago debe ser mayor que cero.");
        }

        return new PaymentRecord
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            ClientId = clientId,
            PaymentPlanId = paymentPlanId,
            PaymentDate = paymentDate,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            CurrencyCode = currencyCode,
            Method = method,
            Reference = reference,
            Status = PaymentRecordStatus.PendingReview,
            NotesShared = notesShared,
            InternalNotes = internalNotes,
            RecordedBy = recordedBy,
            SubmittedByClient = submittedByClient,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Approve(Guid approvedBy, DateTimeOffset now)
    {
        if (Status != PaymentRecordStatus.PendingReview)
        {
            throw new DomainRuleException(
                "Solo un pago pendiente puede aprobarse.");
        }

        if (SubmittedByClient && approvedBy == RecordedBy)
        {
            throw new DomainRuleException(
                "El cliente no puede aprobar su propio pago.");
        }

        Status = PaymentRecordStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = now;
        UpdatedAt = now;
    }

    public void Reject(Guid rejectedBy, string reason, DateTimeOffset now)
    {
        if (Status != PaymentRecordStatus.PendingReview)
        {
            throw new DomainRuleException(
                "Solo un pago pendiente puede rechazarse.");
        }

        Status = PaymentRecordStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectedAt = now;
        RejectionReason = reason;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is PaymentRecordStatus.Refunded
            or PaymentRecordStatus.Cancelled)
        {
            throw new DomainRuleException(
                "El pago ya no admite cancelación.");
        }

        Status = PaymentRecordStatus.Cancelled;
        UpdatedAt = now;
    }

    public void Refund(DateTimeOffset now)
    {
        if (Status != PaymentRecordStatus.Approved)
        {
            throw new DomainRuleException(
                "Solo un pago aprobado puede marcarse como reembolsado.");
        }

        Status = PaymentRecordStatus.Refunded;
        UpdatedAt = now;
    }
}

public sealed class PaymentAllocation : ITenantEntity
{
    private PaymentAllocation()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid PaymentRecordId { get; private set; }

    public Guid PaymentInstallmentId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReversedAt { get; private set; }

    public static PaymentAllocation Create(
        Guid organizationId,
        Guid paymentRecordId,
        Guid paymentInstallmentId,
        decimal amount,
        DateTimeOffset now)
    {
        if (amount <= 0m)
        {
            throw new DomainRuleException(
                "La asignación debe ser mayor que cero.");
        }

        return new PaymentAllocation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PaymentRecordId = paymentRecordId,
            PaymentInstallmentId = paymentInstallmentId,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            CreatedAt = now
        };
    }

    public void Reverse(DateTimeOffset now) => ReversedAt ??= now;
}

public sealed class PaymentReceipt : ITenantEntity
{
    private PaymentReceipt()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid PaymentRecordId { get; private set; }

    public Guid DocumentId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static PaymentReceipt Create(
        Guid organizationId,
        Guid paymentRecordId,
        Guid documentId,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PaymentRecordId = paymentRecordId,
            DocumentId = documentId,
            CreatedAt = now
        };
}
