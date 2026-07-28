using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Plannyt.Api.IntegrationTests.Infrastructure;

public sealed record TestSession(
    Guid UserAccountId,
    Guid OrganizationId,
    string AccessToken,
    string Email);

public static class TestSessionFactory
{
    public static async Task<TestSession> RegisterPlannerAsync(
        ApiFactory factory,
        string prefix)
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
        return new TestSession(
            payload.GetProperty("userAccountId").GetGuid(),
            payload.GetProperty("organizationId").GetGuid(),
            payload.GetProperty("accessToken").GetString()
                ?? throw new InvalidOperationException("No se recibió access token."),
            email);
    }

    public static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string url,
        string accessToken,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
