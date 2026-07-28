using Microsoft.AspNetCore.Mvc;

namespace Plannyt.Api.Modules.Documents.Application;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var administration = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/events/{eventId:guid}/documents")
            .WithTags("Documentos")
            .RequireAuthorization();

        administration.MapGet(
            "/",
            async (
                Guid organizationId,
                Guid eventId,
                DocumentService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAdminDocumentsAsync(
                    organizationId,
                    eventId,
                    cancellationToken)));

        administration.MapPost(
                "/",
                async (
                    Guid organizationId,
                    Guid eventId,
                    [FromForm] UploadDocumentRequest request,
                    DocumentService service,
                    CancellationToken cancellationToken) =>
                {
                    var document = await service.UploadAsync(
                        organizationId,
                        eventId,
                        request,
                        cancellationToken);
                    return Results.Created(
                        $"/api/organizations/{organizationId}/events/{eventId}/documents/{document.Id}",
                        document);
                })
            .DisableAntiforgery()
            .WithMetadata(
                new RequestSizeLimitAttribute(
                    DocumentFileValidator.MaxFileSize + 1024 * 1024));

        administration.MapGet(
            "/{documentId:guid}/download",
            async (
                Guid organizationId,
                Guid eventId,
                Guid documentId,
                DocumentService service,
                CancellationToken cancellationToken) =>
            {
                var download = await service.DownloadAdminAsync(
                    organizationId,
                    eventId,
                    documentId,
                    cancellationToken);
                return Results.File(
                    download.Content,
                    download.MimeType,
                    download.FileName);
            });

        administration.MapDelete(
            "/{documentId:guid}",
            async (
                Guid organizationId,
                Guid eventId,
                Guid documentId,
                DocumentService service,
                CancellationToken cancellationToken) =>
            {
                await service.DeleteAsync(
                    organizationId,
                    eventId,
                    documentId,
                    cancellationToken);
                return Results.NoContent();
            });

        var portal = endpoints
            .MapGroup("/api/client-portal/events/{eventId:guid}/documents")
            .WithTags("Portal del cliente")
            .RequireAuthorization();

        portal.MapGet(
            "/",
            async (
                Guid eventId,
                DocumentService service,
                CancellationToken cancellationToken) =>
                Results.Ok(await service.GetPortalDocumentsAsync(
                    eventId,
                    cancellationToken)));

        portal.MapGet(
            "/{documentId:guid}/download",
            async (
                Guid eventId,
                Guid documentId,
                DocumentService service,
                CancellationToken cancellationToken) =>
            {
                var download = await service.DownloadPortalAsync(
                    eventId,
                    documentId,
                    cancellationToken);
                return Results.File(
                    download.Content,
                    download.MimeType,
                    download.FileName);
            });

        return endpoints;
    }
}
