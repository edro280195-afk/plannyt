using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;
using Plannyt.Api.Modules.Audit.Domain;
using Plannyt.Api.Modules.Events.Domain;
using Plannyt.Api.Modules.Guests.Domain;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Invitations.Domain;
using Plannyt.Api.Modules.Organizations.Domain;
using Plannyt.Api.Modules.Rsvp.Domain;

namespace Plannyt.Api.IntegrationTests.Rsvp;

[Collection(ApiCollection.Name)]
public sealed class RsvpIntegrationTests(ApiFactory factory)
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FutureExpiry =
        new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // =========================================================================
    // Configuration & Settings
    // =========================================================================

    [Fact]
    public async Task RSVP_Settings_Created_Defaults_Draft()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-draft");
        var eventId = await CreateConfirmedEventAsync(session);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(1),
            closesAt = Now.AddDays(30),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = true,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = true,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });

        var payload = await GetSettingsAsync(session, eventId);

        Assert.Equal("Draft", payload.GetProperty("status").GetString());
        Assert.NotEqual(Guid.Empty, payload.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task RSVP_Settings_Update_Draft_Persists()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-update");
        var eventId = await CreateConfirmedEventAsync(session);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(10),
            closesAt = Now.AddDays(60),
            timeZone = "America/Mexico_City",
            allowChangesAfterSubmission = true,
            changesCloseAt = Now.AddDays(55),
            allowTentativeResponse = true,
            allowGroupDecline = true,
            requireResponseForEveryNamedGuest = true,
            requireCompanionNames = true,
            allowContactInformationUpdate = true,
            showAttendanceSummaryAfterSubmission = true,
            confirmationTitle = "Listo",
            confirmationMessage = "Gracias",
            declineMessage = "Te esperamos",
            closedMessage = "Cerrado",
            privacyNotice = "Aviso",
            sensitiveDataConsentText = "Consentimiento"
        });

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(5),
            closesAt = Now.AddDays(45),
            timeZone = "America/Monterrey",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = false,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = "Confirmado V2",
            confirmationMessage = "Gracias V2",
            declineMessage = "Lástima V2",
            closedMessage = "Cerrado V2",
            privacyNotice = "Aviso V2",
            sensitiveDataConsentText = "Consentimiento V2"
        });

        var payload = await GetSettingsAsync(session, eventId);

        Assert.Equal("America/Monterrey", payload.GetProperty("timeZone").GetString());
        Assert.False(payload.GetProperty("allowChangesAfterSubmission").GetBoolean());
        Assert.False(payload.GetProperty("allowGroupDecline").GetBoolean());
        Assert.Equal("Confirmado V2", payload.GetProperty("confirmationTitle").GetString());
        Assert.Equal("Cerrado V2", payload.GetProperty("closedMessage").GetString());
    }

    [Fact]
    public async Task RSVP_Settings_Publish_Then_Open()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-pubopen");
        var eventId = await CreateConfirmedEventAsync(session);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(-1),
            closesAt = Now.AddDays(90),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = false,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });

        var readyPayload = await PublishSettingsAsync(session, eventId);
        Assert.Equal("Ready", readyPayload.GetProperty("status").GetString());

        var openPayload = await OpenSettingsAsync(session, eventId);
        Assert.Equal("Open", openPayload.GetProperty("status").GetString());

        var getPayload = await GetSettingsAsync(session, eventId);
        Assert.Equal("Open", getPayload.GetProperty("status").GetString());
    }

    [Fact]
    public async Task RSVP_Settings_Close_Prevents_Submissions()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-close");
        var eventId = await CreateConfirmedEventAsync(session);
        var groupId = await CreateGroupAsync(session, eventId, "Familia Sección", 2);
        await CreateGuestAsync(session, eventId, groupId, "Laura", "Sección", true);
        await PublishInvitationExperienceAsync(session, eventId);
        var token = await GenerateLinkTokenAsync(session, eventId, groupId);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(-1),
            closesAt = Now.AddDays(90),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = true,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });
        await PublishSettingsAsync(session, eventId);
        await OpenSettingsAsync(session, eventId);
        await CreateAndPublishFormAsync(session, eventId);

        var closePayload = await CloseSettingsAsync(session, eventId);
        Assert.Equal("Closed", closePayload.GetProperty("status").GetString());

        using var submitResponse = await PostGuestRsvpAsync(
            token,
            $"close-{Guid.NewGuid():N}",
            new
            {
                expectedRevision = 0,
                overallStatus = "Confirmed",
                contactName = "Laura Sección",
                contactEmail = (string?)null,
                contactPhone = (string?)null,
                guests = new[]
                {
                    new
                    {
                        eventGuestId = (Guid?)null,
                        displayName = "Laura",
                        ageCategory = "Adult",
                        attendanceStatus = "Attending",
                        menuSelectionsJson = "[]",
                        transportSelectionJson = "[]",
                        accommodationSelectionJson = "[]",
                        dietaryJson = "[]",
                        isUnnamedCompanion = false
                    }
                },
                answers = Array.Empty<object>(),
                consentSnapshot = (string?)null
            });

        Assert.Equal(HttpStatusCode.Conflict, submitResponse.StatusCode);
    }

    [Fact]
    public async Task RSVP_Settings_Cross_Tenant_Isolation()
    {
        var owner = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-owner");
        var stranger = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-stranger");
        var eventId = await CreateConfirmedEventAsync(owner);

        await PutSettingsAsync(owner, eventId, new
        {
            opensAt = Now.AddDays(1),
            closesAt = Now.AddDays(30),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = false,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });

        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{owner.OrganizationId}/events/{eventId}/rsvp/settings",
            stranger.AccessToken);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // =========================================================================
    // Form Lifecycle
    // =========================================================================

    [Fact]
    public async Task RSVP_Form_Create_And_Submit_For_Review()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-form-review");
        var eventId = await CreateConfirmedEventAsync(session);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(1),
            closesAt = Now.AddDays(30),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = false,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });

        var createdForm = await CreateFormAsync(session, eventId);
        Assert.Equal("Draft", createdForm.GetProperty("status").GetString());

        var submittedForm = await SubmitFormForReviewAsync(session, eventId);
        Assert.Equal("InReview", submittedForm.GetProperty("status").GetString());
    }

    [Fact]
    public async Task RSVP_Form_Version_Create_And_Approve()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-ver-approve");
        var eventId = await CreateConfirmedEventAsync(session);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(1),
            closesAt = Now.AddDays(30),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = false,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });

        await CreateFormAsync(session, eventId);

        var version = await CreateFormVersionAsync(session, eventId);
        Assert.Equal(1, version.GetProperty("versionNumber").GetInt32());
        Assert.Equal(JsonValueKind.Null, version.GetProperty("approvedAt").ValueKind);

        await SubmitFormForReviewAsync(session, eventId);

        var approved = await ApproveFormVersionAsync(session, eventId, version.GetProperty("id").GetGuid());
        Assert.NotEqual(default(DateTimeOffset), approved.GetProperty("approvedAt").GetDateTimeOffset());
        Assert.NotEqual(Guid.Empty, approved.GetProperty("approvedBy").GetGuid());
    }

    [Fact]
    public async Task RSVP_Form_Publish_Version()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-form-pub");
        var eventId = await CreateConfirmedEventAsync(session);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(1),
            closesAt = Now.AddDays(30),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = false,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });

        await CreateFormAsync(session, eventId);
        var version = await CreateFormVersionAsync(session, eventId);
        var versionId = version.GetProperty("id").GetGuid();
        await SubmitFormForReviewAsync(session, eventId);
        await ApproveFormVersionAsync(session, eventId, versionId);

        var published = await PublishFormVersionAsync(session, eventId, versionId);
        Assert.NotEqual(default(DateTimeOffset), published.GetProperty("publishedAt").GetDateTimeOffset());

        var form = await GetFormAsync(session, eventId);
        Assert.Equal("Published", form.GetProperty("status").GetString());
        Assert.Equal(versionId, form.GetProperty("activePublishedVersionId").GetGuid());
    }

    [Fact]
    public async Task RSVP_Form_Cross_Tenant()
    {
        var owner = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-f-owner");
        var stranger = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-f-stranger");
        var eventId = await CreateConfirmedEventAsync(owner);

        await PutSettingsAsync(owner, eventId, new
        {
            opensAt = Now.AddDays(1),
            closesAt = Now.AddDays(30),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = false,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });

        await CreateFormAsync(owner, eventId);

        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{owner.OrganizationId}/events/{eventId}/rsvp/form",
            stranger.AccessToken);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // =========================================================================
    // Public RSVP Flow
    // =========================================================================

    [Fact]
    public async Task RSVP_Public_State_When_Open()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-pub-state");
        var eventId = await CreateConfirmedEventAsync(session);
        var groupId = await CreateGroupAsync(session, eventId, "Familia Estatal", 2);
        await CreateGuestAsync(session, eventId, groupId, "Pedro", "Estatal", true);
        await CreateGuestAsync(session, eventId, groupId, "Ana", "Estatal", false);
        await PublishInvitationExperienceAsync(session, eventId);
        var token = await GenerateLinkTokenAsync(session, eventId, groupId);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(-1),
            closesAt = Now.AddDays(90),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = true,
            changesCloseAt = Now.AddDays(60),
            allowTentativeResponse = false,
            allowGroupDecline = true,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = "[Cerrado] mensaje de cierre",
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });
        await PublishSettingsAsync(session, eventId);
        await OpenSettingsAsync(session, eventId);
        await CreateAndPublishFormAsync(session, eventId);

        using var stateResponse = await factory.CreateClient().GetAsync(
            $"/api/guest/rsvp/{token}/state");

        Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode);
        var state = await stateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Familia Estatal", state.GetProperty("groupName").GetString());
        Assert.True(state.GetProperty("canRespond").GetBoolean());
        Assert.True(state.GetProperty("canModify").GetBoolean());
        Assert.NotEqual(Guid.Empty, state.GetProperty("settings").GetProperty("id").GetGuid());
        Assert.NotEqual(Guid.Empty, state.GetProperty("activeForm").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task RSVP_Public_Submit_Valid()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-pub-submit");
        var eventId = await CreateConfirmedEventAsync(session);
        var groupId = await CreateGroupAsync(session, eventId, "Familia Pública", 2);
        var guestId = await CreateGuestAsync(session, eventId, groupId, "Roberto", "Pública", true);
        await PublishInvitationExperienceAsync(session, eventId);

        var token = await GenerateLinkTokenAsync(session, eventId, groupId);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(-1),
            closesAt = Now.AddDays(90),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = true,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });
        await PublishSettingsAsync(session, eventId);
        await OpenSettingsAsync(session, eventId);
        await CreateAndPublishFormAsync(session, eventId);

        using var submitResponse = await PostGuestRsvpAsync(
            token,
            $"submit-{Guid.NewGuid():N}",
            new
            {
                expectedRevision = 0,
                overallStatus = "Confirmed",
                contactName = "Roberto Pública",
                contactEmail = "roberto@example.invalid",
                contactPhone = "+528991234567",
                guests = new[]
                {
                    new
                    {
                        eventGuestId = (Guid?)guestId,
                        displayName = "Roberto Pública",
                        ageCategory = "Adult",
                        attendanceStatus = "Attending",
                        menuSelectionsJson = "[]",
                        transportSelectionJson = "[]",
                        accommodationSelectionJson = "[]",
                        dietaryJson = "[]",
                        isUnnamedCompanion = false
                    }
                },
                answers = Array.Empty<object>(),
                consentSnapshot = "\"Consentimiento otorgado vía web\""
            });

        Assert.True(
            submitResponse.StatusCode == HttpStatusCode.OK,
            $"Se esperaba 200 OK, se recibió {(int)submitResponse.StatusCode}: "
            + await submitResponse.Content.ReadAsStringAsync());
        var submission = await submitResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotNull(submission.GetProperty("confirmationCode").GetString());
        Assert.Equal("Confirmed", submission.GetProperty("overallStatus").GetString());
        Assert.Equal("Roberto Pública", submission.GetProperty("contactNameSnapshot").GetString());
    }

    [Fact]
    public async Task RSVP_Public_Submit_Closed()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "rsvp-sub-closed");
        var eventId = await CreateConfirmedEventAsync(session);
        var groupId = await CreateGroupAsync(session, eventId, "Familia Cerrada", 1);
        await CreateGuestAsync(session, eventId, groupId, "Diana", "Cerrada", true);
        await PublishInvitationExperienceAsync(session, eventId);
        var token = await GenerateLinkTokenAsync(session, eventId, groupId);

        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(-1),
            closesAt = Now.AddDays(90),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = false,
            changesCloseAt = (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = false,
            requireResponseForEveryNamedGuest = false,
            requireCompanionNames = false,
            allowContactInformationUpdate = false,
            showAttendanceSummaryAfterSubmission = false,
            confirmationTitle = (string?)null,
            confirmationMessage = (string?)null,
            declineMessage = (string?)null,
            closedMessage = (string?)null,
            privacyNotice = (string?)null,
            sensitiveDataConsentText = (string?)null
        });
        await PublishSettingsAsync(session, eventId);
        await OpenSettingsAsync(session, eventId);
        await CreateAndPublishFormAsync(session, eventId);
        await CloseSettingsAsync(session, eventId);

        using var submitResponse = await PostGuestRsvpAsync(
            token,
            $"closed-{Guid.NewGuid():N}",
            new
            {
                expectedRevision = 0,
                overallStatus = "Confirmed",
                contactName = "Diana Cerrada",
                contactEmail = (string?)null,
                contactPhone = (string?)null,
                guests = new[]
                {
                    new
                    {
                        eventGuestId = (Guid?)null,
                        displayName = "Diana",
                        ageCategory = "Adult",
                        attendanceStatus = "Attending",
                        menuSelectionsJson = "[]",
                        transportSelectionJson = "[]",
                        accommodationSelectionJson = "[]",
                        dietaryJson = "[]",
                        isUnnamedCompanion = false
                    }
                },
                answers = Array.Empty<object>(),
                consentSnapshot = (string?)null
            });

        Assert.Equal(HttpStatusCode.Conflict, submitResponse.StatusCode);
    }

    [Fact]
    public async Task RSVP_Idempotency_Concurrency_And_Revision_Chain()
    {
        var scenario = await CreateOpenScenarioAsync("rsvp-idempotency");
        var firstBody = CreateSubmissionBody(
            scenario.GuestId,
            expectedRevision: 0,
            contactName: "Operación original");
        const string firstKey = "attempt-idempotency-000001";

        var concurrent = await Task.WhenAll(
            PostGuestRsvpAsync(scenario.Token, firstKey, firstBody),
            PostGuestRsvpAsync(scenario.Token, firstKey, firstBody));
        try
        {
            Assert.All(
                concurrent,
                response => Assert.Equal(
                    HttpStatusCode.OK,
                    response.StatusCode));
            var firstPayload = await concurrent[0].Content
                .ReadFromJsonAsync<JsonElement>();
            var retryPayload = await concurrent[1].Content
                .ReadFromJsonAsync<JsonElement>();
            Assert.Equal(
                firstPayload.GetProperty("id").GetGuid(),
                retryPayload.GetProperty("id").GetGuid());
            Assert.Equal(
                1,
                firstPayload.GetProperty("revisionNumber").GetInt32());
        }
        finally
        {
            foreach (var response in concurrent)
            {
                response.Dispose();
            }
        }

        using var conflictingIdempotency = await PostGuestRsvpAsync(
            scenario.Token,
            firstKey,
            CreateSubmissionBody(
                scenario.GuestId,
                expectedRevision: 0,
                contactName: "Contenido diferente"));
        Assert.Equal(
            HttpStatusCode.Conflict,
            conflictingIdempotency.StatusCode);

        using var secondRevision = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-idempotency-000002",
            CreateSubmissionBody(
                scenario.GuestId,
                expectedRevision: 1,
                contactName: "Segunda revisión"));
        Assert.Equal(HttpStatusCode.OK, secondRevision.StatusCode);
        var secondPayload = await secondRevision.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            2,
            secondPayload.GetProperty("revisionNumber").GetInt32());

        using var staleRevision = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-idempotency-000003",
            CreateSubmissionBody(
                scenario.GuestId,
                expectedRevision: 1,
                contactName: "Edición obsoleta"));
        Assert.Equal(HttpStatusCode.Conflict, staleRevision.StatusCode);
        var stalePayload = await staleRevision.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.True(stalePayload.GetProperty("reloadRequired").GetBoolean());
        Assert.Equal(
            2,
            stalePayload.GetProperty("currentRevision").GetInt32());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlannytDbContext>();
        var submissions = await dbContext.RsvpSubmissions
            .Where(submission =>
                submission.OrganizationId
                == scenario.Session.OrganizationId
                && submission.EventId == scenario.EventId
                && submission.InvitationGroupId == scenario.GroupId)
            .OrderBy(submission => submission.RevisionNumber)
            .ToListAsync();
        Assert.Equal(2, submissions.Count);
        Assert.Null(submissions[0].PreviousSubmissionId);
        Assert.Equal(
            submissions[0].Id,
            submissions[1].PreviousSubmissionId);
    }

    [Fact]
    public async Task RSVP_Rolls_Back_All_Projections_On_Intermediate_Failure()
    {
        var scenario = await CreateOpenScenarioAsync("rsvp-rollback");
        using var failed = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-rollback-000001",
            CreateSubmissionBody(
                scenario.GuestId,
                expectedRevision: 0,
                contactName: "Rollback",
                duplicateAnswers: true));

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            failed.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlannytDbContext>();
        Assert.False(await dbContext.RsvpSubmissions.AnyAsync(submission =>
            submission.OrganizationId
            == scenario.Session.OrganizationId
            && submission.EventId == scenario.EventId
            && submission.InvitationGroupId == scenario.GroupId));
        Assert.False(await dbContext.CurrentGuestRsvps.AnyAsync(current =>
            current.OrganizationId
            == scenario.Session.OrganizationId
            && current.EventId == scenario.EventId
            && current.InvitationGroupId == scenario.GroupId));
        Assert.False(await dbContext.GuestDietaryAndAccessibilities
            .AnyAsync(data =>
                data.OrganizationId
                == scenario.Session.OrganizationId
                && data.EventId == scenario.EventId
                && data.EventGuestId == scenario.GuestId));
    }

    [Fact]
    public async Task RSVP_Group_Exception_Opens_And_Closes_Public_Window()
    {
        var scenario = await CreateOpenScenarioAsync(
            "rsvp-exception",
            closeAfterSetup: true,
            allowChanges: true);
        using var openRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/rsvp/groups/{scenario.GroupId}/exception",
            scenario.Session.AccessToken,
            JsonContent.Create(new
            {
                expiresAt = FutureExpiry,
                reason = "Atención autorizada por soporte"
            }));
        using var opened = await factory.CreateClient().SendAsync(openRequest);
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);

        using var accepted = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-exception-000001",
            CreateSubmissionBody(
                scenario.GuestId,
                expectedRevision: 0,
                contactName: "Excepción activa"));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        using var closeRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/rsvp/groups/{scenario.GroupId}/exception/close",
            scenario.Session.AccessToken);
        using var closed = await factory.CreateClient().SendAsync(closeRequest);
        Assert.Equal(HttpStatusCode.NoContent, closed.StatusCode);

        using var rejected = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-exception-000002",
            CreateSubmissionBody(
                scenario.GuestId,
                expectedRevision: 1,
                contactName: "Excepción cerrada"));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlannytDbContext>();
        var exception = await dbContext.RsvpGroupExceptions
            .SingleAsync(entity =>
                entity.OrganizationId
                == scenario.Session.OrganizationId
                && entity.EventId == scenario.EventId
                && entity.InvitationGroupId == scenario.GroupId);
        Assert.Equal(RsvpGroupExceptionStatus.Closed, exception.Status);
        Assert.Equal(
            scenario.Session.UserAccountId,
            exception.ClosedBy);
        Assert.True(await dbContext.AuditEntries.AnyAsync(entry =>
            entry.OrganizationId
            == scenario.Session.OrganizationId
            && entry.EventId == scenario.EventId
            && entry.Action
            == AuditActions.RsvpGroupExceptionClosed.Value));
        Assert.True(await dbContext.AuditEntries.AnyAsync(entry =>
            entry.OrganizationId
            == scenario.Session.OrganizationId
            && entry.EventId == scenario.EventId
            && entry.Action
            == AuditActions.RsvpGroupExceptionOpened.Value));
    }

    [Fact]
    public async Task RSVP_Expired_Or_Other_Group_Exception_Does_Not_Open_Window()
    {
        var scenario = await CreateOpenScenarioAsync(
            "rsvp-exception-scope",
            closeAfterSetup: true);
        var otherGroupId = await CreateGroupAsync(
            scenario.Session,
            scenario.EventId,
            "Grupo con excepción ajena",
            1);
        var otherEventId = await CreateConfirmedEventAsync(scenario.Session);
        var otherEventGroupId = await CreateGroupAsync(
            scenario.Session,
            otherEventId,
            "Grupo de otro evento",
            1);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<PlannytDbContext>();
            var createdInThePast = DateTimeOffset.UtcNow.AddHours(-2);
            var expired = RsvpGroupException.Create(
                scenario.Session.OrganizationId,
                scenario.EventId,
                scenario.GroupId,
                DateTimeOffset.UtcNow.AddHours(-1),
                "Excepción ya expirada",
                scenario.Session.UserAccountId,
                createdInThePast);
            var otherGroup = RsvpGroupException.Create(
                scenario.Session.OrganizationId,
                scenario.EventId,
                otherGroupId,
                FutureExpiry,
                "Excepción para otro grupo",
                scenario.Session.UserAccountId,
                DateTimeOffset.UtcNow);
            var otherEvent = RsvpGroupException.Create(
                scenario.Session.OrganizationId,
                otherEventId,
                otherEventGroupId,
                FutureExpiry,
                "Excepción para otro evento",
                scenario.Session.UserAccountId,
                DateTimeOffset.UtcNow);
            dbContext.RsvpGroupExceptions.AddRange(
                expired,
                otherGroup,
                otherEvent);
            await dbContext.SaveChangesAsync();
        }

        using var rejected = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-exception-scope-000001",
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Excepción fuera de alcance"));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
    }

    [Fact]
    public async Task RSVP_Transport_Protects_Last_Seat_And_Promotes_Waitlist()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "rsvp-transport");
        var eventId = await CreateConfirmedEventAsync(session);
        var firstGroupId = await CreateGroupAsync(
            session,
            eventId,
            "Grupo Transporte Uno",
            1);
        var secondGroupId = await CreateGroupAsync(
            session,
            eventId,
            "Grupo Transporte Dos",
            1);
        var firstGuestId = await CreateGuestAsync(
            session,
            eventId,
            firstGroupId,
            "Primera",
            "Persona",
            true);
        var secondGuestId = await CreateGuestAsync(
            session,
            eventId,
            secondGroupId,
            "Segunda",
            "Persona",
            true);
        await PublishInvitationExperienceAsync(session, eventId);
        var firstToken = await GenerateLinkTokenAsync(
            session,
            eventId,
            firstGroupId);
        var secondToken = await GenerateLinkTokenAsync(
            session,
            eventId,
            secondGroupId);
        await PrepareOpenRsvpAsync(session, eventId);
        Guid optionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<PlannytDbContext>();
            var option = EventTransportOption.Create(
                session.OrganizationId,
                eventId,
                "Camioneta única",
                null,
                TransportDirection.ToCeremony,
                "Lobby",
                Now.AddMonths(3),
                null,
                1,
                true,
                1,
                Now);
            dbContext.EventTransportOptions.Add(option);
            await dbContext.SaveChangesAsync();
            optionId = option.Id;
        }

        var submissions = await Task.WhenAll(
            PostGuestRsvpAsync(
                firstToken,
                "attempt-transport-000001",
                CreateSubmissionBody(
                    firstGuestId,
                    0,
                    "Primera Persona",
                    optionId)),
            PostGuestRsvpAsync(
                secondToken,
                "attempt-transport-000002",
                CreateSubmissionBody(
                    secondGuestId,
                    0,
                    "Segunda Persona",
                    optionId)));
        try
        {
            Assert.All(
                submissions,
                response => Assert.Equal(
                    HttpStatusCode.OK,
                    response.StatusCode));
        }
        finally
        {
            foreach (var response in submissions)
            {
                response.Dispose();
            }
        }

        Guid confirmedGuestId;
        string confirmedToken;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<PlannytDbContext>();
            var selections = await dbContext.GuestTransportSelections
                .Where(selection =>
                    selection.OrganizationId == session.OrganizationId
                    && selection.EventId == eventId
                    && selection.EventTransportOptionId == optionId)
                .ToListAsync();
            Assert.Single(selections, selection =>
                selection.Status
                == TransportSelectionStatus.Confirmed);
            Assert.Single(selections, selection =>
                selection.Status
                == TransportSelectionStatus.Waitlisted);
            Assert.NotNull(selections.Single(selection =>
                selection.Status
                == TransportSelectionStatus.Waitlisted).WaitlistSequence);
            confirmedGuestId = selections.Single(selection =>
                selection.Status
                == TransportSelectionStatus.Confirmed).EventGuestId;
            confirmedToken = confirmedGuestId == firstGuestId
                ? firstToken
                : secondToken;
        }

        using var cancellation = await PostGuestRsvpAsync(
            confirmedToken,
            "attempt-transport-000003",
            CreateSubmissionBody(
                confirmedGuestId,
                1,
                "Cancela transporte"));
        Assert.Equal(HttpStatusCode.OK, cancellation.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<PlannytDbContext>();
            var selections = await dbContext.GuestTransportSelections
                .Where(selection =>
                    selection.OrganizationId == session.OrganizationId
                    && selection.EventId == eventId
                    && selection.EventTransportOptionId == optionId)
                .ToListAsync();
            Assert.Single(selections, selection =>
                selection.Status
                == TransportSelectionStatus.Confirmed);
            Assert.Single(selections, selection =>
                selection.Status
                == TransportSelectionStatus.Cancelled);
            Assert.True(await dbContext.AuditEntries.AnyAsync(entry =>
                entry.OrganizationId == session.OrganizationId
                && entry.EventId == eventId
                && entry.Action
                == AuditActions.TransportWaitlistPromoted.Value));
            Assert.True(await dbContext.GuestTransportSelectionHistory
                .AnyAsync(history =>
                    history.OrganizationId == session.OrganizationId
                    && history.EventId == eventId
                    && history.NewStatus
                    == TransportSelectionStatus.Confirmed
                    && history.PreviousStatus
                    == TransportSelectionStatus.Waitlisted));
        }
    }

    [Fact]
    public async Task RSVP_Sensitive_Audit_Reconciliation_And_Dead_Key_Table()
    {
        var scenario = await CreateOpenScenarioAsync("rsvp-sensitive");
        using var submitted = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-sensitive-000001",
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Datos sensibles",
                dietaryJson:
                    """{"allergies":"Nuez de prueba","dietaryRestrictions":"Sin lácteos","accessibilityRequirements":"Rampa","additionalNotes":"Nota privada","consentGranted":true}"""));
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var publicPayload = await submitted.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "{}",
            publicPayload.GetProperty("guests")[0]
                .GetProperty("dietaryJson")
                .GetString());

        using var sensitiveRequest =
            TestSessionFactory.CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/rsvp/sensitive-data",
                scenario.Session.AccessToken);
        using var sensitiveResponse = await factory.CreateClient()
            .SendAsync(sensitiveRequest);
        Assert.Equal(HttpStatusCode.OK, sensitiveResponse.StatusCode);
        var sensitive = await sensitiveResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            "Nuez de prueba",
            sensitive[0].GetProperty("allergies").GetString());

        using var exportRequest =
            TestSessionFactory.CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/rsvp/exports/sensitive",
                scenario.Session.AccessToken);
        using var exported = await factory.CreateClient()
            .SendAsync(exportRequest);
        Assert.Equal(HttpStatusCode.OK, exported.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<PlannytDbContext>();
            await dbContext.CurrentGuestRsvps
                .Where(current =>
                    current.OrganizationId
                    == scenario.Session.OrganizationId
                    && current.EventId == scenario.EventId
                    && current.EventGuestId == scenario.GuestId)
                .ExecuteDeleteAsync();
        }

        using var diagnosisRequest =
            TestSessionFactory.CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/rsvp/projections/diagnosis",
                scenario.Session.AccessToken);
        using var diagnosis = await factory.CreateClient()
            .SendAsync(diagnosisRequest);
        Assert.Equal(HttpStatusCode.OK, diagnosis.StatusCode);
        var diagnosisPayload = await diagnosis.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.True(
            diagnosisPayload.GetProperty("issuesDetected").GetInt32() > 0);
        Assert.Contains(
            diagnosisPayload.GetProperty("issues").EnumerateArray(),
            issue => issue.GetProperty("code").GetString()
                     == "current_guest_rsvp.missing");

        using var repairRequest =
            TestSessionFactory.CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/rsvp/projections/repair",
                scenario.Session.AccessToken);
        using var repair = await factory.CreateClient()
            .SendAsync(repairRequest);
        Assert.Equal(HttpStatusCode.OK, repair.StatusCode);
        var repairPayload = await repair.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.True(
            repairPayload.GetProperty("issuesRepaired").GetInt32() > 0);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<PlannytDbContext>();
        Assert.True(await verificationDb.CurrentGuestRsvps.AnyAsync(current =>
            current.OrganizationId == scenario.Session.OrganizationId
            && current.EventId == scenario.EventId
            && current.EventGuestId == scenario.GuestId));
        var sensitiveAudits = await verificationDb.AuditEntries
            .Where(entry =>
                entry.OrganizationId
                == scenario.Session.OrganizationId
                && entry.EventId == scenario.EventId
                && (entry.Action
                    == AuditActions.GuestSensitiveDataViewed.Value
                    || entry.Action
                    == AuditActions.GuestSensitiveDataExported.Value))
            .ToListAsync();
        Assert.Equal(2, sensitiveAudits.Count);
        Assert.All(
            sensitiveAudits,
            audit =>
            {
                Assert.Contains("recordCount", audit.Metadata);
                Assert.Contains("operationType", audit.Metadata);
                Assert.DoesNotContain("Nuez de prueba", audit.Metadata);
                Assert.NotEmpty(audit.CorrelationId);
                Assert.NotEqual(default, audit.OccurredAt);
            });

        await verificationDb.Database.OpenConnectionAsync();
        await using var command = verificationDb.Database
            .GetDbConnection()
            .CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM information_schema.tables "
            + "WHERE table_schema = 'public' "
            + "AND table_name = 'guest_access_token_keys'";
        var tableCount = Convert.ToInt32(
            await command.ExecuteScalarAsync());
        Assert.Equal(0, tableCount);
    }

    [Fact]
    public async Task RSVP_Manual_Capture_And_Support_Correction_Create_Revision_Chain()
    {
        var scenario = await CreateOpenScenarioAsync(
            "rsvp-support-correction");
        using var first = await PostManualRsvpAsync(
            scenario,
            "attempt-manual-000001",
            "PlannerManual",
            "Respuesta recibida por teléfono",
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Captura inicial"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstPayload = await first.Content
            .ReadFromJsonAsync<JsonElement>();

        using var correction = await PostManualRsvpAsync(
            scenario,
            "attempt-support-000001",
            "SupportCorrection",
            "Corrección solicitada por la persona invitada",
            CreateSubmissionBody(
                scenario.GuestId,
                1,
                "Captura corregida"));
        Assert.Equal(HttpStatusCode.Created, correction.StatusCode);
        var correctionPayload = await correction.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(
            firstPayload.GetProperty("id").GetGuid(),
            correctionPayload.GetProperty("id").GetGuid());
        Assert.Equal(
            2,
            correctionPayload.GetProperty("revisionNumber").GetInt32());

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlannytDbContext>();
        var submissions = await dbContext.RsvpSubmissions
            .Where(submission =>
                submission.OrganizationId
                == scenario.Session.OrganizationId
                && submission.EventId == scenario.EventId
                && submission.InvitationGroupId == scenario.GroupId)
            .OrderBy(submission => submission.RevisionNumber)
            .ToListAsync();
        Assert.Equal(2, submissions.Count);
        Assert.Equal(
            RsvpSubmissionSource.PlannerManual,
            submissions[0].Source);
        Assert.Equal(
            RsvpSubmissionSource.SupportCorrection,
            submissions[1].Source);
        Assert.Equal(
            submissions[0].Id,
            submissions[1].PreviousSubmissionId);
        var correctionAudit = await dbContext.AuditEntries
            .SingleAsync(entry =>
                entry.OrganizationId
                == scenario.Session.OrganizationId
                && entry.EventId == scenario.EventId
                && entry.Action
                == AuditActions.RsvpSupportCorrected.Value);
        Assert.Contains(
            "Corrección solicitada por la persona invitada",
            correctionAudit.Metadata);
    }

    [Fact]
    public async Task RSVP_Client_Portal_Capture_Uses_The_Same_Revision_Chain()
    {
        var scenario = await CreateOpenScenarioAsync(
            "rsvp-client-portal");
        var portal = await CreatePortalSessionAsync(
            scenario.Session,
            scenario.EventId);

        using var dashboardRequest =
            TestSessionFactory.CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/client-portal/events/{scenario.EventId}/rsvp/dashboard",
                portal.AccessToken);
        using var dashboard = await factory.CreateClient()
            .SendAsync(dashboardRequest);
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);

        using var first = await PostPortalRsvpAsync(
            scenario,
            portal.AccessToken,
            "attempt-client-portal-000001",
            "ClientPortal",
            "Captura realizada en portal",
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Captura de portal"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var correction = await PostPortalRsvpAsync(
            scenario,
            portal.AccessToken,
            "attempt-client-portal-000002",
            "SupportCorrection",
            "Corrección desde portal",
            CreateSubmissionBody(
                scenario.GuestId,
                1,
                "Corrección de portal"));
        Assert.Equal(HttpStatusCode.Created, correction.StatusCode);

        using var sensitiveAttempt = await PostPortalRsvpAsync(
            scenario,
            portal.AccessToken,
            "attempt-client-portal-000003",
            "ClientPortal",
            "Intento sensible sin concesión",
            CreateSubmissionBody(
                scenario.GuestId,
                2,
                "Dato sensible denegado",
                dietaryJson:
                    """{"allergies":"Nuez","consentGranted":true}"""));
        Assert.Equal(
            HttpStatusCode.Forbidden,
            sensitiveAttempt.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlannytDbContext>();
        var submissions = await dbContext.RsvpSubmissions
            .Where(submission =>
                submission.OrganizationId
                == scenario.Session.OrganizationId
                && submission.EventId == scenario.EventId
                && submission.InvitationGroupId == scenario.GroupId)
            .OrderBy(submission => submission.RevisionNumber)
            .ToListAsync();
        Assert.Equal(2, submissions.Count);
        Assert.Equal(RsvpSubmissionSource.ClientPortal, submissions[0].Source);
        Assert.Equal(
            RsvpSubmissionSource.SupportCorrection,
            submissions[1].Source);
        Assert.Equal(
            submissions[0].Id,
            submissions[1].PreviousSubmissionId);
        Assert.All(
            submissions,
            submission => Assert.Equal(
                portal.UserAccountId,
                submission.SubmittedByUserId));
    }

    [Fact]
    public async Task RSVP_Database_Enforces_Idempotency_And_Revision_Uniqueness()
    {
        var scenario = await CreateOpenScenarioAsync("rsvp-db-unique");
        const string firstKey = "attempt-db-unique-000001";
        using var submitted = await PostGuestRsvpAsync(
            scenario.Token,
            firstKey,
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Restricciones únicas"));
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlannytDbContext>();
        var original = await dbContext.RsvpSubmissions
            .AsNoTracking()
            .SingleAsync(submission =>
                submission.OrganizationId
                == scenario.Session.OrganizationId
                && submission.EventId == scenario.EventId
                && submission.InvitationGroupId == scenario.GroupId);
        var duplicateKey = RsvpSubmission.Create(
            original.OrganizationId,
            original.EventId,
            original.InvitationGroupId,
            original.RsvpFormVersionId,
            null,
            2,
            RsvpSubmissionSource.PlannerManual,
            RsvpOverallStatus.Confirmed,
            scenario.Session.UserAccountId,
            "Llave repetida",
            null,
            null,
            null,
            null,
            null,
            firstKey,
            original.Id,
            Now,
            new string('A', 64));
        dbContext.RsvpSubmissions.Add(duplicateKey);
        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());

        dbContext.ChangeTracker.Clear();
        var duplicateRevision = RsvpSubmission.Create(
            original.OrganizationId,
            original.EventId,
            original.InvitationGroupId,
            original.RsvpFormVersionId,
            null,
            1,
            RsvpSubmissionSource.PlannerManual,
            RsvpOverallStatus.Confirmed,
            scenario.Session.UserAccountId,
            "Revisión repetida",
            null,
            null,
            null,
            null,
            null,
            "attempt-db-revision-000002",
            null,
            Now,
            new string('B', 64));
        dbContext.RsvpSubmissions.Add(duplicateRevision);
        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task RSVP_Database_Rejects_Cross_Tenant_Transport_Projection()
    {
        var scenario = await CreateOpenScenarioAsync(
            "rsvp-db-transport-tenant");
        using var submitted = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-db-tenant-000001",
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Aislamiento de transporte"));
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var otherTenant = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "rsvp-db-other-tenant");

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PlannytDbContext>();
        var submission = await dbContext.RsvpSubmissions
            .AsNoTracking()
            .SingleAsync(entity =>
                entity.OrganizationId
                == scenario.Session.OrganizationId
                && entity.EventId == scenario.EventId
                && entity.InvitationGroupId == scenario.GroupId);
        var option = EventTransportOption.Create(
            scenario.Session.OrganizationId,
            scenario.EventId,
            "Opción del tenant original",
            null,
            TransportDirection.ToCeremony,
            null,
            null,
            null,
            10,
            false,
            0,
            Now);
        dbContext.EventTransportOptions.Add(option);
        await dbContext.SaveChangesAsync();

        var invalidProjection = GuestTransportSelection.Create(
            otherTenant.OrganizationId,
            scenario.EventId,
            scenario.GuestId,
            option.Id,
            TransportSelectionStatus.Confirmed,
            submission.Id,
            null,
            Now);
        dbContext.GuestTransportSelections.Add(invalidProjection);
        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task RSVP_Public_Rejects_Suspended_Event_Revoked_And_Expired_Link()
    {
        var scenario = await CreateOpenScenarioAsync(
            "rsvp-public-link-states");
        using (var suspendRequest =
               TestSessionFactory.CreateAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/status",
                   scenario.Session.AccessToken,
                   JsonContent.Create(new
                   {
                       newStatus = "Suspended",
                       reason = "Prueba de suspensión RSVP"
                   })))
        using (var suspended = await factory.CreateClient()
                   .SendAsync(suspendRequest))
        {
            Assert.Equal(HttpStatusCode.OK, suspended.StatusCode);
        }

        using var suspendedSubmit = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-suspended-event-000001",
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Evento suspendido"));
        Assert.Equal(
            HttpStatusCode.Conflict,
            suspendedSubmit.StatusCode);

        using (var resumeRequest =
               TestSessionFactory.CreateAuthorizedRequest(
                   HttpMethod.Post,
                   $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/status",
                   scenario.Session.AccessToken,
                   JsonContent.Create(new
                   {
                       newStatus = "Confirmed",
                       reason = "Reanudar prueba RSVP"
                   })))
        using (var resumed = await factory.CreateClient()
                   .SendAsync(resumeRequest))
        {
            Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<PlannytDbContext>();
            var link = await dbContext.GuestAccessLinks
                .SingleAsync(entity =>
                    entity.OrganizationId
                    == scenario.Session.OrganizationId
                    && entity.EventId == scenario.EventId
                    && entity.InvitationGroupId == scenario.GroupId
                    && entity.Status == GuestAccessLinkStatus.Active);
            link.Revoke(DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync();
        }

        using var revoked = await PostGuestRsvpAsync(
            scenario.Token,
            "attempt-revoked-link-000001",
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Enlace revocado"));
        Assert.Equal(HttpStatusCode.NotFound, revoked.StatusCode);

        var expiringToken = await GenerateLinkTokenAsync(
            scenario.Session,
            scenario.EventId,
            scenario.GroupId,
            DateTimeOffset.UtcNow.AddSeconds(2));
        await Task.Delay(TimeSpan.FromMilliseconds(2200));
        using var expired = await PostGuestRsvpAsync(
            expiringToken,
            "attempt-expired-link-000001",
            CreateSubmissionBody(
                scenario.GuestId,
                0,
                "Enlace expirado"));
        Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);
    }

    [Fact]
    public async Task RSVP_Public_Invalid_Token()
    {
        using var stateResponse = await factory.CreateClient().GetAsync(
            "/api/guest/rsvp/token-que-no-existe/state");

        Assert.Equal(HttpStatusCode.NotFound, stateResponse.StatusCode);
    }

    // =========================================================================
    // Multi-Tenant Isolation (DB level)
    // =========================================================================

    [Fact]
    public async Task RSVP_Tenant_Cannot_Read_Other_Org_Menus()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var account = UserAccount.Create(
            $"menu-{Guid.NewGuid():N}@example.invalid",
            $"menu-{Guid.NewGuid():N}@EXAMPLE.INVALID",
            "not-a-real-password-hash",
            Now);
        var orgA = Organization.Create(
            "Org Menú A", $"menu-a-{Guid.NewGuid():N}",
            OrganizationType.IndependentPlanner, "America/Matamoros", "MX", "MXN", Now);
        var orgB = Organization.Create(
            "Org Menú B", $"menu-b-{Guid.NewGuid():N}",
            OrganizationType.IndependentPlanner, "America/Matamoros", "MX", "MXN", Now);
        var eventA = Event.Create(orgA.Id, "Evento A", "Wedding",
            Now.AddMonths(1), Now.AddMonths(1).AddHours(8),
            "America/Matamoros", "Reynosa", "MX", "Desc", 100, account.Id, Now);
        var eventB = Event.Create(orgB.Id, "Evento B", "Wedding",
            Now.AddMonths(1), Now.AddMonths(1).AddHours(8),
            "America/Matamoros", "Reynosa", "MX", "Desc", 100, account.Id, Now);

        dbContext.AddRange(account, orgA, orgB, eventA, eventB);
        await dbContext.SaveChangesAsync();

        var menuA = EventMenu.Create(orgA.Id, eventA.Id, "Menú A", null,
            MenuCategory.AdultMeal, true, 1, 3, 0, Now);
        var optionA = EventMenuOption.Create(orgA.Id, menuA.Id, "Opción A",
            null, "", null, 0, Now);
        dbContext.AddRange(menuA, optionA);
        await dbContext.SaveChangesAsync();

        var crossTenantQuery = await dbContext.EventMenuOptions
            .AsNoTracking()
            .Where(o => o.OrganizationId == orgB.Id && o.EventMenuId == menuA.Id)
            .AnyAsync();

        Assert.False(crossTenantQuery);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task RSVP_Tenant_Cannot_Read_Other_Org_Submissions()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var account = UserAccount.Create(
            $"sub-{Guid.NewGuid():N}@example.invalid",
            $"sub-{Guid.NewGuid():N}@EXAMPLE.INVALID",
            "not-a-real-password-hash",
            Now);
        var orgA = Organization.Create(
            "Org Sub A", $"sub-a-{Guid.NewGuid():N}",
            OrganizationType.IndependentPlanner, "America/Matamoros", "MX", "MXN", Now);
        var orgB = Organization.Create(
            "Org Sub B", $"sub-b-{Guid.NewGuid():N}",
            OrganizationType.IndependentPlanner, "America/Matamoros", "MX", "MXN", Now);
        var eventA = Event.Create(orgA.Id, "Evento A", "Wedding",
            Now.AddMonths(1), Now.AddMonths(1).AddHours(8),
            "America/Matamoros", "Reynosa", "MX", "Desc", 8, account.Id, Now);
        dbContext.AddRange(account, orgA, orgB, eventA);
        await dbContext.SaveChangesAsync();

        var groupA = InvitationGroup.Create(
            orgA.Id, eventA.Id, InvitationGroupType.Family,
            "Familia A", "Contacto", null, "a@example.com",
            2, false, 0, "Manual", null, account.Id, Now);
        var form = RsvpForm.Create(orgA.Id, eventA.Id, account.Id, Now);
        var version = RsvpFormVersion.Create(
            orgA.Id, form.Id, 1,
            "{}", "[]", "[]", "[]", "[]", account.Id, Now);
        dbContext.AddRange(form, version, groupA);
        await dbContext.SaveChangesAsync();

        var submission = RsvpSubmission.Create(
            orgA.Id, eventA.Id, groupA.Id, version.Id,
            null, 1, RsvpSubmissionSource.PlannerManual,
            RsvpOverallStatus.Confirmed, null,
            "Contacto A", "a@example.com", null,
            null, null, null,
            $"immutable:{Guid.NewGuid():N}", null, Now);
        dbContext.RsvpSubmissions.Add(submission);
        await dbContext.SaveChangesAsync();

        var crossTenantRead = await dbContext.RsvpSubmissions
            .AsNoTracking()
            .Where(s => s.OrganizationId == orgB.Id && s.Id == submission.Id)
            .AnyAsync();

        Assert.False(crossTenantRead);

        await transaction.RollbackAsync();
    }

    // =========================================================================
    // Immutability (DB level)
    // =========================================================================

    [Fact]
    public async Task RSVP_Submission_Immutable()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var account = UserAccount.Create(
            $"imm-sub-{Guid.NewGuid():N}@example.invalid",
            $"imm-sub-{Guid.NewGuid():N}@EXAMPLE.INVALID",
            "not-a-real-password-hash",
            Now);
        var org = Organization.Create(
            "Org Immutable Sub", $"imm-sub-{Guid.NewGuid():N}",
            OrganizationType.IndependentPlanner, "America/Matamoros", "MX", "MXN", Now);
        var evt = Event.Create(org.Id, "Evento Immutable", "Wedding",
            Now.AddMonths(1), Now.AddMonths(1).AddHours(8),
            "America/Matamoros", "Reynosa", "MX", "Desc", 50, account.Id, Now);
        var group = InvitationGroup.Create(
            org.Id, evt.Id, InvitationGroupType.Family,
            "Familia Imm", "Contacto", null, "imm@example.com",
            2, false, 0, "Manual", null, account.Id, Now);
        var form = RsvpForm.Create(org.Id, evt.Id, account.Id, Now);
        var version = RsvpFormVersion.Create(
            org.Id, form.Id, 1,
            "{}", "[]", "[]", "[]", "[]", account.Id, Now);
        dbContext.AddRange(account, org, evt, group, form, version);
        await dbContext.SaveChangesAsync();

        var originalIdempotencyKey = $"sub-original:{Guid.NewGuid():N}";
        var originalContactName = "Nombre Original";
        var submission = RsvpSubmission.Create(
            org.Id, evt.Id, group.Id, version.Id,
            null, 1, RsvpSubmissionSource.PlannerManual,
            RsvpOverallStatus.Confirmed, null,
            originalContactName, "orig@example.com", "+521111111111",
            null, null, null,
            originalIdempotencyKey, null, Now);
        dbContext.RsvpSubmissions.Add(submission);
        await dbContext.SaveChangesAsync();

        var loaded = await dbContext.RsvpSubmissions
            .SingleAsync(s => s.Id == submission.Id);
        Assert.Equal(originalContactName, loaded.ContactNameSnapshot);
        Assert.Equal(originalIdempotencyKey, loaded.IdempotencyKey);
        Assert.NotEqual(Guid.Empty, loaded.Id);
        Assert.Equal(1, loaded.RevisionNumber);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task RSVP_FormVersion_Snapshot_Immutable()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        var account = UserAccount.Create(
            $"imm-ver-{Guid.NewGuid():N}@example.invalid",
            $"imm-ver-{Guid.NewGuid():N}@EXAMPLE.INVALID",
            "not-a-real-password-hash",
            Now);
        var org = Organization.Create(
            "Org Imm Ver", $"imm-ver-{Guid.NewGuid():N}",
            OrganizationType.IndependentPlanner, "America/Matamoros", "MX", "MXN", Now);
        var evt = Event.Create(org.Id, "Evento Imm Ver", "Wedding",
            Now.AddMonths(1), Now.AddMonths(1).AddHours(8),
            "America/Matamoros", "Reynosa", "MX", "Desc", 50, account.Id, Now);
        dbContext.AddRange(account, org, evt);
        await dbContext.SaveChangesAsync();

        var form = RsvpForm.Create(org.Id, evt.Id, Guid.NewGuid(), Now);
        dbContext.RsvpForms.Add(form);
        await dbContext.SaveChangesAsync();

        var originalQuestions = "[\"pregunta1\"]";
        var originalMenu = "[\"menu1\"]";
        var version = RsvpFormVersion.Create(
            org.Id, form.Id, 1,
            "{}", originalQuestions, originalMenu, "[]", "[]",
            Guid.NewGuid(), Now);
        version.Approve(Guid.NewGuid(), Now.AddMinutes(1));
        version.Publish(Now.AddMinutes(2));
        dbContext.RsvpFormVersions.Add(version);
        await dbContext.SaveChangesAsync();

        var loaded = await dbContext.RsvpFormVersions
            .SingleAsync(v => v.Id == version.Id);
        Assert.Equal(originalQuestions, loaded.QuestionsSnapshot);
        Assert.Equal(originalMenu, loaded.MenuSnapshot);
        Assert.NotNull(loaded.PublishedAt);
        Assert.NotEqual(Guid.Empty, loaded.Id);

        await transaction.RollbackAsync();
    }

    // =========================================================================
    // Private helpers — Event & Guest setup
    // =========================================================================

    private async Task<HttpResponseMessage> PostGuestRsvpAsync(
        string token,
        string idempotencyKey,
        object body)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/guest/rsvp/{token}/submit")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await factory.CreateClient().SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostManualRsvpAsync(
        OpenRsvpScenario scenario,
        string idempotencyKey,
        string source,
        string reason,
        object submission)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{scenario.Session.OrganizationId}/events/{scenario.EventId}/rsvp/groups/{scenario.GroupId}/manual-capture",
            scenario.Session.AccessToken,
            JsonContent.Create(new
            {
                source,
                reason,
                submission
            }));
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await factory.CreateClient().SendAsync(request);
    }

    private async Task<HttpResponseMessage> PostPortalRsvpAsync(
        OpenRsvpScenario scenario,
        string accessToken,
        string idempotencyKey,
        string source,
        string reason,
        object submission)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/client-portal/events/{scenario.EventId}/rsvp/groups/{scenario.GroupId}/manual-capture",
            accessToken,
            JsonContent.Create(new
            {
                source,
                reason,
                submission
            }));
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await factory.CreateClient().SendAsync(request);
    }

    private async Task<PortalRsvpSession> CreatePortalSessionAsync(
        TestSession owner,
        Guid eventId)
    {
        var email = $"rsvp-portal-{Guid.NewGuid():N}@example.invalid";
        using var invitationRequest =
            TestSessionFactory.CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/organizations/{owner.OrganizationId}/events/{eventId}/access/invitations",
                owner.AccessToken,
                JsonContent.Create(new
                {
                    targetEmail = email,
                    intendedEventRole = "ClientPrimary"
                }));
        using var invitationResponse = await factory.CreateClient()
            .SendAsync(invitationRequest);
        invitationResponse.EnsureSuccessStatusCode();
        var invitation = await invitationResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        var invitationUrl = invitation.GetProperty("invitationUrl").GetString()
            ?? throw new InvalidOperationException(
                "No se recibió la invitación del portal.");
        var token = new Uri(invitationUrl).Segments[^1].Trim('/');
        using var acceptance = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{token}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Cliente",
                lastName = "Portal RSVP",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });
        acceptance.EnsureSuccessStatusCode();
        var auth = await acceptance.Content
            .ReadFromJsonAsync<JsonElement>();
        return new PortalRsvpSession(
            auth.GetProperty("userAccountId").GetGuid(),
            auth.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException(
                "No se recibió access token del portal."));
    }

    private async Task<OpenRsvpScenario> CreateOpenScenarioAsync(
        string prefix,
        bool closeAfterSetup = false,
        bool allowChanges = true)
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            prefix);
        var eventId = await CreateConfirmedEventAsync(session);
        var groupId = await CreateGroupAsync(
            session,
            eventId,
            $"Grupo {prefix}",
            1);
        var guestId = await CreateGuestAsync(
            session,
            eventId,
            groupId,
            "Invitado",
            prefix,
            true);
        await PublishInvitationExperienceAsync(session, eventId);
        var token = await GenerateLinkTokenAsync(
            session,
            eventId,
            groupId);
        await PrepareOpenRsvpAsync(
            session,
            eventId,
            allowChanges);
        if (closeAfterSetup)
        {
            await CloseSettingsAsync(session, eventId);
        }

        return new OpenRsvpScenario(
            session,
            eventId,
            groupId,
            guestId,
            token);
    }

    private async Task PrepareOpenRsvpAsync(
        TestSession session,
        Guid eventId,
        bool allowChanges = true)
    {
        await PutSettingsAsync(session, eventId, new
        {
            opensAt = Now.AddDays(-1),
            closesAt = Now.AddDays(90),
            timeZone = "America/Matamoros",
            allowChangesAfterSubmission = allowChanges,
            changesCloseAt = allowChanges
                ? Now.AddDays(60)
                : (DateTimeOffset?)null,
            allowTentativeResponse = false,
            allowGroupDecline = true,
            requireResponseForEveryNamedGuest = true,
            requireCompanionNames = false,
            allowContactInformationUpdate = true,
            showAttendanceSummaryAfterSubmission = true,
            confirmationTitle = "Confirmación",
            confirmationMessage = "Respuesta recibida",
            declineMessage = "Ausencia recibida",
            closedMessage = "RSVP cerrado",
            privacyNotice = "Aviso de privacidad",
            sensitiveDataConsentText = "Autorizo el tratamiento"
        });
        await PublishSettingsAsync(session, eventId);
        await OpenSettingsAsync(session, eventId);
        await CreateAndPublishFormAsync(session, eventId);
    }

    private static object CreateSubmissionBody(
        Guid guestId,
        int expectedRevision,
        string contactName,
        Guid? transportOptionId = null,
        string dietaryJson = "{}",
        bool duplicateAnswers = false)
    {
        object[] answers = duplicateAnswers
            ?
            [
                new
                {
                    questionId = "q1",
                    guestId = (Guid?)guestId,
                    answerValue = "\"Uno\"",
                    displayValue = "Uno"
                },
                new
                {
                    questionId = "q1",
                    guestId = (Guid?)guestId,
                    answerValue = "\"Dos\"",
                    displayValue = "Dos"
                }
            ]
            : [];
        return new
        {
            expectedRevision,
            overallStatus = "Confirmed",
            contactName,
            contactEmail = "rsvp@example.invalid",
            contactPhone = "+528991234567",
            guests = new[]
            {
                new
                {
                    eventGuestId = (Guid?)guestId,
                    displayName = contactName,
                    ageCategory = "Adult",
                    attendanceStatus = "Attending",
                    menuSelectionsJson = "{}",
                    transportSelectionJson =
                        transportOptionId.HasValue
                            ? JsonSerializer.Serialize(new
                            {
                                transportOptionId
                            })
                            : "{}",
                    accommodationSelectionJson = "{}",
                    dietaryJson,
                    isUnnamedCompanion = false
                }
            },
            answers,
            consentSnapshot = """{"accepted":true}"""
        };
    }

    private async Task<Guid> CreateConfirmedEventAsync(TestSession session)
    {
        using var createRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events",
            session.AccessToken,
            JsonContent.Create(new
            {
                name = "Evento RSVP Test",
                eventType = "Boda",
                startDateTime = Now.AddMonths(3),
                endDateTime = Now.AddMonths(3).AddHours(8),
                timeZone = "America/Matamoros",
                city = "Reynosa",
                countryCode = "MX",
                sharedDescription = "Evento de pruebas RSVP",
                estimatedGuestCount = 100
            }));
        using var createResponse = await factory.CreateClient().SendAsync(createRequest);
        createResponse.EnsureSuccessStatusCode();
        var eventPayload = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventId = eventPayload.GetProperty("id").GetGuid();

        using var confirmRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/status",
            session.AccessToken,
            JsonContent.Create(new
            {
                newStatus = "Confirmed",
                reason = "Prueba RSVP"
            }));
        using var confirmResponse = await factory.CreateClient().SendAsync(confirmRequest);
        confirmResponse.EnsureSuccessStatusCode();
        return eventId;
    }

    private async Task<Guid> CreateGroupAsync(
        TestSession session, Guid eventId, string name, int capacity)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/groups",
            session.AccessToken,
            JsonContent.Create(new
            {
                groupType = "Family",
                displayName = name,
                contactName = $"{name} Contacto",
                contactPhone = "8991234567",
                contactEmail = $"{name.Replace(" ", "").ToLower()}@example.invalid",
                allowedGuestCount = capacity,
                allowUnnamedCompanions = true,
                maxUnnamedCompanions = 1,
                internalNotes = "Grupo de prueba RSVP",
                tagIds = Array.Empty<Guid>()
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateGuestAsync(
        TestSession session, Guid eventId, Guid groupId,
        string firstName, string lastName, bool primary)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests",
            session.AccessToken,
            JsonContent.Create(new
            {
                invitationGroupId = groupId,
                personId = (Guid?)null,
                firstName,
                lastName,
                email = primary ? $"{firstName.ToLower()}@example.invalid" : null,
                phone = primary ? "8991234567" : null,
                guestType = "Family",
                ageCategory = "Adult",
                isPrimaryContact = primary,
                isNamed = true,
                isPlusOne = false,
                isVip = primary,
                sortOrder = primary ? 0 : 1,
                internalNotes = (string?)null
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    // =========================================================================
    // Private helpers — Invitation experience & access links
    // =========================================================================

    private async Task<Guid> CreateDesignAsync(TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/designs",
            session.AccessToken,
            JsonContent.Create(new
            {
                name = "Invitación RSVP Test",
                templateId = Guid.Parse("33333333-3333-3333-3333-333333333333")
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    private async Task<Guid> SubmitDesignReviewAsync(
        TestSession session, Guid eventId, Guid designId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/designs/{designId}/submit-review",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("versions")[0].GetProperty("id").GetGuid();
    }

    private async Task ApproveDesignAsync(
        TestSession session, Guid eventId, Guid designId, Guid versionId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/designs/{designId}/versions/{versionId}/approve",
            session.AccessToken,
            JsonContent.Create(new { message = "Aprobada para publicación" }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task PublishDesignAsync(
        TestSession session, Guid eventId, Guid designId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/designs/{designId}/publish",
            session.AccessToken,
            JsonContent.Create(new { bypassApprovalForTesting = false }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task PublishInvitationExperienceAsync(
        TestSession session, Guid eventId)
    {
        var designId = await CreateDesignAsync(session, eventId);
        var versionId = await SubmitDesignReviewAsync(session, eventId, designId);
        await ApproveDesignAsync(session, eventId, designId, versionId);
        await PublishDesignAsync(session, eventId, designId);
    }

    private async Task<string> GenerateLinkTokenAsync(
        TestSession session,
        Guid eventId,
        Guid groupId,
        DateTimeOffset? expiresAt = null)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/groups/{groupId}/links",
            session.AccessToken,
            JsonContent.Create(new
            {
                expiresAt = expiresAt ?? FutureExpiry
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var publicUrl = payload.GetProperty("publicUrl").GetString()
            ?? throw new InvalidOperationException("No se recibió el enlace público.");
        return publicUrl[(publicUrl.LastIndexOf("/i/", StringComparison.Ordinal) + 3)..];
    }

    // =========================================================================
    // Private helpers — RSVP Settings endpoints
    // =========================================================================

    private async Task<JsonElement> PutSettingsAsync(
        TestSession session, Guid eventId, object body)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/settings",
            session.AccessToken,
            JsonContent.Create(body));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> GetSettingsAsync(
        TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/settings",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> PublishSettingsAsync(
        TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/settings/publish",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> OpenSettingsAsync(
        TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/settings/open",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> CloseSettingsAsync(
        TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/settings/close",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    // =========================================================================
    // Private helpers — RSVP Form endpoints
    // =========================================================================

    private async Task<JsonElement> CreateFormAsync(
        TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/form",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> GetFormAsync(
        TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/form",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> CreateFormVersionAsync(
        TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/form/version",
            session.AccessToken,
            JsonContent.Create(new
            {
                questionsJson = "[{\"id\":\"q1\",\"label\":\"Nombre\",\"questionType\":\"ShortText\",\"scope\":\"InvitationGroup\",\"category\":\"General\",\"isRequired\":true,\"isActive\":true,\"sortOrder\":1,\"options\":[]}]",
                menuJson = "[]",
                transportJson = "[]",
                accommodationJson = "[]"
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> SubmitFormForReviewAsync(
        TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/form/submit-review",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> ApproveFormVersionAsync(
        TestSession session, Guid eventId, Guid versionId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/form/versions/{versionId}/approve",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> PublishFormVersionAsync(
        TestSession session, Guid eventId, Guid versionId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/rsvp/form/versions/{versionId}/publish",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task CreateAndPublishFormAsync(
        TestSession session, Guid eventId)
    {
        await CreateFormAsync(session, eventId);
        var version = await CreateFormVersionAsync(session, eventId);
        var versionId = version.GetProperty("id").GetGuid();
        await SubmitFormForReviewAsync(session, eventId);
        await ApproveFormVersionAsync(session, eventId, versionId);
        await PublishFormVersionAsync(session, eventId, versionId);
    }

    private sealed record OpenRsvpScenario(
        TestSession Session,
        Guid EventId,
        Guid GroupId,
        Guid GuestId,
        string Token);

    private sealed record PortalRsvpSession(
        Guid UserAccountId,
        string AccessToken);
}
