using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Contracts.Domain;
using Plannyt.Api.Modules.Contracts.Pdf;
using Plannyt.Api.Modules.Contracts.Rendering;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Documents.Storage;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Proposals.Domain;

namespace Plannyt.Api.Modules.Contracts.Application;

public sealed class ContractService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    ContractTemplateRenderer renderer,
    ContractVariableValueService variableValueService,
    IContractPdfGenerator pdfGenerator,
    IFileStorage fileStorage,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public const string DefaultConsentText =
        "Declaro que he revisado el documento mostrado, que los datos "
        + "proporcionados son correctos y que acepto utilizar medios "
        + "electrónicos para expresar mi consentimiento y firma respecto "
        + "de esta versión del contrato.";

    private const long MaximumExternalPdfSize = 10 * 1024 * 1024;

    private const string BuiltInTemplate =
        "<h1>Contrato de prestación de servicios</h1>"
        + "<p>Celebrado entre <strong>{{organization.name}}</strong> y "
        + "<strong>{{client.displayName}}</strong> para el evento "
        + "<strong>{{event.name}}</strong>, a realizarse el "
        + "{{event.date}} en {{event.city}}, {{event.country}}.</p>"
        + "<p>El valor total acordado es {{proposal.grandTotal}} "
        + "{{proposal.currency}} conforme a la propuesta "
        + "{{proposal.number}}, versión {{proposal.version}}.</p>"
        + "<p>Número de contrato: {{contract.number}}.</p>";

    private const string BuiltInManualTemplate =
        "<h1>Contrato de prestación de servicios</h1>"
        + "<p>Celebrado entre <strong>{{organization.name}}</strong> y "
        + "<strong>{{client.displayName}}</strong> para el evento "
        + "<strong>{{event.name}}</strong>, a realizarse el "
        + "{{event.date}} en {{event.city}}, {{event.country}}.</p>"
        + "<p>Número de contrato: {{contract.number}}.</p>";

    public async Task<IReadOnlyList<ContractListItemResponse>> GetAllAsync(
        Guid organizationId,
        Guid? eventId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsView,
            eventId,
            cancellationToken);
        var query = dbContext.Contracts
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);
        if (eventId is not null)
        {
            query = query.Where(entity => entity.EventId == eventId);
        }

        return await query
            .OrderByDescending(entity => entity.UpdatedAt)
            .Select(entity => new ContractListItemResponse(
                entity.Id,
                entity.EventId,
                entity.ClientId,
                entity.ContractNumber,
                entity.Name,
                entity.SourceType,
                entity.Status,
                entity.CurrentVersionNumber,
                entity.ContractGrandTotal,
                entity.CurrencyCode,
                entity.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContractResponse> GetAsync(
        Guid organizationId,
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            true,
            cancellationToken);
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsView,
            contract.EventId,
            cancellationToken);
        return await BuildResponseAsync(contract, cancellationToken);
    }

    public async Task<ContractResponse> CreateFromProposalAsync(
        Guid organizationId,
        CreateContractFromProposalRequest request,
        CancellationToken cancellationToken)
    {
        ValidateNameAndConsent(request.Name, request.ConsentText);
        var proposal = await dbContext.Proposals
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == request.ProposalId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la propuesta.");
        if (proposal.Status != ProposalStatus.Accepted
            || proposal.AcceptedVersionId is null)
        {
            throw new ConflictException(
                "Solo una propuesta aceptada puede originar un contrato.");
        }

        if (proposal.EventId is null || proposal.ClientId is null)
        {
            throw new ConflictException(
                "La propuesta aceptada debe estar vinculada a un evento y cliente.");
        }

        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsCreate,
            proposal.EventId,
            cancellationToken);
        await EnsureCommercialContextAsync(
            organizationId,
            proposal.EventId.Value,
            proposal.ClientId.Value,
            cancellationToken);
        var version = await dbContext.ProposalVersions
            .AsNoTracking()
            .SingleAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == proposal.AcceptedVersionId
                    && entity.ProposalId == proposal.Id,
                cancellationToken);
        return await CreateDraftAsync(
            organizationId,
            proposal.EventId.Value,
            proposal.ClientId.Value,
            proposal.Id,
            version.Id,
            request.Name,
            ContractSourceType.GeneratedFromProposal,
            version.GrandTotal,
            version.CurrencyCode,
            request.TemplateId,
            request.Content,
            request.ConsentText,
            request.ValidUntil,
            access.UserAccountId,
            cancellationToken);
    }

    public async Task<ContractResponse> CreateManualAsync(
        Guid organizationId,
        CreateManualContractRequest request,
        CancellationToken cancellationToken)
    {
        ValidateNameAndConsent(request.Name, request.ConsentText);
        ValidateMoney(request.ContractGrandTotal, request.CurrencyCode);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsCreate,
            request.EventId,
            cancellationToken);
        await EnsureCommercialContextAsync(
            organizationId,
            request.EventId,
            request.ClientId,
            cancellationToken);
        return await CreateDraftAsync(
            organizationId,
            request.EventId,
            request.ClientId,
            null,
            null,
            request.Name,
            ContractSourceType.Manual,
            request.ContractGrandTotal,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            request.TemplateId,
            request.Content,
            request.ConsentText,
            request.ValidUntil,
            access.UserAccountId,
            cancellationToken);
    }

    public async Task<ContractResponse> CreateExternalAsync(
        Guid organizationId,
        CreateExternalContractRequest request,
        CancellationToken cancellationToken)
    {
        ValidateNameAndConsent(request.Name, DefaultConsentText);
        ValidateMoney(request.ContractGrandTotal, request.CurrencyCode);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsUploadExternal,
            request.EventId,
            cancellationToken);
        await EnsureCommercialContextAsync(
            organizationId,
            request.EventId,
            request.ClientId,
            cancellationToken);
        await ValidateExternalPdfAsync(request.File, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var contract = Contract.Create(
            organizationId,
            request.EventId,
            request.ClientId,
            null,
            null,
            CreateContractNumber(now),
            request.Name.Trim(),
            ContractSourceType.ExternalUpload,
            request.ContractGrandTotal,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            access.UserAccountId,
            now);
        var policy = await GetOrCreatePolicyAsync(
            organizationId,
            now,
            cancellationToken);
        var snapshot = ContractingRequirementSnapshot.Create(
            organizationId,
            contract.Id,
            policy,
            contract.ContractGrandTotal,
            contract.CurrencyCode,
            now);
        var parties = await BuildDefaultPartiesAsync(
            contract,
            now,
            cancellationToken);
        string? storageKey = null;
        try
        {
            await using var source = request.File.OpenReadStream();
            using var bytes = new MemoryStream();
            await source.CopyToAsync(bytes, cancellationToken);
            var content = bytes.ToArray();
            storageKey = await fileStorage.SaveAsync(
                new MemoryStream(content, writable: false),
                ".pdf",
                cancellationToken);
            var hash = Convert.ToHexString(SHA256.HashData(content));
            var version = ContractVersion.CreateDraft(
                organizationId,
                contract.Id,
                1,
                null,
                null,
                "<p>Contrato firmado externamente y cargado en Plannyt.</p>",
                DefaultConsentText,
                request.ValidUntil,
                access.UserAccountId,
                now);
            version.Publish(
                storageKey,
                SafePdfFileName(request.File.FileName, contract.ContractNumber),
                content.LongLength,
                hash,
                now);
            contract.RecordVersion(1, now);
            contract.MarkPublished(now);
            dbContext.AddRange(contract, snapshot, version);
            dbContext.ContractParties.AddRange(parties);
            auditService.Add(
                organizationId,
                contract.EventId,
                access.UserAccountId,
                "contract.external_uploaded",
                nameof(Contract),
                contract.Id,
                new Dictionary<string, object?>
                {
                    ["sha256"] = hash,
                    ["sizeBytes"] = content.LongLength
                });
            await dbContext.SaveChangesAsync(cancellationToken);
            return await BuildResponseAsync(contract, cancellationToken);
        }
        catch
        {
            if (storageKey is not null)
            {
                await fileStorage.DeleteAsync(storageKey, CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<ContractResponse> UpdateDraftAsync(
        Guid organizationId,
        Guid contractId,
        UpdateContractDraftRequest request,
        CancellationToken cancellationToken)
    {
        ValidateNameAndConsent(request.Name, request.ConsentText);
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw Validation("content", "El contenido es obligatorio.");
        }

        var contract = await FindContractAsync(
            organizationId,
            contractId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsUpdateDraft,
            contract.EventId,
            cancellationToken);
        if (contract.SourceType == ContractSourceType.ExternalUpload)
        {
            throw new ConflictException(
                "Un contrato externo publicado no admite edición.");
        }

        var current = await dbContext.ContractVersions
            .SingleAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.ContractId == contractId
                    && entity.VersionNumber == contract.CurrentVersionNumber,
                cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (current.PublishedAt is not null)
        {
            current.Supersede(now);
            await RevokePendingRequestsAsync(
                organizationId,
                contractId,
                current.Id,
                now,
                cancellationToken);
            var signers = await dbContext.ContractSigners
                .Where(entity =>
                    entity.OrganizationId == organizationId
                    && entity.ContractId == contractId)
                .ToListAsync(cancellationToken);
            foreach (var signer in signers)
            {
                signer.ResetForNewVersion(now);
            }

            contract.RecordVersion(contract.CurrentVersionNumber + 1, now);
            current = ContractVersion.CreateDraft(
                organizationId,
                contractId,
                contract.CurrentVersionNumber,
                request.TemplateId,
                contract.AcceptedProposalVersionId,
                string.Empty,
                request.ConsentText.Trim(),
                request.ValidUntil,
                access.UserAccountId,
                now);
            dbContext.ContractVersions.Add(current);
        }

        contract.RenameDraft(request.Name.Trim(), now);
        var values = await variableValueService.BuildAsync(
            organizationId,
            contract.EventId,
            contract.ClientId,
            contract.AcceptedProposalVersionId,
            contract.Id,
            contract.ContractNumber,
            contract.CreatedAt,
            request.ValidUntil,
            cancellationToken);
        var rendered = renderer.Render(request.Content, values);
        if (rendered.UnknownVariables.Count > 0)
        {
            throw Validation(
                "content",
                $"Variables desconocidas: {string.Join(", ", rendered.UnknownVariables)}.");
        }

        current.UpdateDraft(
            request.TemplateId,
            rendered.RenderedContent,
            request.ConsentText.Trim(),
            request.ValidUntil);
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "contract.draft_updated",
            nameof(Contract),
            contract.Id,
            new Dictionary<string, object?>
            {
                ["versionNumber"] = current.VersionNumber
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(contract, cancellationToken);
    }

    public async Task<ContractVersionResponse> PublishAsync(
        Guid organizationId,
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsPublish,
            contract.EventId,
            cancellationToken);
        contract.EnsureDraftMutable();
        var version = await dbContext.ContractVersions
            .SingleAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.ContractId == contractId
                    && entity.VersionNumber == contract.CurrentVersionNumber,
                cancellationToken);
        var values = await variableValueService.BuildAsync(
            organizationId,
            contract.EventId,
            contract.ClientId,
            contract.AcceptedProposalVersionId,
            contract.Id,
            contract.ContractNumber,
            contract.CreatedAt,
            version.ValidUntil,
            cancellationToken);
        var rendered = renderer.Render(version.RenderedContent, values);
        if (!rendered.CanPublish)
        {
            var variables = rendered.UnknownVariables
                .Concat(rendered.MissingVariables)
                .Distinct(StringComparer.Ordinal);
            throw Validation(
                "content",
                $"No se puede publicar. Revisa: {string.Join(", ", variables)}.");
        }

        version.UpdateDraft(
            version.TemplateId,
            rendered.RenderedContent,
            version.ConsentText,
            version.ValidUntil);
        var organizationName = await dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.Id == organizationId)
            .Select(entity => entity.Name)
            .SingleAsync(cancellationToken);
        var parties = await GetPartyResponsesAsync(
            organizationId,
            contractId,
            cancellationToken);
        var model = new ContractPdfModel(
            organizationName,
            contract.ContractNumber,
            contract.Name,
            version.VersionNumber,
            version.RenderedContent,
            version.ConsentText,
            version.ValidUntil,
            string.Empty,
            parties);
        var pdf = pdfGenerator.GeneratePublished(model);
        var hash = Convert.ToHexString(SHA256.HashData(pdf));
        string? storageKey = null;
        try
        {
            storageKey = await fileStorage.SaveAsync(
                new MemoryStream(pdf, writable: false),
                ".pdf",
                cancellationToken);
            var now = timeProvider.GetUtcNow();
            version.Publish(
                storageKey,
                $"contrato-{contract.ContractNumber}-v{version.VersionNumber}.pdf",
                pdf.LongLength,
                hash,
                now);
            contract.MarkPublished(now);
            auditService.Add(
                organizationId,
                contract.EventId,
                access.UserAccountId,
                "contract.version_published",
                nameof(ContractVersion),
                version.Id,
                new Dictionary<string, object?>
                {
                    ["versionNumber"] = version.VersionNumber,
                    ["documentSha256"] = hash,
                    ["sizeBytes"] = pdf.LongLength
                });
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToVersionResponse(version);
        }
        catch
        {
            if (storageKey is not null)
            {
                await fileStorage.DeleteAsync(storageKey, CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<ContractFileDownload> DownloadVersionAsync(
        Guid organizationId,
        Guid contractId,
        Guid versionId,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            true,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsView,
            contract.EventId,
            cancellationToken);
        var version = await dbContext.ContractVersions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.ContractId == contractId
                    && entity.Id == versionId
                    && entity.PublishedAt != null,
                cancellationToken)
            ?? throw new NotFoundException(
                "No se encontró la versión publicada.");
        var stream = await fileStorage.OpenReadAsync(
            version.DocumentStorageKey
                ?? throw new InvalidOperationException(
                    "La versión no tiene almacenamiento."),
            cancellationToken);
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "contract.version_downloaded",
            nameof(ContractVersion),
            version.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ContractFileDownload(
            stream,
            version.DocumentMimeType ?? "application/pdf",
            version.DocumentFileName ?? "contrato.pdf");
    }

    public async Task<ContractFileDownload> DownloadFinalAsync(
        Guid organizationId,
        Guid contractId,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            true,
            cancellationToken);
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsView,
            contract.EventId,
            cancellationToken);
        var document = await dbContext.ContractFinalDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.ContractId == contractId,
                cancellationToken)
            ?? throw new NotFoundException(
                "El contrato aún no tiene un documento final.");
        return new ContractFileDownload(
            await fileStorage.OpenReadAsync(document.StorageKey, cancellationToken),
            "application/pdf",
            document.FileName);
    }

    public async Task CancelAsync(
        Guid organizationId,
        Guid contractId,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000)
        {
            throw Validation(
                "reason",
                "El motivo es obligatorio y admite 1,000 caracteres.");
        }

        var contract = await FindContractAsync(
            organizationId,
            contractId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsCancel,
            contract.EventId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        contract.Cancel(reason.Trim(), now);
        await RevokePendingRequestsAsync(
            organizationId,
            contractId,
            null,
            now,
            cancellationToken);
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "contract.cancelled",
            nameof(Contract),
            contract.Id,
            new Dictionary<string, object?> { ["reason"] = reason.Trim() });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContractPartyResponse> AddPartyAsync(
        Guid organizationId,
        Guid contractId,
        CreateContractPartyRequest request,
        CancellationToken cancellationToken)
    {
        ValidateParty(request);
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ContractsUpdateDraft,
            contract.EventId,
            cancellationToken);
        contract.EnsureDraftMutable();
        if (request.ClientId is not null
            && !await dbContext.Clients.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == request.ClientId,
                cancellationToken))
        {
            throw new NotFoundException("No se encontró el cliente de la parte.");
        }

        var party = ContractParty.Create(
            organizationId,
            contractId,
            request.PartyType,
            request.ClientId,
            request.PartyType == ContractPartyType.PlannerOrganization
                ? organizationId
                : null,
            request.DisplayName.Trim(),
            Normalize(request.LegalName),
            Normalize(request.TaxId),
            Normalize(request.Address),
            request.SortOrder,
            timeProvider.GetUtcNow());
        dbContext.ContractParties.Add(party);
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "contract.party_added",
            nameof(ContractParty),
            party.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPartyResponse(party);
    }

    public async Task<ContractSignerResponse> AddSignerAsync(
        Guid organizationId,
        Guid contractId,
        UpsertContractSignerRequest request,
        CancellationToken cancellationToken)
    {
        ValidateSigner(request);
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.SignaturesManageSigners,
            contract.EventId,
            cancellationToken);
        contract.EnsureSignableOrDraft();
        await EnsureSignerReferencesAsync(
            organizationId,
            contractId,
            request,
            cancellationToken);
        var signer = ContractSigner.Create(
            organizationId,
            contractId,
            request.ContractPartyId,
            request.PersonId,
            request.UserAccountId,
            request.Name.Trim(),
            request.Email.Trim(),
            request.SignerRole.Trim(),
            request.SigningOrder,
            request.IsRequired,
            timeProvider.GetUtcNow());
        dbContext.ContractSigners.Add(signer);
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "contract.signer_added",
            nameof(ContractSigner),
            signer.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSignerResponse(signer);
    }

    public async Task<ContractSignerResponse> UpdateSignerAsync(
        Guid organizationId,
        Guid contractId,
        Guid signerId,
        UpsertContractSignerRequest request,
        CancellationToken cancellationToken)
    {
        ValidateSigner(request);
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.SignaturesManageSigners,
            contract.EventId,
            cancellationToken);
        await EnsureSignerReferencesAsync(
            organizationId,
            contractId,
            request,
            cancellationToken);
        var signer = await FindSignerAsync(
            organizationId,
            contractId,
            signerId,
            cancellationToken);
        if (await dbContext.SignatureEvidence.AnyAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.ContractSignerId == signerId,
            cancellationToken))
        {
            throw new ConflictException(
                "Un firmante con evidencia ya no admite cambios.");
        }

        signer.Update(
            request.ContractPartyId,
            request.PersonId,
            request.UserAccountId,
            request.Name.Trim(),
            request.Email.Trim(),
            request.SignerRole.Trim(),
            request.SigningOrder,
            request.IsRequired,
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "contract.signer_updated",
            nameof(ContractSigner),
            signer.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSignerResponse(signer);
    }

    public async Task DeleteSignerAsync(
        Guid organizationId,
        Guid contractId,
        Guid signerId,
        CancellationToken cancellationToken)
    {
        var contract = await FindContractAsync(
            organizationId,
            contractId,
            false,
            cancellationToken);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.SignaturesManageSigners,
            contract.EventId,
            cancellationToken);
        var signer = await FindSignerAsync(
            organizationId,
            contractId,
            signerId,
            cancellationToken);
        if (await dbContext.SignatureEvidence.AnyAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.ContractSignerId == signerId,
            cancellationToken))
        {
            throw new ConflictException(
                "Un firmante con evidencia no puede eliminarse.");
        }

        var now = timeProvider.GetUtcNow();
        var requests = await dbContext.SignatureRequests
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ContractSignerId == signerId
                && entity.RevokedAt == null
                && entity.SignedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var signatureRequest in requests)
        {
            signatureRequest.Revoke(now);
        }

        dbContext.ContractSigners.Remove(signer);
        auditService.Add(
            organizationId,
            contract.EventId,
            access.UserAccountId,
            "contract.signer_removed",
            nameof(ContractSigner),
            signer.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContractingPolicyResponse> GetPolicyAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.OrganizationView,
            null,
            cancellationToken);
        var policy = await GetOrCreatePolicyAsync(
            organizationId,
            timeProvider.GetUtcNow(),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPolicyResponse(policy);
    }

    public async Task<ContractingPolicyResponse> UpdatePolicyAsync(
        Guid organizationId,
        UpdateContractingPolicyRequest request,
        CancellationToken cancellationToken)
    {
        OrganizationContractingPolicy.ValidateDeposit(
            request.DepositRequirementType,
            request.DepositRequirementValue);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.OrganizationUpdate,
            null,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var policy = await GetOrCreatePolicyAsync(
            organizationId,
            now,
            cancellationToken);
        policy.Update(
            request.RequireAcceptedProposal,
            request.RequireCompletedContract,
            request.DepositRequirementType,
            request.DepositRequirementValue,
            request.ConfirmationMode,
            now);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "contracting_policy.updated",
            nameof(OrganizationContractingPolicy),
            policy.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPolicyResponse(policy);
    }

    private async Task<ContractResponse> CreateDraftAsync(
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        Guid? proposalId,
        Guid? proposalVersionId,
        string name,
        ContractSourceType sourceType,
        decimal total,
        string currencyCode,
        Guid? templateId,
        string? requestedContent,
        string consentText,
        DateTimeOffset? validUntil,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var contract = Contract.Create(
            organizationId,
            eventId,
            clientId,
            proposalId,
            proposalVersionId,
            CreateContractNumber(now),
            name.Trim(),
            sourceType,
            total,
            currencyCode,
            createdBy,
            now);
        var policy = await GetOrCreatePolicyAsync(
            organizationId,
            now,
            cancellationToken);
        var snapshot = ContractingRequirementSnapshot.Create(
            organizationId,
            contract.Id,
            policy,
            total,
            currencyCode,
            now);
        var template = await ResolveTemplateAsync(
            organizationId,
            templateId,
            cancellationToken);
        var content = requestedContent;
        if (string.IsNullOrWhiteSpace(content))
        {
            content = template?.Content
                ?? (sourceType == ContractSourceType.GeneratedFromProposal
                    ? BuiltInTemplate
                    : BuiltInManualTemplate);
            templateId = template?.Id;
        }

        var values = await variableValueService.BuildAsync(
            organizationId,
            eventId,
            clientId,
            proposalVersionId,
            null,
            contract.ContractNumber,
            contract.CreatedAt,
            validUntil,
            cancellationToken);
        var rendered = renderer.Render(content, values);
        if (rendered.UnknownVariables.Count > 0)
        {
            throw Validation(
                "content",
                $"Variables desconocidas: {string.Join(", ", rendered.UnknownVariables)}.");
        }

        var version = ContractVersion.CreateDraft(
            organizationId,
            contract.Id,
            1,
            templateId,
            proposalVersionId,
            rendered.RenderedContent,
            consentText.Trim(),
            validUntil,
            createdBy,
            now);
        contract.RecordVersion(1, now);
        var parties = await BuildDefaultPartiesAsync(
            contract,
            now,
            cancellationToken);
        dbContext.AddRange(contract, snapshot, version);
        dbContext.ContractParties.AddRange(parties);
        auditService.Add(
            organizationId,
            eventId,
            createdBy,
            "contract.created",
            nameof(Contract),
            contract.Id,
            new Dictionary<string, object?>
            {
                ["sourceType"] = sourceType.ToString(),
                ["proposalVersionId"] = proposalVersionId
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(contract, cancellationToken);
    }

    private async Task<IReadOnlyList<ContractParty>> BuildDefaultPartiesAsync(
        Contract contract,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var organizationName = await dbContext.Organizations
            .AsNoTracking()
            .Where(entity => entity.Id == contract.OrganizationId)
            .Select(entity => entity.Name)
            .SingleAsync(cancellationToken);
        var clientName = await dbContext.Clients
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.Id == contract.ClientId)
            .Select(entity => entity.DisplayName)
            .SingleAsync(cancellationToken);
        return
        [
            ContractParty.Create(
                contract.OrganizationId,
                contract.Id,
                ContractPartyType.PlannerOrganization,
                null,
                contract.OrganizationId,
                organizationName,
                null,
                null,
                null,
                0,
                now),
            ContractParty.Create(
                contract.OrganizationId,
                contract.Id,
                ContractPartyType.Client,
                contract.ClientId,
                null,
                clientName,
                null,
                null,
                null,
                1,
                now)
        ];
    }

    private async Task EnsureCommercialContextAsync(
        Guid organizationId,
        Guid eventId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var valid = await dbContext.Events
            .AsNoTracking()
            .AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId,
                cancellationToken)
            && await dbContext.Clients
                .AsNoTracking()
                .AnyAsync(
                    entity =>
                        entity.OrganizationId == organizationId
                        && entity.Id == clientId
                        && entity.Status == ClientStatus.Active,
                    cancellationToken)
            && await dbContext.EventClients
                .AsNoTracking()
                .AnyAsync(
                    entity =>
                        entity.OrganizationId == organizationId
                        && entity.EventId == eventId
                        && entity.ClientId == clientId,
                    cancellationToken);
        if (!valid)
        {
            throw new ConflictException(
                "El evento y cliente deben pertenecer a la misma relación comercial.");
        }
    }

    private async Task<ContractTemplate?> ResolveTemplateAsync(
        Guid organizationId,
        Guid? templateId,
        CancellationToken cancellationToken)
    {
        if (templateId is not null)
        {
            return await dbContext.ContractTemplates
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    entity =>
                        entity.OrganizationId == organizationId
                        && entity.Id == templateId
                        && entity.IsActive
                        && entity.ArchivedAt == null,
                    cancellationToken)
                ?? throw new NotFoundException(
                    "No se encontró una plantilla activa.");
        }

        return await dbContext.ContractTemplates
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.IsDefault
                && entity.IsActive
                && entity.ArchivedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<OrganizationContractingPolicy> GetOrCreatePolicyAsync(
        Guid organizationId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var policy = await dbContext.OrganizationContractingPolicies
            .SingleOrDefaultAsync(
                entity => entity.OrganizationId == organizationId,
                cancellationToken);
        if (policy is null)
        {
            policy = OrganizationContractingPolicy.CreateDefault(
                organizationId,
                now);
            dbContext.OrganizationContractingPolicies.Add(policy);
        }

        return policy;
    }

    private async Task RevokePendingRequestsAsync(
        Guid organizationId,
        Guid contractId,
        Guid? versionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var requests = await dbContext.SignatureRequests
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ContractId == contractId
                && (versionId == null || entity.ContractVersionId == versionId)
                && entity.SignedAt == null
                && entity.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var signatureRequest in requests)
        {
            signatureRequest.Revoke(now);
        }
    }

    private async Task<ContractResponse> BuildResponseAsync(
        Contract contract,
        CancellationToken cancellationToken)
    {
        var versions = await dbContext.ContractVersions
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .OrderByDescending(entity => entity.VersionNumber)
            .Select(entity => ToVersionResponse(entity))
            .ToListAsync(cancellationToken);
        var parties = await GetPartyResponsesAsync(
            contract.OrganizationId,
            contract.Id,
            cancellationToken);
        var signers = await dbContext.ContractSigners
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == contract.OrganizationId
                && entity.ContractId == contract.Id)
            .OrderBy(entity => entity.SigningOrder)
            .Select(entity => ToSignerResponse(entity))
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

    private async Task<IReadOnlyList<ContractPartyResponse>>
        GetPartyResponsesAsync(
            Guid organizationId,
            Guid contractId,
            CancellationToken cancellationToken) =>
        await dbContext.ContractParties
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.ContractId == contractId)
            .OrderBy(entity => entity.SortOrder)
            .Select(entity => ToPartyResponse(entity))
            .ToListAsync(cancellationToken);

    private async Task<Contract> FindContractAsync(
        Guid organizationId,
        Guid contractId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Contracts.Where(entity =>
            entity.OrganizationId == organizationId
            && entity.Id == contractId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró el contrato.");
    }

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

    private async Task EnsureSignerReferencesAsync(
        Guid organizationId,
        Guid contractId,
        UpsertContractSignerRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.ContractParties.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.ContractId == contractId
                    && entity.Id == request.ContractPartyId,
                cancellationToken))
        {
            throw new NotFoundException(
                "No se encontró la parte contractual del firmante.");
        }

        if (request.PersonId is not null
            && !await dbContext.People.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == request.PersonId,
                cancellationToken))
        {
            throw new NotFoundException("No se encontró la persona firmante.");
        }

        if (request.UserAccountId is not null
            && !await dbContext.UserAccounts.AsNoTracking().AnyAsync(
                entity =>
                    entity.Id == request.UserAccountId
                    && entity.IsActive,
                cancellationToken))
        {
            throw new NotFoundException(
                "No se encontró la cuenta asociada al firmante.");
        }
    }

    private static async Task ValidateExternalPdfAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaximumExternalPdfSize)
        {
            throw new PayloadTooLargeException(
                "El PDF externo debe tener entre 1 byte y 10 MB.");
        }

        var safeName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (!string.Equals(
                Path.GetExtension(safeName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                file.ContentType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnsupportedMediaTypeException(
                "El contrato externo debe ser un archivo PDF.");
        }

        await using var stream = file.OpenReadStream();
        var header = new byte[5];
        var read = await stream.ReadAsync(header, cancellationToken);
        if (read != header.Length || !header.AsSpan().SequenceEqual("%PDF-"u8))
        {
            throw new UnsupportedMediaTypeException(
                "El archivo no contiene una firma válida de PDF.");
        }
    }

    private static void ValidateNameAndConsent(string name, string consent)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
        {
            errors["name"] = ["El nombre es obligatorio y admite 200 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(consent) || consent.Trim().Length > 4000)
        {
            errors["consentText"] =
                ["El consentimiento es obligatorio y admite 4,000 caracteres."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static void ValidateMoney(decimal total, string currency)
    {
        if (total < 0m)
        {
            throw Validation("contractGrandTotal", "El total no puede ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(currency)
            || currency.Trim().Length != 3)
        {
            throw Validation(
                "currencyCode",
                "La moneda debe ser un código ISO de tres letras.");
        }
    }

    private static void ValidateParty(CreateContractPartyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName)
            || request.DisplayName.Trim().Length > 200)
        {
            throw Validation(
                "displayName",
                "El nombre de la parte es obligatorio y admite 200 caracteres.");
        }

        if (request.SortOrder < 0)
        {
            throw Validation("sortOrder", "El orden no puede ser negativo.");
        }
    }

    private static void ValidateSigner(UpsertContractSignerRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 200)
        {
            errors["name"] = ["El nombre es obligatorio y admite 200 caracteres."];
        }

        if (!request.Email.Contains('@', StringComparison.Ordinal)
            || request.Email.Trim().Length > 254)
        {
            errors["email"] = ["El correo del firmante no es válido."];
        }

        if (string.IsNullOrWhiteSpace(request.SignerRole)
            || request.SignerRole.Trim().Length > 120)
        {
            errors["signerRole"] = ["El rol del firmante es obligatorio."];
        }

        if (request.SigningOrder < 0)
        {
            errors["signingOrder"] = ["El orden de firma no puede ser negativo."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static ContractVersionResponse ToVersionResponse(
        ContractVersion version) =>
        new(
            version.Id,
            version.VersionNumber,
            version.TemplateId,
            version.SourceProposalVersionId,
            version.RenderedContent,
            version.DocumentFileName,
            version.DocumentSizeBytes,
            version.DocumentSha256,
            version.ConsentText,
            version.ValidUntil,
            version.CreatedAt,
            version.PublishedAt,
            version.SupersededAt);

    private static ContractPartyResponse ToPartyResponse(ContractParty party) =>
        new(
            party.Id,
            party.PartyType,
            party.ClientId,
            party.OrganizationPartyId,
            party.DisplayName,
            party.LegalName,
            party.TaxId,
            party.Address,
            party.SortOrder);

    private static ContractSignerResponse ToSignerResponse(
        ContractSigner signer) =>
        new(
            signer.Id,
            signer.ContractPartyId,
            signer.PersonId,
            signer.UserAccountId,
            signer.Name,
            signer.Email,
            signer.SignerRole,
            signer.SigningOrder,
            signer.IsRequired,
            signer.Status,
            signer.SignedAt,
            signer.DeclinedAt);

    private static ContractingPolicyResponse ToPolicyResponse(
        OrganizationContractingPolicy policy) =>
        new(
            policy.RequireAcceptedProposal,
            policy.RequireCompletedContract,
            policy.DepositRequirementType,
            policy.DepositRequirementValue,
            policy.ConfirmationMode,
            policy.UpdatedAt);

    private static string CreateContractNumber(DateTimeOffset now) =>
        $"C-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static string SafePdfFileName(
        string original,
        string contractNumber)
    {
        var name = Path.GetFileName(original.Replace('\\', '/'));
        return string.IsNullOrWhiteSpace(name) || name.Length > 255
            ? $"contrato-{contractNumber}.pdf"
            : name;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static RequestValidationException Validation(
        string field,
        string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

internal static class ContractStateExtensions
{
    public static void EnsureSignableOrDraft(this Contract contract)
    {
        if (contract.Status is ContractStatus.Completed
            or ContractStatus.Cancelled
            or ContractStatus.Declined
            or ContractStatus.Expired)
        {
            throw new ConflictException(
                "El contrato ya no admite cambios de firmantes.");
        }
    }
}
