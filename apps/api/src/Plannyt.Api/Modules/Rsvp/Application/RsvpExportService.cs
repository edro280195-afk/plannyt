using System.Text;
using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed class RsvpExportService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService)
{
    public async Task<byte[]> ExportAttendanceAsync(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        await RequireEventAsync(organizationId, eventId, Permissions.RsvpResponsesExport, ct);

        var rsvps = await dbContext.CurrentGuestRsvps
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.EventId == eventId)
            .OrderBy(r => r.InvitationGroupId)
            .ToListAsync(ct);

        var groups = await dbContext.InvitationGroups
            .AsNoTracking()
            .Where(g => g.OrganizationId == organizationId && g.EventId == eventId)
            .ToDictionaryAsync(g => g.Id, g => g.DisplayName, ct);

        var sb = new StringBuilder();
        sb.AppendLine("\uFEFFGroupName,GuestDisplayName,AttendanceStatus,AgeCategory,IsUnnamedCompanion");

        foreach (var r in rsvps)
        {
            var groupName = groups.GetValueOrDefault(r.InvitationGroupId, "");
            var displayName = SanitizeCsv(r.CurrentDisplayName ?? "");
            var groupNameSafe = SanitizeCsv(groupName);
            sb.AppendLine($"{groupNameSafe},{displayName},{r.AttendanceStatus},{r.IsUnnamedCompanion}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportCateringAsync(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        await RequireEventAsync(organizationId, eventId, Permissions.EventMenusExport, ct);

        var menus = await dbContext.EventMenus
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId && m.EventId == eventId)
            .ToDictionaryAsync(m => m.Id, ct);

        var options = await dbContext.EventMenuOptions
            .AsNoTracking()
            .Where(o => o.OrganizationId == organizationId && menus.Keys.Contains(o.EventMenuId))
            .ToDictionaryAsync(o => o.Id, o => new { o.Name, o.EventMenuId, o.DietaryTags }, ct);

        var lastSubmissions = await dbContext.RsvpSubmissions
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && s.EventId == eventId)
            .GroupBy(s => s.InvitationGroupId)
            .Select(g => g.OrderByDescending(s => s.RevisionNumber).First().Id)
            .ToListAsync(ct);

        var guests = await dbContext.RsvpSubmissionGuests
            .AsNoTracking()
            .Where(g => lastSubmissions.Contains(g.RsvpSubmissionId))
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("\uFEFFGuestDisplayName,MenuCategory,MenuOption,DietaryTags,AttendanceStatus");

        foreach (var guest in guests)
        {
            if (guest.MenuSelectionsSnapshot is "[]" or "{}" or null or "")
                continue;
            try
            {
                var selections = System.Text.Json.JsonSerializer.Deserialize<List<MenuItem>>(guest.MenuSelectionsSnapshot);
                if (selections is null) continue;
                foreach (var selection in selections)
                {
                    options.TryGetValue(selection.OptionId, out var opt);
                    var menuName = opt is not null && menus.TryGetValue(opt.EventMenuId, out var menu) ? menu.MenuCategory.ToString() : "";
                    var optionName = SanitizeCsv(opt?.Name ?? "");
                    var dietary = SanitizeCsv(opt?.DietaryTags ?? "");
                    var displayName = SanitizeCsv(guest.DisplayName);
                    sb.AppendLine($"{displayName},{menuName},{optionName},{dietary},{guest.AttendanceStatus}");
                }
            }
            catch { /* skip malformed JSON */ }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportTransportAsync(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        await RequireEventAsync(organizationId, eventId, Permissions.GuestTravelExport, ct);

        var selections = await dbContext.GuestTransportSelections
            .AsNoTracking()
            .Where(s =>
                s.OrganizationId == organizationId
                && s.EventId == eventId
                && s.Status != Domain.TransportSelectionStatus.NotNeeded)
            .ToListAsync(ct);

        var options = await dbContext.EventTransportOptions
            .AsNoTracking()
            .Where(o => o.OrganizationId == organizationId && o.EventId == eventId)
            .ToDictionaryAsync(o => o.Id, ct);

        var guestRsvps = await dbContext.CurrentGuestRsvps
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.EventId == eventId && r.EventGuestId != null)
            .ToDictionaryAsync(r => r.EventGuestId!.Value, r => r, ct);

        var groups = await dbContext.InvitationGroups
            .AsNoTracking()
            .Where(g => g.OrganizationId == organizationId && g.EventId == eventId)
            .ToDictionaryAsync(g => g.Id, g => g.DisplayName, ct);

        var sb = new StringBuilder();
        sb.AppendLine("\uFEFFGuestDisplayName,GroupName,TransportOption,Direction,Status,DepartureAt,WaitlistPosition");

        var selectionsWithGuests = selections
            .Where(s => guestRsvps.ContainsKey(s.EventGuestId))
            .ToList();

        foreach (var sel in selectionsWithGuests)
        {
            var rsvp = guestRsvps[sel.EventGuestId];
            var groupName = SanitizeCsv(groups.GetValueOrDefault(rsvp.InvitationGroupId, ""));
            var displayName = SanitizeCsv(rsvp.CurrentDisplayName ?? "");
            options.TryGetValue(sel.EventTransportOptionId, out var opt);
            var optionName = SanitizeCsv(opt?.Name ?? "");
            var direction = opt?.Direction.ToString() ?? "";
            var departAt = opt?.DepartureAt?.ToString("s") ?? "";
            var status = sel.Status.ToString();
            var waitlistPos = sel.Status == Domain.TransportSelectionStatus.Waitlisted
                ? (await GetWaitlistPosition(sel.EventTransportOptionId, sel.EventGuestId, ct)).ToString()
                : "";
            sb.AppendLine($"{displayName},{groupName},{optionName},{direction},{status},{departAt},{waitlistPos}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportAccommodationAsync(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        await RequireEventAsync(organizationId, eventId, Permissions.GuestTravelExport, ct);

        var selections = await dbContext.GuestAccommodationSelections
            .AsNoTracking()
            .Where(s =>
                s.OrganizationId == organizationId
                && s.EventId == eventId)
            .ToListAsync(ct);

        var options = await dbContext.EventAccommodationOptions
            .AsNoTracking()
            .Where(o => o.OrganizationId == organizationId && o.EventId == eventId)
            .ToDictionaryAsync(o => o.Id, ct);

        var groups = await dbContext.InvitationGroups
            .AsNoTracking()
            .Where(g => g.OrganizationId == organizationId && g.EventId == eventId)
            .ToDictionaryAsync(g => g.Id, g => g.DisplayName, ct);

        var sb = new StringBuilder();
        sb.AppendLine("\uFEFFGroupName,GuestDisplayName,AccommodationOption,Status,ReservationName,NeedAssistance");

        foreach (var sel in selections)
        {
            var groupName = SanitizeCsv(groups.GetValueOrDefault(sel.InvitationGroupId, ""));
            options.TryGetValue(sel.EventAccommodationOptionId ?? Guid.Empty, out var opt);
            var optionName = SanitizeCsv(opt?.Name ?? "");
            var displayName = SanitizeCsv(sel.ReservationName ?? groupName);
            var status = sel.Status.ToString();
            var needAssist = sel.Status == Domain.AccommodationSelectionStatus.NeedAssistance ? "Yes" : "";
            sb.AppendLine($"{groupName},{displayName},{optionName},{status},{SanitizeCsv(sel.ReservationName ?? "")},{needAssist}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportSensitiveDataAsync(
        Guid organizationId, Guid eventId, CancellationToken ct)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestSensitiveDataExport,
            ct);

        var data = await dbContext.GuestDietaryAndAccessibilities
            .AsNoTracking()
            .Where(d => d.OrganizationId == organizationId && d.EventId == eventId)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("\uFEFFGuestDisplayName,Allergies,DietaryRestrictions,AccessibilityRequirements,AdditionalNotes,ConsentGrantedAt,LastUpdatedAt");

        foreach (var d in data)
        {
            var guest = await dbContext.CurrentGuestRsvps
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.EventGuestId == d.EventGuestId, ct);
            var displayName = SanitizeCsv(guest?.CurrentDisplayName ?? "");
            sb.AppendLine($"{displayName},{SanitizeCsv(d.Allergies ?? "")},{SanitizeCsv(d.DietaryRestrictions ?? "")},{SanitizeCsv(d.AccessibilityRequirements ?? "")},{SanitizeCsv(d.AdditionalNotes ?? "")},{d.ConsentGrantedAt?.ToString("s") ?? ""},{d.UpdatedAt:s}");
        }

        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.GuestSensitiveDataExported,
            nameof(Domain.GuestDietaryAndAccessibility),
            eventId,
            new Dictionary<string, object?>
            {
                ["recordCount"] = data.Count,
                ["operationType"] = "csv-export"
            });
        await dbContext.SaveChangesAsync(ct);
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string SanitizeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        if (value.StartsWith('=') || value.StartsWith('+') || value.StartsWith('-') || value.StartsWith('@'))
            return $"'{value}";
        return value;
    }

    private async Task<int> GetWaitlistPosition(Guid transportOptionId, Guid eventGuestId, CancellationToken ct)
    {
        var waitlisted = await dbContext.GuestTransportSelections
            .AsNoTracking()
            .Where(s => s.EventTransportOptionId == transportOptionId && s.Status == Domain.TransportSelectionStatus.Waitlisted)
            .ToListAsync(ct);
        var ordered = waitlisted
            .OrderBy(s => s.WaitlistSequence ?? long.MaxValue)
            .ThenBy(s => s.RequestedAt)
            .ThenBy(s => s.EventGuestId)
            .ToList();
        return ordered.FindIndex(s => s.EventGuestId == eventGuestId) + 1;
    }

    private async Task<TenantAccess> RequireEventAsync(Guid organizationId, Guid eventId, string permission, CancellationToken ct)
    {
        var access = await tenantAccessService.RequireAsync(organizationId, permission, eventId, ct);
        if (!await dbContext.Events.AsNoTracking().AnyAsync(e => e.OrganizationId == organizationId && e.Id == eventId, ct))
            throw new NotFoundException("No se encontró el evento.");
        return access;
    }
}

internal sealed record MenuItem(Guid OptionId);
