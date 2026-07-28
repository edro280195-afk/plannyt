using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.BuildingBlocks.Http;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Crm.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.Modules.Crm.Application;

public sealed class ProspectService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    ProspectStatusTransitionService transitionService,
    AuditService auditService,
    TimeProvider timeProvider)
{
    public async Task<PagedResponse<ProspectListItemResponse>> GetPageAsync(
        Guid organizationId,
        int page,
        int pageSize,
        string? search,
        string? status,
        Guid? assignedUserId,
        string? eventType,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProspectsView,
            null,
            cancellationToken);
        ValidatePage(page, pageSize);
        var query = dbContext.Prospects
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(entity =>
                EF.Functions.ILike(entity.DisplayName, $"%{term}%")
                || (entity.Email != null && EF.Functions.ILike(entity.Email, $"%{term}%"))
                || (entity.Phone != null && EF.Functions.ILike(entity.Phone, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ProspectStatus>(status, true, out var parsedStatus))
            {
                throw new RequestValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["status"] = ["El estado solicitado no es válido."]
                    });
            }

            query = query.Where(entity => entity.Status == parsedStatus);
        }

        if (assignedUserId is not null)
        {
            query = query.Where(entity => entity.AssignedUserId == assignedUserId);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(entity =>
                entity.EventTypeInterest != null
                && EF.Functions.ILike(
                    entity.EventTypeInterest,
                    $"%{eventType.Trim()}%"));
        }

        if (dateFrom is not null)
        {
            query = query.Where(entity => entity.EstimatedEventDate >= dateFrom);
        }

        if (dateTo is not null)
        {
            query = query.Where(entity => entity.EstimatedEventDate <= dateTo);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entity => entity.UpdatedAt)
            .ThenBy(entity => entity.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(entity => new ProspectListItemResponse(
                entity.Id,
                entity.DisplayName,
                entity.Email,
                entity.Phone,
                entity.EventTypeInterest,
                entity.EstimatedEventDate,
                entity.EstimatedBudget,
                entity.CurrencyCode,
                entity.AssignedUserId,
                entity.Status,
                entity.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResponse<ProspectListItemResponse>(
            items,
            page,
            pageSize,
            totalCount);
    }

    public async Task<ProspectResponse> GetAsync(
        Guid organizationId,
        Guid prospectId,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProspectsView,
            null,
            cancellationToken);
        var prospect = await FindAsync(
            organizationId,
            prospectId,
            true,
            cancellationToken);
        return await BuildResponseAsync(
            prospect,
            access.Permissions.Contains(Permissions.ProspectsPrivateNotesView),
            cancellationToken);
    }

    public async Task<ProspectResponse> CreateAsync(
        Guid organizationId,
        ProspectDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProspectsCreate,
            null,
            cancellationToken);
        ProspectRequestValidator.Validate(request);
        await EnsureAssignmentAllowedAsync(
            organizationId,
            request.AssignedUserId,
            access.Permissions,
            cancellationToken);
        EnsureCanManageNotes(request.Notes, access.Permissions);
        var now = timeProvider.GetUtcNow();
        var prospect = Prospect.Create(
            organizationId,
            request.DisplayName.Trim(),
            Normalize(request.FirstName),
            Normalize(request.LastName),
            Normalize(request.CompanyName),
            Normalize(request.Email),
            Normalize(request.Phone),
            Normalize(request.Source),
            Normalize(request.EventTypeInterest),
            request.EstimatedEventDate,
            request.EstimatedGuestCount,
            request.EstimatedBudget,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            Normalize(request.City),
            Normalize(request.Notes),
            request.AssignedUserId,
            now);
        dbContext.Prospects.Add(prospect);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "prospect.created",
            nameof(Prospect),
            prospect.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(
            prospect,
            access.Permissions.Contains(Permissions.ProspectsPrivateNotesView),
            cancellationToken);
    }

    public async Task<ProspectResponse> UpdateAsync(
        Guid organizationId,
        Guid prospectId,
        ProspectDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProspectsUpdate,
            null,
            cancellationToken);
        ProspectRequestValidator.Validate(request);
        EnsureCanManageNotes(request.Notes, access.Permissions);
        var prospect = await FindAsync(
            organizationId,
            prospectId,
            false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        prospect.Update(
            request.DisplayName.Trim(),
            Normalize(request.FirstName),
            Normalize(request.LastName),
            Normalize(request.CompanyName),
            Normalize(request.Email),
            Normalize(request.Phone),
            Normalize(request.Source),
            Normalize(request.EventTypeInterest),
            request.EstimatedEventDate,
            request.EstimatedGuestCount,
            request.EstimatedBudget,
            request.CurrencyCode.Trim().ToUpperInvariant(),
            Normalize(request.City),
            access.Permissions.Contains(Permissions.ProspectsPrivateNotesManage)
                ? Normalize(request.Notes)
                : prospect.Notes,
            now);

        if (request.AssignedUserId != prospect.AssignedUserId)
        {
            if (!access.Permissions.Contains(Permissions.ProspectsAssign))
            {
                throw new ForbiddenException(
                    "No tienes permiso para cambiar al responsable.");
            }

            await EnsureAssignmentExistsAsync(
                organizationId,
                request.AssignedUserId,
                cancellationToken);
            prospect.Assign(request.AssignedUserId, now);
            auditService.Add(
                organizationId,
                null,
                access.UserAccountId,
                "prospect.assigned",
                nameof(Prospect),
                prospect.Id,
                new Dictionary<string, object?>
                {
                    ["assignedUserId"] = request.AssignedUserId
                });
        }

        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "prospect.updated",
            nameof(Prospect),
            prospect.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(
            prospect,
            access.Permissions.Contains(Permissions.ProspectsPrivateNotesView),
            cancellationToken);
    }

    public async Task<ProspectResponse> ChangeStatusAsync(
        Guid organizationId,
        Guid prospectId,
        ChangeProspectStatusRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProspectsChangeStatus,
            null,
            cancellationToken);
        var prospect = await FindAsync(
            organizationId,
            prospectId,
            false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var history = transitionService.ChangeStatus(
            prospect,
            request.NewStatus,
            access.UserAccountId,
            now,
            Normalize(request.Reason));
        dbContext.ProspectStatusHistory.Add(history);
        dbContext.ProspectActivities.Add(ProspectActivity.Create(
            organizationId,
            prospect.Id,
            ProspectActivityType.StatusChange,
            $"Estado cambiado a {request.NewStatus}",
            Normalize(request.Reason),
            null,
            now,
            null,
            CommercialVisibility.Internal,
            access.UserAccountId,
            now));
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "prospect.status_changed",
            nameof(Prospect),
            prospect.Id,
            new Dictionary<string, object?>
            {
                ["previousStatus"] = history.PreviousStatus.ToString(),
                ["newStatus"] = history.NewStatus.ToString(),
                ["reason"] = history.Reason
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(
            prospect,
            access.Permissions.Contains(Permissions.ProspectsPrivateNotesView),
            cancellationToken);
    }

    public Task<ProspectResponse> ArchiveAsync(
        Guid organizationId,
        Guid prospectId,
        CancellationToken cancellationToken) =>
        ChangeStatusWithPermissionAsync(
            organizationId,
            prospectId,
            ProspectStatus.Archived,
            Permissions.ProspectsArchive,
            cancellationToken);

    public async Task<ProspectActivityResponse> AddActivityAsync(
        Guid organizationId,
        Guid prospectId,
        CreateProspectActivityRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProspectsUpdate,
            null,
            cancellationToken);
        ProspectRequestValidator.Validate(request);
        if (request.Visibility == CommercialVisibility.Internal
            && !access.Permissions.Contains(Permissions.ProspectsPrivateNotesManage))
        {
            throw new ForbiddenException(
                "No tienes permiso para registrar actividad interna.");
        }

        await EnsureAssignmentExistsAsync(
            organizationId,
            request.AssignedUserId,
            cancellationToken);
        _ = await FindAsync(
            organizationId,
            prospectId,
            true,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        var activity = ProspectActivity.Create(
            organizationId,
            prospectId,
            request.ActivityType,
            request.Subject.Trim(),
            Normalize(request.Description),
            request.ScheduledAt,
            request.CompletedAt,
            request.AssignedUserId,
            request.Visibility,
            access.UserAccountId,
            now);
        dbContext.ProspectActivities.Add(activity);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "prospect.activity_created",
            nameof(ProspectActivity),
            activity.Id,
            new Dictionary<string, object?>
            {
                ["prospectId"] = prospectId,
                ["activityType"] = activity.ActivityType.ToString()
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToActivity(activity);
    }

    public async Task<ProspectActivityResponse> CompleteActivityAsync(
        Guid organizationId,
        Guid prospectId,
        Guid activityId,
        CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProspectsUpdate,
            null,
            cancellationToken);
        var activity = await dbContext.ProspectActivities.SingleOrDefaultAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.ProspectId == prospectId
                && entity.Id == activityId,
            cancellationToken)
            ?? throw new NotFoundException("No se encontró la actividad.");
        activity.Complete(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToActivity(activity);
    }

    public async Task<IReadOnlyList<ClientMatchSuggestionResponse>>
        GetClientMatchesAsync(
            Guid organizationId,
            Guid prospectId,
            CancellationToken cancellationToken)
    {
        await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ClientsView,
            null,
            cancellationToken);
        var prospect = await FindAsync(
            organizationId,
            prospectId,
            true,
            cancellationToken);
        return await FindMatchesAsync(prospect, cancellationToken);
    }

    public async Task<ConvertProspectResponse> ConvertAsync(
        Guid organizationId,
        Guid prospectId,
        ConvertProspectRequest request,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            Permissions.ProposalsConvertClient,
            null,
            cancellationToken);
        var prospect = await FindAsync(
            organizationId,
            prospectId,
            false,
            cancellationToken);
        if (prospect.ConvertedClientId is Guid convertedClientId)
        {
            return new ConvertProspectResponse(
                prospect.Id,
                convertedClientId,
                false);
        }

        var now = timeProvider.GetUtcNow();
        Client client;
        var created = false;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        if (request.ExistingClientId is Guid existingClientId)
        {
            client = await dbContext.Clients.SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == existingClientId
                    && entity.Status == ClientStatus.Active,
                cancellationToken)
                ?? throw new NotFoundException("No se encontró el cliente.");
        }
        else
        {
            var matches = await FindMatchesAsync(prospect, cancellationToken);
            if (matches.Count > 0 && !request.ConfirmCreateDespiteMatches)
            {
                throw new ConflictException(
                    "Hay posibles clientes coincidentes. Confirma la creación o relaciona uno existente.");
            }

            client = await CreateClientFromProspectAsync(
                prospect,
                request.NewClientType,
                now,
                cancellationToken);
            created = true;
        }

        prospect.MarkConverted(client.Id, now);
        if (prospect.Status != ProspectStatus.Won)
        {
            var history = transitionService.ChangeStatus(
                prospect,
                ProspectStatus.Won,
                access.UserAccountId,
                now,
                "Conversión a cliente");
            dbContext.ProspectStatusHistory.Add(history);
        }

        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "prospect.converted",
            nameof(Prospect),
            prospect.Id,
            new Dictionary<string, object?>
            {
                ["clientId"] = client.Id,
                ["createdNewClient"] = created
            });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ConvertProspectResponse(prospect.Id, client.Id, created);
    }

    public async Task<LinkPreliminaryEventResponse> LinkPreliminaryEventAsync(
        Guid organizationId,
        Guid prospectId,
        LinkPreliminaryEventRequest request,
        CancellationToken cancellationToken)
    {
        ProspectRequestValidator.Validate(request);
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            request.ExistingEventId is null
                ? Permissions.EventsCreate
                : Permissions.EventsUpdate,
            null,
            cancellationToken);
        var prospect = await FindAsync(
            organizationId,
            prospectId,
            false,
            cancellationToken);
        var now = timeProvider.GetUtcNow();
        Event eventEntity;
        var created = false;

        if (request.ExistingEventId is Guid existingEventId)
        {
            eventEntity = await dbContext.Events.SingleOrDefaultAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == existingEventId
                    && entity.Status == EventStatus.Preliminary,
                cancellationToken)
                ?? throw new NotFoundException(
                    "No se encontró el evento preliminar.");
        }
        else
        {
            ValidateTimeZone(request.TimeZone!);
            eventEntity = Event.Create(
                organizationId,
                request.Name!.Trim(),
                request.EventType!.Trim(),
                request.StartDateTime!.Value,
                null,
                request.TimeZone!.Trim(),
                request.City!.Trim(),
                request.CountryCode!.Trim().ToUpperInvariant(),
                null,
                request.EstimatedGuestCount,
                access.UserAccountId,
                now);
            dbContext.Events.Add(eventEntity);
            created = true;
        }

        if (prospect.ConvertedClientId is Guid clientId
            && !await dbContext.EventClients.AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.EventId == eventEntity.Id
                    && entity.ClientId == clientId,
                cancellationToken))
        {
            dbContext.EventClients.Add(EventClient.Create(
                organizationId,
                eventEntity.Id,
                clientId,
                EventClientRelationshipType.PrimaryClient,
                true,
                true,
                now));
        }

        auditService.Add(
            organizationId,
            eventEntity.Id,
            access.UserAccountId,
            created ? "prospect.preliminary_event_created" : "prospect.event_linked",
            nameof(Prospect),
            prospect.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new LinkPreliminaryEventResponse(
            prospect.Id,
            eventEntity.Id,
            created);
    }

    private async Task<ProspectResponse> ChangeStatusWithPermissionAsync(
        Guid organizationId,
        Guid prospectId,
        ProspectStatus status,
        string permission,
        CancellationToken cancellationToken)
    {
        var access = await tenantAccessService.RequireAsync(
            organizationId,
            permission,
            null,
            cancellationToken);
        var prospect = await FindAsync(
            organizationId,
            prospectId,
            false,
            cancellationToken);
        var history = transitionService.ChangeStatus(
            prospect,
            status,
            access.UserAccountId,
            timeProvider.GetUtcNow());
        dbContext.ProspectStatusHistory.Add(history);
        auditService.Add(
            organizationId,
            null,
            access.UserAccountId,
            "prospect.archived",
            nameof(Prospect),
            prospect.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await BuildResponseAsync(
            prospect,
            access.Permissions.Contains(Permissions.ProspectsPrivateNotesView),
            cancellationToken);
    }

    private async Task<Prospect> FindAsync(
        Guid organizationId,
        Guid prospectId,
        bool noTracking,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Prospects.Where(entity =>
            entity.OrganizationId == organizationId
            && entity.Id == prospectId);
        if (noTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("No se encontró el prospecto.");
    }

    private async Task<ProspectResponse> BuildResponseAsync(
        Prospect prospect,
        bool canViewInternal,
        CancellationToken cancellationToken)
    {
        var activitiesQuery = dbContext.ProspectActivities
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == prospect.OrganizationId
                && entity.ProspectId == prospect.Id);
        if (!canViewInternal)
        {
            activitiesQuery = activitiesQuery.Where(
                entity => entity.Visibility == CommercialVisibility.ClientShared);
        }

        var activities = await activitiesQuery
            .OrderByDescending(entity => entity.CreatedAt)
            .Select(entity => new ProspectActivityResponse(
                entity.Id,
                entity.ActivityType,
                entity.Subject,
                entity.Description,
                entity.ScheduledAt,
                entity.CompletedAt,
                entity.AssignedUserId,
                entity.Visibility,
                entity.CreatedBy,
                entity.CreatedAt))
            .ToListAsync(cancellationToken);
        var history = await dbContext.ProspectStatusHistory
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == prospect.OrganizationId
                && entity.ProspectId == prospect.Id)
            .OrderByDescending(entity => entity.ChangedAt)
            .Select(entity => new ProspectStatusHistoryResponse(
                entity.Id,
                entity.PreviousStatus,
                entity.NewStatus,
                entity.Reason,
                entity.ChangedBy,
                entity.ChangedAt))
            .ToListAsync(cancellationToken);
        return new ProspectResponse(
            prospect.Id,
            prospect.DisplayName,
            prospect.FirstName,
            prospect.LastName,
            prospect.CompanyName,
            prospect.Email,
            prospect.Phone,
            prospect.Source,
            prospect.EventTypeInterest,
            prospect.EstimatedEventDate,
            prospect.EstimatedGuestCount,
            prospect.EstimatedBudget,
            prospect.CurrencyCode,
            prospect.City,
            canViewInternal ? prospect.Notes : null,
            prospect.AssignedUserId,
            prospect.Status,
            prospect.LostReason,
            prospect.ConvertedClientId,
            activities,
            history,
            prospect.CreatedAt,
            prospect.UpdatedAt,
            prospect.ArchivedAt);
    }

    private async Task<List<ClientMatchSuggestionResponse>> FindMatchesAsync(
        Prospect prospect,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(prospect.Email)
            && string.IsNullOrWhiteSpace(prospect.Phone))
        {
            return [];
        }

        var people = dbContext.People
            .AsNoTracking()
            .Where(entity => entity.OrganizationId == prospect.OrganizationId);
        if (!string.IsNullOrWhiteSpace(prospect.Email)
            && !string.IsNullOrWhiteSpace(prospect.Phone))
        {
            people = people.Where(entity =>
                entity.ContactEmail != null
                    && EF.Functions.ILike(entity.ContactEmail, prospect.Email)
                || entity.ContactPhone == prospect.Phone);
        }
        else if (!string.IsNullOrWhiteSpace(prospect.Email))
        {
            people = people.Where(entity =>
                entity.ContactEmail != null
                && EF.Functions.ILike(entity.ContactEmail, prospect.Email));
        }
        else
        {
            people = people.Where(entity => entity.ContactPhone == prospect.Phone);
        }

        var personIds = people.Select(entity => entity.Id);
        var clients = await dbContext.Clients
            .AsNoTracking()
            .Where(entity =>
                entity.OrganizationId == prospect.OrganizationId
                && entity.Status == ClientStatus.Active
                && (entity.PersonId != null && personIds.Contains(entity.PersonId.Value)
                    || dbContext.ClientContacts.Any(contact =>
                        contact.OrganizationId == prospect.OrganizationId
                        && contact.ClientId == entity.Id
                        && personIds.Contains(contact.PersonId))))
            .Select(entity => new
            {
                entity.Id,
                entity.DisplayName
            })
            .ToListAsync(cancellationToken);
        var matchField = !string.IsNullOrWhiteSpace(prospect.Email)
            ? "email o teléfono"
            : "teléfono";
        var matchValue = prospect.Email ?? prospect.Phone!;
        return clients
            .Select(client => new ClientMatchSuggestionResponse(
                client.Id,
                client.DisplayName,
                matchField,
                matchValue))
            .ToList();
    }

    private async Task<Client> CreateClientFromProspectAsync(
        Prospect prospect,
        ClientType? requestedType,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var type = requestedType
            ?? (!string.IsNullOrWhiteSpace(prospect.CompanyName)
                ? ClientType.Company
                : ClientType.Person);
        if (type == ClientType.Company)
        {
            var companyName = prospect.CompanyName ?? prospect.DisplayName;
            var client = Client.CreateCompany(
                prospect.OrganizationId,
                companyName,
                prospect.DisplayName,
                prospect.Source,
                now);
            dbContext.Clients.Add(client);
            return client;
        }

        if (string.IsNullOrWhiteSpace(prospect.FirstName)
            || string.IsNullOrWhiteSpace(prospect.LastName))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["newClientType"] =
                    [
                        "Para crear un cliente persona completa nombre y apellido del prospecto."
                    ]
                });
        }

        var organization = await dbContext.Organizations
            .AsNoTracking()
            .SingleAsync(
                entity => entity.Id == prospect.OrganizationId,
                cancellationToken);
        var person = Person.Create(
            prospect.OrganizationId,
            null,
            prospect.FirstName,
            prospect.LastName,
            prospect.DisplayName,
            prospect.Email,
            prospect.Phone,
            "es",
            organization.TimeZone,
            now);
        var personClient = Client.CreatePerson(
            prospect.OrganizationId,
            person.Id,
            prospect.DisplayName,
            prospect.Source,
            now);
        dbContext.AddRange(person, personClient);
        return personClient;
    }

    private static ProspectActivityResponse ToActivity(ProspectActivity activity) =>
        new(
            activity.Id,
            activity.ActivityType,
            activity.Subject,
            activity.Description,
            activity.ScheduledAt,
            activity.CompletedAt,
            activity.AssignedUserId,
            activity.Visibility,
            activity.CreatedBy,
            activity.CreatedAt);

    private async Task EnsureAssignmentAllowedAsync(
        Guid organizationId,
        Guid? assignedUserId,
        IReadOnlySet<string> permissions,
        CancellationToken cancellationToken)
    {
        if (assignedUserId is null)
        {
            return;
        }

        if (!permissions.Contains(Permissions.ProspectsAssign))
        {
            throw new ForbiddenException(
                "No tienes permiso para asignar prospectos.");
        }

        await EnsureAssignmentExistsAsync(
            organizationId,
            assignedUserId,
            cancellationToken);
    }

    private async Task EnsureAssignmentExistsAsync(
        Guid organizationId,
        Guid? assignedUserId,
        CancellationToken cancellationToken)
    {
        if (assignedUserId is null)
        {
            return;
        }

        var exists = await dbContext.OrganizationMemberships.AnyAsync(
            entity =>
                entity.OrganizationId == organizationId
                && entity.UserAccountId == assignedUserId
                && entity.Status == MembershipStatus.Active,
            cancellationToken);
        if (!exists)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["assignedUserId"] =
                    [
                        "El responsable debe tener una membresía activa en la organización."
                    ]
                });
        }
    }

    private static void EnsureCanManageNotes(
        string? notes,
        IReadOnlySet<string> permissions)
    {
        if (!string.IsNullOrWhiteSpace(notes)
            && !permissions.Contains(Permissions.ProspectsPrivateNotesManage))
        {
            throw new ForbiddenException(
                "No tienes permiso para administrar notas privadas.");
        }
    }

    private static void ValidateTimeZone(string timeZone)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (Exception exception)
            when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["timeZone"] = ["La zona horaria IANA no es válida."]
                });
        }
    }

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
