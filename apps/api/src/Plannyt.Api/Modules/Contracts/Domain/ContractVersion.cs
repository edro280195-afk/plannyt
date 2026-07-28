using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Contracts.Domain;

public sealed class ContractVersion : ITenantEntity
{
    private ContractVersion()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ContractId { get; private set; }

    public int VersionNumber { get; private set; }

    public Guid? TemplateId { get; private set; }

    public Guid? SourceProposalVersionId { get; private set; }

    public string RenderedContent { get; private set; } = string.Empty;

    public string? DocumentStorageKey { get; private set; }

    public string? DocumentFileName { get; private set; }

    public string? DocumentMimeType { get; private set; }

    public long? DocumentSizeBytes { get; private set; }

    public string? DocumentSha256 { get; private set; }

    public string ConsentText { get; private set; } = string.Empty;

    public DateTimeOffset? ValidUntil { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? SupersededAt { get; private set; }

    public static ContractVersion CreateDraft(
        Guid organizationId,
        Guid contractId,
        int versionNumber,
        Guid? templateId,
        Guid? sourceProposalVersionId,
        string renderedContent,
        string consentText,
        DateTimeOffset? validUntil,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ContractId = contractId,
            VersionNumber = versionNumber,
            TemplateId = templateId,
            SourceProposalVersionId = sourceProposalVersionId,
            RenderedContent = renderedContent,
            ConsentText = consentText,
            ValidUntil = validUntil,
            CreatedBy = createdBy,
            CreatedAt = now
        };

    public void UpdateDraft(
        Guid? templateId,
        string renderedContent,
        string consentText,
        DateTimeOffset? validUntil)
    {
        EnsureDraft();
        TemplateId = templateId;
        RenderedContent = renderedContent;
        ConsentText = consentText;
        ValidUntil = validUntil;
    }

    public void Publish(
        string storageKey,
        string fileName,
        long sizeBytes,
        string sha256,
        DateTimeOffset now)
    {
        EnsureDraft();
        if (sizeBytes <= 0 || sha256.Length != 64)
        {
            throw new DomainRuleException(
                "El documento publicado no tiene una huella válida.");
        }

        DocumentStorageKey = storageKey;
        DocumentFileName = fileName;
        DocumentMimeType = "application/pdf";
        DocumentSizeBytes = sizeBytes;
        DocumentSha256 = sha256;
        PublishedAt = now;
    }

    public void Supersede(DateTimeOffset now)
    {
        if (PublishedAt is null)
        {
            throw new DomainRuleException(
                "Solo una versión publicada puede sustituirse.");
        }

        SupersededAt ??= now;
    }

    public bool IsAvailableForSigning(DateTimeOffset now) =>
        PublishedAt is not null
        && SupersededAt is null
        && (ValidUntil is null || ValidUntil >= now);

    private void EnsureDraft()
    {
        if (PublishedAt is not null)
        {
            throw new DomainRuleException(
                "Una versión publicada es inmutable.");
        }
    }
}
