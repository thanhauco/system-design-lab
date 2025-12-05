# System Design Mastery Platform (SDMP)

> A production-grade, runnable curriculum for learning system design by **building, running, breaking, scaling, and observing** real services.

SDMP is built as if it were a real SaaS company operating at global scale. It teaches 100+ system
design concepts through real code, deployable services, observability, chaos engineering, and
distributed-systems simulations — inspired by the engineering cultures of Netflix, Uber, Amazon,
Stripe, Discord, Cloudflare, and OpenAI.

---

## Why this repo exists

Most system-design learning is whiteboard theory. SDMP is the opposite: every concept ships with
**documentation, diagrams, code, metrics, a failure path, and a scaling path** that you can run on
your own machine.

```
Learn → Build → Run → Break → Observe → Scale → Recover → Understand
```

---

## Core principles

1. Everything runs locally with **Docker Compose**.
2. Everything also supports **Kubernetes**.
3. Every concept ships with: docs, diagrams, code, metrics, a failure scenario, and a scaling scenario.
4. Every service exposes: **REST API, OpenAPI spec, `/metrics`, `/health`, structured logs, tracing**.
5. Every distributed concept demonstrates a **happy path, failure path, and recovery path**.
6. All examples are production-oriented. Toy examples are avoided.

---

## Architectural evolution

The same domain (a global commerce + AI platform) is re-implemented across six phases so you can
*see* the architecture evolve and understand the tradeoffs at each step.

| Phase | Style | What it teaches |
|------|-------|-----------------|
| 1 | Monolith | Clean boundaries, observability, reliability baseline |
| 2 | Modular Monolith | Bounded contexts, in-process messaging |
| 3 | Microservices | Service decomposition, API gateway, BFF |
| 4 | Event-Driven | Kafka, CQRS, Event Sourcing, Saga, Outbox |
| 5 | Cloud Native | Kubernetes, Helm, service mesh, GitOps |
| 6 | Global Scale | Sharding, multi-region, edge, capacity planning |

**Phase 1 is implemented and runnable today.** See [services/monolith](services/monolith/README.md).

---

## Repository structure

```
docs/                 Concept curriculum (distributed systems, APIs, DBs, messaging, ...)
services/             Runnable services per phase (Phase 1 monolith is live)
patterns/             Reusable pattern implementations (retry, circuit breaker, saga, ...)
data/                 Database schemas, seed data, modeling exercises
infrastructure/       Docker, Kubernetes, Helm, Terraform
observability/        Prometheus, Grafana, OTel collector, dashboards
chaos-engineering/    Failure injection labs
load-tests/           k6 / load scenarios (Black Friday, traffic spikes, ...)
interview-prep/       System design interview drills mapped to running code
ai-system-design/     RAG, GraphRAG, agents, vector search
```

Every top-level folder contains a `README` describing its contents and exercises.

---

## Quick start

Prerequisites: **Docker Desktop**, **.NET 9+ SDK**, and (optionally) **Node 20+**.

```bash
# 1. Start the full local platform (databases + observability + monolith)
docker compose up -d --build

# 2. Explore the running service
#    API + Swagger:   http://localhost:8080/swagger
#    Health:          http://localhost:8080/health
#    Metrics:         http://localhost:8080/metrics

# 3. Explore the observability stack
#    Prometheus:      http://localhost:9090
#    Grafana:         http://localhost:3000   (admin / admin)
#    Jaeger (traces): http://localhost:16686
```

To run only the monolith locally without containers:

```bash
cd services/monolith
dotnet run
```

---

## Learning objectives

SDMP covers the full system-design surface area. The concept curriculum lives in [docs/](docs/README.md):

- **Distributed Systems** — scalability, availability, CAP, consensus, replication, sharding, leader election, eventual consistency
- **APIs** — REST, GraphQL, gRPC, versioning, API Gateway, BFF
- **Databases** — PostgreSQL, MongoDB, Redis, Elasticsearch, ClickHouse, Neo4j, vector DBs
- **Messaging** — Kafka, RabbitMQ, NATS, event streaming, pub/sub
- **Architecture** — monolith → microservices, EDA, CQRS, event sourcing, saga, outbox
- **Reliability** — retry, timeout, circuit breaker, bulkhead, load shedding, backpressure
- **Platform Engineering** — Docker, Kubernetes, Helm, Terraform, service mesh, GitOps
- **Observability** — metrics, logs, traces, monitoring, alerting, correlation IDs
- **AI Systems** — RAG, GraphRAG, MCP, agents, semantic search, AI observability

---

## Platform standards (enforced in every service)

| Category | Requirement |
|---------|-------------|
| Observability | `/health`, `/metrics`, OpenTelemetry traces, structured JSON logs, correlation IDs |
| Reliability | Retry, timeout, circuit breaker, bulkhead, graceful shutdown, DLQ, idempotency |
| Scalability | Horizontal scaling, autoscaling, load balancing, caching, read replicas, sharding, CDN |
| Security | OAuth2/OIDC, JWT, RBAC/ABAC, TLS, secrets management, audit logging |

See [docs/standards](docs/standards/README.md) for the full specification.

---

## Status

| Area | State |
|------|-------|
| Phase 1 Monolith (.NET 9) | ✅ Runnable — health, metrics, tracing, logs, reliability patterns |
| Local platform (Docker Compose) | ✅ Postgres, Redis, Prometheus, Grafana, Jaeger, OTel Collector |
| Concept curriculum | 🚧 Scaffolded, growing |
| Phases 2–6 | 🗺️ Roadmapped |

This is a foundation designed to grow. Start with the monolith, then follow the roadmap in
[docs/roadmap.md](docs/roadmap.md).
