"""
E1 — NLLB-200 translation service (Amharic ↔ English)
Self-hosted. Safe fallback when the model is unavailable.
"""
from __future__ import annotations

import os
from typing import Optional

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

app = FastAPI(title="ISA Translation Service", version="1.0.0")

MODEL_ID = os.getenv("MODEL_ID", "facebook/nllb-200-distilled-600M")
DEVICE = os.getenv("DEVICE", "cpu")

# NLLB language codes
LANG = {"eng": "eng_Latn", "amh": "amh_Ethi"}

_tokenizer = None
_model = None
_ready = False


def load_model() -> None:
    global _tokenizer, _model, _ready
    try:
        from transformers import AutoModelForSeq2SeqLM, AutoTokenizer
        import torch

        _tokenizer = AutoTokenizer.from_pretrained(MODEL_ID)
        _model = AutoModelForSeq2SeqLM.from_pretrained(MODEL_ID)
        if DEVICE == "cuda" and torch.cuda.is_available():
            _model = _model.to("cuda")
        _ready = True
    except Exception as exc:  # model may be downloading or OOM
        print(f"[translation] model load failed: {exc}")
        _ready = False


@app.on_event("startup")
def startup() -> None:
    load_model()


class TranslateRequest(BaseModel):
    text: str = Field(..., min_length=1, max_length=4000)
    source_lang: str = Field(..., pattern="^(eng|amh)$")
    target_lang: str = Field(..., pattern="^(eng|amh)$")


class TranslateResponse(BaseModel):
    translated_text: str
    model_id: str
    fallback: bool


@app.get("/health")
def health():
    return {"ok": True, "model_ready": _ready, "model_id": MODEL_ID}


@app.post("/translate", response_model=TranslateResponse)
def translate(req: TranslateRequest):
    if req.source_lang == req.target_lang:
        return TranslateResponse(
            translated_text=req.text, model_id=MODEL_ID, fallback=False
        )

    if not _ready or _model is None or _tokenizer is None:
        # Safe fallback — never claim we translated
        return TranslateResponse(
            translated_text=req.text, model_id=MODEL_ID, fallback=True
        )

    try:
        src = LANG[req.source_lang]
        tgt = LANG[req.target_lang]
        _tokenizer.src_lang = src
        inputs = _tokenizer(req.text, return_tensors="pt", truncation=True, max_length=512)
        if DEVICE == "cuda":
            inputs = {k: v.to("cuda") for k, v in inputs.items()}
        forced_bos = _tokenizer.convert_tokens_to_ids(tgt)
        outputs = _model.generate(
            **inputs,
            forced_bos_token_id=forced_bos,
            max_length=512,
            num_beams=4,
        )
        text = _tokenizer.batch_decode(outputs, skip_special_tokens=True)[0]
        return TranslateResponse(translated_text=text, model_id=MODEL_ID, fallback=False)
    except Exception as exc:
        print(f"[translation] error: {exc}")
        return TranslateResponse(
            translated_text=req.text, model_id=MODEL_ID, fallback=True
        )
