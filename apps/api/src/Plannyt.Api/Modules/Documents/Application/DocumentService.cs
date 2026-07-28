using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Application;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Documents.Domain;
using Plannyt.Api.Modules.Documents.Storage;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Documents.Application;

public sealed class DocumentService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    PortalAccessService portalAccessService,
    DocumentFileValidator fileValidator,
    IFileStorage fileStorage,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<DocumentResponse>> GetAdminDocumentsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.ResolveAsync(
            organizationId,
            eventId,
            timeProvider.GetUtcNow(),
            cancellationToken);
        var canViewInternal =
            access.Permissions.Contains(Permissions.DocumentsViewInternal);
        var canViewShared =
            access.Permissions.Contains(Permissions.DocumentsViewShared);
        if (!canViewInternal && !canViewShared)
        {
            throw new ForbiddenException(
                "No tienes permiso para consultar documentos.");
        }

        await EnsureEventExistsAsync(
            organizationId,
            eventId,
            cancellationToken);
        return await dbContext.BasicDocuments
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.DeletedAt == null
                && ((canViewInternal
                        && entity.Visibility == DocumentVisibility.Internal)
                    || (canViewShared
                        && entity.Visibility == DocumentVisibility.ClientShared)))
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => ToResponse(entity))
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentResponse> UploadAsync(
        Guid organizationId,
        Guid eventId,
        UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var permission = request.Visibility == DocumentVisibility.Internal
            ? Permissions.DocumentsUploadInternal
            : Permissions.DocumentsUploadShared;
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            permission,
            eventId,
            cancellationToken);
        await EnsureEventExistsAsync(
            organizationId,
            eventId,
            cancellationToken);
        var validated = await fileValidator.ValidateAsync(
            request,
            cancellationToken);
        string? storageKey = null;

        try
        {
            await using var content = request.File.OpenReadStream();
            storageKey = await fileStorage.SaveAsync(
                content,
                validated.Extension,
                cancellationToken);
            var document = BasicDocument.Create(
                organizationId,
                eventId,
                null,
                validated.DocumentType,
                validated.SafeFileName,
                fileStorage.ProviderName,
                storageKey,
                validated.MimeType,
                validated.SizeBytes,
                validated.Visibility,
                access.UserAccountId,
                timeProvider.GetUtcNow());
            dbContext.BasicDocuments.Add(document);
            auditService.Add(
                organizationId,
                eventId,
                access.UserAccountId,
                "document.uploaded",
                nameof(BasicDocument),
                document.Id,
                new Dictionary<string, object?>
                {
                    ["visibility"] = document.Visibility.ToString(),
                    ["mimeType"] = document.MimeType,
                    ["sizeBytes"] = document.SizeBytes
                });
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToResponse(document);
        }
        catch
        {
            if (storageKey is not null)
            {
                await fileStorage.DeleteAsync(
                    storageKey,
                    CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<DocumentDownload> DownloadAdminAsync(
        Guid organizationId,
        Guid eventId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await FindActiveDocumentAsync(
            organizationId,
            eventId,
            documentId,
            cancellationToken);
        var permission = document.Visibility == DocumentVisibility.Internal
            ? Permissions.DocumentsViewInternal
            : Permissions.DocumentsViewShared;
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            permission,
            eventId,
            cancellationToken);
        return await OpenAndAuditAsync(
            document,
            access.UserAccountId,
            "document.downloaded",
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid organizationId,
        Guid eventId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.DocumentsDelete,
            eventId,
            cancellationToken);
        var document = await FindActiveDocumentAsync(
            organizationId,
            eventId,
            documentId,
            cancellationToken);
        await fileStorage.DeleteAsync(document.StorageKey, cancellationToken);
        document.MarkDeleted(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "document.deleted",
            nameof(BasicDocument),
            document.Id,
            new Dictionary<string, object?>
            {
                ["visibility"] = document.Visibility.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortalDocumentResponse>> GetPortalDocumentsAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.DocumentsViewShared,
            cancellationToken);
        return await dbContext.BasicDocuments
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.Visibility == DocumentVisibility.ClientShared
                && entity.DeletedAt == null)
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new PortalDocumentResponse(
                entity.Id,
                entity.DocumentType,
                entity.FileName,
                entity.MimeType,
                entity.SizeBytes,
                entity.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentDownload> DownloadPortalAsync(
        Guid eventId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.DocumentsViewShared,
            cancellationToken);
        var document = await dbContext.BasicDocuments.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.Id == documentId
                && entity.Visibility == DocumentVisibility.ClientShared
                && entity.DeletedAt == null,
            cancellationToken)
            ?? throw new NotFoundException(
                "No se encontró el documento compartido.");
        return await OpenAndAuditAsync(
            document,
            access.EventAccessId,
            "document.portal_downloaded",
            cancellationToken,
            actorIsEventAccess: true);
    }

    private async Task<DocumentDownload> OpenAndAuditAsync(
        BasicDocument document,
        Guid actorId,
        string action,
        CancellationToken cancellationToken,
        bool actorIsEventAccess = false)
    {
        var content = await fileStorage.OpenReadAsync(
            document.StorageKey,
            cancellationToken);
        try
        {
            Guid? actorUserId = actorIsEventAccess
                ? await dbContext.EventAccesses
                    .AsNoTracking()
                    .Where(entity => entity.Id == actorId)
                    .Select(entity => (Guid?)entity.UserAccountId)
                    .SingleAsync(cancellationToken)
                : actorId;
            auditService.Add(
                document.OrganizationId,
                document.EventId,
                actorUserId,
                action,
                nameof(BasicDocument),
                document.Id);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new DocumentDownload(
                content,
                document.MimeType,
                document.FileName);
        }
        catch
        {
            await content.DisposeAsync();
            throw;
        }
    }

    private async Task<BasicDocument> FindActiveDocumentAsync(
        Guid organizationId,
        Guid eventId,
        Guid documentId,
        CancellationToken cancellationToken) =>
        await dbContext.BasicDocuments.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == documentId
                && entity.DeletedAt == null,
            cancellationToken)
        ?? throw new NotFoundException("No se encontró el documento.");

    private async Task EnsureEventExistsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Events.AsNoTracking().AnyAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == eventId,
            cancellationToken))
        {
            throw new NotFoundException("No se encontró el evento.");
        }
    }

    private static DocumentResponse ToResponse(BasicDocument document) =>
        new(
            document.Id,
            document.DocumentType,
            document.FileName,
            document.MimeType,
            document.SizeBytes,
            document.Visibility,
            document.UploadedBy,
            document.CreatedAt);
}
