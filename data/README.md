# Data

Database schemas, seed data, and data-modeling exercises across the polyglot persistence stack
SDMP teaches: PostgreSQL, MongoDB, Redis, Elasticsearch, ClickHouse, Neo4j, and Qdrant.

| Topic | Status |
|-------|--------|
| Relational modeling (PostgreSQL) | 🚧 |
| Document modeling (MongoDB) | 🗺️ |
| Caching patterns (Redis) | 🗺️ |
| Search indexing (Elasticsearch) | 🗺️ |
| Analytical store (ClickHouse) | 🗺️ |
| Graph modeling (Neo4j) | 🗺️ |
| Vector search (Qdrant) | 🗺️ |

Phase 1 uses an in-memory store by default with a Postgres-ready repository abstraction
(`IRepository<T>`); see [services/monolith](../services/monolith/README.md). Concrete schemas land as
each datastore is introduced in later phases — see [docs/roadmap.md](../docs/roadmap.md).
