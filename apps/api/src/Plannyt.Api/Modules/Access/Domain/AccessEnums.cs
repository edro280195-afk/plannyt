namespace Plannyt.Api.Modules.Access.Domain;

public enum EventAccessRole
{
    ClientAuthority,
    ClientPrimary,
    ClientCollaborator,
    ClientGuestManager,
    ClientPayer,
    ClientApprover,
    ClientViewer
}

public enum EventAccessStatus
{
    Invited,
    Active,
    Suspended,
    Revoked
}

public enum InvitationType
{
    OrganizationMembership,
    EventAccess
}
