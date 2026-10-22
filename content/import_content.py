#!/usr/bin/env python3
"""Add new files (or a whole folder) to the content database.

Usage:
    python content/import_content.py <file or folder> [<file or folder> ...] [--replace] [--no-build]

What it does:
  1. Reads each .docx, .pptx and .pdf (a folder is searched, subfolders included).
  2. Splits the text into passages of about 800 characters, on paragraph / sentence boundaries.
  3. Appends them to content/content.json. Existing passages keep their ids.
  4. Runs content/build_content_db.py, which rebuilds data/content.db.
  5. Adds the new documents to data/content_categories.csv as "Uncategorized" if they are not listed.

A file already in content.json (same path under content/) is skipped. --replace swaps its passages for
the new ones instead. Stop Isa first: the rebuild replaces data/content.db.
Needs only Python. PDFs also need the pypdf package (pip install pypdf) or pdftotext on the path.
"""
import json, os, re, subprocess, sys, zipfile, csv
import xml.etree.ElementTree as ET

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, ".."))
JSON_PATH = os.path.join(HERE, "content.json")
CAT_PATH = os.path.join(ROOT, "data", "content_categories.csv")
MAX = 800
KINDS = (".docx", ".pptx", ".pdf")
W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"
A = "{http://schemas.openxmlformats.org/drawingml/2006/main}"


def split_long(unit):
    if len(unit) <= MAX:
        return [unit]
    parts, cur = [], ""
    for s in re.split(r"(?<=[.!?])\s+", unit):
        if cur and len(cur) + 1 + len(s) > MAX:
            parts.append(cur); cur = s
        else:
            cur = (cur + " " + s).strip()
    if cur:
        parts.append(cur)
    return parts


def chunk(lines):
    """Pack lines into passages of at most ~MAX characters, never splitting inside a sentence."""
    out, cur = [], ""
    for line in lines:
        for part in split_long(line):
            if cur and len(cur) + 1 + len(part) > MAX:
                out.append(cur); cur = part
            else:
                cur = (cur + "\n" + part) if cur else part
    if cur:
        out.append(cur)
    return out


# ---- readers: each returns [(section, page, [lines])] -------------------------

def read_docx(path):
    with zipfile.ZipFile(path) as z:
        root = ET.fromstring(z.read("word/document.xml"))
    body = root.find(W + "body")
    sections, heading, lines = [], "Introduction", []

    def flush():
        if lines:
            sections.append((heading, "n/a", list(lines)))
        lines.clear()

    def para_text(p):
        return "".join(t.text or "" for t in p.iter(W + "t")).strip()

    for el in body:
        if el.tag == W + "p":
            txt = para_text(el)
            if not txt:
                continue
            style = el.find(W + "pPr/" + W + "pStyle")
            sid = style.get(W + "val") if style is not None else ""
            if re.match(r"(?i)^(heading\d|title)", sid or ""):
                flush(); heading = txt
            else:
                if el.find(W + "pPr/" + W + "numPr") is not None:
                    txt = "• " + txt
                lines.append(txt)
        elif el.tag == W + "tbl":
            for tr in el.iter(W + "tr"):
                cells = [" ".join(para_text(p) for p in tc.iter(W + "p")).strip() for tc in tr.iter(W + "tc")]
                cells = [c for c in cells if c]
                if cells:
                    lines.append(" | ".join(cells))
    flush()
    return sections


def read_pptx(path):
    with zipfile.ZipFile(path) as z:
        names = sorted((n for n in z.namelist() if re.match(r"ppt/slides/slide\d+\.xml$", n)),
                       key=lambda n: int(re.findall(r"\d+", n)[0]))
        out = []
        for n in names:
            num = str(int(re.findall(r"\d+", n)[0]))
            root = ET.fromstring(z.read(n))
            lines = []
            for p in root.iter(A + "p"):
                t = "".join(x.text or "" for x in p.iter(A + "t")).strip()
                if t:
                    lines.append(t)
            if lines:
                out.append(("Slide " + num, num, lines))
    return out


def read_pdf(path):
    pages = []
    try:
        import pypdf
        for i, pg in enumerate(pypdf.PdfReader(path).pages, 1):
            pages.append((i, pg.extract_text() or ""))
    except ImportError:
        try:
            raw = subprocess.run(["pdftotext", "-layout", path, "-"], capture_output=True, check=True).stdout.decode("utf-8", "replace")
            pages = [(i, t) for i, t in enumerate(raw.split("\f"), 1) if t.strip()]
        except Exception:
            sys.exit("Cannot read PDFs: run  pip install pypdf  and try again.")
    out = []
    for n, text in pages:
        text = text.replace("", "\n•").replace("", "\n•")
        raw_lines = [l.strip() for l in text.split("\n") if l.strip()]
        lines = []
        for l in raw_lines:
            # join hard-wrapped lines; a short line is treated as a heading and kept apart
            if lines and not l.startswith(("•", "- ")) and not re.search(r"[.!?:]$", lines[-1]) and len(lines[-1]) >= 55:
                lines[-1] += " " + l
            else:
                lines.append(l)
        lines = [l for l in lines if not re.fullmatch(r"\d{1,3}", l)]       # stray page numbers
        if lines:
            out.append(("Page " + str(n), str(n), lines))
    return out


READERS = {".docx": read_docx, ".pptx": read_pptx, ".pdf": read_pdf}

DOT_LEADER = re.compile(r"\.{5,}\s*\d+\s*$", re.M)


def is_toc(text):
    return "table of contents" in text.lower() or len(DOT_LEADER.findall(text)) >= 2


def collect(paths):
    files = []
    for p in paths:
        p = os.path.abspath(p)
        if os.path.isdir(p):
            for d, _, fs in os.walk(p):
                files += [os.path.join(d, f) for f in sorted(fs) if f.lower().endswith(KINDS) and not f.startswith("~$")]
        elif os.path.isfile(p) and p.lower().endswith(KINDS):
            files.append(p)
        else:
            print("skipped (not a .docx, .pptx or .pdf, or not found):", p)
    return files


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    replace = "--replace" in sys.argv
    build = "--no-build" not in sys.argv
    if not args:
        sys.exit(__doc__)
    data = json.load(open(JSON_PATH, encoding="utf-8"))
    next_id = max([p["id"] for p in data["passages"]] + [0]) + 1
    known = {d["source"]: d for d in data["documents"]}
    titles = {d["document"] for d in data["documents"]}
    added_docs, added_passages = [], 0

    for f in collect(args):
        content_root = os.path.join(ROOT, "content")
        rel = os.path.relpath(f, content_root).replace("\\", "/") if os.path.abspath(f).startswith(os.path.abspath(content_root)) else os.path.basename(f)
        title = os.path.splitext(os.path.basename(f))[0]
        folder = rel.split("/")[0] if "/" in rel else "(root)"
        if rel in known:
            if not replace:
                print("already loaded, skipped (use --replace to reload):", rel); continue
            old = known[rel]["document"]
            data["passages"] = [p for p in data["passages"] if p["document"] != old]
            data["documents"] = [d for d in data["documents"] if d["source"] != rel]
            titles.discard(old)
        if folder not in ("(root)", "Chapter Leader Processes"):
            title = folder + " - " + title          # "Chapter 360 - Introduction", not just "Introduction"
        if title in titles:
            title = title + " (" + folder + ")"
        sections = READERS[os.path.splitext(f)[1].lower()](f)
        n = 0
        for sec, page, lines in sections:
            for text in chunk(lines):
                if is_toc(text) or len(text.strip()) < 25:
                    continue
                data["passages"].append({"document": title, "section": sec, "page": page, "text": text.strip(), "id": next_id})
                next_id += 1; n += 1
        if n == 0:
            print("no readable text found, skipped:", rel); continue
        data["documents"].append({"document": title, "source": rel, "folder": folder})
        titles.add(title); added_docs.append(title); added_passages += n
        print("added  %-60s %3d passages" % (title, n))

    if not added_docs:
        print("Nothing new to add."); return
    json.dump(data, open(JSON_PATH, "w", encoding="utf-8", newline="\n"), ensure_ascii=False, indent=1)

    cats = {}
    if os.path.exists(CAT_PATH):
        with open(CAT_PATH, encoding="utf-8-sig", newline="") as fh:
            cats = {r["document"]: r["category"] for r in csv.DictReader(fh)}
    new_cats = [t for t in added_docs if t not in cats]
    if new_cats:
        os.makedirs(os.path.dirname(CAT_PATH), exist_ok=True)
        with open(CAT_PATH, "a", encoding="utf-8", newline="") as fh:
            if not cats:
                fh.write("document,category\n")
            w = csv.writer(fh, lineterminator="\n")
            for t in new_cats:
                w.writerow([t, "Uncategorized"])
        print("\nSet a category for these in data/content_categories.csv (Governance, Operations, Programs or Products):")
        for t in new_cats:
            print("   ", t)

    print("\nTotal: %d documents added, %d passages." % (len(added_docs), added_passages))
    if build:
        print("Rebuilding data/content.db ...")
        r = subprocess.run([sys.executable, os.path.join(HERE, "build_content_db.py")])
        if r.returncode:
            sys.exit("The rebuild failed. Is Isa still running? Stop Isa and run it again (the new passages are already in content.json).")
        print("Done. Start Isa to use the new content.")


if __name__ == "__main__":
    main()
