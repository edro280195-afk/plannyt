namespace Plannyt.Api.Modules.Organizations.Domain;

public enum OrganizationType
{
    IndependentPlanner,
    Agency
}

public enum OrganizationStatus
{
    Active,
    Suspended,
    Archived
}

public enum OrganizationRole
{
    Owner,
    OrganizationAdmin,
    Planner,
    Coordinator,
    Assistant,
    Commercial,
    Finance
}

public enum MembershipStatus
{
    Invited,
    Active,
    Suspended,
    Revoked
}

public enum PermissionEffect
{
    Allow,
    Deny
}

public enum PermissionScope
{
    Organization,
    Event
}
