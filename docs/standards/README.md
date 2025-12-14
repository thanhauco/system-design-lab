# Platform Standards

Every service in SDMP must satisfy these standards. They are the contract that makes the platform
observable, reliable, scalable, and secure. Phase 1's monolith implements all of the Observability
and Reliability standards as a reference.

## Observability

| Requirement | Endpoint / Mechanism |
|-------------|----------------------|
| Health check | `GET /health` (liveness) and `GET /health/ready` (readiness) |
| Metrics | `GET /metrics` (Prometheus exposition format) |
| Tracing | OpenTelemetry spans exported via OTLP |
| Logs | Structured JSON to stdout |
| Correlation | `X-Correlation-Id` propagated across every request and log line |

## Reliability

| Pattern | Purpose |
|---------|---------|
| Retry (with jittered backoff) | Survive transient downstream failures |
| Timeout | Bound the blast radius of a slow dependency |
| Circuit breaker | Stop hammering a failing dependency; fail fast |
| Bulkhead | Isolate resource pools so one dependency can't starve others |
| Graceful shutdown | Drain in-flight requests on SIGTERM |
| Dead letter queue | Capture un-processable messages for later inspection |
| Idempotency | `Idempotency-Key` makes unsafe operations safe to retry |

## Scalability

Horizontal scaling, autoscaling, load balancing, caching, read replicas, sharding, CDN integration.

## Security

OAuth2 / OIDC, JWT, RBAC + ABAC, TLS everywhere, secrets management (Vault), audit logging.

---

## Reference implementation

See [services/monolith](../../services/monolith/README.md) for a service that satisfies the
Observability and Reliability standards end to end, including the exact middleware and configuration
used.
