using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;
using Plannyt.Api.Modules.Identity.Domain;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.IntegrationTests.Organizations;

[Collection(ApiCollection.Name)]
public sealed class OrganizationAccessTests(ApiFactory factory)
{
    [Fact]
    public async Task GetOrganization_WithOwnerMembership_ReturnsTenant()
    {
        var session = await RegisterPlannerAsync("owner");
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{session.OrganizationId}",
            session.AccessToken);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(session.OrganizationId, payload.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetOrganization_FromAnotherTenant_IsForbidden()
    {
        var first = await RegisterPlannerAsync("first");
        var second = await RegisterPlannerAsync("second");
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{first.OrganizationId}",
            second.AccessToken);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_WithOwnerAndAdditionalMember_ReturnsOrderedList()
    {
        var owner = await RegisterPlannerAsync("owner-list");
        var memberAccount = UserAccount.Create(
            $"member-{Guid.NewGuid():N}@example.invalid",
            $"MEMBER-{Guid.NewGuid():N}@EXAMPLE.INVALID",
            "not-used-for-this-test",
            DateTimeOffset.UtcNow);
        var person = Person.Create(
            owner.OrganizationId,
            memberAccount.Id,
            "Ángel",
            "Ríos",
            "Ángel Ríos",
            memberAccount.Email,
            null,
            "es",
            "America/Matamoros",
            DateTimeOffset.UtcNow);
        var membership = OrganizationMembership.Create(
            owner.OrganizationId,
            memberAccount.Id,
            person.Id,
            OrganizationRole.Assistant,
            DateTimeOffset.UtcNow,
            null,
            DateTimeOffset.UtcNow);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
            dbContext.AddRange(memberAccount, person, membership);
            await dbContext.SaveChangesAsync();
        }

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/organizations/{owner.OrganizationId}/members",
            owner.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, payload.GetArrayLength());
        Assert.Equal("Ángel Ríos", payload[0].GetProperty("displayName").GetString());
        Assert.Equal("Assistant", payload[0].GetProperty("role").GetString());
        Assert.Equal("Mariana Torres", payload[1].GetProperty("displayName").GetString());
        Assert.Equal("Owner", payload[1].GetProperty("role").GetString());
    }

    [Fact]
    public async Task UpdateOrganization_WithOwnerPermission_PersistsChanges()
    {
        var owner = await RegisterPlannerAsync("owner-update");
        using var request = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/organizations/{owner.OrganizationId}",
            owner.AccessToken,
            JsonContent.Create(new
            {
                name = "  Organización Renombrada Ñ  ",
                organizationType = "Agency",
                timeZone = "America/Matamoros",
                countryCode = "mx",
                currencyCode = "mxn"
            }));

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Organización Renombrada Ñ", payload.GetProperty("name").GetString());
        Assert.Equal("MX", payload.GetProperty("countryCode").GetString());
        Assert.Equal("MXN", payload.GetProperty("currencyCode").GetString());
    }

    [Fact]
    public async Task UpdateOrganization_WithoutPermission_IsForbidden()
    {
        var owner = await RegisterPlannerAsync("owner-update-denied");
        var targetEmail = $"finance-{Guid.NewGuid():N}@example.invalid";
        using var inviteRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/organizations/{owner.OrganizationId}/members/invitations",
            owner.AccessToken,
            JsonContent.Create(new { targetEmail, intendedOrganizationRole = "Finance" }));
        using var inviteResponse = await factory.CreateClient().SendAsync(inviteRequest);
        inviteResponse.EnsureSuccessStatusCode();
        var invitePayload = await inviteResponse.Content.ReadFromJsonAsync<JsonElement>();
        var invitationUrl = invitePayload.GetProperty("invitationUrl").GetString()
            ?? throw new InvalidOperationException("No se recibió la URL de invitación.");
        var invitationToken = new Uri(invitationUrl).Segments[^1].Trim('/');

        using var acceptResponse = await factory.CreateClient().PostAsJsonAsync(
            $"/api/access-invitations/{invitationToken}/register-and-accept",
            new
            {
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Finanzas",
                lastName = "QA",
                preferredLanguage = "es",
                timeZone = "America/Matamoros"
            });
        acceptResponse.EnsureSuccessStatusCode();
        var acceptPayload = await acceptResponse.Content.ReadFromJsonAsync<JsonElement>();
        var financeToken = acceptPayload.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("No se recibió access token.");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/organizations/{owner.OrganizationId}",
            financeToken,
            JsonContent.Create(new
            {
                name = "Intento no autorizado",
                organizationType = "Agency",
                timeZone = "America/Matamoros",
                countryCode = "MX",
                currencyCode = "MXN"
            }));
        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        var organizationName = await verificationDb.Organizations
            .Where(entity => entity.Id == owner.OrganizationId)
            .Select(entity => entity.Name)
            .SingleAsync();
        Assert.NotEqual("Intento no autorizado", organizationName);
    }

    [Fact]
    public async Task RevokeMember_WhenTargetIsOnlyOwner_IsRejected()
    {
        var session = await RegisterPlannerAsync("sole-owner");
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        var membershipId = await dbContext.OrganizationMemberships
            .Where(entity =>
                entity.OrganizationId == session.OrganizationId
                && entity.UserAccountId == session.UserAccountId)
            .Select(entity => entity.Id)
            .SingleAsync();
        using var request = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/organizations/{session.OrganizationId}/members/{membershipId}",
            session.AccessToken);

        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RevokeMember_WithPermission_RevokesMembership()
    {
        var owner = await RegisterPlannerAsync("owner-revoke");
        var memberAccount = UserAccount.Create(
            $"member-{Guid.NewGuid():N}@example.invalid",
            $"MEMBER-{Guid.NewGuid():N}@EXAMPLE.INVALID",
            "not-used-for-this-test",
            DateTimeOffset.UtcNow);
        var person = Person.Create(
            owner.OrganizationId,
            memberAccount.Id,
            "Ángel",
            "Ríos",
            "Ángel Ríos",
            memberAccount.Email,
            null,
            "es",
            "America/Matamoros",
            DateTimeOffset.UtcNow);
        var membership = OrganizationMembership.Create(
            owner.OrganizationId,
            memberAccount.Id,
            person.Id,
            OrganizationRole.Assistant,
            DateTimeOffset.UtcNow,
            null,
            DateTimeOffset.UtcNow);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
            dbContext.AddRange(memberAccount, person, membership);
            await dbContext.SaveChangesAsync();
        }

        using var request = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/organizations/{owner.OrganizationId}/members/{membership.Id}",
            owner.AccessToken);
        using var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        var savedMembership = await verificationDb.OrganizationMemberships
            .SingleAsync(entity => entity.Id == membership.Id);
        Assert.Equal(MembershipStatus.Revoked, savedMembership.Status);
    }

    private async Task<RegisteredSession> RegisterPlannerAsync(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.invalid";
        using var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register-planner",
            new
            {
                email,
                password = "Correct-Horse-Battery-Staple-123!",
                firstName = "Mariana",
                lastName = "Torres",
                organizationName = $"Organización {Guid.NewGuid():N}",
                organizationType = "IndependentPlanner",
                timeZone = "America/Matamoros",
                countryCode = "MX",
                currencyCode = "MXN"
            });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new RegisteredSession(
            payload.GetProperty("userAccountId").GetGuid(),
            payload.GetProperty("organizationId").GetGuid(),
            payload.GetProperty("accessToken").GetString()
                ?? throw new InvalidOperationException("No se recibió access token."));
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string accessToken,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private sealed record RegisteredSession(
        Guid UserAccountId,
        Guid OrganizationId,
        string AccessToken);
}
