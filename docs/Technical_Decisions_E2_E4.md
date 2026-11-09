# Technical decisions: search, embeddings, RAG and the knowledge graph (E2 and E4)

Status: what Isa uses now, written 2026-11-09. This replaces Eliyas's proposal (`TECHNICAL_DECISIONS.md`), which chose Postgres with pgvector. The decisions were settled while the search was built into Isa.

## E2.1 Vector store
**SQLite, `data/vectors.db`.** The vectors are loaded into memory and compared by cosine similarity. The passage text is read from `content.db`, so only approved content can come back. No separate database is needed. Built by `content/build_vectors.py`; searched by `Services/Search/VectorSearchService.cs`.

## E2.2 Embedding model
**`intfloat/multilingual-e5-base`** (768 dimensions), self-hosted in `ai-services/embeddings`, called through `Services/Search/EmbeddingClient.cs`. It uses the `query: ` and `passage: ` prefixes.

## E2.3 RAG approach
**Hand-built, inside the `rag` stage.** On a miss from `database_search`: the meaning map is tried first, then vector search over ISACA's own approved documents (top `VectorSearch:TopK` = 3, at or above `VectorSearch:MinScore`). Passages go to the model with the question. A miss, or an embedding service that is down, escalates to a person; the escalation records `vector_miss=<reason>`. No external RAG framework (Kernel Memory) is used. Question text is never indexed or stored.

Open point: `VectorSearch:MinScore` (0.80) is a starting value, not calibrated against real questions.

## E4 Knowledge graph
**Kept simple.** No graph database. The stage is a pass-through inside the RAG step. Reason: no graph query has been shown to improve retrieval on the approved content. Revisit when concrete graph queries exist and a before-and-after test shows a gain. "Graph-cached answers" in the Sprint 1 proposal is therefore not built.
