using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Rsvp.Domain;

public sealed class RsvpForm : ITenantEntity
{
    private RsvpForm() { }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public RsvpFormStatus Status { get; private set; }
    public int CurrentDraftVersion { get; private set; }
    public Guid? ActivePublishedVersionId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static RsvpForm Create(
        Guid organizationId,
        Guid eventId,
        Guid createdBy,
        DateTimeOffset now)
    {
        return new RsvpForm
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            Status = RsvpFormStatus.Draft,
            CurrentDraftVersion = 1,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SubmitForReview(DateTimeOffset now)
    {
        if (Status != RsvpFormStatus.Draft && Status != RsvpFormStatus.ChangesRequested)
        {
            throw new DomainRuleException("Solo puede enviarse a revisión desde Draft o ChangesRequested.");
        }

        Status = RsvpFormStatus.InReview;
        UpdatedAt = now;
    }

    public void RequestChanges(DateTimeOffset now)
    {
        if (Status != RsvpFormStatus.InReview)
        {
            throw new DomainRuleException("Solo puede solicitar cambios en revisión.");
        }

        Status = RsvpFormStatus.ChangesRequested;
        UpdatedAt = now;
    }

    public void Approve(DateTimeOffset now)
    {
        if (Status != RsvpFormStatus.InReview)
        {
            throw new DomainRuleException("Solo puede aprobarse en estado InReview.");
        }

        Status = RsvpFormStatus.Approved;
        UpdatedAt = now;
    }

    public void Publish(Guid versionId, DateTimeOffset now)
    {
        if (Status != RsvpFormStatus.Approved)
        {
            throw new DomainRuleException("Solo puede publicarse una versión aprobada.");
        }

        Status = RsvpFormStatus.Published;
        ActivePublishedVersionId = versionId;
        UpdatedAt = now;
    }

    public void NewDraft(DateTimeOffset now)
    {
        if (Status != RsvpFormStatus.Published && Status != RsvpFormStatus.Archived)
        {
            throw new DomainRuleException("Solo puede crearse borrador desde Published o Archived.");
        }

        Status = RsvpFormStatus.Draft;
        CurrentDraftVersion++;
        ActivePublishedVersionId = null;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        if (Status == RsvpFormStatus.Draft || Status == RsvpFormStatus.InReview)
        {
            throw new DomainRuleException("No se puede archivar un formulario en edición o revisión.");
        }

        Status = RsvpFormStatus.Archived;
        UpdatedAt = now;
    }
}
