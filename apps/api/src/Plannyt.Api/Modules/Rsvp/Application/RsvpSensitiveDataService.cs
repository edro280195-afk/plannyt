using Microsoft.EntityFrameworkCore;
using Plannyt.Api.BuildingBlocks.Errors;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.Modules.Audit.Application;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Organizations.Authorization;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.Modules.Rsvp.Application;

public sealed class RsvpSensitiveDataService(
    PlannytDbContext dbContext,
    TenantAccessService tenantAccessService,
    AuditService auditService)
{
    public async Task<IReadOnlyList<SensitiveGuestDataResponse>> GetAsync(
        Guid organizationId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestSensitiveDataView,
            cancellationToken);
        var result = await (
                from sensitive in dbContext.GuestDietaryAndAccessibilities
                    .AsNoTracking()
                join guest in dbContext.EventGuests.AsNoTracking()
                    on new
                    {
                        sensitive.OrganizationId,
                        sensitive.EventId,
                        Id = sensitive.EventGuestId
                    }
                    equals new
                    {
                        guest.OrganizationId,
                        guest.EventId,
                        guest.Id
                    }
                where sensitive.OrganizationId == organizationId
                      && sensitive.EventId == eventId
                orderby guest.SortOrder
                select new SensitiveGuestDataResponse(
                    guest.Id,
                    (guest.FirstName + " " + guest.LastName).Trim(),
                    sensitive.Allergies,
                    sensitive.DietaryRestrictions,
                    sensitive.AccessibilityRequirements,
                    sensitive.AdditionalNotes,
                    sensitive.ConsentGrantedAt,
                    sensitive.UpdatedAt))
            .ToListAsync(cancellationToken);
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.GuestSensitiveDataViewed,
            nameof(GuestDietaryAndAccessibility),
            eventId,
            Metadata(result.Count, "view"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<SensitiveQuestionAnswerResponse>>
        GetQuestionAnswersAsync(
            Guid organizationId,
            Guid eventId,
            CancellationToken cancellationToken)
    {
        var access = await RequireEventAsync(
            organizationId,
            eventId,
            Permissions.GuestSensitiveDataView,
            cancellationToken);
        var latestSubmissionIds = dbContext.RsvpSubmissions
            .AsNoTracking()
            .Where(submission =>
                submission.OrganizationId == organizationId
                && submission.EventId == eventId)
            .GroupBy(submission => submission.InvitationGroupId)
            .Select(group => group
                .OrderByDescending(submission =>
                    submission.RevisionNumber)
                .Select(submission => submission.Id)
                .First());
        var rows = await (
                from answer in dbContext.RsvpSubmissionAnswers.AsNoTracking()
                join submission in dbContext.RsvpSubmissions.AsNoTracking()
                    on answer.RsvpSubmissionId equals submission.Id
                where latestSubmissionIds.Contains(answer.RsvpSubmissionId)
                      && answer.IsSensitive
                orderby submission.SubmittedAt, answer.QuestionId
                select new
                {
                    SubmissionId = submission.Id,
                    submission.RevisionNumber,
                    answer.QuestionId,
                    answer.GuestId,
                    answer.GuestDisplayNameSnapshot,
                    answer.QuestionLabelSnapshot,
                    answer.QuestionTypeSnapshot,
                    answer.AnswerValue,
                    answer.DisplayValueSnapshot,
                    answer.OptionLabelsSnapshot,
                    submission.SubmittedAt
                })
            .ToListAsync(cancellationToken);
        var result = rows
            .Select(row => new SensitiveQuestionAnswerResponse(
                row.SubmissionId,
                row.RevisionNumber,
                row.QuestionId,
                row.GuestId,
                row.GuestDisplayNameSnapshot,
                row.QuestionLabelSnapshot,
                row.QuestionTypeSnapshot,
                ReadJsonValue(row.AnswerValue),
                row.DisplayValueSnapshot,
                row.OptionLabelsSnapshot,
                row.SubmittedAt))
            .ToList();
        auditService.Add(
            organizationId,
            eventId,
            access.UserAccountId,
            AuditActions.GuestSensitiveDataViewed,
            nameof(RsvpSubmissionAnswer),
            eventId,
            Metadata(result.Count, "question-answer-view"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
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
        if (!await dbContext.Events.AsNoTracking().AnyAsync(
                entity =>
                    entity.OrganizationId == organizationId
                    && entity.Id == eventId,
                cancellationToken))
        {
            throw new NotFoundException("No se encontró el evento.");
        }

        return access;
    }

    private static IReadOnlyDictionary<string, object?> Metadata(
        int recordCount,
        string operationType) =>
        new Dictionary<string, object?>
        {
            ["recordCount"] = recordCount,
            ["operationType"] = operationType
        };

    private static string ReadJsonValue(string value)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(value);
            return document.RootElement.ValueKind
                   == System.Text.Json.JsonValueKind.String
                ? document.RootElement.GetString() ?? string.Empty
                : document.RootElement.GetRawText();
        }
        catch (System.Text.Json.JsonException)
        {
            return value;
        }
    }
}
