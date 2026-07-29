using System.Net;
using System.Text;
using System.Text.Json;
using Plannyt.Api.IntegrationTests.Infrastructure;

namespace Plannyt.Api.IntegrationTests.BuildingBlocks;

[Collection(ApiCollection.Name)]
public sealed class HealthAndHeadersTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task LiveHealth_WhenApiStarts_ReturnsHealthy()
    {
        using var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RootResponse_IncludesSecurityAndCorrelationHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Correlation-ID", "test-correlation-123");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "nosniff",
            Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal(
            "DENY",
            Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal(
            "test-correlation-123",
            Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
        Assert.Contains(
            "default-src 'none'",
            Assert.Single(response.Headers.GetValues("Content-Security-Policy")),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_WhenJsonIsMalformed_ReturnsBadRequestProblemDetails()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = new StringContent(
                "{email:\"mariana@example.com\"}",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("X-Correlation-ID", "malformed-json-test");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "La solicitud no es válida",
            problem.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "malformed-json-test",
            problem.RootElement.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task ApiResponse_EvenWhenUnauthorized_DisablesHttpCaching()
    {
        using var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.Equal(
            "no-cache",
            Assert.Single(response.Headers.GetValues("Pragma")));
        Assert.Equal(
            "0",
            Assert.Single(response.Content.Headers.GetValues("Expires")));
    }

    [Theory]
    [InlineData("/api/public/proposals/invalid-token")]
    [InlineData("/api/guest/rsvp/invalid-token/state")]
    [InlineData("/api/access-invitations/invalid-token")]
    public async Task TokenizedPublicResponse_DisablesReferrersAndIndexing(
        string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(
            "no-referrer",
            Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal(
            "noindex, nofollow, noarchive",
            Assert.Single(response.Headers.GetValues("X-Robots-Tag")));
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task OpenApiDocument_InDevelopment_ReturnsOperations()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "3.1.1",
            document.RootElement.GetProperty("openapi").GetString());
        Assert.True(
            document.RootElement.GetProperty("paths").EnumerateObject().Any());
    }
}
