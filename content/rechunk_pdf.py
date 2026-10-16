#!/usr/bin/env python3
"""Re-split the PDF guidebook passages so no passage starts or ends mid-sentence.

Why: the first load cut the PDF every ~900 characters, so answers started halfway through a sentence
and carried the page number on the end. This joins the PDF text back together, repairs the hard line
wraps, drops the stray page numbers, and splits again on sentence / bullet boundaries (max ~800 chars).
Word documents are left alone (their lines already break cleanly). Safe to re-run.
Usage:  python content/rechunk_pdf.py      then      python content/build_content_db.py
"""
import json, re, os, bisect
here = os.path.dirname(os.path.abspath(__file__))
path = os.path.join(here, "content.json")
data = json.load(open(path, encoding="utf-8"))
MAX = 800
PDF = [d["document"] for d in data["documents"] if d["source"].lower().endswith(".pdf")]

def repair(text):
    """Join hard-wrapped lines; keep a break before bullets and after a sentence/heading end."""
    text = text.replace("\uf0a7", "\n•").replace("\uf0b7", "\n•")      # PDF bullet glyphs
    out = []
    for line in text.split("\n"):
        line = line.strip()
        if not line: continue
        if out and not line.startswith(("•", "- ")) and not re.search(r"[.!?:]$", out[-1]) and len(out[-1]) >= 55:   # short line = heading, keep apart
            out[-1] += " " + line
        else:
            out.append(line)
    return "\n".join(out)

def split_long(unit):
    if len(unit) <= MAX: return [unit]
    parts, cur = [], ""
    for s in re.split(r"(?<=[.!?])\s+", unit):
        if cur and len(cur) + 1 + len(s) > MAX: parts.append(cur); cur = s
        else: cur = (cur + " " + s).strip()
    if cur: parts.append(cur)
    return parts

new = []
for doc in [d["document"] for d in data["documents"]]:
    old = [p for p in data["passages"] if p["document"] == doc]
    if doc not in PDF:
        new += old; continue
    # one stream for the whole document, remembering where each old passage began
    stream, marks = "", []          # marks: (offset, section, page)
    for p in old:
        if re.search(r"\.{8,}", p["text"]): continue                     # table of contents (dot leaders): not content
        t = p["text"].rstrip()
        t = re.sub(r"\n\s*" + re.escape(p["page"]) + r"\s*$", "", t)     # trailing page number line
        if stream and not re.search(r"[.!?:]\s*$", stream): stream += " "
        elif stream: stream += "\n"
        marks.append((len(stream), p["section"], p["page"]))
        stream += t
    stream = repair(stream)
    # offsets moved during repair, so re-find each mark by its first words
    pos = 0; marks2 = []
    old = [p for p in old if not re.search(r"\.{8,}", p["text"])]
    for (_, sec, pg), p in zip(marks, old):
        head = repair(p["text"]).split("\n")[0][:40]
        k = stream.find(head, pos)
        marks2.append((k if k >= 0 else pos, sec, pg)); pos = max(pos, marks2[-1][0])
    starts = [m[0] for m in marks2]
    units, off = [], 0
    for line in stream.split("\n"):
        for part in split_long(line): units.append((off, part))
        off += len(line) + 1
    chunk, cstart = "", 0
    def flush():
        global chunk
        if chunk.strip():
            i = max(0, bisect.bisect_right(starts, cstart) - 1)
            new.append({"document": doc, "section": marks2[i][1], "page": marks2[i][2], "text": chunk.strip()})
        chunk = ""
    for off, u in units:
        if chunk and len(chunk) + 1 + len(u) > MAX: flush()
        if not chunk: cstart = off
        chunk = (chunk + "\n" + u) if chunk else u
    flush()

for i, p in enumerate(new, 1): p["id"] = i
print("passages:", len(data["passages"]), "->", len(new))
data["passages"] = new
json.dump(data, open(path, "w", encoding="utf-8", newline="\n"), ensure_ascii=False, indent=1)
