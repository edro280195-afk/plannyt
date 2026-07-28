using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Contracts.Domain;

public sealed class SignatureRequest : ITenantEntity
{
    private SignatureRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ContractId { get; private set; }

    public Guid ContractVersionId { get; private set; }

    public Guid ContractSignerId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? ViewedAt { get; private set; }

    public DateTimeOffset? SignedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static SignatureRequest Create(
        Guid organizationId,
        Guid contractId,
        Guid contractVersionId,
        Guid contractSignerId,
        string tokenHash,
        DateTimeOffset expiresAt,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ContractId = contractId,
            ContractVersionId = contractVersionId,
            ContractSignerId = contractSignerId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy,
            CreatedAt = now
        };

    public bool IsAvailable(DateTimeOffset now) =>
        RevokedAt is null && SignedAt is null && ExpiresAt >= now;

    public void MarkViewed(DateTimeOffset now) => ViewedAt ??= now;

    public void MarkSigned(DateTimeOffset now)
    {
        if (!IsAvailable(now))
        {
            throw new DomainRuleException(
                "La solicitud de firma ya no está disponible.");
        }

        SignedAt = now;
    }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}

public sealed class SignatureEvidence : ITenantEntity
{
    private SignatureEvidence()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ContractId { get; private set; }

    public Guid ContractVersionId { get; private set; }

    public Guid ContractSignerId { get; private set; }

    public Guid? SignatureRequestId { get; private set; }

    public SigningMethod SigningMethod { get; private set; }

    public string DeclaredSignerName { get; private set; } = string.Empty;

    public string DeclaredSignerEmail { get; private set; } = string.Empty;

    public Guid? UserAccountId { get; private set; }

    public Guid? SessionId { get; private set; }

    public string? SignatureImageStorageKey { get; private set; }

    public string DocumentSha256 { get; private set; } = string.Empty;

    public string ConsentText { get; private set; } = string.Empty;

    public DateTimeOffset SignedAt { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public string EvidenceJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; private set; }

    public static SignatureEvidence Create(
        Guid organizationId,
        Guid contractId,
        Guid contractVersionId,
        Guid contractSignerId,
        Guid? signatureRequestId,
        SigningMethod signingMethod,
        string declaredSignerName,
        string declaredSignerEmail,
        Guid? userAccountId,
        Guid? sessionId,
        string? signatureImageStorageKey,
        string documentSha256,
        string consentText,
        DateTimeOffset signedAt,
        string? ipAddress,
        string? userAgent,
        string correlationId,
        string evidenceJson) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ContractId = contractId,
            ContractVersionId = contractVersionId,
            ContractSignerId = contractSignerId,
            SignatureRequestId = signatureRequestId,
            SigningMethod = signingMethod,
            DeclaredSignerName = declaredSignerName,
            DeclaredSignerEmail = declaredSignerEmail,
            UserAccountId = userAccountId,
            SessionId = sessionId,
            SignatureImageStorageKey = signatureImageStorageKey,
            DocumentSha256 = documentSha256,
            ConsentText = consentText,
            SignedAt = signedAt,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CorrelationId = correlationId,
            EvidenceJson = evidenceJson,
            CreatedAt = signedAt
        };
}

public sealed class ContractFinalDocument : ITenantEntity
{
    private ContractFinalDocument()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ContractId { get; private set; }

    public Guid ContractVersionId { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string Sha256 { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static ContractFinalDocument Create(
        Guid organizationId,
        Guid contractId,
        Guid contractVersionId,
        string storageKey,
        string fileName,
        long sizeBytes,
        string sha256,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ContractId = contractId,
            ContractVersionId = contractVersionId,
            StorageKey = storageKey,
            FileName = fileName,
            SizeBytes = sizeBytes,
            Sha256 = sha256,
            CreatedAt = now
        };
}
