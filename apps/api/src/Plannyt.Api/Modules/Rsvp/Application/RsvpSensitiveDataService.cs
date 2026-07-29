using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed class RsvpSensitiveDataService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService)
{
    public async Task<IReadOnlyList<SensitiveGuestDataResponse>> GetAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestSensitiveDataView,
            cancellationToken);
        var result = await (
                from sensitive in dbContext.GuestDietaryAndAccessibilities
                    .AsNoTracking()
                join guest in dbContext.EventGuests.AsNoTracking()
                    on new
                    {
                        sensitive.OrganizationId,
                        sensitive.EventId,
                        Id = sensitive.EventGuestId
                    }
                    equals new
                    {
                        guest.OrganizationId,
                        guest.EventId,
                        guest.Id
                    }
                where sensitive.OrganizationId == organizationId
                      && sensitive.EventId == eventId
                orderby guest.SortOrder
                select new SensitiveGuestDataResponse(
                    guest.Id,
                    (guest.FirstName + " " + guest.LastName).Trim(),
                    sensitive.Allergies,
                    sensitive.DietaryRestrictions,
                    sensitive.AccessibilityRequirements,
                    sensitive.AdditionalNotes,
                    sensitive.ConsentGrantedAt,
                    sensitive.UpdatedAt))
            .ToListAsync(cancellationToken);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.GuestSensitiveDataViewed,
            nameof(GuestDietaryAndAccessibility),
            eventId,
            Metadata(result.Count, "view"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private async Task<TenantAccess> RequireEventAsync(
        Guid organizationId,
        Guid eventId,
        string permission,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            permission,
            eventId,
            cancellationToken);
        if (!await dbContext.Events.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId,
                cancellationToken))
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        return access;
    }

    private static IReadOnlyDictionary<string, object?> Metadata(
        int recordCount,
        string operationType) =>
        new Dictionary<string, object?>
        {
            ["recordCount"] = recordCount,
            ["operationType"] = operationType
        };
}
