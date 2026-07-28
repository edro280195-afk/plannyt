namespace Plannyt.Api.BuildingBlocks.Domain;

public interface ITenantEntity
{
    Guid OrganizationId { get; }
}
