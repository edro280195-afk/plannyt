using System.ComponentModel.DataAnnotations;
using Plannyt.Api.Modules.Guests.Domain;

namespace Plannyt.Api.BuildingBlocks.Configuration;

public sealed class GuestPlanOptions
{
    public const string SectionName = "GuestPlan";

    [EnumDataType(typeof(GuestPlanTier))]
    public GuestPlanTier DefaultTier { get; init; } = GuestPlanTier.Community;

    public Dictionary<Guid, GuestPlanTier> OrganizationOverrides { get; init; } = [];
}
