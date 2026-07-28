namespace Plannyt.Api.BuildingBlocks.Http;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestedCorrelationId = context.Request.Headers[HeaderName].FirstOrDefault();
        context.TraceIdentifier = IsSafeCorrelationId(requestedCorrelationId)
            ? requestedCorrelationId!
            : Guid.NewGuid().ToString("N");

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        using var scope = context.RequestServices
            .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
            .BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = context.TraceIdentifier
            });

        await next(context);
    }

    private static bool IsSafeCorrelationId(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 64
        && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
}
