using Microsoft.AspNetCore.Mvc;
using Plannyt.Api.BuildingBlocks.Http;
using Plannyt.Api.Modules.Documents.Application;

namespace Plannyt.Api.Modules.Contracts.Application;

public static class ContractEndpoints
{
    public static IEndpointRouteBuilder MapContractEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        MapTemplateEndpoints(endpoints);
        MapAdministrationEndpoints(endpoints);
        MapPublicEndpoints(endpoints);
        MapPortalEndpoints(endpoints);
        return endpoints;
    }

    private static void MapTemplateEndpoints(IEndpointRouteBuilder endpoints)
    {
        var templates = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/contract-templates")
            .WithTags("Plantillas de contrato")
            .RequireAuthorization();
        templates.MapGet("/", async (
            Guid organizationId,
            ContractTemplateService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(
                organizationId,
                cancellationToken)));
        templates.MapPost("/", async (
            Guid organizationId,
            UpsertContractTemplateRequest request,
            ContractTemplateService service,
            CancellationToken cancellationToken) =>
        {
            var template = await service.CreateAsync(
                organizationId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/contract-templates/{template.Id}",
                template);
        });
        templates.MapPut("/{templateId:guid}", async (
            Guid organizationId,
            Guid templateId,
            UpsertContractTemplateRequest request,
            ContractTemplateService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateAsync(
                organizationId,
                templateId,
                request,
                cancellationToken)));
        templates.MapDelete("/{templateId:guid}", async (
            Guid organizationId,
            Guid templateId,
            ContractTemplateService service,
            CancellationToken cancellationToken) =>
        {
            await service.ArchiveAsync(
                organizationId,
                templateId,
                cancellationToken);
            return Results.NoContent();
        });
        templates.MapPost("/preview", async (
            Guid organizationId,
            PreviewContractTemplateRequest request,
            ContractTemplateService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.PreviewAsync(
                organizationId,
                request,
                cancellationToken)));

        var policy = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/contracting-policy")
            .WithTags("Política de contratación")
            .RequireAuthorization();
        policy.MapGet("/", async (
            Guid organizationId,
            ContractService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPolicyAsync(
                organizationId,
                cancellationToken)));
        policy.MapPut("/", async (
            Guid organizationId,
            UpdateContractingPolicyRequest request,
            ContractService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdatePolicyAsync(
                organizationId,
                request,
                cancellationToken)));
    }

    private static void MapAdministrationEndpoints(
        IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/contracts")
            .WithTags("Contratos")
            .RequireAuthorization();
        group.MapGet("/", async (
            Guid organizationId,
            Guid? eventId,
            ContractService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(
                organizationId,
                eventId,
                cancellationToken)));
        group.MapPost("/from-proposal", async (
            Guid organizationId,
            CreateContractFromProposalRequest request,
            ContractService service,
            CancellationToken cancellationToken) =>
        {
            var contract = await service.CreateFromProposalAsync(
                organizationId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/contracts/{contract.Id}",
                contract);
        });
        group.MapPost("/manual", async (
            Guid organizationId,
            CreateManualContractRequest request,
            ContractService service,
            CancellationToken cancellationToken) =>
        {
            var contract = await service.CreateManualAsync(
                organizationId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/contracts/{contract.Id}",
                contract);
        });
        group.MapPost(
                "/external",
                async (
                    Guid organizationId,
                    [FromForm] CreateExternalContractRequest request,
                    ContractService service,
                    CancellationToken cancellationToken) =>
                {
                    var contract = await service.CreateExternalAsync(
                        organizationId,
                        request,
                        cancellationToken);
                    return Results.Created(
                        $"/api/organizations/{organizationId}/contracts/{contract.Id}",
                        contract);
                })
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(
                DocumentFileValidator.MaxFileSize + 1024 * 1024));
        group.MapGet("/{contractId:guid}", async (
            Guid organizationId,
            Guid contractId,
            ContractService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(
                organizationId,
                contractId,
                cancellationToken)));
        group.MapPut("/{contractId:guid}/draft", async (
            Guid organizationId,
            Guid contractId,
            UpdateContractDraftRequest request,
            ContractService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateDraftAsync(
                organizationId,
                contractId,
                request,
                cancellationToken)));
        group.MapPost("/{contractId:guid}/publish", async (
            Guid organizationId,
            Guid contractId,
            ContractService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.PublishAsync(
                organizationId,
                contractId,
                cancellationToken)));
        group.MapGet(
            "/{contractId:guid}/versions/{versionId:guid}/pdf",
            async (
                Guid organizationId,
                Guid contractId,
                Guid versionId,
                ContractService service,
                CancellationToken cancellationToken) =>
            {
                var file = await service.DownloadVersionAsync(
                    organizationId,
                    contractId,
                    versionId,
                    cancellationToken);
                return Results.File(file.Content, file.MimeType, file.FileName);
            });
        group.MapGet("/{contractId:guid}/final", async (
            Guid organizationId,
            Guid contractId,
            ContractService service,
            CancellationToken cancellationToken) =>
        {
            var file = await service.DownloadFinalAsync(
                organizationId,
                contractId,
                cancellationToken);
            return Results.File(file.Content, file.MimeType, file.FileName);
        });
        group.MapPost("/{contractId:guid}/cancel", async (
            Guid organizationId,
            Guid contractId,
            CancelContractRequest request,
            ContractService service,
            CancellationToken cancellationToken) =>
        {
            await service.CancelAsync(
                organizationId,
                contractId,
                request.Reason,
                cancellationToken);
            return Results.NoContent();
        });
        group.MapPost("/{contractId:guid}/parties", async (
            Guid organizationId,
            Guid contractId,
            CreateContractPartyRequest request,
            ContractService service,
            CancellationToken cancellationToken) =>
        {
            var party = await service.AddPartyAsync(
                organizationId,
                contractId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/contracts/{contractId}/parties/{party.Id}",
                party);
        });
        group.MapPost("/{contractId:guid}/signers", async (
            Guid organizationId,
            Guid contractId,
            UpsertContractSignerRequest request,
            ContractService service,
            CancellationToken cancellationToken) =>
        {
            var signer = await service.AddSignerAsync(
                organizationId,
                contractId,
                request,
                cancellationToken);
            return Results.Created(
                $"/api/organizations/{organizationId}/contracts/{contractId}/signers/{signer.Id}",
                signer);
        });
        group.MapPut(
            "/{contractId:guid}/signers/{signerId:guid}",
            async (
                Guid organizationId,
                Guid contractId,
                Guid signerId,
                UpsertContractSignerRequest request,
                ContractService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.UpdateSignerAsync(
                    organizationId,
                    contractId,
                    signerId,
                    request,
                    cancellationToken)));
        group.MapDelete(
            "/{contractId:guid}/signers/{signerId:guid}",
            async (
                Guid organizationId,
                Guid contractId,
                Guid signerId,
                ContractService service,
                CancellationToken cancellationToken) =>
            {
                await service.DeleteSignerAsync(
                    organizationId,
                    contractId,
                    signerId,
                    cancellationToken);
                return Results.NoContent();
            });
        group.MapPost(
            "/{contractId:guid}/signers/{signerId:guid}/requests",
            async (
                Guid organizationId,
                Guid contractId,
                Guid signerId,
                CreateSignatureRequest request,
                SignatureService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.CreateRequestAsync(
                    organizationId,
                    contractId,
                    signerId,
                    request,
                    cancellationToken)));
        group.MapDelete(
            "/{contractId:guid}/requests/{requestId:guid}",
            async (
                Guid organizationId,
                Guid contractId,
                Guid requestId,
                SignatureService service,
                CancellationToken cancellationToken) =>
            {
                await service.RevokeRequestAsync(
                    organizationId,
                    contractId,
                    requestId,
                    cancellationToken);
                return Results.NoContent();
            });
        group.MapPost(
            "/{contractId:guid}/signers/{signerId:guid}/sign",
            async (
                Guid organizationId,
                Guid contractId,
                Guid signerId,
                SubmitSignatureRequest request,
                SignatureService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.SignAsOrganizationAsync(
                    organizationId,
                    contractId,
                    signerId,
                    request,
                    cancellationToken)));
        group.MapPost("/{contractId:guid}/validate-external", async (
            Guid organizationId,
            Guid contractId,
            ValidateExternalContractRequest request,
            SignatureService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ValidateExternalAsync(
                organizationId,
                contractId,
                request,
                cancellationToken)));
        group.MapGet("/{contractId:guid}/evidence", async (
            Guid organizationId,
            Guid contractId,
            SignatureService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetEvidenceAsync(
                organizationId,
                contractId,
                cancellationToken)));

        var events = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/events/{eventId:guid}")
            .WithTags("Contratación del evento")
            .RequireAuthorization();
        events.MapGet("/contracting-readiness", async (
            Guid organizationId,
            Guid eventId,
            ContractingReadinessService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAdminAsync(
                organizationId,
                eventId,
                cancellationToken)));
        events.MapPost("/confirm", async (
            Guid organizationId,
            Guid eventId,
            ContractingReadinessService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ConfirmAsync(
                organizationId,
                eventId,
                cancellationToken)));
    }

    private static void MapPublicEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/public/signatures/{token}")
            .WithTags("Firma pública")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Sensitive);
        group.MapGet("/", async (
            string token,
            SignatureService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPublicAsync(token, cancellationToken)));
        group.MapGet("/pdf", async (
            string token,
            SignatureService service,
            CancellationToken cancellationToken) =>
        {
            var file = await service.DownloadPublicAsync(
                token,
                cancellationToken);
            return Results.File(file.Content, file.MimeType, file.FileName);
        });
        group.MapPost("/sign", async (
            string token,
            SubmitSignatureRequest request,
            SignatureService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.SignPublicAsync(
                token,
                request,
                cancellationToken)));
        group.MapPost("/decline", async (
            string token,
            DeclineSignatureRequest request,
            SignatureService service,
            CancellationToken cancellationToken) =>
        {
            await service.DeclinePublicAsync(
                token,
                request,
                cancellationToken);
            return Results.NoContent();
        });
    }

    private static void MapPortalEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/client-portal/events/{eventId:guid}/contracting-readiness",
                async (
                    Guid eventId,
                    ContractingReadinessService service,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetPortalAsync(
                        eventId,
                        cancellationToken)))
            .WithTags("Portal del cliente")
            .RequireAuthorization();

        var group = endpoints
            .MapGroup("/api/client-portal/contracts")
            .WithTags("Portal del cliente")
            .RequireAuthorization();
        group.MapGet("/", async (
            SignatureService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPortalContractsAsync(
                cancellationToken)));
        group.MapGet("/{contractId:guid}", async (
            Guid contractId,
            SignatureService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetPortalContractAsync(
                contractId,
                cancellationToken)));
        group.MapGet("/{contractId:guid}/pdf", async (
            Guid contractId,
            SignatureService service,
            CancellationToken cancellationToken) =>
        {
            var file = await service.DownloadPortalVersionAsync(
                contractId,
                cancellationToken);
            return Results.File(file.Content, file.MimeType, file.FileName);
        });
        group.MapGet("/{contractId:guid}/final", async (
            Guid contractId,
            SignatureService service,
            CancellationToken cancellationToken) =>
        {
            var file = await service.DownloadPortalFinalAsync(
                contractId,
                cancellationToken);
            return Results.File(file.Content, file.MimeType, file.FileName);
        });
        group.MapPost(
            "/{contractId:guid}/signers/{signerId:guid}/sign",
            async (
                Guid contractId,
                Guid signerId,
                SubmitSignatureRequest request,
                SignatureService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.SignFromPortalAsync(
                    contractId,
                    signerId,
                    request,
                    cancellationToken)));
    }
}
