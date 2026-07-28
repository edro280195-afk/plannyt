using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Plannyt.Api.Infrastructure.Persistence;
using Plannyt.Api.IntegrationTests.Infrastructure;
using Plannyt.Api.Modules.Organizations.Domain;

namespace Plannyt.Api.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class AuthFlowTests(ApiFactory factory)
{
    [Fact]
    public async Task RegisterPlanner_WithValidData_CreatesCompleteOwnerContext()
    {
        using var client = factory.CreateClient();
        var email = $"planner-{Guid.NewGuid():N}@example.invalid";

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register-planner",
            CreateRegistration(email));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = payload.GetProperty("accessToken").GetString();
        var organizationId = payload.GetProperty("organizationId").GetGuid();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value => value.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase)
                && value.Contains("Secure", StringComparison.OrdinalIgnoreCase)
                && value.Contains("SameSite=Lax", StringComparison.OrdinalIgnoreCase)
                && value.Contains("Path=/api/auth", StringComparison.OrdinalIgnoreCase));

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlannytDbContext>();
        var account = await dbContext.UserAccounts.SingleAsync(
            entity => entity.NormalizedEmail == email.ToUpperInvariant());
        var person = await dbContext.People.SingleAsync(
            entity => entity.OrganizationId == organizationId
                && entity.LinkedUserAccountId == account.Id);
        var membership = await dbContext.OrganizationMemberships.SingleAsync(
            entity => entity.OrganizationId == organizationId
                && entity.UserAccountId == account.Id);

        Assert.Equal(person.Id, membership.PersonId);
        Assert.Equal(OrganizationRole.Owner, membership.BaseRole);
        Assert.NotEqual("Correct-Horse-Battery-Staple-123!", account.PasswordHash);
    }

    [Fact]
    public async Task Refresh_WhenRotatedTokenIsReused_RevokesReplacementSession()
    {
        using var client = factory.CreateClient();
        var email = $"refresh-{Guid.NewGuid():N}@example.invalid";
        using var registration = await client.PostAsJsonAsync(
            "/api/auth/register-planner",
            CreateRegistration(email));
        registration.EnsureSuccessStatusCode();
        var firstCookie = ExtractRefreshCookie(registration);

        using var firstRefresh = await SendRefreshAsync(client, firstCookie);
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);
        var refreshedPayload =
            await firstRefresh.Content.ReadFromJsonAsync<JsonElement>();
        var refreshedAccessToken =
            refreshedPayload.GetProperty("accessToken").GetString();
        var secondCookie = ExtractRefreshCookie(firstRefresh);

        using var reuse = await SendRefreshAsync(client, firstCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", refreshedAccessToken);
        using var meResponse = await client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
        Assert.NotEqual(firstCookie, secondCookie);
    }

    private static object CreateRegistration(string email) => new
    {
        email,
        password = "Correct-Horse-Battery-Staple-123!",
        firstName = "Mariana",
        lastName = "Torres",
        organizationName = $"Armonía {Guid.NewGuid():N}",
        organizationType = "IndependentPlanner",
        timeZone = "America/Matamoros",
        countryCode = "MX",
        currencyCode = "MXN"
    };

    private static string ExtractRefreshCookie(HttpResponseMessage response)
    {
        var setCookie = Assert.Single(
            response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("plannyt_refresh=", StringComparison.Ordinal));
        return setCookie.Split(';', 2)[0];
    }

    private static Task<HttpResponseMessage> SendRefreshAsync(
        HttpClient client,
        string cookie)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("X-Plannyt-Client", "web");
        return client.SendAsync(request);
    }
}
