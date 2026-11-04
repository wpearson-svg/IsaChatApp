"""
E3 helper — multilingual-e5-base embedding service
Produces 768-d vectors with the required query:/passage: prefixes.
"""
from __future__ import annotations

import os
from typing import List

from fastapi import FastAPI
from pydantic import BaseModel, Field

app = FastAPI(title="ISA Embeddings Service", version="1.0.0")

MODEL_ID = os.getenv("MODEL_ID", "intfloat/multilingual-e5-base")

_model = None
_ready = False


def load_model() -> None:
    global _model, _ready
    try:
        from sentence_transformers import SentenceTransformer

        _model = SentenceTransformer(MODEL_ID)
        _ready = True
    except Exception as exc:
        print(f"[embeddings] model load failed: {exc}")
        _ready = False


@app.on_event("startup")
def startup() -> None:
    load_model()


class EmbedRequest(BaseModel):
    texts: List[str] = Field(..., min_length=1, max_length=64)
    mode: str = Field("query", pattern="^(query|passage)$")


class EmbedResponse(BaseModel):
    vectors: List[List[float]]
    dimensions: int
    model_id: str


@app.get("/health")
def health():
    return {"ok": True, "model_ready": _ready, "model_id": MODEL_ID, "dimensions": 768}


@app.post("/embed", response_model=EmbedResponse)
def embed(req: EmbedRequest):
    if not _ready or _model is None:
        return EmbedResponse(vectors=[], dimensions=768, model_id=MODEL_ID)

    prefix = "query: " if req.mode == "query" else "passage: "
    prepared = [prefix + t for t in req.texts]
    vectors = _model.encode(prepared, normalize_embeddings=True).tolist()
    return EmbedResponse(vectors=vectors, dimensions=768, model_id=MODEL_ID)
