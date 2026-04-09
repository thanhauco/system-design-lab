# Observability Stack

The local observability backends for SDMP, wired into the platform via
[docker-compose.yml](../docker-compose.yml).

| Component | Config | URL |
|-----------|--------|-----|
| Prometheus | [prometheus/prometheus.yml](prometheus/prometheus.yml) | http://localhost:9090 |
| Grafana | [grafana/provisioning](grafana/provisioning) | http://localhost:3000 (admin/admin) |
| OTel Collector | [otel-collector/config.yaml](otel-collector/config.yaml) | OTLP :4317 / :4318 |
| Jaeger | (all-in-one) | http://localhost:16686 |

## Data flow

```
monolith ──/metrics──►  Prometheus ──►  Grafana
monolith ──OTLP──►  OTel Collector ──►  Jaeger
```

- The monolith exposes Prometheus metrics at `/metrics`; Prometheus scrapes it every 5s.
- The monolith exports OpenTelemetry traces via OTLP to the collector, which forwards them to Jaeger.
- Grafana is pre-provisioned with Prometheus and Jaeger data sources.

## Pre-provisioned dashboard

Grafana auto-loads the **SDMP Monolith — RED & Reliability** dashboard
([json/sdmp-monolith.json](grafana/provisioning/dashboards/json/sdmp-monolith.json)) on first boot.
It shows request rate, error rate, latency percentiles (p50/p95/p99), circuit-breaker state, and
retry/order rates. Open Grafana → Dashboards → SDMP folder.

## Useful Prometheus queries

```promql
# Request rate
sum(rate(http_server_request_duration_seconds_count[1m]))

# p95 latency
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le))

# Orders created
sdmp_orders_created_total

# Circuit breaker state (0 closed, 1 half-open, 2 open)
sdmp_circuit_breaker_state

# Retry volume
rate(sdmp_retries_total[1m])
```

See the concept guide in [docs/observability](../docs/observability/README.md).
