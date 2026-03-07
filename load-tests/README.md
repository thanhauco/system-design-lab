# Load Tests

Load scenarios that push the platform so you can watch scalability behavior and tail latency in
Grafana while traffic ramps.

## Prerequisites

- [k6](https://k6.io) installed.
- Platform running: `docker compose up -d --build`.

## Scenarios

| Scenario | What it simulates | File |
|----------|-------------------|------|
| Black Friday | A hard traffic spike with an 80% browse / 20% buy mix | [scenarios/black-friday.js](scenarios/black-friday.js) |

```bash
k6 run load-tests/scenarios/black-friday.js
# point at a different host:
BASE_URL=http://localhost:8080 k6 run load-tests/scenarios/black-friday.js
```

## What to watch while it runs

Grafana (http://localhost:3000) / Prometheus (http://localhost:9090):

```promql
# Throughput
sum(rate(http_server_request_duration_seconds_count[1m]))

# p95 latency
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le))

# Error rate
sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[1m]))
  / sum(rate(http_server_request_duration_seconds_count[1m]))

# Orders created during the spike
rate(sdmp_orders_created_total[1m])
```

## Thresholds

The Black Friday scenario asserts `<5%` errors and `p95 < 800ms`. When those fail, that is your
signal to scale (Phase 5 adds autoscaling) or add caching / read replicas (Phase 6).
