using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Guests.Domain;

public sealed class EventGuest : ITenantEntity
{
    private EventGuest()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid? InvitationGroupId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public GuestType GuestType { get; private set; }
    public AgeCategory AgeCategory { get; private set; }
    public bool IsPrimaryContact { get; private set; }
    public bool IsNamed { get; private set; }
    public bool IsPlusOne { get; private set; }
    public bool IsVip { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public string? InternalNotes { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public static EventGuest Create(
        Guid organizationId,
        Guid eventId,
        Guid? invitationGroupId,
        Guid? personId,
        string firstName,
        string lastName,
        string? email,
        string? phone,
        GuestType guestType,
        AgeCategory ageCategory,
        bool isPrimaryContact,
        bool isNamed,
        bool isPlusOne,
        bool isVip,
        int sortOrder,
        string? internalNotes,
        Guid createdBy,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            InvitationGroupId = invitationGroupId,
            PersonId = personId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            GuestType = guestType,
            AgeCategory = ageCategory,
            IsPrimaryContact = isPrimaryContact,
            IsNamed = isNamed,
            IsPlusOne = isPlusOne,
            IsVip = isVip,
            IsActive = true,
            SortOrder = sortOrder,
            InternalNotes = internalNotes,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void Update(
        Guid? invitationGroupId,
        Guid? personId,
        string firstName,
        string lastName,
        string? email,
        string? phone,
        GuestType guestType,
        AgeCategory ageCategory,
        bool isPrimaryContact,
        bool isNamed,
        bool isPlusOne,
        bool isVip,
        int sortOrder,
        string? internalNotes,
        DateTimeOffset now)
    {
        if (ArchivedAt is not null)
        {
            throw new DomainRuleException("Un invitado archivado no admite cambios.");
        }

        InvitationGroupId = invitationGroupId;
        PersonId = personId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        GuestType = guestType;
        AgeCategory = ageCategory;
        IsPrimaryContact = isPrimaryContact;
        IsNamed = isNamed;
        IsPlusOne = isPlusOne;
        IsVip = isVip;
        SortOrder = sortOrder;
        InternalNotes = internalNotes;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        IsActive = false;
        IsPrimaryContact = false;
        ArchivedAt = now;
        UpdatedAt = now;
    }
}
