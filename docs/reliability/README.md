# Reliability

Reliability patterns keep a system responsive when its dependencies misbehave. In SDMP these are not
slides — they are implemented in the Phase 1 monolith and exercised by chaos labs.

## Patterns and where they live

| Pattern | Implementation | Chaos lab |
|---------|----------------|-----------|
| Retry + jittered backoff | `Reliability/ResiliencePipelines.cs` | Slow downstream |
| Timeout | `Reliability/ResiliencePipelines.cs` | Slow downstream |
| Circuit breaker | `Reliability/ResiliencePipelines.cs` | Downstream outage |
| Idempotency | `Reliability/IdempotencyMiddleware.cs` | Duplicate requests |
| Graceful shutdown | host lifetime + SIGTERM drain | Rolling restart |

## The three paths

Every reliability concept demonstrates:

1. **Happy path** — the dependency is healthy; requests succeed with low latency.
2. **Failure path** — the dependency fails or slows; the pattern contains the damage.
3. **Recovery path** — the dependency heals; the system returns to normal automatically.

### Circuit breaker example

```mermaid
stateDiagram-v2
    [*] --> Closed
    Closed --> Open: failure threshold exceeded
    Open --> HalfOpen: break duration elapsed
    HalfOpen --> Closed: probe succeeds
    HalfOpen --> Open: probe fails
```

- **Closed:** requests flow; failures are counted.
- **Open:** requests fail fast without touching the dependency.
- **Half-open:** a limited number of probe requests test recovery.

## Metrics to watch

- `sdmp_dependency_call_duration_seconds` — latency distribution per dependency.
- `sdmp_circuit_breaker_state` — 0 closed, 1 half-open, 2 open.
- `sdmp_retries_total` — retry volume (a spike is an early warning).

## Tradeoffs

- Retries amplify load on an already-struggling dependency — always pair with a circuit breaker.
- Aggressive timeouts reduce tail latency but can turn slow successes into failures.
- Idempotency requires storing keys — bounded by TTL to avoid unbounded growth.
