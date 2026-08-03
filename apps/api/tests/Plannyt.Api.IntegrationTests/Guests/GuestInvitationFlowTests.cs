using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Plannyt.Api.IntegrationTests.Infrastructure;

namespace Plannyt.Api.IntegrationTests.Guests;

[Collection(ApiCollection.Name)]
public sealed class GuestInvitationFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task FullFlow_PublishesPrivateGroupAndReplacesAccessLink()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "guest-full");
        var eventId = await CreateConfirmedEventAsync(session);
        var groupId = await CreateGroupAsync(session, eventId, "Familia Luna", 3);
        await CreateGuestAsync(
            session,
            eventId,
            groupId,
            "Elena",
            "Luna",
            true);
        await CreateGuestAsync(
            session,
            eventId,
            groupId,
            "Mateo",
            "Luna",
            false);
        var designId = await CreateDesignAsync(session, eventId);
        var versionId = await SubmitReviewAsync(session, eventId, designId);
        await ReviewAsync(
            session,
            eventId,
            designId,
            versionId,
            "approve",
            "Aprobada para publicación");
        await PublishAsync(session, eventId, designId);
        var first = await GenerateLinkAsync(session, eventId, groupId);
        var firstToken = ExtractToken(first.PublicUrl);
        var listedLinks = await GetLinksAsync(session, eventId);
        Assert.Equal(
            first.PublicUrl,
            listedLinks[0].GetProperty("publicUrl").GetString());

        using var publicResponse = await factory.CreateClient().GetAsync(
            $"/api/public/invitations/{firstToken}");

        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.Contains("no-store", publicResponse.Headers.CacheControl?.ToString());
        Assert.Equal("no-referrer", publicResponse.Headers.GetValues("Referrer-Policy").Single());
        var publicPayload = await publicResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Familia Luna", publicPayload.GetProperty("groupDisplayName").GetString());
        Assert.Equal(2, publicPayload.GetProperty("participants").GetArrayLength());
        Assert.DoesNotContain("contactEmail", publicPayload.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("internalNotes", publicPayload.ToString(), StringComparison.Ordinal);
        await UpdateExperienceVisibilityAsync(session, eventId, false);
        using var privateProjectionResponse = await factory.CreateClient().GetAsync(
            $"/api/public/invitations/{firstToken}");
        privateProjectionResponse.EnsureSuccessStatusCode();
        var privateProjection =
            await privateProjectionResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, privateProjection.GetProperty("eventStartsAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, privateProjection.GetProperty("city").ValueKind);
        Assert.Empty(privateProjection.GetProperty("participants").EnumerateArray());
        Assert.DoesNotContain(
            privateProjection.GetProperty("blocks").EnumerateArray(),
            block => block.GetProperty("type").GetString() is "Participants" or "EventDate");
        await UpdateExperienceVisibilityAsync(session, eventId, true);

        var replacement = await RegenerateLinkAsync(
            session,
            eventId,
            first.Id);
        using var oldResponse = await factory.CreateClient().GetAsync(
            $"/api/public/invitations/{firstToken}");
        Assert.Equal(HttpStatusCode.Gone, oldResponse.StatusCode);
        var oldProblem = await oldResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("replaced", oldProblem.GetProperty("reason").GetString());

        var replacementToken = ExtractToken(replacement.PublicUrl);
        using var replacementResponse = await factory.CreateClient().GetAsync(
            $"/api/public/invitations/{replacementToken}");
        Assert.Equal(HttpStatusCode.OK, replacementResponse.StatusCode);

        await RevokeLinkAsync(session, eventId, replacement.Id);
        using var revokedResponse = await factory.CreateClient().GetAsync(
            $"/api/public/invitations/{replacementToken}");
        Assert.Equal(HttpStatusCode.Gone, revokedResponse.StatusCode);
        var revokedProblem = await revokedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("revoked", revokedProblem.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task CsvImport_IsValidatedTransactionalAndIdempotent()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "guest-csv");
        var eventId = await CreateConfirmedEventAsync(session);
        const string csv =
            "GroupName,GroupType,AllowedGuestCount,ContactName,ContactPhone,ContactEmail,GuestFirstName,GuestLastName,AgeCategory,IsPrimaryContact,IsVip,Tags\r\n"
            + "Familia Solís,Family,2,Ana Solís,8991234567,ana@example.com,Ana,Solís,Adult,true,true,Familia|VIP\r\n"
            + "Familia Solís,Family,2,Ana Solís,8991234567,ana@example.com,Leo,Solís,Child,false,false,Familia|VIP\r\n";
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        content.Add(file, "file", "invitados.csv");
        using var analyzeRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/imports/analyze",
            session.AccessToken,
            content);
        using var analyzeResponse = await factory.CreateClient().SendAsync(analyzeRequest);
        Assert.Equal(HttpStatusCode.OK, analyzeResponse.StatusCode);
        var analysis = await analyzeResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, analysis.GetProperty("validRows").GetInt32());
        Assert.Equal(0, analysis.GetProperty("errorRows").GetInt32());
        var importId = analysis.GetProperty("importId").GetGuid();

        var firstResult = await ConfirmImportAsync(session, eventId, importId);
        var repeatedResult = await ConfirmImportAsync(session, eventId, importId);

        Assert.Equal(2, firstResult.GetProperty("createdGuests").GetInt32());
        Assert.Equal(
            firstResult.GetProperty("completedAt").GetDateTimeOffset(),
            repeatedResult.GetProperty("completedAt").GetDateTimeOffset());
        var dashboard = await GetDashboardAsync(session, eventId);
        Assert.Equal(2, dashboard.GetProperty("activeGuestCount").GetInt32());
        Assert.Single(dashboard.GetProperty("groups").EnumerateArray());
    }

    [Fact]
    public async Task ImportTemplate_ReturnsCsvAndExcelInSpanishAndEnglish()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "guest-template");
        var eventId = await CreateConfirmedEventAsync(session);

        await AssertTemplateAsync(
            session,
            eventId,
            "csv",
            "es",
            "text/csv",
            csv => Assert.Contains("Nombre del grupo,Tipo de grupo", csv, StringComparison.Ordinal));
        await AssertTemplateAsync(
            session,
            eventId,
            "csv",
            "en",
            "text/csv",
            csv => Assert.Contains("Group name,Group type", csv, StringComparison.Ordinal));
        await AssertXlsxTemplateAsync(session, eventId, "es", "Invitados", "Instrucciones");
        await AssertXlsxTemplateAsync(session, eventId, "en", "Guests", "Instructions");
    }

    [Fact]
    public async Task CsvImport_AcceptsSpanishHeadersAndReadableValues()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "guest-csv-es");
        var eventId = await CreateConfirmedEventAsync(session);
        const string csv =
            "Nombre del grupo,Tipo de grupo,Invitados permitidos,Nombre de contacto,Teléfono de contacto,Correo de contacto,Nombre del invitado,Apellido del invitado,Categoría de edad,Contacto principal,VIP,Etiquetas\r\n"
            + "Familia Robles,Familia,2,Ana Robles,8991234567,ana@example.com,Ana,Robles,Adulto,Sí,Sí,Familia|VIP\r\n"
            + "Familia Robles,Familia,2,Ana Robles,8991234567,ana@example.com,Luis,Robles,Niño,No,No,Familia|VIP\r\n";

        var importId = await AnalyzeCsvAsync(
            session,
            eventId,
            csv,
            expectedErrorRows: 0);
        var result = await ConfirmImportAsync(session, eventId, importId);

        Assert.Equal(1, result.GetProperty("createdGroups").GetInt32());
        Assert.Equal(2, result.GetProperty("createdGuests").GetInt32());
    }

    [Fact]
    public async Task XlsxImport_AcceptsExcelFileWithSpanishHeaders()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "guest-xlsx-es");
        var eventId = await CreateConfirmedEventAsync(session);
        var xlsx = BuildSpanishImportWorkbook();

        var importId = await AnalyzeFileAsync(
            session,
            eventId,
            xlsx,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "invitados.xlsx",
            expectedErrorRows: 0);
        var result = await ConfirmImportAsync(session, eventId, importId);

        Assert.Equal(1, result.GetProperty("createdGroups").GetInt32());
        Assert.Equal(2, result.GetProperty("createdGuests").GetInt32());
    }

    [Fact]
    public async Task CsvImport_BlocksInvalidAndPlanLimit_AndExportEscapesFormula()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "guest-csv-security");
        var eventId = await CreateConfirmedEventAsync(session);
        var invalidImportId = await AnalyzeCsvAsync(
            session,
            eventId,
            "Unknown\r\nvalue\r\n",
            expectedErrorRows: 1);
        using var invalidConfirmRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/imports/{invalidImportId}/confirm",
            session.AccessToken);
        using var invalidConfirmResponse =
            await factory.CreateClient().SendAsync(invalidConfirmRequest);
        Assert.Equal(HttpStatusCode.Conflict, invalidConfirmResponse.StatusCode);

        var groupId = await CreateGroupAsync(session, eventId, "FÃ³rmulas", 1);
        await CreateGuestAsync(session, eventId, groupId, "=2+2", string.Empty, true);
        using var exportRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/export",
            session.AccessToken);
        using var exportResponse = await factory.CreateClient().SendAsync(exportRequest);
        exportResponse.EnsureSuccessStatusCode();
        var csvExport = await exportResponse.Content.ReadAsStringAsync();
        Assert.Contains("\"'=2+2\"", csvExport, StringComparison.Ordinal);

        var rows = new StringBuilder(
            "GroupName,GroupType,AllowedGuestCount,GuestFirstName,AgeCategory,IsPrimaryContact,IsVip\r\n");
        for (var index = 0; index < 100; index++)
        {
            rows.AppendLine($"Grupo {index},Individual,1,Invitado {index},Adult,true,false");
        }

        var overLimitImportId = await AnalyzeCsvAsync(
            session,
            eventId,
            rows.ToString(),
            expectedErrorRows: 0);
        using var overLimitRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/imports/{overLimitImportId}/confirm",
            session.AccessToken);
        using var overLimitResponse = await factory.CreateClient().SendAsync(overLimitRequest);
        Assert.Equal(HttpStatusCode.Conflict, overLimitResponse.StatusCode);
    }

    [Fact]
    public async Task PublicAccess_DistinguishesInvalidSuspendedArchivedAndExpired()
    {
        using var invalidResponse = await factory.CreateClient().GetAsync(
            "/api/public/invitations/not-a-valid-token");
        Assert.Equal(HttpStatusCode.NotFound, invalidResponse.StatusCode);
        var invalidProblem = await invalidResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid", invalidProblem.GetProperty("reason").GetString());

        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "guest-link-states");
        var eventId = await CreateConfirmedEventAsync(session);
        var firstGroupId = await CreateGroupAsync(session, eventId, "Familia Uno", 1);
        await CreateGuestAsync(
            session,
            eventId,
            firstGroupId,
            "Elena",
            "Uno",
            true);
        var designId = await CreateDesignAsync(session, eventId);
        var versionId = await SubmitReviewAsync(session, eventId, designId);
        await ReviewAsync(session, eventId, designId, versionId, "approve", "Aprobada");
        await PublishAsync(session, eventId, designId);
        var link = await GenerateLinkAsync(session, eventId, firstGroupId);
        var token = ExtractToken(link.PublicUrl);

        await SetExperienceSuspendedAsync(session, eventId, true);
        using var suspendedResponse = await factory.CreateClient().GetAsync(
            $"/api/public/invitations/{token}");
        Assert.Equal(HttpStatusCode.Gone, suspendedResponse.StatusCode);
        var suspendedProblem =
            await suspendedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("suspended", suspendedProblem.GetProperty("reason").GetString());
        await SetExperienceSuspendedAsync(session, eventId, false);

        using var archiveRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/groups/{firstGroupId}",
            session.AccessToken);
        using var archiveResponse = await factory.CreateClient().SendAsync(archiveRequest);
        archiveResponse.EnsureSuccessStatusCode();
        using var archivedResponse = await factory.CreateClient().GetAsync(
            $"/api/public/invitations/{token}");
        Assert.Equal(HttpStatusCode.Gone, archivedResponse.StatusCode);

        var secondGroupId = await CreateGroupAsync(session, eventId, "Familia Dos", 1);
        var expiringLink = await GenerateLinkAsync(
            session,
            eventId,
            secondGroupId,
            DateTimeOffset.UtcNow.AddSeconds(2));
        await Task.Delay(TimeSpan.FromMilliseconds(2200));
        using var expiredResponse = await factory.CreateClient().GetAsync(
            $"/api/public/invitations/{ExtractToken(expiringLink.PublicUrl)}");
        Assert.Equal(HttpStatusCode.Gone, expiredResponse.StatusCode);
        var expiredProblem = await expiredResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("expired", expiredProblem.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task GuestDashboard_FromAnotherTenant_IsForbidden()
    {
        var owner = await TestSessionFactory.RegisterPlannerAsync(factory, "guest-owner");
        var stranger = await TestSessionFactory.RegisterPlannerAsync(factory, "guest-stranger");
        var eventId = await CreateConfirmedEventAsync(owner);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{owner.OrganizationId}/events/{eventId}/guests/dashboard",
            stranger.AccessToken);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> CreateConfirmedEventAsync(TestSession session)
    {
        using var createRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events",
            session.AccessToken,
            JsonContent.Create(new
            {
                name = "Noche de verano",
                eventType = "Boda",
                startDateTime = DateTimeOffset.UtcNow.AddMonths(4),
                endDateTime = DateTimeOffset.UtcNow.AddMonths(4).AddHours(7),
                timeZone = "America/Matamoros",
                city = "Reynosa",
                countryCode = "MX",
                sharedDescription = "Ceremonia y cena",
                estimatedGuestCount = 90
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
                reason = "Fecha validada"
            }));
        using var confirmResponse = await factory.CreateClient().SendAsync(confirmRequest);
        confirmResponse.EnsureSuccessStatusCode();
        return eventId;
    }

    private async Task<Guid> CreateGroupAsync(
        TestSession session,
        Guid eventId,
        string name,
        int capacity)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/groups",
            session.AccessToken,
            JsonContent.Create(new
            {
                groupType = "Family",
                displayName = name,
                contactName = "Elena Luna",
                contactPhone = "8991234567",
                contactEmail = "elena@example.com",
                allowedGuestCount = capacity,
                allowUnnamedCompanions = true,
                maxUnnamedCompanions = 1,
                internalNotes = "Dato privado",
                tagIds = Array.Empty<Guid>()
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    private async Task CreateGuestAsync(
        TestSession session,
        Guid eventId,
        Guid groupId,
        string firstName,
        string lastName,
        bool primary)
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
                email = primary ? "elena@example.com" : null,
                phone = primary ? "8991234567" : null,
                guestType = "Family",
                ageCategory = "Adult",
                isPrimaryContact = primary,
                isNamed = true,
                isPlusOne = false,
                isVip = primary,
                sortOrder = primary ? 0 : 1,
                internalNotes = primary ? "No exponer" : null
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<Guid> CreateDesignAsync(TestSession session, Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/designs",
            session.AccessToken,
            JsonContent.Create(new
            {
                name = "Invitación principal",
                templateId = Guid.Parse("33333333-3333-3333-3333-333333333333")
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    private async Task<Guid> SubmitReviewAsync(
        TestSession session,
        Guid eventId,
        Guid designId)
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

    private async Task ReviewAsync(
        TestSession session,
        Guid eventId,
        Guid designId,
        Guid versionId,
        string action,
        string message)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/designs/{designId}/versions/{versionId}/{action}",
            session.AccessToken,
            JsonContent.Create(new { message }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task PublishAsync(
        TestSession session,
        Guid eventId,
        Guid designId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/designs/{designId}/publish",
            session.AccessToken,
            JsonContent.Create(new { bypassApprovalForTesting = false }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<LinkResult> GenerateLinkAsync(
        TestSession session,
        Guid eventId,
        Guid groupId,
        DateTimeOffset? expiresAt = null)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/groups/{groupId}/links",
            session.AccessToken,
            JsonContent.Create(new { expiresAt }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new LinkResult(
            payload.GetProperty("id").GetGuid(),
            payload.GetProperty("publicUrl").GetString()
                ?? throw new InvalidOperationException("No se recibió el enlace."));
    }

    private async Task<LinkResult> RegenerateLinkAsync(
        TestSession session,
        Guid eventId,
        Guid linkId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/links/{linkId}/regenerate",
            session.AccessToken,
            JsonContent.Create(new { expiresAt = (DateTimeOffset?)null }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new LinkResult(
            payload.GetProperty("id").GetGuid(),
            payload.GetProperty("publicUrl").GetString()
                ?? throw new InvalidOperationException("No se recibió el enlace."));
    }

    private async Task RevokeLinkAsync(
        TestSession session,
        Guid eventId,
        Guid linkId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/links/{linkId}",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task UpdateExperienceVisibilityAsync(
        TestSession session,
        Guid eventId,
        bool visible)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/experience",
            session.AccessToken,
            JsonContent.Create(new
            {
                language = "es",
                publicTitle = "Noche de verano",
                celebrantDisplayName = "Elena & Mateo",
                welcomeMessage = "Nos alegrará compartir este día.",
                closingMessage = "Con cariño.",
                showEventName = visible,
                showEventDate = visible,
                showParticipantNames = visible,
                showCity = visible,
                privateAccessOnly = true
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<JsonElement> GetLinksAsync(
        TestSession session,
        Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/links",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload;
    }

    private async Task<JsonElement> ConfirmImportAsync(
        TestSession session,
        Guid eventId,
        Guid importId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/imports/{importId}/confirm",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<Guid> AnalyzeCsvAsync(
        TestSession session,
        Guid eventId,
        string csv,
        int expectedErrorRows)
    {
        return await AnalyzeFileAsync(
            session,
            eventId,
            Encoding.UTF8.GetBytes(csv),
            "text/csv",
            "invitados.csv",
            expectedErrorRows);
    }

    private async Task<Guid> AnalyzeFileAsync(
        TestSession session,
        Guid eventId,
        byte[] bytes,
        string contentType,
        string fileName,
        int expectedErrorRows)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/imports/analyze",
            session.AccessToken,
            content);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedErrorRows, payload.GetProperty("errorRows").GetInt32());
        return payload.GetProperty("importId").GetGuid();
    }

    private async Task AssertTemplateAsync(
        TestSession session,
        Guid eventId,
        string format,
        string language,
        string expectedMediaType,
        Action<string> assertContent)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/imports/template?format={format}&language={language}",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType?.MediaType);
        assertContent(await response.Content.ReadAsStringAsync());
    }

    private async Task AssertXlsxTemplateAsync(
        TestSession session,
        Guid eventId,
        string language,
        string expectedDataSheet,
        string expectedGuideSheet)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/imports/template?format=xlsx&language={language}",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var workbook = new XLWorkbook(stream);
        Assert.True(workbook.TryGetWorksheet(expectedDataSheet, out var dataSheet));
        Assert.True(workbook.TryGetWorksheet(expectedGuideSheet, out var guideSheet));
        Assert.False(string.IsNullOrWhiteSpace(dataSheet.Cell(1, 1).GetString()));
        Assert.False(string.IsNullOrWhiteSpace(guideSheet.Cell(1, 1).GetString()));
    }

    private static byte[] BuildSpanishImportWorkbook()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Invitados");
        var headers = new[]
        {
            "Nombre del grupo",
            "Tipo de grupo",
            "Invitados permitidos",
            "Nombre de contacto",
            "Teléfono de contacto",
            "Correo de contacto",
            "Nombre del invitado",
            "Apellido del invitado",
            "Categoría de edad",
            "Contacto principal",
            "VIP",
            "Etiquetas"
        };

        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }

        string[][] rows =
        {
            new[]
            {
                "Familia Excel",
                "Familia",
                "2",
                "Ana Excel",
                string.Empty,
                string.Empty,
                "Ana",
                "Excel",
                "Adulto",
                "Sí",
                "Sí",
                "Familia|VIP"
            },
            new[]
            {
                "Familia Excel",
                "Familia",
                "2",
                "Ana Excel",
                string.Empty,
                string.Empty,
                "Luis",
                "Excel",
                "Niño",
                "No",
                "No",
                "Familia|VIP"
            }
        };

        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
            {
                worksheet.Cell(rowIndex + 2, columnIndex + 1).Value = rows[rowIndex][columnIndex];
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private async Task SetExperienceSuspendedAsync(
        TestSession session,
        Guid eventId,
        bool suspended)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/invitations/experience/{(suspended ? "suspend" : "resume")}",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<JsonElement> GetDashboardAsync(
        TestSession session,
        Guid eventId)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/guests/dashboard",
            session.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string ExtractToken(string publicUrl) =>
        publicUrl[(publicUrl.LastIndexOf("/i/", StringComparison.Ordinal) + 3)..];

    private sealed record LinkResult(Guid Id, string PublicUrl);
}
