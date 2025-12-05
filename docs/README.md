# Documentation — Concept Curriculum

This is the curriculum index for SDMP. Each concept area maps to **runnable code** elsewhere in the
repository, so you can read the theory and then immediately run, break, and scale the implementation.

## How to use this curriculum

Each concept follows the same template:

1. **What & why** — the problem it solves and when to reach for it.
2. **Diagram** — how it fits into the system.
3. **Code** — where the runnable implementation lives.
4. **Metrics** — what to watch in Prometheus/Grafana.
5. **Failure path** — how it breaks (and the chaos lab that proves it).
6. **Scaling path** — how it scales (and the load test that proves it).
7. **Tradeoffs** — what you give up.

## Concept areas

| Area | Folder | Status |
|------|--------|--------|
| Platform standards | [standards/](standards/README.md) | ✅ |
| Roadmap & phases | [roadmap.md](roadmap.md) | ✅ |
| Distributed systems | distributed-systems/ | 🚧 |
| APIs | apis/ | 🚧 |
| Databases | databases/ | 🚧 |
| Messaging | messaging/ | 🚧 |
| Architecture | architecture/ | 🚧 |
| Reliability | [reliability/](reliability/README.md) | ✅ |
| Observability | [observability/](observability/README.md) | ✅ |
| Platform engineering | platform-engineering/ | 🚧 |
| AI systems | ai-systems/ | 🚧 |

## Domain model

Every concept reuses one domain — a global commerce + AI platform:

```
Users · Auth · Catalog · Search · Orders · Payments
Recommendations · Chat · Notifications · Analytics · AI Assistant
```

Reusing a single domain across all concepts lets you compare approaches (e.g. "orders as CRUD" vs
"orders as event sourcing") on identical requirements.
