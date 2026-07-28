using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;
using Plannyt.Api.Modules.Events.Domain;

namespace Plannyt.Api.IntegrationTests.Events;

[Collection(ApiCollection.Name)]
public sealed class EventFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task CreateAndConfirmEvent_RecordsStatusHistory()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "event-status");
        var eventId = await CreateEventAsync(session);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/status",
            session.AccessToken,
            JsonContent.Create(new
            {
                newStatus = "Confirmed",
                reason = "Contrato firmado"
            }));

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", payload.GetProperty("status").GetString());
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        var history = await dbContext.EventStatusHistory.SingleAsync(
            entity => entity.OrganizationId == session.OrganizationId
                && entity.EventId == eventId);
        Assert.Equal(EventStatus.Preliminary, history.PreviousStatus);
        Assert.Equal(EventStatus.Confirmed, history.NewStatus);
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidTransition_ReturnsBadRequest()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "event-invalid");
        var eventId = await CreateEventAsync(session);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/status",
            session.AccessToken,
            JsonContent.Create(new
            {
                newStatus = "Closed",
                reason = "Transición no permitida"
            }));

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_FromAnotherTenant_IsForbidden()
    {
        var owner = await TestSessionFactory.RegisterPlannerAsync(factory, "event-owner");
        var stranger = await TestSessionFactory.RegisterPlannerAsync(factory, "event-stranger");
        var eventId = await CreateEventAsync(owner);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{owner.OrganizationId}/events/{eventId}",
            stranger.AccessToken);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddParticipant_PersistsExplicitClientVisibility()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "event-participant");
        var eventId = await CreateEventAsync(session);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/participants",
            session.AccessToken,
            JsonContent.Create(new
            {
                firstName = "Carlos",
                lastName = "Santos",
                contactEmail = "carlos@example.invalid",
                contactPhone = (string?)null,
                preferredLanguage = "es",
                timeZone = "America/Matamoros",
                participantType = "Homenajeado",
                displayOrder = 1,
                isVisibleToClient = false,
                sharedDescription = "Participante principal"
            }));

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(payload.GetProperty("isVisibleToClient").GetBoolean());
    }

    [Fact]
    public async Task LinkClient_ToEvent_CreatesPrimaryRelationship()
    {
        var session = await TestSessionFactory.RegisterPlannerAsync(factory, "event-client");
        var eventId = await CreateEventAsync(session);
        var clientId = await CreateClientAsync(session);
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events/{eventId}/clients",
            session.AccessToken,
            JsonContent.Create(new
            {
                clientId,
                relationshipType = "ContractingClient",
                isPrimary = true,
                hasTransferAuthority = true
            }));

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(clientId, payload.GetProperty("clientId").GetGuid());
        Assert.True(payload.GetProperty("isPrimary").GetBoolean());
    }

    private async Task<Guid> CreateEventAsync(TestSession session)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/events",
            session.AccessToken,
            JsonContent.Create(new
            {
                name = "Ana & Carlos",
                eventType = "Boda",
                startDateTime = DateTimeOffset.UtcNow.AddMonths(3),
                endDateTime = DateTimeOffset.UtcNow.AddMonths(3).AddHours(8),
                timeZone = "America/Matamoros",
                city = "Reynosa",
                countryCode = "MX",
                sharedDescription = "Ceremonia y recepción",
                estimatedGuestCount = 180
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateClientAsync(TestSession session)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{session.OrganizationId}/clients",
            session.AccessToken,
            JsonContent.Create(new
            {
                clientType = "Company",
                displayName = "Familia Contreras",
                companyName = "Familia Contreras",
                source = "Recomendación"
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }
}
