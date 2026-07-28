using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Modules.Documents.Domain;

namespace Plannyt.Api.Modules.Documents.Application;

public sealed class DocumentFileValidator
{
    public const long MaxFileSize = 10 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> MimeByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    public async Task<ValidatedDocumentFile> ValidateAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File.Length is <= 0)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["Selecciona un archivo con contenido."]
                });
        }

        if (request.File.Length > MaxFileSize)
        {
            throw new PayloadTooLargeException(
                "El tamaño máximo por documento es de 10 MB.");
        }

        if (string.IsNullOrWhiteSpace(request.DocumentType)
            || request.DocumentType.Trim().Length > 80)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["documentType"] =
                        ["El tipo de documento es obligatorio y admite hasta 80 caracteres."]
                });
        }

        var safeFileName = Path.GetFileName(
            request.File.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(safeFileName)
            || safeFileName.Length > 255)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["El nombre del archivo no es válido."]
                });
        }

        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        if (!MimeByExtension.TryGetValue(extension, out var expectedMime)
            || !string.Equals(
                request.File.ContentType,
                expectedMime,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new UnsupportedMediaTypeException(
                "Solo se permiten PDF, JPEG y PNG con extensión y MIME coincidentes.");
        }

        await using var stream = request.File.OpenReadStream();
        var header = new byte[8];
        var read = await stream.ReadAsync(header, cancellationToken);
        if (!HasExpectedSignature(expectedMime, header.AsSpan(0, read)))
        {
            throw new UnsupportedMediaTypeException(
                "La firma del archivo no coincide con el tipo declarado.");
        }

        return new ValidatedDocumentFile(
            safeFileName,
            extension,
            expectedMime,
            request.File.Length,
            request.DocumentType.Trim(),
            request.Visibility);
    }

    private static bool HasExpectedSignature(
        string mimeType,
        ReadOnlySpan<byte> header) =>
        mimeType switch
        {
            "application/pdf" => header.StartsWith("%PDF-"u8),
            "image/jpeg" => header.StartsWith(
                new byte[] { 0xFF, 0xD8, 0xFF }),
            "image/png" => header.StartsWith(
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
                }),
            _ => false
        };
}

public sealed record ValidatedDocumentFile(
    string SafeFileName,
    string Extension,
    string MimeType,
    long SizeBytes,
    string DocumentType,
    DocumentVisibility Visibility);
