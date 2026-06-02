# Outbox Pattern

> Reliable event publishing: never lose an event, even when the broker is down.

## The problem it solves

When an operation must both **change data** and **publish an event**, a naive implementation does two
separate writes:

```
save order to DB        ✅
publish OrderCreated    ❌ broker down → event lost forever
```

There is no transaction spanning the database and the broker, so any failure between the two leaves
the system inconsistent: the order exists but nobody downstream knows.

## The solution

Write the event into an **outbox table in the same database transaction** as the business change. A
separate processor reads the outbox and publishes asynchronously, retrying until it succeeds.

```
┌─────────────── one DB transaction ───────────────┐
│  INSERT order            INSERT outbox(OrderCreated)│  ✅ all-or-nothing
└──────────────────────────────────────────────────┘
                 │
                 ▼   (separate, retryable)
        OutboxProcessor polls → publishes → marks processed
```

Because the event is committed atomically with the data, it can never be lost. The processor
guarantees **at-least-once** delivery (consumers must therefore be idempotent — see
[reliability](../../docs/reliability/README.md)).

## Implementation in this repo

| Piece | File |
|-------|------|
| Message entity | `services/monolith/Messaging/OutboxMessage.cs` |
| Abstraction | `services/monolith/Messaging/IOutbox.cs` |
| In-memory store | `services/monolith/Messaging/InMemoryOutbox.cs` |
| EF (transactional) store | `services/monolith/Messaging/EfOutbox.cs` |
| Background processor | `services/monolith/Messaging/OutboxProcessor.cs` |

Order creation stages the event before persisting the order:

```csharp
await outbox.EnqueueAsync("OrderCreated", payload, ct); // tracked, not yet committed
await orders.AddAsync(order, ct);                        // SaveChanges commits BOTH atomically
```

With the **EF-backed** outbox (Postgres) both rows share one `SaveChanges`, giving the true
transactional guarantee. The in-memory store is a best-effort approximation for the
zero-dependency configuration.

## Observe it

- Metric: `sdmp_outbox_published_total` (rate of published events).
- Trace: each publish is an `OutboxPublish` span in Jaeger.
- Logs: `Publishing outbox message OrderCreated <id>`.

## Tradeoffs

- **Latency:** publishing is asynchronous (poll interval), so consumers see events slightly later.
- **At-least-once:** a crash after publish but before marking processed re-publishes — consumers must
  dedupe. This is the same idempotency discipline used elsewhere in the platform.
- **Polling cost:** mitigated with an index on the pending set; high-throughput systems switch to
  change-data-capture (e.g. Debezium) in Phase 4.
