import http from "k6/http";
import { check, sleep } from "k6";
import { Trend } from "k6/metrics";

// Black Friday traffic spike: ramp hard, hold, then ramp down. Watch the monolith's RED metrics in
// Grafana and the latency histogram in Prometheus while this runs.
//
//   k6 run load-tests/scenarios/black-friday.js
//
// Requires k6 (https://k6.io). The platform must be running: `docker compose up -d --build`.

const BASE = __ENV.BASE_URL || "http://localhost:8080";

const orderLatency = new Trend("order_latency_ms");

export const options = {
  scenarios: {
    black_friday: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "30s", target: 50 },   // warm up
        { duration: "1m", target: 300 },    // the spike
        { duration: "2m", target: 300 },    // sustained peak
        { duration: "30s", target: 0 },     // drain
      ],
      gracefulRampDown: "10s",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.05"],          // <5% errors
    http_req_duration: ["p(95)<800"],         // p95 under 800ms
  },
};

const USER_ID = "11111111-1111-1111-1111-111111111111";
const PRODUCT_ID = "aaaaaaaa-0000-0000-0000-000000000002"; // 10GbE Smart NIC (stock 120)

export default function () {
  // 80% browse, 20% buy — a realistic read-heavy mix.
  if (Math.random() < 0.8) {
    const res = http.get(`${BASE}/api/v1/products`);
    check(res, { "products 200": (r) => r.status === 200 });
  } else {
    const payload = JSON.stringify({
      userId: USER_ID,
      lines: [{ productId: PRODUCT_ID, quantity: 1 }],
    });
    const res = http.post(`${BASE}/api/v1/orders`, payload, {
      headers: { "Content-Type": "application/json" },
    });
    orderLatency.add(res.timings.duration);
    check(res, { "order created": (r) => r.status === 201 });
  }

  sleep(Math.random() * 0.5);
}
