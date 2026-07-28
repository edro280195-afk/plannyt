using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Invitations.Security;

namespace Plannyt.Api.Modules.Invitations.Application;

public sealed class PublicInvitationService(
    PlannytDbContext dbContext,
    GuestAccessTokenService tokenService,
    TimeProvider timeProvider)
{
    public async Task<PublicInvitationResponse> GetAsync(
        string token,
        CancellationToken cancellationToken)
    {
        var hash = tokenService.Hash(token);
        var link = await dbContext.GuestAccessLinks.SingleOrDefaultAsync(
            entity => entity.TokenHash == hash,
            cancellationToken);
        if (link is null)
        {
            throw new PublicInvitationUnavailableException(
                StatusCodes.Status404NotFound,
                "invalid",
                "El enlace no existe o no es válido.");
        }

        var now = timeProvider.GetUtcNow();
        if (link.Status == GuestAccessLinkStatus.Revoked)
        {
            throw Unavailable("revoked", "El enlace fue revocado.");
        }

        if (link.Status == GuestAccessLinkStatus.Replaced)
        {
            throw Unavailable("replaced", "El enlace fue reemplazado por uno nuevo.");
        }

        if (link.Status == GuestAccessLinkStatus.Expired || link.IsExpired(now))
        {
            throw Unavailable("expired", "El enlace ya venció.");
        }

        var experience = await dbContext.EventGuestExperiences.AsNoTracking()
            .SingleOrDefaultAsync(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.EventId == link.EventId,
                cancellationToken);
        if (experience is null
            || experience.Status is GuestExperienceStatus.Suspended)
        {
            throw Unavailable(
                "suspended",
                "La invitación está temporalmente suspendida.");
        }

        if (experience.Status != GuestExperienceStatus.Published
            || experience.ActiveVersionId is null)
        {
            throw Unavailable("unpublished", "La invitación todavía no está publicada.");
        }

        var group = await dbContext.InvitationGroups.AsNoTracking()
            .SingleOrDefaultAsync(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.EventId == link.EventId
                && entity.Id == link.InvitationGroupId
                && entity.ArchivedAt == null,
                cancellationToken)
            ?? throw Unavailable("invalid", "El grupo de invitación ya no está disponible.");
        var eventEntity = await dbContext.Events.AsNoTracking().SingleAsync(entity =>
            entity.OrganizationId == link.OrganizationId
            && entity.Id == link.EventId,
            cancellationToken);
        var version = await dbContext.InvitationDesignVersions.AsNoTracking()
            .SingleAsync(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.EventId == link.EventId
                && entity.Id == experience.ActiveVersionId,
                cancellationToken);
        var guests = await dbContext.EventGuests.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.EventId == link.EventId
                && entity.InvitationGroupId == group.Id
                && entity.ArchivedAt == null)
            .OrderByDescending(entity => entity.IsPrimaryContact)
            .ThenBy(entity => entity.SortOrder)
            .ToListAsync(cancellationToken);
        var tags = await dbContext.InvitationGroupTags.AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.EventId == link.EventId
                && entity.InvitationGroupId == group.Id)
            .Select(entity => entity.GuestTagId)
            .ToListAsync(cancellationToken);
        var blocks = InvitationContentValidator
            .DeserializeBlocks(version.ContentSnapshotJson)
            .Where(block => block.Visible && IsVisible(block, group, guests, tags))
            .Where(block =>
                experience.ShowEventDate
                || block.Type is not (
                    InvitationBlockType.EventDate
                    or InvitationBlockType.Countdown))
            .Where(block =>
                experience.ShowParticipantNames
                || block.Type != InvitationBlockType.Participants)
            .Select(block => Personalize(
                block,
                group,
                eventEntity.Name,
                experience.ShowEventDate ? eventEntity.StartDateTime : null,
                experience.ShowParticipantNames ? guests : []))
            .OrderBy(block => block.SortOrder)
            .ToList();
        link.RegisterOpen(now);
        if (group.Status is InvitationGroupStatus.LinkGenerated
            or InvitationGroupStatus.SharedManually)
        {
            var trackedGroup = await dbContext.InvitationGroups.SingleAsync(entity =>
                entity.OrganizationId == link.OrganizationId
                && entity.EventId == link.EventId
                && entity.Id == group.Id,
                cancellationToken);
            trackedGroup.ChangeStatus(InvitationGroupStatus.Opened, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PublicInvitationResponse(
            "available",
            experience.Language,
            experience.PublicTitle,
            experience.CelebrantDisplayName,
            experience.WelcomeMessage,
            experience.ShowEventName ? eventEntity.Name : null,
            experience.ShowEventDate ? eventEntity.StartDateTime : null,
            eventEntity.TimeZone,
            experience.ShowCity ? eventEntity.City : null,
            experience.ShowCity ? eventEntity.CountryCode : null,
            group.DisplayName,
            group.AllowedGuestCount,
            experience.ShowParticipantNames
                ? guests.Select(guest => new PublicGuestResponse(
                    guest.FirstName,
                    guest.LastName,
                    Enum.Parse<GuestTypeProjection>(guest.GuestType.ToString()),
                    Enum.Parse<AgeCategoryProjection>(guest.AgeCategory.ToString()),
                    guest.IsPrimaryContact,
                    guest.IsVip)).ToList()
                : [],
            InvitationContentValidator.DeserializeTheme(version.ThemeSnapshotJson),
            blocks,
            experience.ClosingMessage);
    }

    private static bool IsVisible(
        InvitationBlockRequest block,
        InvitationGroup group,
        IReadOnlyCollection<EventGuest> guests,
        IReadOnlyCollection<Guid> tagIds) =>
        block.Visibility switch
        {
            BlockVisibility.Everyone => true,
            BlockVisibility.InvitationGroup =>
                Guid.TryParse(block.VisibilityValue, out var groupId)
                && groupId == group.Id,
            BlockVisibility.HasTag =>
                Guid.TryParse(block.VisibilityValue, out var tagId)
                && tagIds.Contains(tagId),
            BlockVisibility.GuestType =>
                Enum.TryParse<GuestType>(
                    block.VisibilityValue,
                    true,
                    out var guestType)
                && guests.Any(guest => guest.GuestType == guestType),
            BlockVisibility.VipOnly => guests.Any(guest => guest.IsVip),
            _ => false
        };

    private static InvitationBlockRequest Personalize(
        InvitationBlockRequest block,
        InvitationGroup group,
        string eventName,
        DateTimeOffset? eventDate,
        IReadOnlyCollection<EventGuest> guests)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["group.displayName"] = group.DisplayName,
            ["group.contactName"] = group.ContactName ?? group.DisplayName,
            ["event.name"] = eventName,
            ["event.date"] = eventDate?.ToString(
                "D",
                CultureInfo.GetCultureInfo("es-MX")) ?? string.Empty,
            ["participants.names"] = string.Join(
                ", ",
                guests.Select(guest => $"{guest.FirstName} {guest.LastName}".Trim()))
        };
        var content = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in block.Content.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                var text = property.Value.GetString() ?? string.Empty;
                foreach (var value in values)
                {
                    text = text.Replace(
                        $"{{{{{value.Key}}}}}",
                        value.Value,
                        StringComparison.Ordinal);
                }

                content[property.Name] = text;
            }
            else
            {
                content[property.Name] = property.Value.Clone();
            }
        }

        return block with { Content = JsonSerializer.SerializeToElement(content) };
    }

    private static PublicInvitationUnavailableException Unavailable(
        string reason,
        string detail) =>
        new(StatusCodes.Status410Gone, reason, detail);
}
