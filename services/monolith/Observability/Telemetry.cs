namespace Sdmp.Monolith.Observability;

/// <summary>
/// Central telemetry primitives. Slices use these to create spans and record custom metrics so all
/// instrumentation shares one service name and one set of sources.
/// </summary>
public static class Telemetry
{
    public const string ServiceName = "sdmp-monolith";
    public const string ServiceVersion = "1.0.0";

    /// <summary>Header used to correlate logs, traces, and responses for a single request.</summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>ActivitySource for manual spans inside domain handlers.</summary>
    public static readonly System.Diagnostics.ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    /// <summary>Meter for custom business + reliability metrics.</summary>
    public static readonly System.Diagnostics.Metrics.Meter Meter = new(ServiceName, ServiceVersion);
}
