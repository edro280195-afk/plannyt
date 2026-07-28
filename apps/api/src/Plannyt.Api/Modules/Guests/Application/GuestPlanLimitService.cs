using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Guests.Domain;

namespace Plannyt.Api.Modules.Guests.Application;

public sealed class GuestPlanLimitService(
    PlannytDbContext dbContext,
    IOptions<GuestPlanOptions> options)
{
    private static readonly IReadOnlyDictionary<GuestPlanTier, int> Limits =
        new Dictionary<GuestPlanTier, int>
        {
            [GuestPlanTier.Community] = 100,
            [GuestPlanTier.EventComplete] = 300,
            [GuestPlanTier.PlannerPro] = 500
        };

    public async Task<GuestPlanUsageResponse> GetUsageAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var activeGuests = await dbContext.EventGuests.CountAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.IsActive
                && entity.ArchivedAt == null,
            cancellationToken);
        return Build(organizationId, activeGuests);
    }

    public async Task EnsureCapacityAsync(
        Guid organizationId,
        Guid eventId,
        int additionalGuests,
        CancellationToken cancellationToken)
    {
        var usage = await GetUsageAsync(organizationId, eventId, cancellationToken);
        if (usage.ActiveGuests + additionalGuests > usage.Limit)
        {
            throw new ConflictException(
                $"El plan {usage.Tier} permite hasta {usage.Limit} invitados activos por evento.");
        }
    }

    public static int GetLimit(GuestPlanTier tier) => Limits[tier];

    private GuestPlanUsageResponse Build(Guid organizationId, int activeGuests)
    {
        var tier = options.Value.OrganizationOverrides.GetValueOrDefault(
            organizationId,
            options.Value.DefaultTier);
        var limit = Limits[tier];
        var percentage = (int)Math.Floor(activeGuests * 100m / limit);
        return new GuestPlanUsageResponse(
            tier,
            activeGuests,
            limit,
            percentage,
            percentage >= 80,
            percentage >= 90,
            activeGuests >= limit);
    }
}
