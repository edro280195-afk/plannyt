using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Plannyt.Api.BuildingBlocks.Configuration;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Guests.Application;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Invitations.Security;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Invitations.Application;

public sealed class PortalGuestCollaborationService(
    PlannytDbContext dbContext,
    PortalAccessService portalAccessService,
    GuestPlanLimitService planLimitService,
    GuestAccessTokenService tokenService,
    IOptions<FrontendOptions> frontendOptions,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<PortalGuestWorkspaceResponse> GetAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsView,
            cancellationToken);
        var groups = await dbContext.InvitationGroups.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null)
            .OrderBy(entity => entity.DisplayName)
            .ToListAsync(cancellationToken);
        var guests = await dbContext.EventGuests.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null)
            .OrderBy(entity => entity.InvitationGroupId)
            .ThenByDescending(entity => entity.IsPrimaryContact)
            .ThenBy(entity => entity.SortOrder)
            .ToListAsync(cancellationToken);
        var design = await dbContext.InvitationDesigns.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.ArchivedAt == null)
            .OrderByDescending(entity => entity.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return new PortalGuestWorkspaceResponse(
            eventId,
            groups.Select(group => new PortalInvitationGroupResponse(
                group.Id,
                Enum.Parse<InvitationGroupTypeProjection>(group.GroupType.ToString()),
                group.DisplayName,
                group.AllowedGuestCount,
                guests.Count(guest => guest.InvitationGroupId == group.Id),
                group.AllowUnnamedCompanions,
                group.MaxUnnamedCompanions)).ToList(),
            guests.Select(ToPortalGuest).ToList(),
            design is null
                ? null
                : await BuildDesignResponseAsync(design, cancellationToken));
    }

    public async Task<PortalInvitationGroupResponse> CreateGroupAsync(
        Guid eventId,
        PortalInvitationGroupRequest request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.InvitationGroupsCreate,
            cancellationToken);
        ValidateGroup(request, 0);
        var group = InvitationGroup.Create(
            access.OrganizationId,
            eventId,
            Enum.Parse<InvitationGroupType>(request.GroupType.ToString()),
            request.DisplayName.Trim(),
            null,
            null,
            null,
            request.AllowedGuestCount,
            request.AllowUnnamedCompanions,
            request.MaxUnnamedCompanions,
            "ClientPortal",
            null,
            access.UserAccountId,
            timeProvider.GetUtcNow());
        dbContext.InvitationGroups.Add(group);
        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            "portal.invitation_group.created",
            nameof(InvitationGroup),
            group.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPortalGroup(group, 0);
    }

    public async Task<PortalInvitationGroupResponse> UpdateGroupAsync(
        Guid eventId,
        Guid groupId,
        PortalInvitationGroupRequest request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.InvitationGroupsUpdate,
            cancellationToken);
        var group = await FindGroupAsync(access, groupId, cancellationToken);
        var count = await dbContext.EventGuests.CountAsync(entity =>
            entity.OrganizationId == access.OrganizationId
            && entity.EventId == eventId
            && entity.InvitationGroupId == groupId
            && entity.ArchivedAt == null,
            cancellationToken);
        ValidateGroup(request, count);
        group.Update(
            Enum.Parse<InvitationGroupType>(request.GroupType.ToString()),
            request.DisplayName.Trim(),
            group.ContactName,
            group.ContactPhone,
            group.ContactEmail,
            request.AllowedGuestCount,
            request.AllowUnnamedCompanions,
            request.MaxUnnamedCompanions,
            group.InternalNotes,
            count,
            false,
            timeProvider.GetUtcNow());
        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            "portal.invitation_group.updated",
            nameof(InvitationGroup),
            group.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPortalGroup(group, count);
    }

    public async Task ArchiveGroupAsync(
        Guid eventId,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.InvitationGroupsArchive,
            cancellationToken);
        var group = await FindGroupAsync(access, groupId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        group.Archive(now);
        var guests = await dbContext.EventGuests.Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.InvitationGroupId == groupId
                && entity.ArchivedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var guest in guests)
        {
            guest.Archive(now);
        }

        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            "portal.invitation_group.archived",
            nameof(InvitationGroup),
            group.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PortalGuestResponse> CreateGuestAsync(
        Guid eventId,
        PortalGuestRequest request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsCreate,
            cancellationToken);
        ValidateGuest(request);
        await planLimitService.EnsureCapacityAsync(
            access.OrganizationId,
            eventId,
            1,
            cancellationToken);
        await ValidateGroupMembershipAsync(
            access,
            null,
            request.InvitationGroupId,
            request.IsPrimaryContact,
            cancellationToken);
        var guest = EventGuest.Create(
            access.OrganizationId,
            eventId,
            request.InvitationGroupId,
            null,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            null,
            null,
            Enum.Parse<GuestType>(request.GuestType.ToString()),
            Enum.Parse<AgeCategory>(request.AgeCategory.ToString()),
            request.IsPrimaryContact,
            true,
            false,
            request.IsVip,
            request.SortOrder,
            null,
            access.UserAccountId,
            timeProvider.GetUtcNow());
        dbContext.EventGuests.Add(guest);
        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            "portal.guest.created",
            nameof(EventGuest),
            guest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPortalGuest(guest);
    }

    public async Task<PortalGuestResponse> UpdateGuestAsync(
        Guid eventId,
        Guid guestId,
        PortalGuestRequest request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsUpdate,
            cancellationToken);
        ValidateGuest(request);
        var guest = await FindGuestAsync(access, guestId, cancellationToken);
        await ValidateGroupMembershipAsync(
            access,
            guestId,
            request.InvitationGroupId,
            request.IsPrimaryContact,
            cancellationToken);
        guest.Update(
            request.InvitationGroupId,
            null,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            null,
            null,
            Enum.Parse<GuestType>(request.GuestType.ToString()),
            Enum.Parse<AgeCategory>(request.AgeCategory.ToString()),
            request.IsPrimaryContact,
            true,
            false,
            request.IsVip,
            request.SortOrder,
            null,
            timeProvider.GetUtcNow());
        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            "portal.guest.updated",
            nameof(EventGuest),
            guest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToPortalGuest(guest);
    }

    public async Task ArchiveGuestAsync(
        Guid eventId,
        Guid guestId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsArchive,
            cancellationToken);
        var guest = await FindGuestAsync(access, guestId, cancellationToken);
        guest.Archive(timeProvider.GetUtcNow());
        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            "portal.guest.archived",
            nameof(EventGuest),
            guest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<InvitationDesignResponse> UpdateDesignAsync(
        Guid eventId,
        Guid designId,
        UpdateInvitationDesignRequest request,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.InvitationDesignsUpdateDraft,
            cancellationToken);
        InvitationContentValidator.Validate(request.Name, request.Theme, request.Blocks);
        var design = await FindDesignAsync(access, designId, cancellationToken);
        design.UpdateDraft(
            request.Name.Trim(),
            InvitationContentValidator.SerializeTheme(request.Theme),
            InvitationContentValidator.SerializeBlocks(request.Blocks),
            timeProvider.GetUtcNow());
        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            "portal.invitation_design.updated",
            nameof(InvitationDesign),
            design.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDesignResponseAsync(design, cancellationToken);
    }

    public async Task<InvitationDesignResponse> ReviewAsync(
        Guid eventId,
        Guid designId,
        Guid versionId,
        InvitationReviewDecision decision,
        InvitationCommentRequest request,
        CancellationToken cancellationToken)
    {
        var permission = decision == InvitationReviewDecision.Comment
            ? Permissions.InvitationDesignsView
            : Permissions.InvitationDesignsApprove;
        var access = await portalAccessService.RequireAsync(
            eventId,
            permission,
            cancellationToken);
        var message = request.Message?.Trim() ?? string.Empty;
        if (decision != InvitationReviewDecision.Approved && message.Length == 0)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["message"] = ["Escribe un comentario para registrar la revisión."]
                });
        }

        if (message.Length > 2000)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["message"] = ["El comentario admite hasta 2,000 caracteres."]
                });
        }

        var design = await FindDesignAsync(access, designId, cancellationToken);
        var version = await dbContext.InvitationDesignVersions.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.InvitationDesignId == designId
                && entity.Id == versionId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró la versión.");
        var now = timeProvider.GetUtcNow();
        if (decision == InvitationReviewDecision.Approved)
        {
            design.Approve(version.Id, now);
            version.MarkApproved(access.UserAccountId, now);
        }
        else if (decision == InvitationReviewDecision.ChangesRequested)
        {
            design.RequestChanges(now);
        }

        dbContext.InvitationDesignComments.Add(InvitationDesignComment.Create(
            version,
            access.UserAccountId,
            decision,
            message.Length == 0 ? "Versión aprobada por el cliente." : message,
            now));
        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            $"portal.invitation_design.review_{decision.ToString().ToLowerInvariant()}",
            nameof(InvitationDesign),
            design.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildDesignResponseAsync(design, cancellationToken);
    }

    public async Task<IReadOnlyList<GuestAccessLinkResponse>> GetLinksAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestLinksView,
            cancellationToken);
        var links = await dbContext.GuestAccessLinks.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId)
            .OrderByDescending(entity => entity.CreatedAt)
            .ToListAsync(cancellationToken);
        return links
            .Select(ToLinkResponse)
            .ToList();
    }

    public async Task<IReadOnlyList<GuestDuplicateSuggestionResponse>>
        GetDuplicateSuggestionsAsync(
            Guid eventId,
            CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestsView,
            cancellationToken);
        var guests = await dbContext.EventGuests.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
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
            "TelÃ©fono repetido",
            "phone",
            suggestions);
        var repeatedNames = guests
            .Where(guest => guest.InvitationGroupId is not null)
            .GroupBy(guest => new
            {
                guest.InvitationGroupId,
                Name = $"{guest.FirstName} {guest.LastName}".Trim().ToLowerInvariant()
            })
            .Where(group => group.Key.Name.Length > 0 && group.Count() > 1);
        suggestions.AddRange(repeatedNames.Select(group =>
            new GuestDuplicateSuggestionResponse(
                "name",
                "Nombre similar dentro del mismo grupo",
                group.Select(guest => guest.Id).ToList(),
                "Ignorar, editar o mover; no se fusionarÃ¡ automÃ¡ticamente.")));
        return suggestions;
    }

    public async Task<GuestAccessLinkResponse> MarkLinkSharedAsync(
        Guid eventId,
        Guid linkId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.GuestLinksMarkShared,
            cancellationToken);
        var link = await dbContext.GuestAccessLinks.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.Id == linkId,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el enlace.");
        if (link.Status != GuestAccessLinkStatus.Active)
        {
            throw new ConflictException("El enlace ya no está activo.");
        }

        link.MarkShared(timeProvider.GetUtcNow());
        auditService.Add(
            access.OrganizationId,
            eventId,
            access.UserAccountId,
            AuditActions.PortalGuestLinkMarkedShared,
            nameof(GuestAccessLink),
            link.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToLinkResponse(link);
    }

    private GuestAccessLinkResponse ToLinkResponse(GuestAccessLink link)
    {
        var publicUrl = link.Status == GuestAccessLinkStatus.Active
                        && !link.IsExpired(timeProvider.GetUtcNow())
            ? $"{frontendOptions.Value.PublicUrl.TrimEnd('/')}/i/"
              + Uri.EscapeDataString(tokenService.Reveal(link.Id, link.DerivationKeyId))
            : null;
        return new GuestAccessLinkResponse(
            link.Id,
            link.InvitationGroupId,
            link.Status,
            publicUrl,
            link.ExpiresAt,
            link.FirstOpenedAt,
            link.LastOpenedAt,
            link.OpenCount,
            link.SharedManuallyAt,
            link.CreatedAt);
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
                "Ignorar, editar o mover; no se fusionarÃ¡ automÃ¡ticamente."));
        }
    }

    private async Task ValidateGroupMembershipAsync(
        PortalEventAccess access,
        Guid? guestId,
        Guid? groupId,
        bool isPrimary,
        CancellationToken cancellationToken)
    {
        if (groupId is null)
        {
            if (isPrimary)
            {
                throw new ConflictException("El contacto principal debe pertenecer a un grupo.");
            }

            return;
        }

        var group = await FindGroupAsync(access, groupId.Value, cancellationToken);
        var count = await dbContext.EventGuests.CountAsync(entity =>
            entity.OrganizationId == access.OrganizationId
            && entity.EventId == access.EventId
            && entity.InvitationGroupId == groupId
            && entity.ArchivedAt == null
            && entity.Id != guestId,
            cancellationToken);
        if (count >= group.AllowedGuestCount)
        {
            throw new ConflictException("El grupo ya alcanzó su capacidad.");
        }

        if (isPrimary && await dbContext.EventGuests.AnyAsync(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == access.EventId
                && entity.InvitationGroupId == groupId
                && entity.IsPrimaryContact
                && entity.ArchivedAt == null
                && entity.Id != guestId,
                cancellationToken))
        {
            throw new ConflictException("El grupo ya tiene un contacto principal.");
        }
    }

    private async Task<InvitationDesignResponse> BuildDesignResponseAsync(
        InvitationDesign design,
        CancellationToken cancellationToken)
    {
        var versions = await dbContext.InvitationDesignVersions.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == design.OrganizationId
                && entity.EventId == design.EventId
                && entity.InvitationDesignId == design.Id)
            .OrderByDescending(entity => entity.VersionNumber)
            .ToListAsync(cancellationToken);
        var comments = await dbContext.InvitationDesignComments.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == design.OrganizationId
                && entity.EventId == design.EventId
                && entity.InvitationDesignId == design.Id)
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new InvitationCommentResponse(
                entity.Id,
                entity.InvitationDesignVersionId,
                entity.Decision,
                entity.Message,
                entity.CreatedAt))
            .ToListAsync(cancellationToken);
        var theme = InvitationContentValidator.DeserializeTheme(design.DraftThemeJson);
        var blocks = InvitationContentValidator.DeserializeBlocks(design.DraftContentJson);
        return new InvitationDesignResponse(
            design.Id,
            design.EventId,
            design.Name,
            design.Status,
            theme,
            blocks,
            design.NextVersionNumber,
            design.ApprovedVersionId,
            versions.Select(version => new InvitationVersionResponse(
                version.Id,
                version.VersionNumber,
                InvitationContentValidator.DeserializeTheme(version.ThemeSnapshotJson),
                InvitationContentValidator.DeserializeBlocks(version.ContentSnapshotJson),
                version.CreatedAt,
                version.ApprovedAt,
                version.PublishedAt)).ToList(),
            comments,
            InvitationContentValidator.GetAccessibilityWarnings(theme, blocks),
            design.UpdatedAt);
    }

    private async Task<InvitationGroup> FindGroupAsync(
        PortalEventAccess access,
        Guid groupId,
        CancellationToken cancellationToken) =>
        await dbContext.InvitationGroups.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == access.EventId
                && entity.Id == groupId
                && entity.ArchivedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el grupo.");

    private async Task<EventGuest> FindGuestAsync(
        PortalEventAccess access,
        Guid guestId,
        CancellationToken cancellationToken) =>
        await dbContext.EventGuests.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == access.EventId
                && entity.Id == guestId
                && entity.ArchivedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el invitado.");

    private async Task<InvitationDesign> FindDesignAsync(
        PortalEventAccess access,
        Guid designId,
        CancellationToken cancellationToken) =>
        await dbContext.InvitationDesigns.SingleOrDefaultAsync(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == access.EventId
                && entity.Id == designId
                && entity.ArchivedAt == null,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró el diseño.");

    private static void ValidateGroup(
        PortalInvitationGroupRequest request,
        int namedCount)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName)
            || request.DisplayName.Trim().Length > 160)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["displayName"] = ["El nombre del grupo es obligatorio."]
                });
        }

        if (request.AllowedGuestCount < Math.Max(1, namedCount)
            || request.MaxUnnamedCompanions < 0
            || request.MaxUnnamedCompanions
                > request.AllowedGuestCount - namedCount)
        {
            throw new ConflictException(
                "La capacidad o los acompañantes exceden los lugares disponibles.");
        }
    }

    private static void ValidateGuest(PortalGuestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName)
            && string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["firstName"] = ["Escribe el nombre o apellido del invitado."]
                });
        }

        if (request.SortOrder < 0)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["sortOrder"] = ["El orden no puede ser negativo."]
                });
        }
    }

    private static PortalInvitationGroupResponse ToPortalGroup(
        InvitationGroup group,
        int namedCount) =>
        new(
            group.Id,
            Enum.Parse<InvitationGroupTypeProjection>(group.GroupType.ToString()),
            group.DisplayName,
            group.AllowedGuestCount,
            namedCount,
            group.AllowUnnamedCompanions,
            group.MaxUnnamedCompanions);

    private static PortalGuestResponse ToPortalGuest(EventGuest guest) =>
        new(
            guest.Id,
            guest.InvitationGroupId,
            guest.FirstName,
            guest.LastName,
            Enum.Parse<GuestTypeProjection>(guest.GuestType.ToString()),
            Enum.Parse<AgeCategoryProjection>(guest.AgeCategory.ToString()),
            guest.IsPrimaryContact,
            guest.IsVip);
}
