using System.Net;
using Plannyt.Api.IntegrationTests.Infrastructure;

namespace Plannyt.Api.IntegrationTests.BuildingBlocks;

public sealed class HealthAndHeadersTests(ApiFactory factory)
    : IClassFixture<ApiFactory>
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
}
