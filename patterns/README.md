# Patterns

Reusable, runnable implementations of system-design patterns. The Phase 1 monolith already wires up
several of these; this folder is where standalone, focused examples live as the curriculum grows.

| Pattern | Category | Reference |
|---------|----------|-----------|
| Retry + backoff | Reliability | `services/monolith/Reliability/ResiliencePipelines.cs` |
| Circuit breaker | Reliability | `services/monolith/Reliability/ResiliencePipelines.cs` |
| Idempotency | Reliability | `services/monolith/Reliability/IdempotencyMiddleware.cs` |
| Outbox | Messaging | ✅ [outbox/](outbox/README.md) — `services/monolith/Messaging/` |
| Saga | Architecture | 🗺️ Phase 4 |
| CQRS | Architecture | 🗺️ Phase 4 |
| Event Sourcing | Architecture | 🗺️ Phase 4 |

Each pattern entry should document: intent, when to use, when **not** to use, code, and tradeoffs.
