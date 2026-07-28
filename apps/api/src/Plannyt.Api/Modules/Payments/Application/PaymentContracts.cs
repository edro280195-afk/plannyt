using Plannyt.Api.Modules.Payments.Domain;

namespace Plannyt.Api.Modules.Payments.Application;

public sealed record PaymentInstallmentRequest(
    int SequenceNumber,
    string Description,
    DateOnly DueDate,
    decimal Amount,
    InstallmentType InstallmentType);

public sealed record CreatePaymentPlanRequest(
    Guid EventId,
    Guid ClientId,
    Guid? ContractId,
    Guid? ProposalVersionId,
    string CurrencyCode,
    decimal TotalAmount,
    IReadOnlyList<PaymentInstallmentRequest> Installments);

public sealed record PaymentInstallmentResponse(
    Guid Id,
    int SequenceNumber,
    string Description,
    DateOnly DueDate,
    decimal Amount,
    decimal ApprovedAmount,
    decimal PendingAmount,
    InstallmentType InstallmentType,
    PaymentInstallmentStatus Status);

public sealed record PaymentPlanResponse(
    Guid Id,
    Guid EventId,
    Guid ClientId,
    Guid? ContractId,
    Guid? ProposalVersionId,
    string CurrencyCode,
    decimal TotalAmount,
    PaymentPlanStatus Status,
    decimal ApprovedAmount,
    decimal PendingAmount,
    IReadOnlyList<PaymentInstallmentResponse> Installments,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreatePaymentRecordRequest(
    Guid EventId,
    Guid ClientId,
    Guid? PaymentPlanId,
    DateOnly PaymentDate,
    decimal Amount,
    string CurrencyCode,
    PaymentMethod Method,
    string? Reference,
    string? NotesShared,
    string? InternalNotes);

public sealed record PaymentAllocationRequest(
    Guid PaymentInstallmentId,
    decimal Amount);

public sealed record PaymentAllocationResponse(
    Guid Id,
    Guid PaymentInstallmentId,
    decimal Amount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReversedAt);

public sealed record PaymentReceiptResponse(
    Guid DocumentId,
    string FileName,
    string MimeType,
    long SizeBytes,
    DateTimeOffset CreatedAt);

public sealed record PaymentRecordResponse(
    Guid Id,
    Guid EventId,
    Guid ClientId,
    Guid? PaymentPlanId,
    DateOnly PaymentDate,
    decimal Amount,
    string CurrencyCode,
    PaymentMethod Method,
    string? Reference,
    PaymentRecordStatus Status,
    string? NotesShared,
    string? InternalNotes,
    bool SubmittedByClient,
    Guid RecordedBy,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    Guid? RejectedBy,
    DateTimeOffset? RejectedAt,
    string? RejectionReason,
    IReadOnlyList<PaymentAllocationResponse> Allocations,
    IReadOnlyList<PaymentReceiptResponse> Receipts,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RejectPaymentRequest(string Reason);

public sealed class UploadPaymentReceiptRequest
{
    public required IFormFile File { get; init; }

    public string? Reference { get; init; }
}

public sealed record PortalCreatePaymentRequest(
    Guid PaymentPlanId,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    string? Reference,
    string? NotesShared);

public sealed record PortalPaymentRecordResponse(
    Guid Id,
    DateOnly PaymentDate,
    decimal Amount,
    string CurrencyCode,
    PaymentMethod Method,
    string? Reference,
    PaymentRecordStatus Status,
    string? NotesShared,
    string? RejectionReason,
    IReadOnlyList<PaymentReceiptResponse> Receipts,
    DateTimeOffset CreatedAt);
