using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Guests.Application;

public sealed class GuestService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    GuestPlanLimitService planLimitService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<GuestDashboardResponse> GetDashboardAsync(
        Guid organizationId,
        Guid eventId,
        string? search,
        Guid? groupId,
        Guid? tagId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsView,
            cancellationToken);
        var groups = await BuildGroupResponsesAsync(
            organizationId,
            eventId,
            includeArchived,
            cancellationToken);
        var guestQuery = dbContext.EventGuests
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId);
        if (!includeArchived)
        {
            guestQuery = guestQuery.Where(entity => entity.ArchivedAt == null);
        }

        if (groupId is not null)
        {
            guestQuery = guestQuery.Where(entity => entity.InvitationGroupId == groupId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            guestQuery = guestQuery.Where(entity =>
                entity.FirstName.ToLower().Contains(term)
                || entity.LastName.ToLower().Contains(term)
                || (entity.Email != null && entity.Email.ToLower().Contains(term))
                || (entity.Phone != null && entity.Phone.Contains(term)));
        }

        if (tagId is not null)
        {
            var taggedGroups = dbContext.InvitationGroupTags
                .Where(entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId
                    && entity.GuestTagId == tagId)
                .Select(entity => entity.InvitationGroupId);
            guestQuery = guestQuery.Where(entity =>
                entity.InvitationGroupId != null
                && taggedGroups.Contains(entity.InvitationGroupId.Value));
        }

        var guests = await guestQuery
            .OrderBy(entity => entity.InvitationGroupId)
            .ThenByDescending(entity => entity.IsPrimaryContact)
            .ThenBy(entity => entity.SortOrder)
            .ThenBy(entity => entity.FirstName)
            .Select(entity => ToGuestResponse(entity))
            .ToListAsync(cancellationToken);
        var plan = await planLimitService.GetUsageAsync(
            organizationId,
            eventId,
            cancellationToken);
        var linkCounts = await dbContext.GuestAccessLinks
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Status == Invitations.Domain.GuestAccessLinkStatus.Active)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Opened = group.Count(entity => entity.OpenCount > 0)
            })
            .SingleOrDefaultAsync(cancellationToken);
        return new GuestDashboardResponse(
            plan.ActiveGuests,
            groups.Count(group => group.ArchivedAt is null),
            linkCounts?.Total ?? 0,
            linkCounts?.Opened ?? 0,
            plan,
            groups,
            guests);
    }

    public async Task<InvitationGroupResponse> CreateGroupAsync(
        Guid organizationId,
        Guid eventId,
        InvitationGroupRequest request,
        string source,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationGroupsCreate,
            cancellationToken);
        GuestRequestValidator.Validate(request);
        await EnsureTagsAsync(
            organizationId,
            eventId,
            request.TagIds,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var group = InvitationGroup.Create(
            organizationId,
            eventId,
            request.GroupType,
            request.DisplayName.Trim(),
            Normalize(request.ContactName),
            GuestRequestValidator.NormalizePhone(request.ContactPhone),
            GuestRequestValidator.NormalizeEmail(request.ContactEmail),
            request.AllowedGuestCount,
            request.AllowUnnamedCompanions,
            request.MaxUnnamedCompanions,
            source,
            Normalize(request.InternalNotes),
            access.UserAccountId,
            now);
        dbContext.InvitationGroups.Add(group);
        dbContext.InvitationGroupTags.AddRange(request.TagIds.Distinct().Select(
            tagId => InvitationGroupTag.Create(
                organizationId,
                eventId,
                group.Id,
                tagId)));
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation_group.created",
            nameof(InvitationGroup),
            group.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildGroupResponsesAsync(
            organizationId,
            eventId,
            true,
            cancellationToken)).Single(item => item.Id == group.Id);
    }

    public async Task<InvitationGroupResponse> UpdateGroupAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        InvitationGroupRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationGroupsUpdate,
            cancellationToken);
        GuestRequestValidator.Validate(request);
        await EnsureTagsAsync(
            organizationId,
            eventId,
            request.TagIds,
            cancellationToken);
        var group = await FindGroupAsync(
            organizationId,
            eventId,
            groupId,
            cancellationToken);
        var activeNamedGuests = await dbContext.EventGuests.CountAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId
                && entity.IsActive
                && entity.ArchivedAt == null,
            cancellationToken);
        group.Update(
            request.GroupType,
            request.DisplayName.Trim(),
            Normalize(request.ContactName),
            GuestRequestValidator.NormalizePhone(request.ContactPhone),
            GuestRequestValidator.NormalizeEmail(request.ContactEmail),
            request.AllowedGuestCount,
            request.AllowUnnamedCompanions,
            request.MaxUnnamedCompanions,
            Normalize(request.InternalNotes),
            activeNamedGuests,
            request.ApplyCapacityOverride,
            timeProvider.GetUtcNow());
        var oldTags = await dbContext.InvitationGroupTags.Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId)
            .ToListAsync(cancellationToken);
        dbContext.InvitationGroupTags.RemoveRange(oldTags);
        dbContext.InvitationGroupTags.AddRange(request.TagIds.Distinct().Select(
            tag => InvitationGroupTag.Create(organizationId, eventId, groupId, tag)));
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            request.ApplyCapacityOverride
                ? "invitation_group.capacity_overridden"
                : "invitation_group.updated",
            nameof(InvitationGroup),
            group.Id,
            request.ApplyCapacityOverride
                ? new Dictionary<string, object?>
                {
                    ["allowedGuestCount"] = request.AllowedGuestCount,
                    ["activeNamedGuests"] = activeNamedGuests
                }
                : null);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await BuildGroupResponsesAsync(
            organizationId,
            eventId,
            true,
            cancellationToken)).Single(item => item.Id == groupId);
    }

    public async Task ArchiveGroupAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.InvitationGroupsArchive,
            cancellationToken);
        var group = await FindGroupAsync(
            organizationId,
            eventId,
            groupId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        group.Archive(now);
        var guests = await dbContext.EventGuests.Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId
                && entity.ArchivedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var guest in guests)
        {
            guest.Archive(now);
        }

        var links = await dbContext.GuestAccessLinks.Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId
                && entity.Status == Invitations.Domain.GuestAccessLinkStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var link in links)
        {
            link.Revoke(now);
        }

        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "invitation_group.archived",
            nameof(InvitationGroup),
            group.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EventGuestResponse> CreateGuestAsync(
        Guid organizationId,
        Guid eventId,
        EventGuestRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsCreate,
            cancellationToken);
        GuestRequestValidator.Validate(request);
        await planLimitService.EnsureCapacityAsync(
            organizationId,
            eventId,
            1,
            cancellationToken);
        await ValidateGuestGroupAsync(
            organizationId,
            eventId,
            null,
            request.InvitationGroupId,
            request.IsPrimaryContact,
            cancellationToken);
        var guest = EventGuest.Create(
            organizationId,
            eventId,
            request.InvitationGroupId,
            request.PersonId,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            GuestRequestValidator.NormalizeEmail(request.Email),
            GuestRequestValidator.NormalizePhone(request.Phone),
            request.GuestType,
            request.AgeCategory,
            request.IsPrimaryContact,
            request.IsNamed,
            request.IsPlusOne,
            request.IsVip,
            request.SortOrder,
            Normalize(request.InternalNotes),
            access.UserAccountId,
            timeProvider.GetUtcNow());
        dbContext.EventGuests.Add(guest);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest.created",
            nameof(EventGuest),
            guest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToGuestResponse(guest);
    }

    public async Task<EventGuestResponse> UpdateGuestAsync(
        Guid organizationId,
        Guid eventId,
        Guid guestId,
        EventGuestRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsUpdate,
            cancellationToken);
        GuestRequestValidator.Validate(request);
        var guest = await FindGuestAsync(
            organizationId,
            eventId,
            guestId,
            cancellationToken);
        await ValidateGuestGroupAsync(
            organizationId,
            eventId,
            guestId,
            request.InvitationGroupId,
            request.IsPrimaryContact,
            cancellationToken);
        guest.Update(
            request.InvitationGroupId,
            request.PersonId,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            GuestRequestValidator.NormalizeEmail(request.Email),
            GuestRequestValidator.NormalizePhone(request.Phone),
            request.GuestType,
            request.AgeCategory,
            request.IsPrimaryContact,
            request.IsNamed,
            request.IsPlusOne,
            request.IsVip,
            request.SortOrder,
            Normalize(request.InternalNotes),
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest.updated",
            nameof(EventGuest),
            guest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToGuestResponse(guest);
    }

    public async Task ArchiveGuestAsync(
        Guid organizationId,
        Guid eventId,
        Guid guestId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsArchive,
            cancellationToken);
        var guest = await FindGuestAsync(
            organizationId,
            eventId,
            guestId,
            cancellationToken);
        guest.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest.archived",
            nameof(EventGuest),
            guest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GuestTagResponse>> GetTagsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsView,
            cancellationToken);
        return await dbContext.GuestTags.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null)
            .OrderBy(entity => entity.Name)
            .Select(entity => new GuestTagResponse(
                entity.Id,
                entity.Name,
                entity.ColorToken))
            .ToListAsync(cancellationToken);
    }

    public async Task<GuestTagResponse> CreateTagAsync(
        Guid organizationId,
        Guid eventId,
        GuestTagRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsManageTags,
            cancellationToken);
        GuestRequestValidator.Validate(request);
        var normalizedName = request.Name.Trim();
        if (await dbContext.GuestTags.AnyAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null
                && entity.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken))
        {
            throw new ConflictException("Ya existe una etiqueta con ese nombre.");
        }

        var tag = GuestTag.Create(
            organizationId,
            eventId,
            normalizedName,
            request.ColorToken,
            timeProvider.GetUtcNow());
        dbContext.GuestTags.Add(tag);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest_tag.created",
            nameof(GuestTag),
            tag.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new GuestTagResponse(tag.Id, tag.Name, tag.ColorToken);
    }

    public async Task ArchiveTagAsync(
        Guid organizationId,
        Guid eventId,
        Guid tagId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsManageTags,
            cancellationToken);
        var tag = await dbContext.GuestTags.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == tagId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la etiqueta.");
        tag.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest_tag.archived",
            nameof(GuestTag),
            tag.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GuestTagResponse> UpdateTagAsync(
        Guid organizationId,
        Guid eventId,
        Guid tagId,
        GuestTagRequest request,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsManageTags,
            cancellationToken);
        GuestRequestValidator.Validate(request);
        var tag = await dbContext.GuestTags.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == tagId
                && entity.ArchivedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("No se encontrÃ³ la etiqueta.");
        var normalizedName = request.Name.Trim();
        if (await dbContext.GuestTags.AsNoTracking().AnyAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id != tagId
                && entity.ArchivedAt == null
                && entity.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken))
        {
            throw new ConflictException("Ya existe una etiqueta con ese nombre.");
        }

        tag.Update(normalizedName, request.ColorToken);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "guest_tag.updated",
            nameof(GuestTag),
            tag.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new GuestTagResponse(tag.Id, tag.Name, tag.ColorToken);
    }

    public async Task<IReadOnlyList<GuestDuplicateSuggestionResponse>>
        GetDuplicateSuggestionsAsync(
            Guid organizationId,
            Guid eventId,
            CancellationToken cancellationToken)
    {
        await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestsView,
            cancellationToken);
        var guests = await dbContext.EventGuests.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null)
            .ToListAsync(cancellationToken);
        var suggestions = new List<GuestDuplicateSuggestionResponse>();
        AddExactDuplicates(
            guests,
            guest => GuestRequestValidator.NormalizeEmail(guest.Email),
            "Correo repetido",
            "email",
            suggestions);
        AddExactDuplicates(
            guests,
            guest => GuestRequestValidator.NormalizePhone(guest.Phone),
            "Teléfono repetido",
            "phone",
            suggestions);
        var names = guests
            .Where(guest => guest.InvitationGroupId is not null)
            .GroupBy(guest => new
            {
                guest.InvitationGroupId,
                Name = $"{guest.FirstName} {guest.LastName}".Trim().ToLowerInvariant()
            })
            .Where(group => group.Key.Name.Length > 0 && group.Count() > 1);
        suggestions.AddRange(names.Select(group =>
            new GuestDuplicateSuggestionResponse(
                "name",
                "Nombre similar dentro del mismo grupo",
                group.Select(guest => guest.Id).ToList(),
                "Revisar, editar o mover; no se fusionará automáticamente.")));
        return suggestions;
    }

    private static void AddExactDuplicates(
        IEnumerable<EventGuest> guests,
        Func<EventGuest, string?> selector,
        string reason,
        string kind,
        ICollection<GuestDuplicateSuggestionResponse> suggestions)
    {
        var duplicates = guests
            .Select(guest => new { Guest = guest, Value = selector(guest) })
            .Where(item => item.Value is not null)
            .GroupBy(item => item.Value, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);
        foreach (var duplicate in duplicates)
        {
            suggestions.Add(new GuestDuplicateSuggestionResponse(
                kind,
                reason,
                duplicate.Select(item => item.Guest.Id).ToList(),
                "Ignorar, editar o mover; no se fusionará automáticamente."));
        }
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
        if (!await dbContext.Events.AsNoTracking().AnyAsync(entity =>
                entity.OrganizationId == organizationId && entity.Id == eventId,
                cancellationToken))
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        return access;
    }

    private async Task ValidateGuestGroupAsync(
        Guid organizationId,
        Guid eventId,
        Guid? currentGuestId,
        Guid? groupId,
        bool isPrimary,
        CancellationToken cancellationToken)
    {
        if (groupId is null)
        {
            if (isPrimary)
            {
                throw new RequestValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["isPrimaryContact"] =
                        [
                            "El contacto principal debe pertenecer a un grupo."
                        ]
                    });
            }

            return;
        }

        var group = await FindGroupAsync(
            organizationId,
            eventId,
            groupId.Value,
            cancellationToken);
        var existingCount = await dbContext.EventGuests.CountAsync(entity =>
            entity.OrganizationId == organizationId
            && entity.EventId == eventId
            && entity.InvitationGroupId == groupId
            && entity.ArchivedAt == null
            && entity.Id != currentGuestId,
            cancellationToken);
        if (existingCount >= group.AllowedGuestCount)
        {
            throw new ConflictException("El grupo ya alcanzó su capacidad.");
        }

        if (isPrimary && await dbContext.EventGuests.AnyAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId
                && entity.IsPrimaryContact
                && entity.ArchivedAt == null
                && entity.Id != currentGuestId,
                cancellationToken))
        {
            throw new ConflictException("El grupo ya tiene un contacto principal activo.");
        }
    }

    private async Task EnsureTagsAsync(
        Guid organizationId,
        Guid eventId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken)
    {
        var distinct = tagIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return;
        }

        var count = await dbContext.GuestTags.CountAsync(entity =>
            entity.OrganizationId == organizationId
            && entity.EventId == eventId
            && entity.ArchivedAt == null
            && distinct.Contains(entity.Id),
            cancellationToken);
        if (count != distinct.Count)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["tagIds"] = ["Todas las etiquetas deben pertenecer al evento."]
                });
        }
    }

    private async Task<InvitationGroup> FindGroupAsync(
        Guid organizationId,
        Guid eventId,
        Guid groupId,
        CancellationToken cancellationToken) =>
        await dbContext.InvitationGroups.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == groupId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el grupo de invitación.");

    private async Task<EventGuest> FindGuestAsync(
        Guid organizationId,
        Guid eventId,
        Guid guestId,
        CancellationToken cancellationToken) =>
        await dbContext.EventGuests.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == guestId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el invitado.");

    private async Task<IReadOnlyList<InvitationGroupResponse>>
        BuildGroupResponsesAsync(
            Guid organizationId,
            Guid eventId,
            bool includeArchived,
            CancellationToken cancellationToken)
    {
        var query = dbContext.InvitationGroups.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId);
        if (!includeArchived)
        {
            query = query.Where(entity => entity.ArchivedAt == null);
        }

        var groups = await query.OrderBy(entity => entity.DisplayName)
            .ToListAsync(cancellationToken);
        var groupIds = groups.Select(group => group.Id).ToList();
        var counts = await dbContext.EventGuests.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId != null
                && groupIds.Contains(entity.InvitationGroupId.Value)
                && entity.ArchivedAt == null)
            .GroupBy(entity => entity.InvitationGroupId!.Value)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);
        var tagRows = await dbContext.InvitationGroupTags.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && groupIds.Contains(entity.InvitationGroupId))
            .Join(
                dbContext.GuestTags.AsNoTracking(),
                relation => new { relation.OrganizationId, relation.GuestTagId },
                tag => new { tag.OrganizationId, GuestTagId = tag.Id },
                (relation, tag) => new
                {
                    relation.InvitationGroupId,
                    Tag = new GuestTagResponse(tag.Id, tag.Name, tag.ColorToken)
                })
            .ToListAsync(cancellationToken);
        return groups.Select(group =>
        {
            var named = counts.GetValueOrDefault(group.Id);
            return new InvitationGroupResponse(
                group.Id,
                group.GroupType,
                group.DisplayName,
                group.ContactName,
                group.ContactPhone,
                group.ContactEmail,
                group.AllowedGuestCount,
                named,
                Math.Max(0, group.AllowedGuestCount - named),
                group.AllowUnnamedCompanions,
                group.MaxUnnamedCompanions,
                group.Status,
                group.Source,
                group.InternalNotes,
                group.CapacityOverrideApplied,
                tagRows.Where(item => item.InvitationGroupId == group.Id)
                    .Select(item => item.Tag)
                    .OrderBy(tag => tag.Name)
                    .ToList(),
                group.UpdatedAt,
                group.ArchivedAt);
        }).ToList();
    }

    private static EventGuestResponse ToGuestResponse(EventGuest guest) =>
        new(
            guest.Id,
            guest.InvitationGroupId,
            guest.PersonId,
            guest.FirstName,
            guest.LastName,
            guest.Email,
            guest.Phone,
            guest.GuestType,
            guest.AgeCategory,
            guest.IsPrimaryContact,
            guest.IsNamed,
            guest.IsPlusOne,
            guest.IsVip,
            guest.IsActive,
            guest.SortOrder,
            guest.InternalNotes,
            guest.UpdatedAt,
            guest.ArchivedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
