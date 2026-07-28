using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Invitations.Domain;

public sealed class InvitationDesign : ITenantEntity
{
    private InvitationDesign()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid? SourceTemplateId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public InvitationDesignStatus Status { get; private set; }
    public string DraftThemeJson { get; private set; } = "{}";
    public string DraftContentJson { get; private set; } = "[]";
    public int NextVersionNumber { get; private set; }
    public Guid? ApprovedVersionId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public static InvitationDesign Create(
        Guid organizationId,
        Guid eventId,
        Guid? sourceTemplateId,
        string name,
        string themeJson,
        string contentJson,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            SourceTemplateId = sourceTemplateId,
            Name = name,
            Status = InvitationDesignStatus.Draft,
            DraftThemeJson = themeJson,
            DraftContentJson = contentJson,
            NextVersionNumber = 1,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void UpdateDraft(
        string name,
        string themeJson,
        string contentJson,
        DateTimeOffset now)
    {
        if (Status is InvitationDesignStatus.InReview
            or InvitationDesignStatus.Archived)
        {
            throw new DomainRuleException(
                "El diseño no puede editarse mientras está en revisión o archivado.");
        }

        Name = name;
        DraftThemeJson = themeJson;
        DraftContentJson = contentJson;
        if (Status is InvitationDesignStatus.Approved
            or InvitationDesignStatus.Published)
        {
            ApprovedVersionId = null;
        }

        Status = InvitationDesignStatus.Draft;
        UpdatedAt = now;
    }

    public int SubmitForReview(DateTimeOffset now)
    {
        if (Status is not (InvitationDesignStatus.Draft
            or InvitationDesignStatus.ChangesRequested))
        {
            throw new DomainRuleException("El diseño no está listo para enviarse a revisión.");
        }

        Status = InvitationDesignStatus.InReview;
        UpdatedAt = now;
        return NextVersionNumber++;
    }

    public void Approve(Guid versionId, DateTimeOffset now)
    {
        if (Status != InvitationDesignStatus.InReview)
        {
            throw new DomainRuleException("Solo un diseño en revisión puede aprobarse.");
        }

        ApprovedVersionId = versionId;
        Status = InvitationDesignStatus.Approved;
        UpdatedAt = now;
    }

    public void RequestChanges(DateTimeOffset now)
    {
        if (Status != InvitationDesignStatus.InReview)
        {
            throw new DomainRuleException("Solo un diseño en revisión admite esta decisión.");
        }

        Status = InvitationDesignStatus.ChangesRequested;
        UpdatedAt = now;
    }

    public void Publish(Guid versionId, DateTimeOffset now)
    {
        if (ApprovedVersionId != versionId)
        {
            throw new DomainRuleException("Debe publicarse exactamente la versión aprobada.");
        }

        Status = InvitationDesignStatus.Published;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = InvitationDesignStatus.Archived;
        ArchivedAt = now;
        UpdatedAt = now;
    }
}

public sealed class InvitationDesignVersion : ITenantEntity
{
    private InvitationDesignVersion()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid InvitationDesignId { get; private set; }
    public int VersionNumber { get; private set; }
    public string ThemeSnapshotJson { get; private set; } = "{}";
    public string ContentSnapshotJson { get; private set; } = "[]";
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public static InvitationDesignVersion Create(
        InvitationDesign design,
        int versionNumber,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = design.OrganizationId,
            EventId = design.EventId,
            InvitationDesignId = design.Id,
            VersionNumber = versionNumber,
            ThemeSnapshotJson = design.DraftThemeJson,
            ContentSnapshotJson = design.DraftContentJson,
            CreatedBy = createdBy,
            CreatedAt = now
        };

    public void MarkApproved(Guid approvedBy, DateTimeOffset now)
    {
        ApprovedBy = approvedBy;
        ApprovedAt = now;
    }

    public void MarkPublished(DateTimeOffset now) => PublishedAt = now;
}

public sealed class InvitationDesignComment : ITenantEntity
{
    private InvitationDesignComment()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid InvitationDesignId { get; private set; }
    public Guid InvitationDesignVersionId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public InvitationReviewDecision Decision { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static InvitationDesignComment Create(
        InvitationDesignVersion version,
        Guid authorUserId,
        InvitationReviewDecision decision,
        string message,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = version.OrganizationId,
            EventId = version.EventId,
            InvitationDesignId = version.InvitationDesignId,
            InvitationDesignVersionId = version.Id,
            AuthorUserId = authorUserId,
            Decision = decision,
            Message = message,
            CreatedAt = now
        };
}
