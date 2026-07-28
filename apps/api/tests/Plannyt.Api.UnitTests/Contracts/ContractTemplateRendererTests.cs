using Plannyt.Api.Modules.Contracts.Rendering;

namespace Plannyt.Api.UnitTests.Contracts;

public sealed class ContractTemplateRendererTests
{
    private readonly ContractTemplateRenderer _renderer = new();

    [Fact]
    public void Render_ReplacesKnownVariablesAndReportsUnknownOnes()
    {
        var result = _renderer.Render(
            "<p>{{client.displayName}}</p><p>{{client.secret}}</p>",
            new Dictionary<string, string?>
            {
                ["client.displayName"] = "Ana & Carlos"
            });

        Assert.Contains("Ana &amp; Carlos", result.RenderedContent);
        Assert.Contains("client.secret", result.UnknownVariables);
        Assert.False(result.CanPublish);
    }

    [Fact]
    public void Render_ReportsMissingKnownVariable()
    {
        var result = _renderer.Render(
            "<p>{{event.city}}</p>",
            new Dictionary<string, string?>());

        Assert.Equal(["event.city"], result.MissingVariables);
        Assert.False(result.CanPublish);
    }

    [Fact]
    public void Sanitize_RemovesExecutableHtml()
    {
        var sanitized = _renderer.Sanitize(
            "<p onclick=\"alert(1)\">Seguro</p>"
            + "<script>alert(2)</script>"
            + "<a href=\"javascript:alert(3)\">Enlace</a>");

        Assert.DoesNotContain("script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "javascript:",
            sanitized,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seguro", sanitized);
    }
}
