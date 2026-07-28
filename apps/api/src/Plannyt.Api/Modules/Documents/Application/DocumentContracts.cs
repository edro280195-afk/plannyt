using Plannyt.Api.Modules.Documents.Domain;

namespace Plannyt.Api.Modules.Documents.Application;

public sealed record UploadDocumentRequest(
    IFormFile File,
    string DocumentType,
    DocumentVisibility Visibility);

public sealed record DocumentResponse(
    Guid Id,
    string DocumentType,
    string FileName,
    string MimeType,
    long SizeBytes,
    DocumentVisibility Visibility,
    Guid UploadedBy,
    DateTimeOffset CreatedAt);

public sealed record DocumentDownload(
    Stream Content,
    string MimeType,
    string FileName);
