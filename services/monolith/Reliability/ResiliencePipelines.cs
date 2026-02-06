using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.DependencyInjection;
using Sdmp.Monolith.Observability;

namespace Sdmp.Monolith.Reliability;

/// <summary>
/// Builds the standard resilience pipeline every outbound dependency call should flow through:
/// timeout (bound the call), retry with jittered backoff (survive transient blips), and a circuit
/// breaker (fail fast when a dependency is genuinely down). Order matters: the breaker wraps the
/// retries so a sustained outage trips the breaker instead of being retried forever.
/// </summary>
public static class ResiliencePipelines
{
    public const string Dependency = "dependency";

    // Reports breaker state to Prometheus: 0 = closed, 1 = half-open, 2 = open.
    private static int _circuitState;
    private static readonly System.Diagnostics.Metrics.ObservableGauge<int> CircuitGauge =
        Telemetry.Meter.CreateObservableGauge("sdmp_circuit_breaker_state",
            () => _circuitState, description: "Circuit breaker state: 0 closed, 1 half-open, 2 open.");

    private static readonly System.Diagnostics.Metrics.Counter<long> Retries =
        Telemetry.Meter.CreateCounter<long>("sdmp_retries_total", description: "Total retry attempts.");

    public static IServiceCollection AddResiliencePipelines(this IServiceCollection services)
    {
        services.AddResiliencePipeline(Dependency, builder =>
        {
            builder
                // Outermost: stop calling a dependency that is clearly failing.
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    MinimumThroughput = 10,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(15),
                    OnOpened = _ => { _circuitState = 2; return default; },
                    OnHalfOpened = _ => { _circuitState = 1; return default; },
                    OnClosed = _ => { _circuitState = 0; return default; }
                })
                // Middle: retry transient failures with exponential backoff + jitter.
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(200),
                    OnRetry = _ => { Retries.Add(1); return default; }
                })
                // Innermost: bound each individual attempt.
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(2)
                });
        });

        return services;
    }
}
