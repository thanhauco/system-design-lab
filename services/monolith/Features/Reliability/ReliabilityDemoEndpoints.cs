using Polly;
using Polly.Registry;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Sdmp.Monolith.Reliability;

namespace Sdmp.Monolith.Features.Reliability;

/// <summary>
/// Demonstrates the resilience pipeline (timeout + retry + circuit breaker) against a simulated
/// downstream dependency whose failure rate and latency are controllable via query string. This is
/// the endpoint the chaos labs drive to show the happy / failure / recovery paths.
/// </summary>
public static class ReliabilityDemoEndpoints
{
    public static IEndpointRouteBuilder MapReliabilityDemo(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reliability").WithTags("Reliability");

        // Example: /api/v1/reliability/call?failRate=0.8&latencyMs=50
        group.MapGet("/call", async (
                double failRate,
                int latencyMs,
                ResiliencePipelineProvider<string> pipelines,
                ILogger<Program> logger,
                CancellationToken ct) =>
            {
                var pipeline = pipelines.GetPipeline(ResiliencePipelines.Dependency);

                try
                {
                    var result = await pipeline.ExecuteAsync(async token =>
                        await SimulateDownstreamAsync(failRate, latencyMs, token), ct);

                    return Results.Ok(new { outcome = "success", result });
                }
                catch (BrokenCircuitException)
                {
                    // Breaker is open: fail fast without touching the dependency.
                    logger.LogWarning("Circuit open — failing fast.");
                    return Results.Json(new { outcome = "circuit_open" }, statusCode: 503);
                }
                catch (TimeoutRejectedException)
                {
                    logger.LogWarning("Downstream timed out.");
                    return Results.Json(new { outcome = "timeout" }, statusCode: 504);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Downstream failed after retries.");
                    return Results.Json(new { outcome = "failed", error = ex.Message }, statusCode: 502);
                }
            })
            .WithName("ReliabilityCall")
            .WithSummary("Call a simulated downstream through the resilience pipeline");

        return app;
    }

    private static readonly Random Rng = Random.Shared;

    private static async Task<string> SimulateDownstreamAsync(double failRate, int latencyMs, CancellationToken ct)
    {
        if (latencyMs > 0)
            await Task.Delay(latencyMs, ct);

        if (Rng.NextDouble() < Math.Clamp(failRate, 0, 1))
            throw new InvalidOperationException("Simulated downstream failure.");

        return $"ok@{DateTimeOffset.UtcNow:O}";
    }
}
