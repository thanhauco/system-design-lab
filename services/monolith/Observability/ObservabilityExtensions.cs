using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Sdmp.Monolith.Observability;

/// <summary>
/// Wires the three observability pillars — metrics, traces, logs — plus health checks, in one place.
/// This is the reference implementation of the platform Observability standard.
/// </summary>
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        // OTLP endpoint (OTel Collector / Jaeger). Defaults to the docker-compose collector.
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                           ?? "http://localhost:4317";

        var resource = ResourceBuilder.CreateDefault()
            .AddService(Telemetry.ServiceName, serviceVersion: Telemetry.ServiceVersion)
            .AddTelemetrySdk();

        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .SetResourceBuilder(resource)
                .AddSource(Telemetry.ServiceName)
                .AddAspNetCoreInstrumentation(o => o.RecordException = true)
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .SetResourceBuilder(resource)
                .AddMeter(Telemetry.ServiceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        // Structured JSON logs to stdout (Loki / any log shipper can ingest these).
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(o =>
        {
            o.IncludeScopes = true;
            o.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
        });

        // Health: liveness has no dependencies; readiness is tagged so we can expose it separately.
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return builder;
    }

    public static WebApplication UseObservability(this WebApplication app)
    {
        // Correlation id first so every downstream log/trace/metric carries it.
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Prometheus scrape endpoint.
        app.MapPrometheusScrapingEndpoint("/metrics");

        // Liveness: is the process up?
        app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthResponse
        }).WithTags("Platform");

        // Readiness: are dependencies ready to serve traffic?
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponse
        }).WithTags("Platform");

        return app;
    }

    private static Task WriteHealthResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }),
            durationMs = report.TotalDuration.TotalMilliseconds
        };
        return context.Response.WriteAsJsonAsync(payload);
    }
}
