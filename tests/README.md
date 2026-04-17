# Tests

Automated tests for the SDMP services.

| Project | Type | Covers |
|---------|------|--------|
| Sdmp.Monolith.Tests | xUnit | Domain unit tests, repository tests, and in-process API integration tests |

## Run

```bash
dotnet test
```

The integration tests boot the real application in-process via `WebApplicationFactory<Program>` using
the default **in-memory** persistence provider, so no Docker or Postgres is required. They assert the
platform standards end to end: health, `/metrics`, seeded catalog, validation, the order happy path,
idempotent replay, the resilience demo endpoint, and correlation-id propagation.
