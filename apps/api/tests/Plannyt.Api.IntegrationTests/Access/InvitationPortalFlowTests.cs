using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.IntegrationTests.Access;

[Collection(ApiCollection.Name)]
public sealed class InvitationPortalFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task RegisterAndAcceptEventInvitation_EnablesSafePortalProjection()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "invite-planner");
        var eventId = await CreateEventAsync(planner);
        await AddParticipantAsync(planner, eventId, "Visible", true, 1);
        await AddParticipantAsync(planner, eventId, "Interno", false, 2);
        var targetEmail = $"client-{Guid.NewGuid():N}@example.invalid";
        var invitation = await CreateEventInvitationAsync(
            planner,
            eventId,
            targetEmail);

        Assert.Contains("/accept-access/", invitation.Url, StringComparison.Ordinal);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
            var saved = await dbContext.AccessInvitations.SingleAsync(
                entity => entity.NormalizedTargetEmail == targetEmail.ToUpperInvariant());
            Assert.DoesNotContain(invitation.Token, saved.TokenHash, StringComparison.Ordinal);
        }

        using var acceptResponse = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{invitation.Token}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Ana",
                lastName = "Martínez",
                contactPhone = "+52 899 111 2233",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var auth = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = auth.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("No se recibió access token.");
        Assert.Contains(
            acceptResponse.Headers.GetValues("Set-Cookie"),
            value => value.Contains("plannyt_refresh=", StringComparison.Ordinal));

        using var portalRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/client-portal/events/{eventId}",
            accessToken);
        using var portalResponse = await factory.CreateClient().SendAsync(portalRequest);

        Assert.Equal(HttpStatusCode.OK, portalResponse.StatusCode);
        var portal = await portalResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(portal.TryGetProperty("organizationId", out _));
        Assert.False(portal.TryGetProperty("status", out _));
        Assert.False(portal.TryGetProperty("createdBy", out _));
        var participants = portal.GetProperty("participants").EnumerateArray().ToArray();
        var participant = Assert.Single(participants);
        Assert.Equal("Visible Persona", participant.GetProperty("displayName").GetString());

        using var reuse = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{invitation.Token}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Ana",
                lastName = "Martínez",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });
        Assert.Equal(HttpStatusCode.Gone, reuse.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_WithDifferentAuthenticatedEmail_IsForbidden()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "invite-owner");
        var stranger = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "invite-stranger");
        var eventId = await CreateEventAsync(planner);
        var invitation = await CreateEventInvitationAsync(
            planner,
            eventId,
            $"expected-{Guid.NewGuid():N}@example.invalid");
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/access-invitations/{invitation.Token}/accept",
            stranger.AccessToken,
            JsonContent.Create(new { }));

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokeEventAccess_ImmediatelyBlocksExistingAccessToken()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "revoke-planner");
        var eventId = await CreateEventAsync(planner);
        var targetEmail = $"revoke-client-{Guid.NewGuid():N}@example.invalid";
        var invitation = await CreateEventInvitationAsync(
            planner,
            eventId,
            targetEmail);
        using var acceptance = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{invitation.Token}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Cliente",
                lastName = "Revocado",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });
        acceptance.EnsureSuccessStatusCode();
        var auth = await acceptance.Content.ReadFromJsonAsync<JsonElement>();
        var clientAccessToken = auth.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("No se recibió access token.");
        var accessId = await GetAccessIdAsync(planner, eventId, targetEmail);
        using var revokeRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/access/{accessId}",
            planner.AccessToken);
        using var revokeResponse = await factory.CreateClient().SendAsync(revokeRequest);
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        using var portalRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/client-portal/events/{eventId}",
            clientAccessToken);
        using var portalResponse = await factory.CreateClient().SendAsync(portalRequest);

        Assert.Equal(HttpStatusCode.Forbidden, portalResponse.StatusCode);
    }

    [Fact]
    public async Task AcceptOrganizationInvitation_CreatesMemberAndPreventsOverDelegation()
    {
        var owner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "team-owner");
        var targetEmail = $"coordinator-{Guid.NewGuid():N}@example.invalid";
        var invitation = await CreateOrganizationInvitationAsync(
            owner,
            targetEmail,
            "Coordinator");
        using var acceptance = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{invitation.Token}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Coordinadora",
                lastName = "Equipo",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });
        acceptance.EnsureSuccessStatusCode();
        var auth = await acceptance.Content.ReadFromJsonAsync<JsonElement>();
        var memberId = auth.GetProperty("userAccountId").GetGuid();
        var memberToken = auth.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("No se recibió access token.");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
            var membership = await dbContext.OrganizationMemberships.SingleAsync(
                entity => entity.OrganizationId == owner.OrganizationId
                    && entity.UserAccountId == memberId);
            Assert.Equal(OrganizationRole.Coordinator, membership.BaseRole);
            dbContext.PermissionGrants.Add(PermissionGrant.Create(
                owner.OrganizationId,
                memberId,
                null,
                "organization.members.invite",
                PermissionEffect.Allow,
                PermissionScope.Organization,
                null,
                null,
                DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync();
        }

        using var delegatedRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{owner.OrganizationId}/members/invitations",
            memberToken,
            JsonContent.Create(new
            {
                targetEmail = $"planner-{Guid.NewGuid():N}@example.invalid",
                intendedOrganizationRole = "Planner"
            }));
        using var delegatedResponse =
            await factory.CreateClient().SendAsync(delegatedRequest);

        Assert.Equal(HttpStatusCode.Forbidden, delegatedResponse.StatusCode);
    }

    [Fact]
    public async Task AcceptInvitation_WhenExpired_ReturnsGone()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "expired-owner");
        var eventId = await CreateEventAsync(planner);
        var invitation = await CreateEventInvitationAsync(
            planner,
            eventId,
            $"expired-{Guid.NewGuid():N}@example.invalid");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE access_invitations SET expires_at = created_at + INTERVAL '1 millisecond' WHERE id = {invitation.Id}");
        }

        using var response = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{invitation.Token}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Invitación",
                lastName = "Vencida",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
    }

    [Fact]
    public async Task PortalGuestCollaboration_ExposesSafeDtoAndCannotPublish()
    {
        var planner = await TestSessionFactory.RegisterPlannerAsync(
            factory,
            "portal-guests");
        var eventId = await CreateEventAsync(planner);
        var invitation = await CreateEventInvitationAsync(
            planner,
            eventId,
            $"portal-guests-{Guid.NewGuid():N}@example.invalid");
        using var acceptance = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{invitation.Token}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Cliente",
                lastName = "Colaborador",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });
        acceptance.EnsureSuccessStatusCode();
        var auth = await acceptance.Content.ReadFromJsonAsync<JsonElement>();
        var clientToken = auth.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("No se recibió access token.");
        using var groupRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/client-portal/events/{eventId}/guest-experience/groups",
            clientToken,
            JsonContent.Create(new
            {
                groupType = "Family",
                displayName = "Familia Portal",
                allowedGuestCount = 2,
                allowUnnamedCompanions = false,
                maxUnnamedCompanions = 0
            }));
        using var groupResponse = await factory.CreateClient().SendAsync(groupRequest);
        Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
        var group = await groupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var groupId = group.GetProperty("id").GetGuid();
        using var guestRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/client-portal/events/{eventId}/guest-experience/guests",
            clientToken,
            JsonContent.Create(new
            {
                invitationGroupId = groupId,
                firstName = "Elena",
                lastName = "Portal",
                guestType = "Family",
                ageCategory = "Adult",
                isPrimaryContact = true,
                isVip = false,
                sortOrder = 0
            }));
        using var guestResponse = await factory.CreateClient().SendAsync(guestRequest);
        Assert.Equal(HttpStatusCode.Created, guestResponse.StatusCode);

        using var workspaceRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/client-portal/events/{eventId}/guest-experience",
            clientToken);
        using var workspaceResponse = await factory.CreateClient().SendAsync(workspaceRequest);
        Assert.Equal(HttpStatusCode.OK, workspaceResponse.StatusCode);
        var workspace = await workspaceResponse.Content.ReadFromJsonAsync<JsonElement>();
        var serialized = workspace.ToString();
        Assert.DoesNotContain("contactEmail", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("contactPhone", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internalNotes", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("organizationId", serialized, StringComparison.Ordinal);

        using var publishRequest = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/invitations/designs/{Guid.NewGuid()}/publish",
            clientToken,
            JsonContent.Create(new { bypassApprovalForTesting = false }));
        using var publishResponse = await factory.CreateClient().SendAsync(publishRequest);
        Assert.Equal(HttpStatusCode.Forbidden, publishResponse.StatusCode);
    }

    private async Task<InvitationLink> CreateEventInvitationAsync(
        TestSession planner,
        Guid eventId,
        string targetEmail)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/access/invitations",
            planner.AccessToken,
            JsonContent.Create(new
            {
                targetEmail,
                intendedEventRole = "ClientPrimary"
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var url = payload.GetProperty("invitationUrl").GetString()
            ?? throw new InvalidOperationException("No se recibió la URL.");
        return new InvitationLink(
            payload.GetProperty("id").GetGuid(),
            url,
            new Uri(url).Segments[^1].Trim('/'));
    }

    private async Task<InvitationLink> CreateOrganizationInvitationAsync(
        TestSession owner,
        string targetEmail,
        string role)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{owner.OrganizationId}/members/invitations",
            owner.AccessToken,
            JsonContent.Create(new
            {
                targetEmail,
                intendedOrganizationRole = role
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var url = payload.GetProperty("invitationUrl").GetString()
            ?? throw new InvalidOperationException("No se recibió la URL.");
        return new InvitationLink(
            payload.GetProperty("id").GetGuid(),
            url,
            new Uri(url).Segments[^1].Trim('/'));
    }

    private async Task<Guid> GetAccessIdAsync(
        TestSession planner,
        Guid eventId,
        string email)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/access",
            planner.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.EnumerateArray()
            .Single(item => item.GetProperty("email").GetString() == email)
            .GetProperty("id")
            .GetGuid();
    }

    private async Task<Guid> CreateEventAsync(TestSession planner)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events",
            planner.AccessToken,
            JsonContent.Create(new
            {
                name = "Portal compartido",
                eventType = "Boda",
                startDateTime = DateTimeOffset.UtcNow.AddMonths(4),
                endDateTime = DateTimeOffset.UtcNow.AddMonths(4).AddHours(7),
                timeZone = "America/Matamoros",
                city = "Reynosa",
                countryCode = "MX",
                sharedDescription = "Información visible",
                estimatedGuestCount = 120
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("id").GetGuid();
    }

    private async Task AddParticipantAsync(
        TestSession planner,
        Guid eventId,
        string firstName,
        bool visible,
        int order)
    {
        using var request = TestSessionFactory.CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{planner.OrganizationId}/events/{eventId}/participants",
            planner.AccessToken,
            JsonContent.Create(new
            {
                firstName,
                lastName = "Persona",
                contactEmail = $"{firstName.ToLowerInvariant()}@example.invalid",
                contactPhone = (string?)null,
                preferredLanguage = "es",
                timeZone = "America/Matamoros",
                participantType = "Homenajeado",
                displayOrder = order,
                isVisibleToClient = visible,
                sharedDescription = visible ? "Compartido" : "Interno"
            }));
        using var response = await factory.CreateClient().SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private sealed record InvitationLink(Guid Id, string Url, string Token);
}
