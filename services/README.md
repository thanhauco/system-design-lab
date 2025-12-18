# Services

Runnable services, organized by architectural phase.

| Service | Phase | Status | Description |
|---------|-------|--------|-------------|
| [monolith](monolith/README.md) | 1 | ✅ Runnable | The reference monolith: full observability + reliability baseline, domain slices for Users, Products, Orders |

Later phases extract these capabilities into modules and then independent services
(API Gateway, Auth, User, Order, Payment, Search, Recommendation, Chat, Analytics, AI Assistant).
See [docs/roadmap.md](../docs/roadmap.md).

## Service standards

Every service must expose `GET /health`, `GET /health/ready`, `GET /metrics`, an OpenAPI spec,
structured JSON logs, and OpenTelemetry traces. See [docs/standards](../docs/standards/README.md).
