# Lab: Slow / Failing Downstream

This lab drives the monolith's reliability demo endpoint to watch the **timeout → retry → circuit
breaker → recovery** sequence in action.

The endpoint simulates a downstream dependency you control:

```
GET /api/v1/reliability/call?failRate=<0..1>&latencyMs=<int>
```

- `failRate` — probability each attempt fails.
- `latencyMs` — artificial latency per attempt (the pipeline timeout is 2s).

## 1. Happy path

Healthy dependency: fast, succeeds.

```bash
curl "http://localhost:8080/api/v1/reliability/call?failRate=0&latencyMs=20"
# {"outcome":"success","result":"ok@..."}
```

## 2. Failure path — trip the circuit breaker

Drive a high failure rate. The retry strategy makes a few attempts; once the failure ratio crosses
50% over the sampling window (min 10 calls), the breaker **opens** and requests fail fast with `503`.

```bash
# Generate sustained failing load (PowerShell)
1..40 | ForEach-Object {
  try { Invoke-WebRequest "http://localhost:8080/api/v1/reliability/call?failRate=0.9&latencyMs=20" -UseBasicParsing | Out-Null }
  catch { $_.Exception.Response.StatusCode.value__ }
}
```

```bash
# Or with curl in a loop (bash)
for i in $(seq 1 40); do
  curl -s -o /dev/null -w "%{http_code}\n" \
    "http://localhost:8080/api/v1/reliability/call?failRate=0.9&latencyMs=20"
done
```

You will see `502` (failed after retries) turn into `503` (`circuit_open`) once the breaker trips.

### Timeout path

Force latency above the 2s pipeline timeout to see `504`:

```bash
curl "http://localhost:8080/api/v1/reliability/call?failRate=0&latencyMs=2500"
# {"outcome":"timeout"}  (504)
```

## 3. Recovery path

Stop sending traffic for ~15s (the breaker's break duration). The breaker moves to **half-open** and
allows a probe. Send a healthy request:

```bash
curl "http://localhost:8080/api/v1/reliability/call?failRate=0&latencyMs=20"
```

If the probe succeeds the breaker **closes** and normal service resumes.

## What to observe

Prometheus (http://localhost:9090) / Grafana (http://localhost:3000):

```promql
sdmp_circuit_breaker_state      # 0 closed, 1 half-open, 2 open
rate(sdmp_retries_total[1m])    # retry volume spikes during the failure path
```

Jaeger (http://localhost:16686): search service `sdmp-monolith` to see the spans, including the
retried attempts and the fast-fail when the circuit is open.

## Takeaways

- Retries alone amplify load on a failing dependency — the circuit breaker is what protects it.
- A tight timeout converts a slow dependency into a fast failure, protecting your own latency budget.
- Recovery is automatic: no human action is needed once the dependency heals.
