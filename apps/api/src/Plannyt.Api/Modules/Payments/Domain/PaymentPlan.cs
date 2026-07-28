using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Payments.Domain;

public sealed class PaymentPlan : ITenantEntity
{
    private PaymentPlan()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid EventId { get; private set; }

    public Guid ClientId { get; private set; }

    public Guid? ContractId { get; private set; }

    public Guid? ProposalVersionId { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public decimal TotalAmount { get; private set; }

    public decimal? ActivatedTotalAmount { get; private set; }

    public PaymentPlanStatus Status { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static PaymentPlan Create(
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        Guid? contractId,
        Guid? proposalVersionId,
        string currencyCode,
        decimal totalAmount,
        Guid createdBy,
        DateTimeOffset now)
    {
        if (totalAmount < 0m)
        {
            throw new DomainRuleException(
                "El total del plan no puede ser negativo.");
        }

        return new PaymentPlan
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            ClientId = clientId,
            ContractId = contractId,
            ProposalVersionId = proposalVersionId,
            CurrencyCode = currencyCode,
            TotalAmount = decimal.Round(
                totalAmount,
                2,
                MidpointRounding.AwayFromZero),
            Status = PaymentPlanStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(decimal totalAmount, DateTimeOffset now)
    {
        EnsureDraft();
        if (totalAmount < 0m)
        {
            throw new DomainRuleException(
                "El total del plan no puede ser negativo.");
        }

        TotalAmount = decimal.Round(
            totalAmount,
            2,
            MidpointRounding.AwayFromZero);
        UpdatedAt = now;
    }

    public void Activate(decimal installmentTotal, DateTimeOffset now)
    {
        EnsureDraft();
        if (decimal.Round(installmentTotal, 2) != TotalAmount)
        {
            throw new DomainRuleException(
                "Las parcialidades activas deben sumar exactamente el total del plan.");
        }

        ActivatedTotalAmount = TotalAmount;
        Status = PaymentPlanStatus.Active;
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        if (Status != PaymentPlanStatus.Active)
        {
            throw new DomainRuleException(
                "Solo un plan activo puede completarse.");
        }

        Status = PaymentPlanStatus.Completed;
        UpdatedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == PaymentPlanStatus.Completed)
        {
            throw new DomainRuleException(
                "Un plan completado no puede cancelarse.");
        }

        Status = PaymentPlanStatus.Cancelled;
        UpdatedAt = now;
    }

    public void EnsureDraft()
    {
        if (Status != PaymentPlanStatus.Draft)
        {
            throw new DomainRuleException(
                "Solo un plan en borrador admite cambios.");
        }
    }
}

public sealed class PaymentInstallment : ITenantEntity
{
    private PaymentInstallment()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid PaymentPlanId { get; private set; }

    public int SequenceNumber { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateOnly DueDate { get; private set; }

    public decimal Amount { get; private set; }

    public InstallmentType InstallmentType { get; private set; }

    public PaymentInstallmentStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static PaymentInstallment Create(
        Guid organizationId,
        Guid paymentPlanId,
        int sequenceNumber,
        string description,
        DateOnly dueDate,
        decimal amount,
        InstallmentType installmentType,
        DateTimeOffset now)
    {
        if (sequenceNumber < 1 || amount < 0m)
        {
            throw new DomainRuleException(
                "La parcialidad contiene valores no válidos.");
        }

        return new PaymentInstallment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            PaymentPlanId = paymentPlanId,
            SequenceNumber = sequenceNumber,
            Description = description,
            DueDate = dueDate,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            InstallmentType = installmentType,
            Status = PaymentInstallmentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(
        int sequenceNumber,
        string description,
        DateOnly dueDate,
        decimal amount,
        InstallmentType installmentType,
        DateTimeOffset now)
    {
        if (Status is PaymentInstallmentStatus.Paid
            or PaymentInstallmentStatus.PartiallyPaid)
        {
            throw new DomainRuleException(
                "Una parcialidad con pagos no admite cambios directos.");
        }

        if (sequenceNumber < 1 || amount < 0m)
        {
            throw new DomainRuleException(
                "La parcialidad contiene valores no válidos.");
        }

        SequenceNumber = sequenceNumber;
        Description = description;
        DueDate = dueDate;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        InstallmentType = installmentType;
        UpdatedAt = now;
    }

    public void RefreshStatus(
        decimal approvedAmount,
        DateOnly today,
        DateTimeOffset now)
    {
        if (Status == PaymentInstallmentStatus.Cancelled)
        {
            return;
        }

        Status = approvedAmount >= Amount
            ? PaymentInstallmentStatus.Paid
            : approvedAmount > 0m
                ? PaymentInstallmentStatus.PartiallyPaid
                : DueDate < today
                    ? PaymentInstallmentStatus.Overdue
                    : PaymentInstallmentStatus.Pending;
        UpdatedAt = now;
    }
}
