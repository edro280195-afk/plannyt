using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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

    [Fact]
    public async Task Refresh_WithoutTrustedOrigin_IsForbidden()
    {
        using var client = factory.CreateClient();
        using var registration = await client.PostAsJsonAsync(
            "/api/auth/register-planner",
            CreateRegistration($"csrf-{Guid.NewGuid():N}@example.invalid"));
        registration.EnsureSuccessStatusCode();
        var cookie = ExtractRefreshCookie(registration);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("Origin", "https://attacker.example.invalid");
        request.Headers.Add("X-Plannyt-Client", "web");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithAlternateLocalhostPortInDevelopment_Succeeds()
    {
        // Regresión QA-016: en este entorno de desarrollo, Angular puede correr
        // en un puerto distinto al configurado en Cors:AllowedOrigin
        // (documentado en docs/qa/next-session-prompt.md por un conflicto de
        // puerto local real). La API se ejecuta en Development durante las
        // pruebas de integración (ApiFactory.UseEnvironment("Development")),
        // así que cualquier origen loopback debe seguir siendo aceptado.
        using var client = factory.CreateClient();
        using var registration = await client.PostAsJsonAsync(
            "/api/auth/register-planner",
            CreateRegistration($"alt-port-{Guid.NewGuid():N}@example.invalid"));
        registration.EnsureSuccessStatusCode();
        var cookie = ExtractRefreshCookie(registration);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/refresh");
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("Origin", "http://localhost:4210");
        request.Headers.Add("X-Plannyt-Client", "web");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_DoesNotShareTheCredentialRateLimit()
    {
        using var limitedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["RateLimit:SensitivePermitLimit"] = "1",
                        ["RateLimit:SessionPermitLimit"] = "5"
                    })));
        using var client = limitedFactory.CreateClient();
        using var registration = await client.PostAsJsonAsync(
            "/api/auth/register-planner",
            CreateRegistration($"rate-limit-{Guid.NewGuid():N}@example.invalid"));
        registration.EnsureSuccessStatusCode();
        var cookie = ExtractRefreshCookie(registration);

        for (var iteration = 0; iteration < 3; iteration++)
        {
            using var refresh = await SendRefreshAsync(client, cookie);
            Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
            cookie = ExtractRefreshCookie(refresh);
        }
    }

    [Fact]
    public async Task Logout_WithValidCookie_ImmediatelyRevokesAccessToken()
    {
        using var client = factory.CreateClient();
        using var registration = await client.PostAsJsonAsync(
            "/api/auth/register-planner",
            CreateRegistration($"logout-{Guid.NewGuid():N}@example.invalid"));
        registration.EnsureSuccessStatusCode();
        var payload = await registration.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = payload.GetProperty("accessToken").GetString();
        var cookie = ExtractRefreshCookie(registration);
        using var logoutRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/auth/logout");
        logoutRequest.Headers.Add("Cookie", cookie);
        logoutRequest.Headers.Add("Origin", "http://localhost:4200");
        logoutRequest.Headers.Add("X-Plannyt-Client", "web");

        using var logoutResponse = await client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var meRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/auth/me");
        meRequest.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        using var meResponse = await client.SendAsync(meRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
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
