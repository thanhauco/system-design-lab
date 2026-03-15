# Interview Prep

System-design interview drills mapped to **running code** in this repo, so you can move from "I can
describe it" to "I have built and operated it."

## Format

Each drill states a prompt, the key talking points, and a link to the running implementation you can
demo or reason about concretely.

| Drill | Backed by | Status |
|-------|-----------|--------|
| Design a resilient order flow | [Orders slice](../services/monolith/Features/Orders/OrdersEndpoints.cs) + [reliability](../docs/reliability/README.md) | ✅ |
| Make a payment API idempotent | [IdempotencyMiddleware](../services/monolith/Reliability/IdempotencyMiddleware.cs) | ✅ |
| Add observability to a service | [observability](../docs/observability/README.md) | ✅ |
| Handle a Black Friday spike | [load test](../load-tests/README.md) | ✅ |
| Decompose a monolith into services | [roadmap Phase 3](../docs/roadmap.md) | 🗺️ |

## Why code-backed drills

Interviewers probe for depth. Being able to say "here is the circuit breaker config, here is the
metric that shows it tripping, here is the recovery behavior" is far stronger than reciting the
pattern. Every drill here points at something you can actually run.
