namespace Plannyt.Api.Modules.Rsvp.Application;

public static class RsvpExportEndpoints
{
    public static IEndpointRouteBuilder MapRsvpExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/events/{eventId:guid}/rsvp/exports")
            .WithTags("RSVP - Exportaciones")
            .RequireAuthorization();

        group.MapGet("/attendance", async (Guid organizationId, Guid eventId, RsvpExportService service, CancellationToken ct) =>
        {
            var csv = await service.ExportAttendanceAsync(organizationId, eventId, ct);
            return Results.File(
                csv,
                "text/csv; charset=utf-8",
                $"rsvp-attendance-{ShortId(eventId)}.csv");
        });

        group.MapGet("/catering", async (Guid organizationId, Guid eventId, RsvpExportService service, CancellationToken ct) =>
        {
            var csv = await service.ExportCateringAsync(organizationId, eventId, ct);
            return Results.File(
                csv,
                "text/csv; charset=utf-8",
                $"rsvp-catering-{ShortId(eventId)}.csv");
        });

        group.MapGet("/transport", async (Guid organizationId, Guid eventId, RsvpExportService service, CancellationToken ct) =>
        {
            var csv = await service.ExportTransportAsync(organizationId, eventId, ct);
            return Results.File(
                csv,
                "text/csv; charset=utf-8",
                $"rsvp-transport-{ShortId(eventId)}.csv");
        });

        group.MapGet("/accommodation", async (Guid organizationId, Guid eventId, RsvpExportService service, CancellationToken ct) =>
        {
            var csv = await service.ExportAccommodationAsync(organizationId, eventId, ct);
            return Results.File(
                csv,
                "text/csv; charset=utf-8",
                $"rsvp-accommodation-{ShortId(eventId)}.csv");
        });

        group.MapGet("/sensitive", async (Guid organizationId, Guid eventId, RsvpExportService service, CancellationToken ct) =>
        {
            var csv = await service.ExportSensitiveDataAsync(organizationId, eventId, ct);
            return Results.File(
                csv,
                "text/csv; charset=utf-8",
                $"rsvp-sensitive-{ShortId(eventId)}.csv");
        });

        return endpoints;
    }

    private static string ShortId(Guid value) =>
        value.ToString("N")[..8];
}
