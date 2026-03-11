# AI System Design

The AI half of the platform: retrieval-augmented generation, GraphRAG, agents, and semantic search —
built with the same production standards (observability, reliability, scalability) as every other
service.

| Topic | Technology | Status |
|-------|-----------|--------|
| RAG (retrieval-augmented generation) | Qdrant + OpenAI SDK | 🗺️ |
| GraphRAG | Neo4j | 🗺️ |
| AI agents / multi-agent | LangGraph | 🗺️ |
| Semantic search | Vector DB (Qdrant) | 🗺️ |
| Model serving | vLLM | 🗺️ |
| AI observability | OpenTelemetry (traces over prompts/tools) | 🗺️ |
| MCP (Model Context Protocol) | — | 🗺️ |

## Design principles

- **AI is a service, not magic.** The AI Assistant exposes the same `/health`, `/metrics`, tracing,
  and structured logs as any other service.
- **Observe the chain.** Every retrieval, prompt, and tool call is a span so you can debug latency
  and quality the same way you debug a distributed request.
- **Reliability applies.** Timeouts, retries, and circuit breakers wrap model and vector-store calls.

The AI Assistant service is introduced in later phases — see [docs/roadmap.md](../docs/roadmap.md).
