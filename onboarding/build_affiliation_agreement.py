#!/usr/bin/env python3
"""Rebuild onboarding/library_texts/affiliation-agreement.json from ISACA's
Chapter Affiliation Agreement (content/Chapter Affiliation Agreement-2024-template.docx).

Used by the Library (Services/Onboarding/LibraryService.cs), which reads and plays
it word for word. Isa does not answer questions from it: it is not in her approved
content (data/content.db). Adding it there is a team decision with Willis.

ISACA's words are kept as they are, including the template's blanks such as
<DATE> and [CHAPTER NAME]. The clause numbers and letters Word shows on screen
(I., A., i.) are put back in front of each paragraph, because the agreement refers
to them ("paragraph D. of Clause III."). Left out, because they are not prose:
  - the two tables near the end: the notice addresses and the signature block,
    which are a form to fill in,
  - the page header and footer.

Usage (from the IsaChatApp folder): python onboarding/build_affiliation_agreement.py
Run it again whenever ISACA sends a new version. No AI is involved."""
import json, os, re, sys, zipfile
from xml.etree import ElementTree as ET

here = os.path.dirname(os.path.abspath(__file__))
src = os.path.join(here, "..", "content", "Chapter Affiliation Agreement-2024-template.docx")
out_dir = os.path.join(here, "library_texts")
out = os.path.join(out_dir, "affiliation-agreement.json")
W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"


def roman(n, upper):
    vals = [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
            (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")]
    s = ""
    for v, r in vals:
        while n >= v:
            s, n = s + r, n - v
    return s if upper else s.lower()


def label(fmt, n):
    if fmt == "upperRoman": return roman(n, True)
    if fmt == "lowerRoman": return roman(n, False)
    if fmt == "upperLetter": return chr(ord("A") + n - 1)
    if fmt == "lowerLetter": return chr(ord("a") + n - 1)
    if fmt == "decimal": return str(n)
    return ""


def clean(t):
    t = t.replace("​", "").replace(" ", " ")
    return re.sub(r"\s+", " ", t).strip()


def main():
    with zipfile.ZipFile(src) as z:
        doc = ET.fromstring(z.read("word/document.xml"))
        numbering = ET.fromstring(z.read("word/numbering.xml"))

    # numId -> its list definition: per level, the number format, its text ("%1.") and start.
    abstract = {}
    for a in numbering.iter(W + "abstractNum"):
        levels = {}
        for lvl in a.iter(W + "lvl"):
            fmt, text, start = lvl.find(W + "numFmt"), lvl.find(W + "lvlText"), lvl.find(W + "start")
            levels[int(lvl.get(W + "ilvl"))] = (
                fmt.get(W + "val") if fmt is not None else "",
                text.get(W + "val") if text is not None else "",
                int(start.get(W + "val")) if start is not None else 1)
        abstract[a.get(W + "abstractNumId")] = levels
    lists = {}
    for n in numbering.iter(W + "num"):
        levels = dict(abstract.get(n.find(W + "abstractNumId").get(W + "val"), {}))
        for o in n.iter(W + "lvlOverride"):
            so = o.find(W + "startOverride")
            il = int(o.get(W + "ilvl"))
            if so is not None and il in levels:
                f, t, _ = levels[il]
                levels[il] = (f, t, int(so.get(W + "val")))
        lists[n.get(W + "numId")] = levels

    in_table = {id(p) for t in doc.iter(W + "tbl") for p in t.iter(W + "p")}
    counters = {}   # numId -> {level: current number}

    sections = [{"heading": None, "paragraphs": []}]
    for p in doc.iter(W + "p"):
        if id(p) in in_table:
            continue
        text = clean("".join(t.text or "" for t in p.iter(W + "t")))
        if not text:
            continue

        prefix, level, num_id = "", None, None
        np_ = p.find(f"{W}pPr/{W}numPr")
        if np_ is not None and np_.find(W + "numId") is not None:
            num_id = np_.find(W + "numId").get(W + "val")
            il = np_.find(W + "ilvl")
            level = int(il.get(W + "val")) if il is not None else 0
            levels = lists.get(num_id, {})
            if level in levels:
                c = counters.setdefault(num_id, {})
                fmt, pattern, start = levels[level]
                c[level] = c.get(level, start - 1) + 1
                for deeper in [k for k in c if k > level]:
                    del c[deeper]
                prefix = pattern
                for k in range(level + 1):
                    if f"%{k + 1}" in prefix:
                        kf, _, ks = levels.get(k, ("", "", 1))
                        prefix = prefix.replace(f"%{k + 1}", label(kf, c.get(k, ks)))
                prefix = prefix.strip()

        # A new section: each numbered clause (I., II., ...) and each exhibit.
        is_clause = num_id == "1" and level == 0
        if is_clause or text.startswith("EXHIBIT"):
            sections.append({"heading": f"{prefix} {text}".strip() if is_clause else text, "paragraphs": []})
            continue
        sections[-1]["paragraphs"].append({"bullet": False, "text": f"{prefix} {text}".strip() if prefix else text})

    sections = [s for s in sections if s["paragraphs"]]
    words = sum(len((s["heading"] or "").split()) + sum(len(p["text"].split()) for p in s["paragraphs"]) for s in sections)
    data = {
        "id": "affiliation-agreement",
        "title": "Chapter Affiliation Agreement",
        "file": "content/Chapter Affiliation Agreement-2024-template.docx",
        "words": words,
        "sections": sections,
    }
    os.makedirs(out_dir, exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    print(f"Wrote {out}: {len(sections)} sections, {words} words.")


if __name__ == "__main__":
    sys.exit(main())
