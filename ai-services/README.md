# Isa helper services

Two small Python services, built by Eliyas (E1 and E3), run in Docker next to Isa. They hold no data: Isa keeps all of its data in SQLite.

| Service | What it does | Port | Check |
|---|---|---|---|
| translation | NLLB-200, English and Amharic. When the model is not available it returns the original text with `fallback: true`, never claiming it translated. | 8091 | http://localhost:8091/health |
| embeddings | multilingual-e5-base, turns text into 768-number vectors | 8092 | http://localhost:8092/health |

## Start

```
cd ai-services
docker compose up -d --build
```

The first start downloads each model into a Docker volume. Until the model is loaded, `/health` says `model_ready: false`.

## Build the search vectors

Search by meaning needs a vector for each approved passage. With the embedding service running:

```
python content\build_vectors.py
```

This writes `data\vectors.db` (ignored by Git). Run it again after every rebuild of `content.db`.

## In Isa

`Services/Search/VectorSearchService.cs` reads `data\vectors.db`, asks the embedding service for the question's vector, and returns the closest approved passages with source, section, page and score, or a clean "not found". Settings in `appsettings.json` (all optional): `Embeddings:BaseUrl`, `Embeddings:TimeoutSeconds`, `VectorSearch:Path`, `VectorSearch:MinScore` (default 0.25, not yet checked against the real passages).

Not connected to the chat pipeline yet.
