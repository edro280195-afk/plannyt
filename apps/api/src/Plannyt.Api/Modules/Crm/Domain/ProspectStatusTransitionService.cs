using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Crm.Domain;

public sealed class ProspectStatusTransitionService
{
    public ProspectStatusHistory ChangeStatus(
        Prospect prospect,
        ProspectStatus newStatus,
        Guid changedBy,
        DateTimeOffset now,
        string? reason = null)
    {
        var previousStatus = prospect.Status;
        if (previousStatus == newStatus || !IsAllowed(previousStatus, newStatus))
        {
            throw new DomainRuleException(
                $"La transición de {previousStatus} a {newStatus} no está permitida.");
        }

        if (newStatus == ProspectStatus.Lost
            && string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException(
                "El motivo de pérdida es obligatorio.");
        }

        prospect.ApplyStatus(newStatus, reason, now);
        return ProspectStatusHistory.Create(
            prospect.OrganizationId,
            prospect.Id,
            previousStatus,
            newStatus,
            reason,
            changedBy,
            now);
    }

    private static bool IsAllowed(
        ProspectStatus current,
        ProspectStatus target) =>
        current switch
        {
            ProspectStatus.New =>
                target is ProspectStatus.Contacted
                    or ProspectStatus.Qualified
                    or ProspectStatus.Lost
                    or ProspectStatus.Archived,
            ProspectStatus.Contacted =>
                target is ProspectStatus.Qualified
                    or ProspectStatus.Opportunity
                    or ProspectStatus.Lost
                    or ProspectStatus.Archived,
            ProspectStatus.Qualified =>
                target is ProspectStatus.Opportunity
                    or ProspectStatus.Lost
                    or ProspectStatus.Archived,
            ProspectStatus.Opportunity =>
                target is ProspectStatus.ProposalDraft
                    or ProspectStatus.ProposalSent
                    or ProspectStatus.Negotiation
                    or ProspectStatus.Won
                    or ProspectStatus.Lost
                    or ProspectStatus.Archived,
            ProspectStatus.ProposalDraft =>
                target is ProspectStatus.ProposalSent
                    or ProspectStatus.Negotiation
                    or ProspectStatus.Won
                    or ProspectStatus.Lost
                    or ProspectStatus.Archived,
            ProspectStatus.ProposalSent =>
                target is ProspectStatus.Negotiation
                    or ProspectStatus.Won
                    or ProspectStatus.Lost
                    or ProspectStatus.Archived,
            ProspectStatus.Negotiation =>
                target is ProspectStatus.Won
                    or ProspectStatus.Lost
                    or ProspectStatus.Archived,
            ProspectStatus.Lost =>
                target is ProspectStatus.Contacted or ProspectStatus.Archived,
            ProspectStatus.Won => target == ProspectStatus.Archived,
            ProspectStatus.Archived => false,
            _ => false
        };
}
