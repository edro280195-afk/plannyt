namespace Plannyt.Api.Modules.Guests.Domain;

public enum GuestType
{
    Standard,
    Family,
    Friend,
    Colleague,
    Vendor,
    WeddingParty,
    SponsorOrGodparent,
    StaffGuest,
    VendorGuest,
    Vip,
    Other
}

public enum AgeCategory
{
    Adult,
    Teen,
    Child,
    Infant,
    Unknown
}

public enum InvitationGroupType
{
    Individual,
    Couple,
    Family,
    Group,
    Company,
    CorporateTable,
    Other
}

public enum InvitationGroupStatus
{
    Draft,
    Ready,
    LinkGenerated,
    SharedManually,
    Opened,
    Revoked,
    Archived
}

public enum GuestImportStatus
{
    Analyzed,
    Completed,
    Failed
}

public enum GuestPlanTier
{
    Community,
    EventComplete,
    PlannerPro
}
