using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Access.Authorization;
using Plannyt.Api.Modules.Access.Domain;
using Plannyt.Api.Modules.Identity.Security;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Access.Application;

public sealed class PortalEventService(
    PlannytDbContext dbContext,
    PortalAccessService portalAccessService,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PortalEventListItemResponse>> GetEventsAsync(
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return await dbContext.EventAccesses
            .AsNoTracking()
            .Where(access =>
                access.UserAccountId == currentUser.UserAccountId
                && access.Status == EventAccessStatus.Active
                && access.StartsAt <= now
                && (access.ExpiresAt == null || access.ExpiresAt > now)
                && access.RevokedAt == null)
            .Join(
                dbContext.Events.AsNoTracking(),
                access => new { access.OrganizationId, access.EventId },
                eventEntity => new
                {
                    eventEntity.OrganizationId,
                    EventId = eventEntity.Id
                },
                (access, eventEntity) => new
                {
                    Access = access,
                    Event = eventEntity
                })
            .Join(
                dbContext.Organizations.AsNoTracking()
                    .Where(entity => entity.Status == OrganizationStatus.Active),
                item => item.Event.OrganizationId,
                organization => organization.Id,
                (item, _) => item)
            .OrderBy(item => item.Event.StartDateTime)
            .Select(item => new PortalEventListItemResponse(
                item.Event.Id,
                item.Event.Name,
                item.Event.EventType,
                item.Event.StartDateTime,
                item.Event.EndDateTime,
                item.Event.TimeZone,
                item.Event.City,
                item.Event.CountryCode,
                item.Event.SharedDescription,
                item.Event.EstimatedGuestCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<PortalEventResponse> GetEventAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await portalAccessService.RequireAsync(
            eventId,
            Permissions.EventsSharedDataView,
            cancellationToken);
        var eventResponse = await dbContext.Events
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.Id == eventId)
            .Select(entity => new PortalEventListItemResponse(
                entity.Id,
                entity.Name,
                entity.EventType,
                entity.StartDateTime,
                entity.EndDateTime,
                entity.TimeZone,
                entity.City,
                entity.CountryCode,
                entity.SharedDescription,
                entity.EstimatedGuestCount))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró el evento.");
        var participants = await dbContext.EventParticipants
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.IsVisibleToClient)
            .Join(
                dbContext.People.AsNoTracking(),
                participant => new
                {
                    participant.OrganizationId,
                    Id = participant.PersonId
                },
                person => new { person.OrganizationId, person.Id },
                (participant, person) => new
                {
                    Participant = participant,
                    Person = person
                })
            .OrderBy(entity => entity.Participant.DisplayOrder)
            .ThenBy(entity => entity.Person.DisplayName)
            .Select(entity => new PortalParticipantResponse(
                entity.Participant.Id,
                entity.Person.DisplayName,
                entity.Participant.ParticipantType,
                entity.Participant.DisplayOrder,
                entity.Participant.SharedDescription))
            .ToListAsync(cancellationToken);
        var documents = await dbContext.BasicDocuments
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == access.OrganizationId
                && entity.EventId == eventId
                && entity.Visibility
                    == Modules.Documents.Domain.DocumentVisibility.ClientShared
                && entity.DeletedAt == null)
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new PortalDocumentResponse(
                entity.Id,
                entity.DocumentType,
                entity.FileName,
                entity.MimeType,
                entity.SizeBytes,
                entity.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PortalEventResponse(
            eventResponse.Id,
            eventResponse.Name,
            eventResponse.EventType,
            eventResponse.StartDateTime,
            eventResponse.EndDateTime,
            eventResponse.TimeZone,
            eventResponse.City,
            eventResponse.CountryCode,
            eventResponse.SharedDescription,
            eventResponse.EstimatedGuestCount,
            participants,
            documents);
    }
}
