using Microsoft.AspNetCore.Http;

namespace Sdmp.Monolith.Observability;

/// <summary>
/// Ensures every request has a correlation id. If the client supplies one via
/// <see cref="Telemetry.CorrelationIdHeader"/> we honor it; otherwise we generate one. The id is
/// attached to the logging scope, the current trace, and echoed back on the response so a caller can
/// pivot from a metric to a log to a full distributed trace.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(Telemetry.CorrelationIdHeader, out var existing)
                            && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("n");

        context.Items["CorrelationId"] = correlationId;

        // Tag the active trace so Jaeger searches by correlation id work.
        System.Diagnostics.Activity.Current?.SetTag("correlation.id", correlationId);

        // Echo back before the response starts so clients always see it, even on errors.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[Telemetry.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? "none"
        }))
        {
            await _next(context);
        }
    }
}
