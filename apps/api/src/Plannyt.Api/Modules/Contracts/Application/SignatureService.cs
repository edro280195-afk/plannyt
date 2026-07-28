using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Contracts.Domain;
using Plannyt.Api.Modules.Contracts.Pdf;
using Plannyt.Api.Modules.Contracts.Security;
using Plannyt.Api.Modules.Documents.Storage;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Contracts.Application;

public sealed class SignatureService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    PortalAccessService portalAccessService,
    ICurrentUser currentUser,
    SignatureTokenService tokenService,
    IContractPdfGenerator pdfGenerator,
    IFileStorage fileStorage,
    IOptions<FrontendOptions> frontendOptions,
    AuditService auditService,
    ContractingReadinessService readinessService,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
{
    private const int MaximumSignatureImageBytes = 1024 * 1024;

    public async Task<SignatureRequestLinkResponse> CreateRequestAsync(
        Guid organizationId,
        Guid contractId,
        Guid signerId,
        CreateSignatureRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.SignaturesCreateRequest,
            contract.EventId,
            cancellationToken);
        contract.EnsureSignable();
        var version = await GetCurrentPublishedVersionAsync(
            contract,
            cancellationToken);
        var signer = await FindSignerAsync(
            organizationId,
            contractId,
            signerId,
            cancellationToken);
        signer.EnsureUnsigned();
        var now = timeProvider.GetUtcNow();
        var expiresAt = request.ExpiresAt ?? now.AddDays(7);
        if (expiresAt <= now || expiresAt > now.AddDays(30))
        {
            throw Validation(
                "expiresAt",
                "La vigencia debe estar entre un instante futuro y 30 días.");
        }

        var currentRequests = await dbContext.SignatureRequests
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ContractSignerId == signerId
                && entity.SignedAt == null
                && entity.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var current in currentRequests)
        {
            current.Revoke(now);
        }

        var token = tokenService.Create();
        var signatureRequest = SignatureRequest.Create(
            organizationId,
            contractId,
            version.Id,
            signerId,
            token.Hash,
            expiresAt,
            access.UserAccountId,
            now);
        dbContext.SignatureRequests.Add(signatureRequest);
        signer.MarkInvited(now);
        contract.MarkSent(now);
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "signature.request_created",
            nameof(SignatureRequest),
            signatureRequest.Id,
            new Dictionary<string, object?>
            {
                ["contractVersionId"] = version.Id,
                ["contractSignerId"] = signer.Id,
                ["expiresAt"] = expiresAt
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SignatureRequestLinkResponse(
            signatureRequest.Id,
            version.Id,
            signer.Id,
            expiresAt,
            $"{frontendOptions.Value.PublicUrl.TrimEnd('/')}/sign/{token.Value}");
    }

    public async Task RevokeRequestAsync(
        Guid organizationId,
        Guid contractId,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.SignaturesRevokeRequest,
            contract.EventId,
            cancellationToken);
        var request = await dbContext.SignatureRequests
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.ContractId == contractId
                    && entity.Id == requestId,
                cancellationToken)
            ?? throw new NotFoundException(
                "No se encontró la solicitud de firma.");
        request.Revoke(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "signature.request_revoked",
            nameof(SignatureRequest),
            request.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PublicContractSignatureResponse> GetPublicAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var context = await ResolvePublicAsync(
            token,
            markViewed: true,
            cancellationToken);
        return await BuildPublicResponseAsync(context, cancellationToken);
    }

    public async Task<ContractFileDownload> DownloadPublicAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var context = await ResolvePublicAsync(
            token,
            markViewed: true,
            cancellationToken);
        return new ContractFileDownload(
            await fileStorage.OpenReadAsync(
                context.Version.DocumentStorageKey
                    ?? throw new InvalidOperationException(
                        "La versión no tiene un documento publicado."),
                cancellationToken),
            "application/pdf",
            context.Version.DocumentFileName ?? "contrato.pdf");
    }

    public async Task<PublicContractSignatureResponse> SignPublicAsync(
        string token,
        SubmitSignatureRequest request,
        CancellationToken cancellationToken)
    {
        var context = await ResolvePublicAsync(
            token,
            markViewed: false,
            cancellationToken);
        if (request.SigningMethod is not (SigningMethod.Drawn or SigningMethod.Typed))
        {
            throw Validation(
                "signingMethod",
                "La firma pública admite firma dibujada o escrita.");
        }

        return await RecordSignatureAsync(
            context.Contract,
            context.Version,
            context.Signer,
            context.Request,
            request,
            null,
            null,
            cancellationToken);
    }

    public async Task DeclinePublicAsync(
        string token,
        DeclineSignatureRequest request,
        CancellationToken cancellationToken)
    {
        var context = await ResolvePublicAsync(
            token,
            markViewed: false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        context.Signer.Decline(now);
        context.Request?.Revoke(now);
        context.Contract.Decline(now);
        auditService.Add(
            context.Contract.OrganizationId,
            context.Contract.EventId,
            null,
            "signature.declined",
            nameof(ContractSigner),
            context.Signer.Id,
            new Dictionary<string, object?>
            {
                ["contractVersionId"] = context.Version.Id,
                ["reason"] = Normalize(request.Reason)
            });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PublicContractSignatureResponse> SignFromPortalAsync(
        Guid contractId,
        Guid signerId,
        SubmitSignatureRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.Contracts
            .SingleOrDefaultAsync(
                entity => entity.Id == contractId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el contrato.");
        await portalAccessService.RequireAsync(
            contract.EventId,
            Permissions.SignaturesView,
            cancellationToken);
        var signer = await FindSignerAsync(
            contract.OrganizationId,
            contractId,
            signerId,
            cancellationToken);
        if (signer.UserAccountId != currentUser.UserAccountId)
        {
            throw new ForbiddenException(
                "La cuenta autenticada no corresponde al firmante.");
        }

        if (request.SigningMethod != SigningMethod.AuthenticatedConfirmation)
        {
            throw Validation(
                "signingMethod",
                "La firma desde el portal usa confirmación autenticada.");
        }

        var version = await GetCurrentPublishedVersionAsync(
            contract,
            cancellationToken);
        return await RecordSignatureAsync(
            contract,
            version,
            signer,
            null,
            request,
            currentUser.UserAccountId,
            currentUser.SessionId,
            cancellationToken);
    }

    public async Task<ContractResponse> SignAsOrganizationAsync(
        Guid organizationId,
        Guid contractId,
        Guid signerId,
        SubmitSignatureRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.SignaturesCountersign,
            contract.EventId,
            cancellationToken);
        var signer = await FindSignerAsync(
            organizationId,
            contractId,
            signerId,
            cancellationToken);
        if (signer.UserAccountId is not null
            && signer.UserAccountId != access.UserAccountId)
        {
            throw new ForbiddenException(
                "La cuenta autenticada no corresponde al firmante.");
        }

        var version = await GetCurrentPublishedVersionAsync(
            contract,
            cancellationToken);
        _ = await RecordSignatureAsync(
            contract,
            version,
            signer,
            null,
            request with
            {
                SigningMethod = SigningMethod.AuthenticatedConfirmation
            },
            access.UserAccountId,
            currentUser.SessionId,
            cancellationToken);
        return await BuildContractResponseAsync(contract, cancellationToken);
    }

    public async Task<ContractResponse> ValidateExternalAsync(
        Guid organizationId,
        Guid contractId,
        ValidateExternalContractRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsValidateExternal,
            contract.EventId,
            cancellationToken);
        if (contract.SourceType != ContractSourceType.ExternalUpload
            || contract.Status != ContractStatus.Ready)
        {
            throw new ConflictException(
                "El contrato externo no está listo para validarse.");
        }

        var version = await GetCurrentPublishedVersionAsync(
            contract,
            cancellationToken);
        var signers = await dbContext.ContractSigners
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ContractId == contractId)
            .OrderBy(entity => entity.SigningOrder)
            .ToListAsync(cancellationToken);
        if (signers.Count == 0)
        {
            throw new ConflictException(
                "Registra al menos un firmante declarado.");
        }

        var signedAt = request.SignedAt;
        if (signedAt > timeProvider.GetUtcNow().AddMinutes(5))
        {
            throw Validation(
                "signedAt",
                "La fecha de firma externa no puede estar en el futuro.");
        }

        var evidence = new List<SignatureEvidence>();
        foreach (var signer in signers)
        {
            signer.Sign(signedAt);
            var item = SignatureEvidence.Create(
                organizationId,
                contractId,
                version.Id,
                signer.Id,
                null,
                SigningMethod.External,
                signer.Name,
                signer.Email,
                access.UserAccountId,
                currentUser.SessionId,
                null,
                version.DocumentSha256
                    ?? throw new InvalidOperationException(
                        "La versión no tiene hash."),
                version.ConsentText,
                signedAt,
                ClientIp(),
                SafeUserAgent(),
                TraceIdentifier(),
                JsonSerializer.Serialize(new
                {
                    source = "external-upload",
                    declaredBy = access.UserAccountId,
                    authenticityVerified = false
                }));
            evidence.Add(item);
        }

        dbContext.SignatureEvidence.AddRange(evidence);
        contract.MarkFullySigned(timeProvider.GetUtcNow());
        contract.Complete(timeProvider.GetUtcNow());
        var finalDocument = ContractFinalDocument.Create(
            organizationId,
            contractId,
            version.Id,
            version.DocumentStorageKey
                ?? throw new InvalidOperationException(
                    "El contrato externo no tiene archivo."),
            version.DocumentFileName ?? "contrato-externo.pdf",
            version.DocumentSizeBytes
                ?? throw new InvalidOperationException(
                    "El contrato externo no tiene tamaño."),
            version.DocumentSha256
                ?? throw new InvalidOperationException(
                    "El contrato externo no tiene hash."),
            timeProvider.GetUtcNow());
        dbContext.ContractFinalDocuments.Add(finalDocument);
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "contract.external_validated",
            nameof(Contract),
            contract.Id,
            new Dictionary<string, object?>
            {
                ["signerCount"] = signers.Count,
                ["authenticityVerified"] = false
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await readinessService.TryAutomaticConfirmationAsync(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            cancellationToken);
        return await BuildContractResponseAsync(contract, cancellationToken);
    }

    public async Task<IReadOnlyList<SignatureEvidenceSummaryResponse>>
        GetEvidenceAsync(
            Guid organizationId,
            Guid contractId,
            CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            cancellationToken);
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.SignaturesViewEvidence,
            contract.EventId,
            cancellationToken);
        return await dbContext.SignatureEvidence
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ContractId == contractId)
            .OrderBy(entity => entity.SignedAt)
            .Select(entity => ToEvidenceSummary(entity))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortalContractListItemResponse>>
        GetPortalContractsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var eventContexts = dbContext.EventAccesses
            .AsNoTracking()
            .Where(access =>
                access.UserAccountId == currentUser.UserAccountId
                && access.Status == EventAccessStatus.Active
                && access.StartsAt <= now
                && (access.ExpiresAt == null || access.ExpiresAt > now)
                && access.RevokedAt == null);
        return await dbContext.Contracts
            .AsNoTracking()
            .Join(
                eventContexts,
                contract => new
                {
                    contract.OrganizationId,
                    contract.EventId
                },
                access => new
                {
                    access.OrganizationId,
                    access.EventId
                },
                (contract, _) => contract)
            .Where(contract =>
                contract.Status != ContractStatus.Draft
                && contract.Status != ContractStatus.Cancelled)
            .OrderByDescending(contract => contract.UpdatedAt)
            .Select(contract => new PortalContractListItemResponse(
                contract.Id,
                contract.EventId,
                contract.ContractNumber,
                contract.Name,
                contract.Status,
                contract.CurrentVersionNumber,
                dbContext.ContractSigners.Any(signer =>
                    signer.OrganizationId == contract.OrganizationId
                    && signer.ContractId == contract.Id
                    && signer.UserAccountId == currentUser.UserAccountId
                    && signer.Status != ContractSignerStatus.Signed),
                dbContext.ContractFinalDocuments.Any(document =>
                    document.OrganizationId == contract.OrganizationId
                    && document.ContractId == contract.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<PortalContractResponse> GetPortalContractAsync(
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Id == contractId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el contrato.");
        await portalAccessService.RequireAsync(
            contract.EventId,
            Permissions.ContractsView,
            cancellationToken);
        var version = await dbContext.ContractVersions
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id
                && entity.PublishedAt != null
                && entity.SupersededAt == null)
            .OrderByDescending(entity => entity.VersionNumber)
            .Select(entity => new ContractVersionResponse(
                entity.Id,
                entity.VersionNumber,
                null,
                entity.SourceProposalVersionId,
                entity.RenderedContent,
                entity.DocumentFileName,
                entity.DocumentSizeBytes,
                entity.DocumentSha256,
                entity.ConsentText,
                entity.ValidUntil,
                entity.CreatedAt,
                entity.PublishedAt,
                entity.SupersededAt))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(
                "El contrato no tiene una versión disponible.");
        var parties = await dbContext.ContractParties
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => new ContractPartyResponse(
                entity.Id,
                entity.PartyType,
                entity.ClientId,
                entity.OrganizationPartyId,
                entity.DisplayName,
                entity.LegalName,
                null,
                null,
                entity.SortOrder))
            .ToListAsync(cancellationToken);
        var signers = await dbContext.ContractSigners
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .OrderBy(entity => entity.SigningOrder)
            .Select(entity => new PublicContractSignerStatusResponse(
                entity.SignerRole,
                entity.Status,
                entity.SignedAt))
            .ToListAsync(cancellationToken);
        var pendingSigner = await dbContext.ContractSigners
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id
                && entity.UserAccountId == currentUser.UserAccountId
                && entity.Status != ContractSignerStatus.Signed)
            .Select(entity => new
            {
                entity.Id,
                entity.Name
            })
            .FirstOrDefaultAsync(cancellationToken);
        var hasFinal = await dbContext.ContractFinalDocuments
            .AsNoTracking()
            .AnyAsync(
                entity =>
                    entity.OrganizationId == contract.OrganizationId
                    && entity.ContractId == contract.Id,
                cancellationToken);
        return new PortalContractResponse(
            contract.Id,
            contract.EventId,
            contract.ContractNumber,
            contract.Name,
            contract.Status,
            version,
            parties,
            signers,
            pendingSigner?.Id,
            pendingSigner?.Name,
            hasFinal);
    }

    public async Task<ContractFileDownload> DownloadPortalVersionAsync(
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Id == contractId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el contrato.");
        await portalAccessService.RequireAsync(
            contract.EventId,
            Permissions.ContractsView,
            cancellationToken);
        var version = await dbContext.ContractVersions
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contractId
                && entity.PublishedAt != null
                && entity.SupersededAt == null)
            .OrderByDescending(entity => entity.VersionNumber)
            .SingleAsync(cancellationToken);
        return new ContractFileDownload(
            await fileStorage.OpenReadAsync(
                version.DocumentStorageKey
                    ?? throw new InvalidOperationException(
                        "La versión no tiene archivo."),
                cancellationToken),
            "application/pdf",
            version.DocumentFileName ?? "contrato.pdf");
    }

    public async Task<ContractFileDownload> DownloadPortalFinalAsync(
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var contract = await dbContext.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => entity.Id == contractId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el contrato.");
        await portalAccessService.RequireAsync(
            contract.EventId,
            Permissions.ContractsView,
            cancellationToken);
        var document = await dbContext.ContractFinalDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == contract.OrganizationId
                    && entity.ContractId == contractId,
                cancellationToken)
            ?? throw new NotFoundException(
                "El contrato aún no tiene documento final.");
        return new ContractFileDownload(
            await fileStorage.OpenReadAsync(document.StorageKey, cancellationToken),
            "application/pdf",
            document.FileName);
    }

    private async Task<PublicContractSignatureResponse> RecordSignatureAsync(
        Contract contract,
        ContractVersion version,
        ContractSigner signer,
        SignatureRequest? signatureRequest,
        SubmitSignatureRequest request,
        Guid? userAccountId,
        Guid? sessionId,
        CancellationToken cancellationToken)
    {
        ValidateSignatureSubmission(request, signer);
        contract.EnsureSignable();
        signer.EnsureUnsigned();
        if (!version.IsAvailableForSigning(timeProvider.GetUtcNow())
            || version.DocumentSha256 is null)
        {
            throw new GoneException(
                "La versión ya no está disponible para firma.");
        }

        string? imageStorageKey = null;
        try
        {
            if (request.SigningMethod == SigningMethod.Drawn)
            {
                var image = DecodeSignatureImage(request.SignatureDataUrl);
                imageStorageKey = await fileStorage.SaveAsync(
                    new MemoryStream(image, writable: false),
                    ".png",
                    cancellationToken);
            }

            var now = timeProvider.GetUtcNow();
            var evidence = SignatureEvidence.Create(
                contract.OrganizationId,
                contract.Id,
                version.Id,
                signer.Id,
                signatureRequest?.Id,
                request.SigningMethod,
                request.DeclaredSignerName.Trim(),
                signer.Email,
                userAccountId,
                sessionId,
                imageStorageKey,
                version.DocumentSha256,
                version.ConsentText,
                now,
                ClientIp(),
                SafeUserAgent(),
                TraceIdentifier(),
                JsonSerializer.Serialize(new
                {
                    signingMethod = request.SigningMethod.ToString(),
                    contractVersion = version.VersionNumber,
                    explicitConsent = true,
                    authenticated = userAccountId is not null
                }));
            dbContext.SignatureEvidence.Add(evidence);
            signer.Sign(now);
            signatureRequest?.MarkSigned(now);
            var signers = await dbContext.ContractSigners
                .Where(entity =>
                    entity.OrganizationId == contract.OrganizationId
                    && entity.ContractId == contract.Id)
                .ToListAsync(cancellationToken);
            var required = signers.Where(entity => entity.IsRequired).ToList();
            if (required.Count > 0
                && required.All(entity =>
                    entity.Id == signer.Id
                    || entity.Status == ContractSignerStatus.Signed))
            {
                contract.MarkFullySigned(now);
                var existingEvidence = await dbContext.SignatureEvidence
                    .AsNoTracking()
                    .Where(entity =>
                        entity.OrganizationId == contract.OrganizationId
                        && entity.ContractId == contract.Id
                        && entity.ContractVersionId == version.Id)
                    .Select(entity => ToEvidenceSummary(entity))
                    .ToListAsync(cancellationToken);
                existingEvidence.Add(ToEvidenceSummary(evidence));
                await GenerateFinalDocumentAsync(
                    contract,
                    version,
                    existingEvidence,
                    now,
                    cancellationToken);
                contract.Complete(now);
                auditService.Add(
                    contract.OrganizationId,
                    contract.EventId,
                    userAccountId,
                    "contract.completed",
                    nameof(Contract),
                    contract.Id);
            }
            else
            {
                contract.MarkPartiallySigned(now);
            }

            auditService.Add(
                contract.OrganizationId,
                contract.EventId,
                userAccountId,
                "signature.completed",
                nameof(SignatureEvidence),
                evidence.Id,
                new Dictionary<string, object?>
                {
                    ["contractVersionId"] = version.Id,
                    ["contractSignerId"] = signer.Id,
                    ["signingMethod"] = request.SigningMethod.ToString(),
                    ["documentSha256"] = version.DocumentSha256
                });
            await dbContext.SaveChangesAsync(cancellationToken);
            await readinessService.TryAutomaticConfirmationAsync(
                contract.OrganizationId,
                contract.EventId,
                userAccountId ?? contract.CreatedBy,
                cancellationToken);
            return await BuildPublicResponseAsync(
                new PublicSignatureContext(
                    signatureRequest,
                    contract,
                    version,
                    signer),
                cancellationToken);
        }
        catch
        {
            if (imageStorageKey is not null)
            {
                await fileStorage.DeleteAsync(
                    imageStorageKey,
                    CancellationToken.None);
            }

            throw;
        }
    }

    private async Task GenerateFinalDocumentAsync(
        Contract contract,
        ContractVersion version,
        IReadOnlyList<SignatureEvidenceSummaryResponse> evidence,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var organizationName = await dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.Id == contract.OrganizationId)
            .Select(entity => entity.Name)
            .SingleAsync(cancellationToken);
        var parties = await dbContext.ContractParties
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => new ContractPartyResponse(
                entity.Id,
                entity.PartyType,
                entity.ClientId,
                entity.OrganizationPartyId,
                entity.DisplayName,
                entity.LegalName,
                entity.TaxId,
                entity.Address,
                entity.SortOrder))
            .ToListAsync(cancellationToken);
        var model = new ContractPdfModel(
            organizationName,
            contract.ContractNumber,
            contract.Name,
            version.VersionNumber,
            version.RenderedContent,
            version.ConsentText,
            version.ValidUntil,
            version.DocumentSha256 ?? string.Empty,
            parties);
        var finalPdf = pdfGenerator.GenerateFinal(model, evidence);
        var key = await fileStorage.SaveAsync(
            new MemoryStream(finalPdf, writable: false),
            ".pdf",
            cancellationToken);
        var final = ContractFinalDocument.Create(
            contract.OrganizationId,
            contract.Id,
            version.Id,
            key,
            $"contrato-{contract.ContractNumber}-firmado.pdf",
            finalPdf.LongLength,
            Convert.ToHexString(SHA256.HashData(finalPdf)),
            now);
        dbContext.ContractFinalDocuments.Add(final);
    }

    private async Task<PublicSignatureContext> ResolvePublicAsync(
        string token,
        bool markViewed,
        CancellationToken cancellationToken)
    {
        var hash = tokenService.Hash(token);
        var request = await dbContext.SignatureRequests
            .SingleOrDefaultAsync(
                entity => entity.TokenHash == hash,
                cancellationToken)
            ?? throw new NotFoundException(
                "El enlace de firma no es válido.");
        var now = timeProvider.GetUtcNow();
        if (!request.IsAvailable(now))
        {
            throw new GoneException(
                "El enlace de firma venció, fue revocado o ya se utilizó.");
        }

        var contract = await FindContractAsync(
            request.OrganizationId,
            request.ContractId,
            cancellationToken);
        var version = await dbContext.ContractVersions
            .SingleAsync(
                entity =>
                    entity.OrganizationId == request.OrganizationId
                    && entity.Id == request.ContractVersionId
                    && entity.ContractId == request.ContractId,
                cancellationToken);
        if (!version.IsAvailableForSigning(now)
            || contract.CurrentVersionNumber != version.VersionNumber
            || contract.Status is ContractStatus.Cancelled
                or ContractStatus.Declined
                or ContractStatus.Completed)
        {
            throw new GoneException(
                "La versión del contrato ya no está disponible para firma.");
        }

        var signer = await FindSignerAsync(
            request.OrganizationId,
            request.ContractId,
            request.ContractSignerId,
            cancellationToken);
        if (markViewed)
        {
            request.MarkViewed(now);
            signer.MarkViewed(now);
            contract.MarkViewed(now);
            auditService.Add(
                contract.OrganizationId,
                contract.EventId,
                null,
                "contract.viewed",
                nameof(ContractVersion),
                version.Id,
                new Dictionary<string, object?>
                {
                    ["contractSignerId"] = signer.Id
                });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new PublicSignatureContext(request, contract, version, signer);
    }

    private async Task<PublicContractSignatureResponse> BuildPublicResponseAsync(
        PublicSignatureContext context,
        CancellationToken cancellationToken)
    {
        var organizationName = await dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.Id == context.Contract.OrganizationId)
            .Select(entity => entity.Name)
            .SingleAsync(cancellationToken);
        var parties = await dbContext.ContractParties
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == context.Contract.OrganizationId
                && entity.ContractId == context.Contract.Id)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => entity.DisplayName)
            .ToListAsync(cancellationToken);
        var signers = await dbContext.ContractSigners
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == context.Contract.OrganizationId
                && entity.ContractId == context.Contract.Id)
            .OrderBy(entity => entity.SigningOrder)
            .Select(entity => new PublicContractSignerStatusResponse(
                entity.SignerRole,
                entity.Status,
                entity.SignedAt))
            .ToListAsync(cancellationToken);
        return new PublicContractSignatureResponse(
            context.Contract.Id,
            context.Version.Id,
            context.Signer.Id,
            context.Contract.ContractNumber,
            context.Contract.Name,
            context.Version.VersionNumber,
            organizationName,
            context.Signer.Name,
            context.Signer.Email,
            parties,
            context.Version.RenderedContent,
            context.Version.ValidUntil,
            context.Version.ConsentText,
            context.Version.DocumentSha256 ?? string.Empty,
            signers,
            context.Request?.IsAvailable(timeProvider.GetUtcNow())
                ?? context.Signer.Status != ContractSignerStatus.Signed);
    }

    private async Task<ContractVersion> GetCurrentPublishedVersionAsync(
        Contract contract,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.ContractVersions
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == contract.OrganizationId
                    && entity.ContractId == contract.Id
                    && entity.VersionNumber == contract.CurrentVersionNumber
                    && entity.PublishedAt != null,
                cancellationToken)
            ?? throw new ConflictException(
                "El contrato no tiene una versión publicada.");
        if (!version.IsAvailableForSigning(timeProvider.GetUtcNow()))
        {
            throw new GoneException(
                "La versión publicada ya no está vigente.");
        }

        return version;
    }

    private async Task<Contract> FindContractAsync(
        Guid organizationId,
        Guid contractId,
        CancellationToken cancellationToken) =>
        await dbContext.Contracts.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == contractId,
            cancellationToken)
        ?? throw new NotFoundException("No se encontró el contrato.");

    private async Task<ContractSigner> FindSignerAsync(
        Guid organizationId,
        Guid contractId,
        Guid signerId,
        CancellationToken cancellationToken) =>
        await dbContext.ContractSigners.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.ContractId == contractId
                && entity.Id == signerId,
            cancellationToken)
        ?? throw new NotFoundException("No se encontró el firmante.");

    private async Task<ContractResponse> BuildContractResponseAsync(
        Contract contract,
        CancellationToken cancellationToken)
    {
        var versions = await dbContext.ContractVersions
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .OrderByDescending(entity => entity.VersionNumber)
            .Select(entity => new ContractVersionResponse(
                entity.Id,
                entity.VersionNumber,
                entity.TemplateId,
                entity.SourceProposalVersionId,
                entity.RenderedContent,
                entity.DocumentFileName,
                entity.DocumentSizeBytes,
                entity.DocumentSha256,
                entity.ConsentText,
                entity.ValidUntil,
                entity.CreatedAt,
                entity.PublishedAt,
                entity.SupersededAt))
            .ToListAsync(cancellationToken);
        var parties = await dbContext.ContractParties
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => new ContractPartyResponse(
                entity.Id,
                entity.PartyType,
                entity.ClientId,
                entity.OrganizationPartyId,
                entity.DisplayName,
                entity.LegalName,
                entity.TaxId,
                entity.Address,
                entity.SortOrder))
            .ToListAsync(cancellationToken);
        var signers = await dbContext.ContractSigners
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .OrderBy(entity => entity.SigningOrder)
            .Select(entity => new ContractSignerResponse(
                entity.Id,
                entity.ContractPartyId,
                entity.PersonId,
                entity.UserAccountId,
                entity.Name,
                entity.Email,
                entity.SignerRole,
                entity.SigningOrder,
                entity.IsRequired,
                entity.Status,
                entity.SignedAt,
                entity.DeclinedAt))
            .ToListAsync(cancellationToken);
        var requirements = await dbContext.ContractingRequirementSnapshots
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .Select(entity => new ContractRequirementSnapshotResponse(
                entity.RequireAcceptedProposal,
                entity.RequireCompletedContract,
                entity.DepositRequirementType,
                entity.DepositRequirementValue,
                entity.RequiredDepositAmount,
                entity.CurrencyCode,
                entity.ConfirmationMode,
                entity.CreatedAt))
            .SingleAsync(cancellationToken);
        return new ContractResponse(
            contract.Id,
            contract.OrganizationId,
            contract.EventId,
            contract.ClientId,
            contract.AcceptedProposalId,
            contract.AcceptedProposalVersionId,
            contract.ContractNumber,
            contract.Name,
            contract.SourceType,
            contract.Status,
            contract.CurrentVersionNumber,
            contract.ContractGrandTotal,
            contract.CurrencyCode,
            versions,
            parties,
            signers,
            requirements,
            contract.CreatedAt,
            contract.UpdatedAt,
            contract.CompletedAt,
            contract.CancelledAt,
            contract.CancellationReason);
    }

    private static void ValidateSignatureSubmission(
        SubmitSignatureRequest request,
        ContractSigner signer)
    {
        var errors = new Dictionary<string, string[]>();
        if (!request.AcceptElectronicMeans)
        {
            errors["acceptElectronicMeans"] =
                ["Debes aceptar el uso de medios electrónicos."];
        }

        if (!request.ConfirmDisplayedVersion)
        {
            errors["confirmDisplayedVersion"] =
                ["Debes confirmar la versión mostrada."];
        }

        if (string.IsNullOrWhiteSpace(request.DeclaredSignerName)
            || !string.Equals(
                CollapseWhitespace(request.DeclaredSignerName),
                CollapseWhitespace(signer.Name),
                StringComparison.OrdinalIgnoreCase))
        {
            errors["declaredSignerName"] =
                ["El nombre declarado no corresponde al firmante."];
        }

        if (request.SigningMethod == SigningMethod.Drawn
            && string.IsNullOrWhiteSpace(request.SignatureDataUrl))
        {
            errors["signatureDataUrl"] = ["Dibuja tu firma antes de continuar."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static byte[] DecodeSignatureImage(string? dataUrl)
    {
        const string prefix = "data:image/png;base64,";
        if (string.IsNullOrWhiteSpace(dataUrl)
            || !dataUrl.StartsWith(prefix, StringComparison.Ordinal)
            || dataUrl.Length > prefix.Length + MaximumSignatureImageBytes * 2)
        {
            throw Validation(
                "signatureDataUrl",
                "La imagen de firma no es un PNG válido.");
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(dataUrl[prefix.Length..]);
        }
        catch (FormatException)
        {
            throw Validation(
                "signatureDataUrl",
                "La imagen de firma no es un PNG válido.");
        }

        if (bytes.Length is <= 8 or > MaximumSignatureImageBytes
            || !bytes.AsSpan(0, 8).SequenceEqual(
                new byte[]
                {
                    0x89,
                    0x50,
                    0x4E,
                    0x47,
                    0x0D,
                    0x0A,
                    0x1A,
                    0x0A
                }))
        {
            throw Validation(
                "signatureDataUrl",
                "La imagen de firma no es un PNG válido.");
        }

        return bytes;
    }

    private static SignatureEvidenceSummaryResponse ToEvidenceSummary(
        SignatureEvidence evidence) =>
        new(
            evidence.Id,
            evidence.ContractVersionId,
            evidence.ContractSignerId,
            evidence.SigningMethod,
            evidence.DeclaredSignerName,
            evidence.DeclaredSignerEmail,
            evidence.DocumentSha256,
            evidence.SignedAt);

    private string? ClientIp() =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? SafeUserAgent()
    {
        var value = httpContextAccessor.HttpContext?
            .Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= 512 ? value : value[..512];
    }

    private string TraceIdentifier() =>
        httpContextAccessor.HttpContext?.TraceIdentifier
        ?? Guid.NewGuid().ToString("N");

    private static string CollapseWhitespace(string value) =>
        string.Join(
            ' ',
            value.Trim().Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RequestValidationException Validation(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private sealed record PublicSignatureContext(
        SignatureRequest? Request,
        Contract Contract,
        ContractVersion Version,
        ContractSigner Signer);
}
