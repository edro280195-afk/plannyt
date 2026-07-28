using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Crm.Domain;

public sealed class ProspectStatusHistory : ITenantEntity
{
    private ProspectStatusHistory()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Guid ProspectId { get; private set; }

    public ProspectStatus PreviousStatus { get; private set; }

    public ProspectStatus NewStatus { get; private set; }

    public string? Reason { get; private set; }

    public Guid ChangedBy { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    public static ProspectStatusHistory Create(
        Guid organizationId,
        Guid prospectId,
        ProspectStatus previousStatus,
        ProspectStatus newStatus,
        string? reason,
        Guid changedBy,
        DateTimeOffset changedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProspectId = prospectId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Reason = reason,
            ChangedBy = changedBy,
            ChangedAt = changedAt
        };
}
