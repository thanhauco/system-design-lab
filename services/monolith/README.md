# Monolith (Phase 1)

The reference Phase 1 service for SDMP: a single deployable ASP.NET Core (.NET 9) minimal API with
clean internal boundaries. It is the baseline that every later phase inherits — full observability,
reliability patterns, and domain slices for Users, Products, and Orders.

## Run locally

```bash
cd services/monolith
dotnet run
# API + Swagger:  http://localhost:8080/swagger
# Health:         http://localhost:8080/health
```

## Run with the full platform

From the repo root:

```bash
docker compose up -d --build
```

This starts the monolith together with PostgreSQL, Redis, Prometheus, Grafana, Jaeger, and the OTel
Collector. See [ARCHITECTURE.md](../../ARCHITECTURE.md) for the topology.

## Endpoints

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Liveness |
| `GET /health/ready` | Readiness (dependencies) |
| `GET /metrics` | Prometheus metrics |
| `GET /swagger` | OpenAPI UI |
| `GET /api/v1/users`, `/products`, `/orders` | Domain slices |

## Design

The service follows clean architecture with vertical slices. Dependencies point inward
(Infrastructure → Domain), which makes the Phase 2/3 extraction mechanical. Details in
[ARCHITECTURE.md](../../ARCHITECTURE.md#2-monolith-internal-architecture-clean--vertical-slices).
