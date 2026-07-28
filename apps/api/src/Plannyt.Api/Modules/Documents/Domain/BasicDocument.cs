using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Documents.Domain;

public enum DocumentVisibility
{
    Internal,
    ClientShared
}

public sealed class BasicDocument : ITenantEntity
{
    private BasicDocument()
    {
    }

    private BasicDocument(
        Guid id,
        Guid organizationId,
        Guid? eventId,
        Guid? clientId,
        string documentType,
        string fileName,
        string storageProvider,
        string storageKey,
        string mimeType,
        long sizeBytes,
        DocumentVisibility visibility,
        Guid uploadedBy,
        DateTimeOffset now)
    {
        Id = id;
        OrganizationId = organizationId;
        EventId = eventId;
        ClientId = clientId;
        DocumentType = documentType;
        FileName = fileName;
        StorageProvider = storageProvider;
        StorageKey = storageKey;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        Visibility = visibility;
        UploadedBy = uploadedBy;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid? EventId { get; private set; }

    public Guid? ClientId { get; private set; }

    public string DocumentType { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public string StorageProvider { get; private set; } = string.Empty;

    public string StorageKey { get; private set; } = string.Empty;

    public string MimeType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DocumentVisibility Visibility { get; private set; }

    public Guid UploadedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static BasicDocument Create(
        Guid organizationId,
        Guid? eventId,
        Guid? clientId,
        string documentType,
        string fileName,
        string storageProvider,
        string storageKey,
        string mimeType,
        long sizeBytes,
        DocumentVisibility visibility,
        Guid uploadedBy,
        DateTimeOffset now)
    {
        const long maxSizeBytes = 10 * 1024 * 1024;

        if (sizeBytes is <= 0 or > maxSizeBytes)
        {
            throw new DomainRuleException(
                "El documento debe tener un tamaño entre 1 byte y 10 MB.");
        }

        return new BasicDocument(
            Guid.NewGuid(),
            organizationId,
            eventId,
            clientId,
            documentType,
            fileName,
            storageProvider,
            storageKey,
            mimeType,
            sizeBytes,
            visibility,
            uploadedBy,
            now);
    }

    public void MarkDeleted(DateTimeOffset now)
    {
        DeletedAt ??= now;
    }
}
