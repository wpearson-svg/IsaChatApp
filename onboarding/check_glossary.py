#!/usr/bin/env python3
"""Check onboarding/glossary.json: every quote must be ISACA's words, word for word.

For each term, each "quote" must appear exactly (spaces aside) in the document it
names, in the same text the Library reads aloud:
  - data/content.db for ISACA's documents Isa answers from (and, when "pages" is
    given, on that page of the President Guidebook),
  - onboarding/chapter360.json for the Chapter 360 documents,
  - onboarding/library_texts/<id>.json for documents such as the Chapter
    Affiliation Agreement.
It also checks that every "document" is in onboarding/library.json.

Usage (from the IsaChatApp folder): python onboarding/check_glossary.py
Prints one line per problem and the number of terms checked. No AI is involved."""
import json, os, re, sqlite3, sys

here = os.path.dirname(os.path.abspath(__file__))
root = os.path.join(here, "..")


def load_jsonc(path):
    with open(path, encoding="utf-8") as f:
        return json.loads("\n".join(l for l in f.read().splitlines() if not l.strip().startswith("//")))


def norm(t):
    return re.sub(r"\s+", " ", t).strip()


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    glossary = load_jsonc(os.path.join(here, "glossary.json"))["terms"]
    library = {d["document"]: d for d in load_jsonc(os.path.join(here, "library.json"))["documents"]}
    db = sqlite3.connect(os.path.join(root, "data", "content.db"))
    c360 = {d["id"]: d for d in json.load(open(os.path.join(here, "chapter360.json"), encoding="utf-8"))["documents"]}

    def text_of(doc, pages):
        entry = library.get(doc)
        if entry is None:
            return None
        source = entry.get("source")
        if source == "chapter360":
            d = c360.get(doc)
            return None if d is None else " ".join(
                [s.get("heading") or "" for s in d["sections"]] + [p["text"] for s in d["sections"] for p in s["paragraphs"]])
        if source == "text":
            d = json.load(open(os.path.join(here, "library_texts", doc + ".json"), encoding="utf-8"))
            return " ".join([p["text"] for s in d["sections"] for p in s["paragraphs"]])
        rows = db.execute("SELECT page, text FROM passages WHERE document = ?", (doc,)).fetchall()
        if pages:
            lo, _, hi = pages.partition("-")
            wanted = {str(p) for p in range(int(lo), int(hi or lo) + 1)}
            rows = [r for r in rows if str(r[0]) in wanted]
        return " ".join(r[1] for r in rows) if rows else None

    problems = 0
    for t in glossary:
        text = text_of(t["document"], t.get("pages"))
        if text is None:
            print(f"{t['term']}: document not in the Library (or no such page): {t['document']} {t.get('pages', '')}")
            problems += 1
            continue
        hay = norm(text)
        for q in t["quote"]:
            if norm(q) not in hay:
                print(f"{t['term']}: not found word for word in {t['document']}: {q[:80]}")
                problems += 1
    terms = [t["term"] for t in glossary]
    if len(terms) != len(set(terms)):
        print("A term is listed twice.")
        problems += 1
    print(f"{len(glossary)} terms checked, {problems} problem(s).")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
