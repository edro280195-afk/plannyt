using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Contracts.Domain;

public sealed class Contract : ITenantEntity
{
    private Contract()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid EventId { get; private set; }

    public Guid ClientId { get; private set; }

    public Guid? AcceptedProposalId { get; private set; }

    public Guid? AcceptedProposalVersionId { get; private set; }

    public string ContractNumber { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public ContractSourceType SourceType { get; private set; }

    public ContractStatus Status { get; private set; }

    public int CurrentVersionNumber { get; private set; }

    public decimal ContractGrandTotal { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public static Contract Create(
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        Guid? acceptedProposalId,
        Guid? acceptedProposalVersionId,
        string contractNumber,
        string name,
        ContractSourceType sourceType,
        decimal contractGrandTotal,
        string currencyCode,
        Guid createdBy,
        DateTimeOffset now)
    {
        if (sourceType == ContractSourceType.GeneratedFromProposal
            && (acceptedProposalId is null || acceptedProposalVersionId is null))
        {
            throw new DomainRuleException(
                "Un contrato generado requiere la propuesta y versión aceptadas.");
        }

        if (sourceType != ContractSourceType.GeneratedFromProposal
            && acceptedProposalId is not null)
        {
            throw new DomainRuleException(
                "Solo un contrato generado puede referenciar una propuesta.");
        }

        if (contractGrandTotal < 0m)
        {
            throw new DomainRuleException(
                "El total contractual no puede ser negativo.");
        }

        return new Contract
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            ClientId = clientId,
            AcceptedProposalId = acceptedProposalId,
            AcceptedProposalVersionId = acceptedProposalVersionId,
            ContractNumber = contractNumber,
            Name = name,
            SourceType = sourceType,
            Status = ContractStatus.Draft,
            ContractGrandTotal = contractGrandTotal,
            CurrencyCode = currencyCode,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void RenameDraft(string name, DateTimeOffset now)
    {
        EnsureDraftMutable();
        Name = name;
        UpdatedAt = now;
    }

    public void RecordVersion(int versionNumber, DateTimeOffset now)
    {
        if (Status is ContractStatus.Completed
            or ContractStatus.Cancelled
            or ContractStatus.FullySigned)
        {
            throw new DomainRuleException(
                "El contrato ya no admite nuevas versiones.");
        }

        CurrentVersionNumber = versionNumber;
        Status = ContractStatus.Draft;
        UpdatedAt = now;
    }

    public void MarkPublished(DateTimeOffset now)
    {
        if (Status != ContractStatus.Draft)
        {
            throw new DomainRuleException(
                "Solo un contrato en borrador puede publicarse.");
        }

        Status = ContractStatus.Ready;
        UpdatedAt = now;
    }

    public void MarkSent(DateTimeOffset now)
    {
        if (Status is not (ContractStatus.Ready
            or ContractStatus.Sent
            or ContractStatus.Viewed
            or ContractStatus.PartiallySigned))
        {
            throw new DomainRuleException(
                "El contrato no está listo para solicitar firmas.");
        }

        if (Status == ContractStatus.Ready)
        {
            Status = ContractStatus.Sent;
        }

        UpdatedAt = now;
    }

    public void MarkViewed(DateTimeOffset now)
    {
        if (Status == ContractStatus.Sent)
        {
            Status = ContractStatus.Viewed;
            UpdatedAt = now;
        }
    }

    public void MarkPartiallySigned(DateTimeOffset now)
    {
        EnsureSignable();
        Status = ContractStatus.PartiallySigned;
        UpdatedAt = now;
    }

    public void MarkFullySigned(DateTimeOffset now)
    {
        EnsureSignable();
        Status = ContractStatus.FullySigned;
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        if (SourceType != ContractSourceType.ExternalUpload
            && Status != ContractStatus.FullySigned)
        {
            throw new DomainRuleException(
                "El contrato requiere todas las firmas antes de completarse.");
        }

        if (SourceType == ContractSourceType.ExternalUpload
            && Status is not (ContractStatus.Ready or ContractStatus.FullySigned))
        {
            throw new DomainRuleException(
                "El contrato externo debe publicarse antes de validarse.");
        }

        Status = ContractStatus.Completed;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void Decline(DateTimeOffset now)
    {
        EnsureSignable();
        Status = ContractStatus.Declined;
        UpdatedAt = now;
    }

    public void Cancel(string reason, DateTimeOffset now)
    {
        if (Status is ContractStatus.Completed or ContractStatus.FullySigned)
        {
            throw new DomainRuleException(
                "Un contrato firmado o completado no puede cancelarse.");
        }

        Status = ContractStatus.Cancelled;
        CancelledAt = now;
        CancellationReason = reason;
        UpdatedAt = now;
    }

    public void EnsureDraftMutable()
    {
        if (Status != ContractStatus.Draft)
        {
            throw new DomainRuleException(
                "Solo un contrato en borrador admite cambios.");
        }
    }

    public void EnsureSignable()
    {
        if (Status is ContractStatus.Draft
            or ContractStatus.Completed
            or ContractStatus.Declined
            or ContractStatus.Expired
            or ContractStatus.Cancelled)
        {
            throw new DomainRuleException(
                "El contrato no admite firmas en su estado actual.");
        }
    }
}
