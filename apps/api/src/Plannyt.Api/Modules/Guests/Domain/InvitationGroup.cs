using Plannyt.Api.BuildingBlocks.Domain;

namespace Plannyt.Api.Modules.Guests.Domain;

public sealed class InvitationGroup : ITenantEntity
{
    private InvitationGroup()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EventId { get; private set; }
    public InvitationGroupType GroupType { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? ContactEmail { get; private set; }
    public int AllowedGuestCount { get; private set; }
    public bool AllowUnnamedCompanions { get; private set; }
    public int MaxUnnamedCompanions { get; private set; }
    public InvitationGroupStatus Status { get; private set; }
    public string Source { get; private set; } = "Manual";
    public string? InternalNotes { get; private set; }
    public bool CapacityOverrideApplied { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public static InvitationGroup Create(
        Guid organizationId,
        Guid eventId,
        InvitationGroupType groupType,
        string displayName,
        string? contactName,
        string? contactPhone,
        string? contactEmail,
        int allowedGuestCount,
        bool allowUnnamedCompanions,
        int maxUnnamedCompanions,
        string source,
        string? internalNotes,
        Guid createdBy,
        DateTimeOffset now)
    {
        ValidateCapacity(allowedGuestCount, allowUnnamedCompanions, maxUnnamedCompanions);
        return new InvitationGroup
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            EventId = eventId,
            GroupType = groupType,
            DisplayName = displayName,
            ContactName = contactName,
            ContactPhone = contactPhone,
            ContactEmail = contactEmail,
            AllowedGuestCount = allowedGuestCount,
            AllowUnnamedCompanions = allowUnnamedCompanions,
            MaxUnnamedCompanions = allowUnnamedCompanions ? maxUnnamedCompanions : 0,
            Status = InvitationGroupStatus.Draft,
            Source = source,
            InternalNotes = internalNotes,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        InvitationGroupType groupType,
        string displayName,
        string? contactName,
        string? contactPhone,
        string? contactEmail,
        int allowedGuestCount,
        bool allowUnnamedCompanions,
        int maxUnnamedCompanions,
        string? internalNotes,
        int activeNamedGuests,
        bool applyCapacityOverride,
        DateTimeOffset now)
    {
        EnsureActive();
        ValidateCapacity(allowedGuestCount, allowUnnamedCompanions, maxUnnamedCompanions);
        if (activeNamedGuests > allowedGuestCount && !applyCapacityOverride)
        {
            throw new DomainRuleException(
                "La capacidad no puede ser menor que los invitados activos del grupo.");
        }

        if (maxUnnamedCompanions > Math.Max(0, allowedGuestCount - activeNamedGuests)
            && !applyCapacityOverride)
        {
            throw new DomainRuleException(
                "Los acompañantes sin nombre exceden los lugares disponibles.");
        }

        GroupType = groupType;
        DisplayName = displayName;
        ContactName = contactName;
        ContactPhone = contactPhone;
        ContactEmail = contactEmail;
        AllowedGuestCount = allowedGuestCount;
        AllowUnnamedCompanions = allowUnnamedCompanions;
        MaxUnnamedCompanions = allowUnnamedCompanions ? maxUnnamedCompanions : 0;
        InternalNotes = internalNotes;
        CapacityOverrideApplied = applyCapacityOverride
            && (activeNamedGuests > allowedGuestCount
                || maxUnnamedCompanions > Math.Max(0, allowedGuestCount - activeNamedGuests));
        UpdatedAt = now;
    }

    public void ChangeStatus(InvitationGroupStatus status, DateTimeOffset now)
    {
        EnsureActive();
        if (status == InvitationGroupStatus.Archived)
        {
            throw new DomainRuleException("Usa la operación de archivo para archivar el grupo.");
        }

        Status = status;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = InvitationGroupStatus.Archived;
        ArchivedAt = now;
        UpdatedAt = now;
    }

    private void EnsureActive()
    {
        if (ArchivedAt is not null)
        {
            throw new DomainRuleException("Un grupo archivado no admite cambios.");
        }
    }

    private static void ValidateCapacity(
        int allowedGuestCount,
        bool allowUnnamedCompanions,
        int maxUnnamedCompanions)
    {
        if (allowedGuestCount < 1)
        {
            throw new DomainRuleException("La capacidad del grupo debe ser al menos uno.");
        }

        if (maxUnnamedCompanions < 0
            || (!allowUnnamedCompanions && maxUnnamedCompanions != 0)
            || maxUnnamedCompanions > allowedGuestCount)
        {
            throw new DomainRuleException(
                "La cantidad de acompañantes sin nombre no es válida.");
        }
    }
}
