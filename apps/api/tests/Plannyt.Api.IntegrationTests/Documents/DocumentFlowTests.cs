using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;

namespace Plannyt.Api.IntegrationTests.Documents;

[Collection(ApiCollection.Name)]
public sealed class DocumentFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task SharedDocument_CanBeDownloadedByAdminAndPortal()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "document-planner");
        var eventId = await CreateEventAsync(planner);
        var fileContent = Encoding.UTF8.GetBytes(
            "%PDF-1.4\nDocumento compartido de prueba");
        var shared = await UploadAsync(
            planner,
            eventId,
            "ClientShared",
            "../../contrato.pdf",
            "application/pdf",
            fileContent);

        Assert.Equal("contrato.pdf", shared.FileName);
        using var adminDownload = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/documents/{shared.Id}/download",
            planner.AccessToken);
        using var adminResponse = await factory.CreateClient().SendAsync(adminDownload);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal(fileContent, await adminResponse.Content.ReadAsByteArrayAsync());

        var clientToken = await InviteAndRegisterClientAsync(planner, eventId);
        using var portalList = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/client-portal/events/{eventId}/documents",
            clientToken);
        using var listResponse = await factory.CreateClient().SendAsync(portalList);
        listResponse.EnsureSuccessStatusCode();
        var documents = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var portalDocument = Assert.Single(documents.EnumerateArray().ToArray());
        Assert.Equal(shared.Id, portalDocument.GetProperty("id").GetGuid());

        using var portalDownload = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/client-portal/events/{eventId}/documents/{shared.Id}/download",
            clientToken);
        using var portalResponse = await factory.CreateClient().SendAsync(portalDownload);
        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        Assert.Equal(fileContent, await portalResponse.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task InternalDocument_IsNeverVisibleThroughPortal()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "internal-document");
        var eventId = await CreateEventAsync(planner);
        var internalDocument = await UploadAsync(
            planner,
            eventId,
            "Internal",
            "interno.png",
            "image/png",
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01]);
        var clientToken = await InviteAndRegisterClientAsync(planner, eventId);
        using var portalDownload = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/client-portal/events/{eventId}/documents/{internalDocument.Id}/download",
            clientToken);

        using var response = await factory.CreateClient().SendAsync(portalDownload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WhenSignatureDoesNotMatch_ReturnsUnsupportedMediaType()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "invalid-document");
        var eventId = await CreateEventAsync(planner);
        using var multipart = CreateMultipart(
            "ClientShared",
            "malicioso.pdf",
            "application/pdf",
            [0x4D, 0x5A, 0x90, 0x00]);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/documents",
            planner.AccessToken,
            multipart);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WhenFileExceedsTenMegabytes_ReturnsPayloadTooLarge()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "large-document");
        var eventId = await CreateEventAsync(planner);
        var content = new byte[10 * 1024 * 1024 + 1];
        "%PDF-"u8.CopyTo(content);
        using var multipart = CreateMultipart(
            "ClientShared",
            "grande.pdf",
            "application/pdf",
            content);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/documents",
            planner.AccessToken,
            multipart);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_RemovesMetadataFromViewsAndPhysicalFile()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "delete-document");
        var eventId = await CreateEventAsync(planner);
        var document = await UploadAsync(
            planner,
            eventId,
            "Internal",
            "eliminar.jpg",
            "image/jpeg",
            [0xFF, 0xD8, 0xFF, 0xE0, 0x01]);
        string storageKey;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
            storageKey = await dbContext.BasicDocuments
                .Where(entity => entity.Id == document.Id)
                .Select(entity => entity.StorageKey)
                .SingleAsync();
        }

        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/documents/{document.Id}",
            planner.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(File.Exists(Path.Combine(
            factory.StorageRoot,
            storageKey.Replace('/', Path.DirectorySeparatorChar))));
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        var deletedAt = await verificationDb.BasicDocuments
            .Where(entity => entity.Id == document.Id)
            .Select(entity => entity.DeletedAt)
            .SingleAsync();
        Assert.NotNull(deletedAt);
    }

    private async Task<UploadedDocument> UploadAsync(
        TestSession planner,
        Guid eventId,
        string visibility,
        string fileName,
        string mimeType,
        byte[] content)
    {
        using var multipart = CreateMultipart(
            visibility,
            fileName,
            mimeType,
            content);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/documents",
            planner.AccessToken,
            multipart);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new UploadedDocument(
            payload.GetProperty("id").GetGuid(),
            payload.GetProperty("fileName").GetString()
                ?? throw new InvalidOperationException("No se recibió el nombre."));
    }

    private async Task<Guid> CreateEventAsync(TestSession planner)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events",
            planner.AccessToken,
            JsonContent.Create(new
            {
                name = "Evento con documentos",
                eventType = "Boda",
                startDateTime = DateTimeOffset.UtcNow.AddMonths(2),
                endDateTime = DateTimeOffset.UtcNow.AddMonths(2).AddHours(6),
                timeZone = "America/Matamoros",
                city = "Reynosa",
                countryCode = "MX",
                sharedDescription = "Documentos compartidos",
                estimatedGuestCount = 80
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    private async Task<string> InviteAndRegisterClientAsync(
        TestSession planner,
        Guid eventId)
    {
        var email = $"document-client-{Guid.NewGuid():N}@example.invalid";
        using var inviteRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/access/invitations",
            planner.AccessToken,
            JsonContent.Create(new
            {
                targetEmail = email,
                intendedEventRole = "ClientViewer"
            }));
        using var inviteResponse = await factory.CreateClient().SendAsync(inviteRequest);
        inviteResponse.EnsureSuccessStatusCode();
        var invite = await inviteResponse.Content.ReadFromJsonAsync<JsonElement>();
        var url = invite.GetProperty("invitationUrl").GetString()
            ?? throw new InvalidOperationException("No se recibió invitación.");
        var token = new Uri(url).Segments[^1].Trim('/');
        using var acceptance = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{token}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Cliente",
                lastName = "Documentos",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });
        acceptance.EnsureSuccessStatusCode();
        var auth = await acceptance.Content.ReadFromJsonAsync<JsonElement>();
        return auth.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("No se recibió access token.");
    }

    private static MultipartFormDataContent CreateMultipart(
        string visibility,
        string fileName,
        string mimeType,
        byte[] content)
    {
        var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("Contrato"), "documentType");
        multipart.Add(new StringContent(visibility), "visibility");
        var file = new ByteArrayContent(content);
        file.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
        multipart.Add(file, "file", fileName);
        return multipart;
    }

    private sealed record UploadedDocument(Guid Id, string FileName);
}
