namespace Plannyt.Api.Modules.Rsvp.Application;

public static class RsvpEndpoints
{
    public static IEndpointRouteBuilder MapRsvpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var professional = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/events/{eventId:guid}/rsvp")
            .WithTags("RSVP")
            .RequireAuthorization();

        MapSettingsEndpoints(professional);
        MapFormEndpoints(professional);
        MapResponseEndpoints(professional);
        MapMenuEndpoints(endpoints);
        MapTransportEndpoints(endpoints);
        MapAccommodationEndpoints(endpoints);
        MapReminderEndpoints(professional);
        MapPortalEndpoints(endpoints);
        MapPublicEndpoints(endpoints);

        return endpoints;
    }

    private static void MapSettingsEndpoints(IEndpointRouteBuilder group)
    {
        group.MapGet("/settings", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetSettingsAsync(organizationId, eventId, ct)));

        group.MapPut("/settings", async (
            Guid organizationId, Guid eventId, RsvpSettingsRequest request,
            RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.CreateOrUpdateDraftAsync(organizationId, eventId, request, ct)));

        group.MapPost("/settings/publish", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.PublishSettingsAsync(organizationId, eventId, ct)));

        group.MapPost("/settings/open", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.OpenAsync(organizationId, eventId, ct)));

        group.MapPost("/settings/close", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.CloseAsync(organizationId, eventId, ct)));
    }

    private static void MapFormEndpoints(IEndpointRouteBuilder group)
    {
        group.MapGet("/form", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetFormAsync(organizationId, eventId, ct)));

        group.MapPost("/form", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.CreateFormAsync(organizationId, eventId, ct)));

        group.MapGet("/form/question-catalog", async (
            Guid organizationId, Guid eventId,
            RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetQuestionCatalogAsync(
                organizationId,
                eventId,
                ct)));

        group.MapGet("/form/versions/{versionId:guid}", async (
            Guid organizationId, Guid eventId, Guid versionId,
            RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetFormVersionAsync(
                organizationId,
                eventId,
                versionId,
                ct)));

        group.MapGet("/form/draft-version", async (
            Guid organizationId, Guid eventId,
            RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetDraftFormVersionAsync(
                organizationId,
                eventId,
                ct)));

        group.MapPost("/form/version", async (
            Guid organizationId, Guid eventId, CreateVersionRequest request,
            RsvpService service, CancellationToken ct) =>
        {
            var result = await service.CreateVersionAsync(
                organizationId, eventId,
                request.QuestionsJson, request.MenuJson,
                request.TransportJson, request.AccommodationJson, ct);
            return Results.Created(
                $"/api/organizations/{organizationId}/events/{eventId}/rsvp/form/versions/{result.Id}",
                result);
        });

        group.MapPost("/form/new-draft", async (
            Guid organizationId, Guid eventId,
            RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.CreateNewDraftAsync(
                organizationId,
                eventId,
                ct)));

        group.MapPost("/form/submit-review", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.SubmitForReviewAsync(organizationId, eventId, ct)));

        group.MapPost("/form/versions/{versionId:guid}/approve", async (
            Guid organizationId, Guid eventId, Guid versionId,
            RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.ApproveFormAsync(organizationId, eventId, versionId, ct)));

        group.MapPost("/form/versions/{versionId:guid}/publish", async (
            Guid organizationId, Guid eventId, Guid versionId,
            RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.PublishFormAsync(organizationId, eventId, versionId, ct)));
    }

    private static void MapResponseEndpoints(IEndpointRouteBuilder group)
    {
        group.MapGet("/dashboard", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetDashboardAsync(organizationId, eventId, ct)));

        group.MapGet("/sensitive-data", async (
            Guid organizationId, Guid eventId,
            RsvpSensitiveDataService sensitiveDataService,
            CancellationToken ct) =>
            Results.Ok(await sensitiveDataService.GetAsync(
                organizationId,
                eventId,
                ct)));

        group.MapGet("/sensitive-question-answers", async (
            Guid organizationId, Guid eventId,
            RsvpSensitiveDataService sensitiveDataService,
            CancellationToken ct) =>
            Results.Ok(await sensitiveDataService.GetQuestionAnswersAsync(
                organizationId,
                eventId,
                ct)));

        group.MapGet("/projections/diagnosis", async (
            Guid organizationId, Guid eventId,
            RsvpProjectionReconciliationService reconciliationService,
            CancellationToken ct) =>
            Results.Ok(await reconciliationService.DiagnoseAsync(
                organizationId,
                eventId,
                ct)));

        group.MapPost("/projections/repair", async (
            Guid organizationId, Guid eventId,
            RsvpProjectionReconciliationService reconciliationService,
            CancellationToken ct) =>
            Results.Ok(await reconciliationService.RepairAsync(
                organizationId,
                eventId,
                ct)));

        group.MapPost("/groups/{groupId:guid}/manual-capture", async (
            Guid organizationId, Guid eventId, Guid groupId,
            ManualRsvpRequest request, HttpContext httpContext,
            RsvpService service, CancellationToken ct) =>
        {
            var result = await service.ManualCaptureAsync(
                organizationId,
                eventId,
                groupId,
                request,
                httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault(),
                ct);
            return Results.Created(
                $"/api/organizations/{organizationId}/events/{eventId}/rsvp/submissions/{result.Id}",
                result);
        });

        group.MapPost("/groups/{groupId:guid}/exception", async (
            Guid organizationId, Guid eventId, Guid groupId,
            OpenGroupExceptionRequest request, RsvpService service, CancellationToken ct) =>
        {
            await service.OpenGroupExceptionAsync(
                organizationId, eventId, groupId, request.ExpiresAt, request.Reason, ct);
            return Results.Ok();
        });

        group.MapPost("/groups/{groupId:guid}/exception/close", async (
            Guid organizationId, Guid eventId, Guid groupId,
            RsvpService service, CancellationToken ct) =>
        {
            await service.CloseGroupExceptionAsync(
                organizationId,
                eventId,
                groupId,
                ct);
            return Results.NoContent();
        });
    }

    private static void MapMenuEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/events/{eventId:guid}/menus")
            .WithTags("RSVP - Menús")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetMenusAsync(organizationId, eventId, ct)));

        group.MapPost("/", async (
            Guid organizationId, Guid eventId, EventMenuRequest request,
            RsvpService service, CancellationToken ct) =>
        {
            var result = await service.CreateMenuAsync(organizationId, eventId, request, ct);
            return Results.Created(
                $"/api/organizations/{organizationId}/events/{eventId}/menus/{result.Id}",
                result);
        });

        group.MapPost("/{menuId:guid}/options", async (
            Guid organizationId, Guid eventId, Guid menuId,
            EventMenuOptionRequest request, RsvpService service, CancellationToken ct) =>
        {
            var result = await service.AddMenuOptionAsync(organizationId, eventId, menuId, request, ct);
            return Results.Created(
                $"/api/organizations/{organizationId}/events/{eventId}/menus/{menuId}/options/{result.Id}",
                result);
        });
    }

    private static void MapTransportEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/events/{eventId:guid}/transport")
            .WithTags("RSVP - Transporte")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetTransportOptionsAsync(organizationId, eventId, ct)));

        group.MapPost("/", async (
            Guid organizationId, Guid eventId, EventTransportOptionRequest request,
            RsvpService service, CancellationToken ct) =>
        {
            var result = await service.CreateTransportOptionAsync(organizationId, eventId, request, ct);
            return Results.Created(
                $"/api/organizations/{organizationId}/events/{eventId}/transport/{result.Id}",
                result);
        });
    }

    private static void MapAccommodationEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/events/{eventId:guid}/accommodation")
            .WithTags("RSVP - Hospedaje")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetAccommodationOptionsAsync(organizationId, eventId, ct)));

        group.MapPost("/", async (
            Guid organizationId, Guid eventId, EventAccommodationOptionRequest request,
            RsvpService service, CancellationToken ct) =>
        {
            var result = await service.CreateAccommodationOptionAsync(organizationId, eventId, request, ct);
            return Results.Created(
                $"/api/organizations/{organizationId}/events/{eventId}/accommodation/{result.Id}",
                result);
        });
    }

    private static void MapReminderEndpoints(IEndpointRouteBuilder group)
    {
        group.MapGet("/reminders/templates", async (
            Guid organizationId, Guid eventId, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetTemplatesAsync(organizationId, eventId, ct)));

        group.MapPost("/reminders/templates", async (
            Guid organizationId, Guid eventId, ReminderTemplateRequest request,
            RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.CreateTemplateAsync(organizationId, eventId, request, ct)));

        group.MapPost("/reminders/groups/{groupId:guid}/templates/{templateId:guid}/mark-sent", async (
            Guid organizationId, Guid eventId, Guid groupId, Guid templateId,
            MarkReminderRequest request, RsvpService service, CancellationToken ct) =>
        {
            await service.MarkReminderSentAsync(
                organizationId, eventId, groupId, templateId, request, ct);
            return Results.Ok();
        });
    }

    private static void MapPublicEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/guest/rsvp/{token}")
            .WithTags("RSVP - Público");

        group.MapGet("/state", async (
            string token, RsvpService service, CancellationToken ct) =>
            Results.Ok(await service.GetGuestRsvpStateByTokenAsync(token, ct)));

        group.MapPost("/submit", async (
            string token, RsvpSubmissionRequest request,
            HttpContext httpContext, RsvpService service, CancellationToken ct) =>
        {
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var result = await service.SubmitRsvpByTokenAsync(
                token,
                request,
                httpContext.Request.Headers["Idempotency-Key"].FirstOrDefault(),
                userAgent,
                ipAddress,
                ct);
            return Results.Ok(result);
        });
    }

    private static void MapPortalEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/client-portal/events/{eventId:guid}/rsvp")
            .WithTags("RSVP - Portal")
            .RequireAuthorization();

        group.MapGet("/dashboard", async (
            Guid eventId,
            RsvpService service,
            CancellationToken ct) =>
            Results.Ok(await service.GetPortalDashboardAsync(
                eventId,
                ct)));

        group.MapGet("/form", async (
            Guid eventId,
            RsvpService service,
            CancellationToken ct) =>
            Results.Ok(await service
                .GetPortalPublishedFormVersionAsync(
                    eventId,
                    ct)));

        group.MapPost("/groups/{groupId:guid}/manual-capture", async (
            Guid eventId,
            Guid groupId,
            ManualRsvpRequest request,
            HttpContext httpContext,
            RsvpService service,
            CancellationToken ct) =>
        {
            var result = await service.PortalManualCaptureAsync(
                eventId,
                groupId,
                request,
                httpContext.Request.Headers["Idempotency-Key"]
                    .FirstOrDefault(),
                ct);
            return Results.Created(
                $"/api/client-portal/events/{eventId}/rsvp/submissions/{result.Id}",
                result);
        });
    }
}

public sealed record CreateVersionRequest(
    string QuestionsJson,
    string MenuJson,
    string TransportJson,
    string AccommodationJson);

public sealed record OpenGroupExceptionRequest(
    DateTimeOffset ExpiresAt,
    string Reason);
