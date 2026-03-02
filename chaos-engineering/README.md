# Chaos Engineering

Chaos labs prove the reliability standards under real failure. Each lab drives the running monolith,
injects a failure, and tells you exactly what to observe in Prometheus, Grafana, and Jaeger.

## Prerequisites

```bash
docker compose up -d --build
```

## Labs

| Lab | Concept proven | File |
|-----|----------------|------|
| Slow / failing downstream | Timeout → retry → circuit breaker → recovery | [labs/slow-downstream.md](labs/slow-downstream.md) |

More labs (database outage, Redis outage, Kafka outage, region failure, network partition, cache
stampede, DDoS) arrive as later phases add those dependencies. See [docs/roadmap.md](../docs/roadmap.md).

## The three paths every lab demonstrates

1. **Happy path** — dependency healthy, low latency, success.
2. **Failure path** — dependency degraded, the reliability pattern contains the blast radius.
3. **Recovery path** — dependency heals, the system returns to normal automatically.
