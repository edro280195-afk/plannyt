using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.BuildingBlocks.Http;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Events.Application;

public sealed class EventService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    EventStatusTransitionService transitionService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<PagedResponse<EventListItemResponse>> GetPageAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsView,
            null,
            cancellationToken);
        ValidatePage(page, pageSize);
        var query = dbContext.Events
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);
        if (!includeArchived)
        {
            query = query.Where(entity => entity.Status != EventStatus.Archived);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(entity =>
                EF.Functions.ILike(entity.Name, $"%{term}%")
                || EF.Functions.ILike(entity.EventType, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(entity => entity.StartDateTime)
            .ThenBy(entity => entity.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new EventListItemResponse(
                entity.Id,
                entity.Name,
                entity.EventType,
                entity.Status,
                entity.StartDateTime,
                entity.EndDateTime,
                entity.TimeZone,
                entity.City,
                entity.EstimatedGuestCount,
                entity.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResponse<EventListItemResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<EventResponse> GetAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsView,
            eventId,
            cancellationToken);
        var eventEntity = await FindEventAsync(
            organizationId,
            eventId,
            true,
            cancellationToken);
        return await BuildResponseAsync(eventEntity, cancellationToken);
    }

    public async Task<EventResponse> CreateAsync(
        Guid organizationId,
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsCreate,
            null,
            cancellationToken);
        EventRequestValidator.Validate(request);
        var now = timeProvider.GetUtcNow();
        var eventEntity = Event.Create(
            organizationId,
            request.Name.Trim(),
            request.EventType.Trim(),
            request.StartDateTime,
            request.EndDateTime,
            request.TimeZone.Trim(),
            request.City.Trim(),
            request.CountryCode.Trim().ToUpperInvariant(),
            Normalize(request.SharedDescription),
            request.EstimatedGuestCount,
            access.UserAccountId,
            now);
        dbContext.Events.Add(eventEntity);
        auditService.Add(
            organizationId,
            eventEntity.Id,
            access.UserAccountId,
            "event.created",
            nameof(Event),
            eventEntity.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(eventEntity, cancellationToken);
    }

    public async Task<EventResponse> UpdateAsync(
        Guid organizationId,
        Guid eventId,
        UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsUpdate,
            eventId,
            cancellationToken);
        EventRequestValidator.Validate(request);
        var eventEntity = await FindEventAsync(
            organizationId,
            eventId,
            false,
            cancellationToken);
        eventEntity.UpdateDetails(
            request.Name.Trim(),
            request.EventType.Trim(),
            request.StartDateTime,
            request.EndDateTime,
            request.TimeZone.Trim(),
            request.City.Trim(),
            request.CountryCode.Trim().ToUpperInvariant(),
            Normalize(request.SharedDescription),
            request.EstimatedGuestCount,
            timeProvider.GetUtcNow());
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "event.updated",
            nameof(Event),
            eventId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(eventEntity, cancellationToken);
    }

    public async Task<EventResponse> ChangeStatusAsync(
        Guid organizationId,
        Guid eventId,
        ChangeEventStatusRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsUpdate,
            eventId,
            cancellationToken);
        var eventEntity = await FindEventAsync(
            organizationId,
            eventId,
            false,
            cancellationToken);
        var isExceptional = IsExceptionalTransition(
            eventEntity.Status,
            request.NewStatus);
        if (isExceptional
            && (!access.Permissions.Contains(Permissions.EventsArchive)
                || string.IsNullOrWhiteSpace(request.Reason)))
        {
            throw new ForbiddenException(
                "La reapertura requiere permiso de archivo y un motivo.");
        }

        var now = timeProvider.GetUtcNow();
        var history = transitionService.ChangeStatus(
            eventEntity,
            request.NewStatus,
            access.UserAccountId,
            now,
            Normalize(request.Reason),
            isExceptional);
        dbContext.EventStatusHistory.Add(history);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "event.status_changed",
            nameof(Event),
            eventId,
            new Dictionary<string, object?>
            {
                ["previousStatus"] = history.PreviousStatus.ToString(),
                ["newStatus"] = history.NewStatus.ToString(),
                ["reason"] = history.Reason
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(eventEntity, cancellationToken);
    }

    public async Task<EventResponse> ArchiveAsync(
        Guid organizationId,
        Guid eventId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsArchive,
            eventId,
            cancellationToken);
        var eventEntity = await FindEventAsync(
            organizationId,
            eventId,
            false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var history = transitionService.ChangeStatus(
            eventEntity,
            EventStatus.Archived,
            access.UserAccountId,
            now,
            Normalize(reason));
        dbContext.EventStatusHistory.Add(history);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "event.archived",
            nameof(Event),
            eventId,
            new Dictionary<string, object?>
            {
                ["previousStatus"] = history.PreviousStatus.ToString(),
                ["reason"] = history.Reason
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(eventEntity, cancellationToken);
    }

    public async Task<IReadOnlyList<EventClientResponse>> GetClientsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsView,
            eventId,
            cancellationToken);
        _ = await FindEventAsync(
            organizationId,
            eventId,
            true,
            cancellationToken);
        return await QueryEventClients(organizationId, eventId)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventClientResponse> AddClientAsync(
        Guid organizationId,
        Guid eventId,
        CreateEventClientRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsUpdate,
            eventId,
            cancellationToken);
        _ = await FindEventAsync(
            organizationId,
            eventId,
            false,
            cancellationToken);
        var client = await dbContext.Clients
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == request.ClientId
                    && entity.Status == ClientStatus.Active,
                cancellationToken)
            ?? throw new NotFoundException("No se encontró un cliente activo.");
        if (request.IsPrimary
            && await dbContext.EventClients.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventId
                    && entity.IsPrimary,
                cancellationToken))
        {
            throw new ConflictException("El evento ya tiene un cliente principal.");
        }

        var relationExists = await dbContext.EventClients.AnyAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.ClientId == request.ClientId
                && entity.RelationshipType == request.RelationshipType,
            cancellationToken);
        if (relationExists)
        {
            throw new ConflictException("La relación con el cliente ya existe.");
        }

        var relation = EventClient.Create(
            organizationId,
            eventId,
            request.ClientId,
            request.RelationshipType,
            request.IsPrimary,
            request.HasTransferAuthority,
            timeProvider.GetUtcNow());
        dbContext.EventClients.Add(relation);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "event.client_linked",
            nameof(EventClient),
            relation.Id,
            new Dictionary<string, object?>
            {
                ["clientId"] = request.ClientId,
                ["relationshipType"] = request.RelationshipType.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new EventClientResponse(
            relation.Id,
            relation.ClientId,
            client.DisplayName,
            relation.RelationshipType,
            relation.IsPrimary,
            relation.HasTransferAuthority);
    }

    public async Task RemoveClientAsync(
        Guid organizationId,
        Guid eventId,
        Guid eventClientId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.EventsUpdate,
            eventId,
            cancellationToken);
        var relation = await dbContext.EventClients.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == eventClientId,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró la relación con el cliente.");
        dbContext.EventClients.Remove(relation);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "event.client_unlinked",
            nameof(EventClient),
            relation.Id,
            new Dictionary<string, object?>
            {
                ["clientId"] = relation.ClientId
            });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EventParticipantResponse>> GetParticipantsAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ParticipantsView,
            eventId,
            cancellationToken);
        _ = await FindEventAsync(
            organizationId,
            eventId,
            true,
            cancellationToken);
        return await QueryParticipants(organizationId, eventId)
            .ToListAsync(cancellationToken);
    }

    public async Task<EventParticipantResponse> AddParticipantAsync(
        Guid organizationId,
        Guid eventId,
        UpsertEventParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ParticipantsManage,
            eventId,
            cancellationToken);
        EventRequestValidator.Validate(request);
        _ = await FindEventAsync(
            organizationId,
            eventId,
            false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var person = CreateParticipantPerson(organizationId, request, now);
        var participant = EventParticipant.Create(
            organizationId,
            eventId,
            person.Id,
            request.ParticipantType.Trim(),
            request.DisplayOrder,
            request.IsVisibleToClient,
            Normalize(request.SharedDescription),
            now);
        dbContext.AddRange(person, participant);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "event.participant_created",
            nameof(EventParticipant),
            participant.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToParticipantResponse(participant, person);
    }

    public async Task<EventParticipantResponse> UpdateParticipantAsync(
        Guid organizationId,
        Guid eventId,
        Guid participantId,
        UpsertEventParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ParticipantsManage,
            eventId,
            cancellationToken);
        EventRequestValidator.Validate(request);
        var participant = await dbContext.EventParticipants.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId
                && entity.Id == participantId,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró el participante.");
        var person = await dbContext.People.SingleAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.Id == participant.PersonId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        UpdateParticipantPerson(person, request, now);
        participant.Update(
            request.ParticipantType.Trim(),
            request.DisplayOrder,
            request.IsVisibleToClient,
            Normalize(request.SharedDescription),
            now);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            "event.participant_updated",
            nameof(EventParticipant),
            participant.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToParticipantResponse(participant, person);
    }

    private async Task<Event> FindEventAsync(
        Guid organizationId,
        Guid eventId,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Events.Where(entity =>
            entity.OrganizationId == organizationId
            && entity.Id == eventId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró el evento.");
    }

    private async Task<EventResponse> BuildResponseAsync(
        Event eventEntity,
        CancellationToken cancellationToken)
    {
        var history = await dbContext.EventStatusHistory
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == eventEntity.OrganizationId
                && entity.EventId == eventEntity.Id)
            .OrderByDescending(entity => entity.ChangedAt)
            .Select(entity => new EventStatusHistoryResponse(
                entity.Id,
                entity.PreviousStatus,
                entity.NewStatus,
                entity.Reason,
                entity.ChangedBy,
                entity.ChangedAt))
            .ToListAsync(cancellationToken);
        return new EventResponse(
            eventEntity.Id,
            eventEntity.OrganizationId,
            eventEntity.Name,
            eventEntity.EventType,
            eventEntity.Status,
            eventEntity.StartDateTime,
            eventEntity.EndDateTime,
            eventEntity.TimeZone,
            eventEntity.City,
            eventEntity.CountryCode,
            eventEntity.SharedDescription,
            eventEntity.EstimatedGuestCount,
            eventEntity.CreatedBy,
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt,
            eventEntity.ArchivedAt,
            history);
    }

    private IQueryable<EventClientResponse> QueryEventClients(
        Guid organizationId,
        Guid eventId) =>
        dbContext.EventClients
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId)
            .Join(
                dbContext.Clients.AsNoTracking(),
                relation => new
                {
                    relation.OrganizationId,
                    Id = relation.ClientId
                },
                client => new { client.OrganizationId, client.Id },
                (relation, client) => new
                {
                    Relation = relation,
                    Client = client
                })
            .OrderByDescending(entity => entity.Relation.IsPrimary)
            .ThenBy(entity => entity.Client.DisplayName)
            .Select(entity => new EventClientResponse(
                entity.Relation.Id,
                entity.Client.Id,
                entity.Client.DisplayName,
                entity.Relation.RelationshipType,
                entity.Relation.IsPrimary,
                entity.Relation.HasTransferAuthority));

    private IQueryable<EventParticipantResponse> QueryParticipants(
        Guid organizationId,
        Guid eventId) =>
        dbContext.EventParticipants
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == organizationId
                && entity.EventId == eventId)
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
            .Select(entity => new EventParticipantResponse(
                entity.Participant.Id,
                entity.Person.Id,
                entity.Person.DisplayName,
                entity.Person.ContactEmail,
                entity.Person.ContactPhone,
                entity.Participant.ParticipantType,
                entity.Participant.DisplayOrder,
                entity.Participant.IsVisibleToClient,
                entity.Participant.SharedDescription));

    private static bool IsExceptionalTransition(
        EventStatus current,
        EventStatus requested) =>
        (current == EventStatus.Closed && requested == EventStatus.Planning)
        || (current == EventStatus.Cancelled
            && requested == EventStatus.Preliminary);

    private static Person CreateParticipantPerson(
        Guid organizationId,
        UpsertEventParticipantRequest request,
        DateTimeOffset now) =>
        Person.Create(
            organizationId,
            null,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            Normalize(request.ContactEmail),
            Normalize(request.ContactPhone),
            request.PreferredLanguage.Trim(),
            request.TimeZone.Trim(),
            now);

    private static void UpdateParticipantPerson(
        Person person,
        UpsertEventParticipantRequest request,
        DateTimeOffset now) =>
        person.UpdateProfile(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            $"{request.FirstName.Trim()} {request.LastName.Trim()}",
            Normalize(request.ContactEmail),
            Normalize(request.ContactPhone),
            request.PreferredLanguage.Trim(),
            request.TimeZone.Trim(),
            now);

    private static EventParticipantResponse ToParticipantResponse(
        EventParticipant participant,
        Person person) =>
        new(
            participant.Id,
            person.Id,
            person.DisplayName,
            person.ContactEmail,
            person.ContactPhone,
            participant.ParticipantType,
            participant.DisplayOrder,
            participant.IsVisibleToClient,
            participant.SharedDescription);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidatePage(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["La página debe ser mayor o igual a 1."];
        }

        if (pageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["El tamaño de página debe estar entre 1 y 100."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
