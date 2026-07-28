using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Crm.Domain;

public sealed class ProspectActivity : ITenantEntity
{
    private ProspectActivity()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProspectId { get; private set; }

    public ProspectActivityType ActivityType { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public DateTimeOffset? ScheduledAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public Guid? AssignedUserId { get; private set; }

    public CommercialVisibility Visibility { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ProspectActivity Create(
        Guid organizationId,
        Guid prospectId,
        ProspectActivityType activityType,
        string subject,
        string? description,
        DateTimeOffset? scheduledAt,
        DateTimeOffset? completedAt,
        Guid? assignedUserId,
        CommercialVisibility visibility,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProspectId = prospectId,
            ActivityType = activityType,
            Subject = subject,
            Description = description,
            ScheduledAt = scheduledAt,
            CompletedAt = completedAt,
            AssignedUserId = assignedUserId,
            Visibility = visibility,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Complete(DateTimeOffset now)
    {
        CompletedAt ??= now;
        UpdatedAt = now;
    }
}
