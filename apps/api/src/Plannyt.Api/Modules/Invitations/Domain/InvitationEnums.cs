namespace Plannyt.Api.Modules.Invitations.Domain;

public enum GuestExperienceStatus
{
    Draft,
    Ready,
    Published,
    Suspended,
    Archived
}

public enum InvitationDesignStatus
{
    Draft,
    InReview,
    ChangesRequested,
    Approved,
    Published,
    Archived
}

public enum InvitationBlockType
{
    Cover,
    Greeting,
    Participants,
    EventDate,
    Countdown,
    Story,
    Image,
    GalleryPreview,
    Text,
    Divider,
    DressCode,
    Contact,
    CustomButton,
    Footer
}

public enum BlockVisibility
{
    Everyone,
    InvitationGroup,
    HasTag,
    GuestType,
    VipOnly
}

public enum InvitationAnimationLevel
{
    None,
    Reduced,
    Standard
}

public enum GuestAccessLinkStatus
{
    Active,
    Revoked,
    Expired,
    Replaced
}

public enum InvitationReviewDecision
{
    Comment,
    Approved,
    ChangesRequested
}
