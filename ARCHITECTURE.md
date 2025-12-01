# Architecture

This document describes the SDMP architecture using ASCII diagrams so it renders the same everywhere
(terminal, plain text, any markdown viewer). It covers the platform topology, the Phase 1 monolith's
internal design, request flow, observability wiring, and the evolution across phases.

---

## 1. Platform topology (local, Docker Compose)

Everything runs locally with `docker compose up`. The monolith is the only application service in
Phase 1; the rest are infrastructure and observability backends it depends on.

```
                                ┌───────────────────────────┐
                                │          Client           │
                                │  (curl / browser / k6)    │
                                └─────────────┬─────────────┘
                                              │ HTTP :8080
                                              ▼
        ┌─────────────────────────────────────────────────────────────────┐
        │                     sdmp-monolith (.NET 9)                        │
        │                                                                   │
        │   /api/v1/*   /health   /health/ready   /metrics   /swagger       │
        │                                                                   │
        │   ┌───────────┐  ┌───────────┐  ┌──────────────┐  ┌───────────┐   │
        │   │ Users     │  │ Products  │  │ Orders       │  │ Reliability│  │
        │   │ slice     │  │ slice     │  │ slice        │  │ + Obsv.    │  │
        │   └───────────┘  └───────────┘  └──────────────┘  └───────────┘   │
        └───────┬───────────────┬──────────────────┬──────────────┬────────┘
                │ SQL           │ cache            │ OTLP traces   │ scrape
                ▼               ▼                  ▼               ▼ /metrics
        ┌─────────────┐  ┌─────────────┐   ┌─────────────┐  ┌─────────────┐
        │ PostgreSQL  │  │   Redis     │   │   Jaeger    │  │ Prometheus  │
        │   :5432     │  │   :6379     │   │   :16686    │  │   :9090     │
        └─────────────┘  └─────────────┘   └──────┬──────┘  └──────┬──────┘
                                                  │                │
                                          ┌───────┴──────┐  ┌──────┴──────┐
                                          │ OTel Collector│ │   Grafana   │
                                          │   :4317/4318 │  │   :3000     │
                                          └──────────────┘  └─────────────┘
```

**Ports**

| Component       | Port(s)        | Purpose                          |
|-----------------|----------------|----------------------------------|
| monolith        | 8080           | REST API, health, metrics, swagger |
| PostgreSQL      | 5432           | Primary datastore                |
| Redis           | 6379           | Cache / idempotency store        |
| OTel Collector  | 4317 / 4318    | OTLP gRPC / HTTP ingest          |
| Jaeger          | 16686          | Trace UI                         |
| Prometheus      | 9090           | Metrics scrape + query           |
| Grafana         | 3000           | Dashboards                       |

---

## 2. Monolith internal architecture (clean / vertical slices)

The monolith uses a clean layering with **vertical slices** per domain capability. Each slice owns
its endpoint, request/response contracts, and handler logic, sharing only cross-cutting concerns.

```
   HTTP request
        │
        ▼
 ┌──────────────────────────────────────────────────────────────┐
 │                       Middleware pipeline                      │
 │  Correlation-Id → Request logging → Metrics → Idempotency      │
 └───────────────────────────────┬──────────────────────────────┘
                                 │
                                 ▼
 ┌──────────────────────────────────────────────────────────────┐
 │                         Endpoints (slices)                     │
 │   UsersEndpoints   ProductsEndpoints   OrdersEndpoints         │
 └───────────────────────────────┬──────────────────────────────┘
                                 │  calls
                                 ▼
 ┌──────────────────────────────────────────────────────────────┐
 │                       Application / Domain                     │
 │   Handlers · domain entities · validation · resilience use     │
 └───────────────────────────────┬──────────────────────────────┘
                                 │  abstractions (IRepository<T>)
                                 ▼
 ┌──────────────────────────────────────────────────────────────┐
 │                        Infrastructure                          │
 │   In-memory repo (default)  ──swap──►  Postgres repo           │
 │   Redis cache · OTel exporters · Polly resilience pipelines    │
 └──────────────────────────────────────────────────────────────┘
```

Dependencies point **inward**: Infrastructure depends on Domain abstractions, never the reverse.
This is what makes Phase 2 (modular monolith) and Phase 3 (microservices) extractions mechanical
rather than risky.

---

## 3. Request flow with observability

A single request emits all three observability pillars, correlated by one id.

```
 Client ──► [Correlation-Id middleware] assigns/honors X-Correlation-Id
                     │
                     ├──► Trace span started (OpenTelemetry)  ──► OTLP ──► Jaeger
                     │
                     ├──► Structured JSON log line (includes correlationId, traceId)
                     │
                     ├──► Metrics recorded (rate, errors, duration)  ──► /metrics ──► Prometheus
                     │
                     ▼
              Handler executes (with retry/timeout/circuit-breaker on deps)
                     │
                     ▼
            Response (echoes X-Correlation-Id header)
```

Pivot workflow when debugging:

```
   Grafana alert (error rate ↑)
        │  copy correlationId from a failing sample
        ▼
   Logs (filter by correlationId)  ──►  find the failing operation
        │  copy traceId
        ▼
   Jaeger (open trace)  ──►  see exactly which span was slow / errored
```

---

## 4. Reliability: the three paths

Each reliability pattern is demonstrated on a happy / failure / recovery path.

```
 Happy path:     request ─► dependency (healthy) ─► fast success

 Failure path:   request ─► dependency (slow/down)
                            └─► timeout fires ─► retry (jittered) ─► still failing
                                                                  └─► circuit OPENS ─► fail fast

 Recovery path:  circuit OPEN ──(break duration)──► HALF-OPEN ─► probe ok ─► CLOSED (normal)
```

Circuit breaker state machine:

```
        failure threshold exceeded
   ┌──────────────────────────────────────┐
   ▼                                       │
 CLOSED ───────────────────────────────►  OPEN
   ▲                                       │
   │ probe succeeds          break elapsed │
   │                                       ▼
   └────────────────────────────────── HALF-OPEN
                 probe fails ──────────────┘
```

---

## 5. Architectural evolution (phases)

The same domain is re-implemented to teach the tradeoffs at each step.

```
 Phase 1            Phase 2              Phase 3                Phase 4
 MONOLITH           MODULAR MONOLITH     MICROSERVICES          EVENT-DRIVEN
 ┌────────┐         ┌────────┐           ┌──┐ ┌──┐ ┌──┐         ┌──┐  Kafka  ┌──┐
 │ U P O  │   ──►   │[U][P][O]│    ──►    │U │ │P │ │O │   ──►   │U │══════►│O │
 │ shared │         │ modules │          └──┘ └──┘ └──┘         └──┘ events └──┘
 └────────┘         └────────┘           gateway + BFF          CQRS/ES/Saga/Outbox
      │                                                              │
      └──────────────────────────────► Phase 5 CLOUD NATIVE ────────┘
                              (K8s + Helm + Istio + GitOps)
                                          │
                                          ▼
                              Phase 6 GLOBAL SCALE
                  (sharding · read replicas · multi-region · edge/CDN)
```

| Phase | Style            | Key tradeoff introduced                              |
|-------|------------------|------------------------------------------------------|
| 1     | Monolith         | Simple to run; coupling grows with size              |
| 2     | Modular monolith | Clear boundaries; still one deploy unit              |
| 3     | Microservices    | Independent scaling; network is now a failure domain |
| 4     | Event-driven     | Decoupling + resilience; eventual consistency        |
| 5     | Cloud native     | Elastic + self-healing; operational complexity       |
| 6     | Global scale     | Low global latency; data partitioning hard problems  |

---

## 6. Domain model

One domain is reused across every concept and phase:

```
   ┌────────┐   places   ┌────────┐   contains   ┌──────────┐
   │  User  │───────────►│ Order  │─────────────►│  Product │
   └────────┘            └────────┘              └──────────┘
        │                    │                        ▲
        │ authenticates      │ triggers               │ indexed by
        ▼                    ▼                        │
   ┌────────┐           ┌─────────┐              ┌──────────┐
   │  Auth  │           │ Payment │              │  Search  │
   └────────┘           └─────────┘              └──────────┘
```

Phase 1 implements **User**, **Product**, and **Order** as in-process slices. Auth, Payment, Search,
Recommendations, Chat, Notifications, Analytics, and the AI Assistant arrive in later phases as the
architecture decomposes.
