#!/usr/bin/env python3
"""Rebuild onboarding/chapter360.json from ISACA's Chapter 360 Word documents.

Used by the sample Chapter 360 page (Components/Pages/SampleChapter360.razor)
and by the Library's Chapter 360 group (Services/Onboarding/Chapter360Service.cs).
ISACA's words are kept as they are. Left out, because they are not prose:
  - "[Node] ..." lines (ISACA's internal cross-reference codes),
  - the text of the diagram in the Introduction ("1.0 Focus Area", "1.1 Practice
    Area" ... repeated), keeping one copy of its "Practice Areas articulate:" list,
  - the asterisk after template names (its meaning isn't known) and
    invisible zero-width spaces.
"TBD" entries are kept, so Read and Listen stay word for word.

Usage (from the IsaChatApp folder): python onboarding/build_chapter360.py
Run it again whenever the Word documents change. No AI is involved."""
import json, os, re, sys, zipfile
from xml.etree import ElementTree as ET

here = os.path.dirname(os.path.abspath(__file__))
src = os.path.join(here, "..", "content", "Chapter 360")
out = os.path.join(here, "chapter360.json")
W = "{http://schemas.openxmlformats.org/wordprocessingml/2006/main}"

# File, Focus Area code, title shown in the Library.
FILES = [
    ("Introduction.docx", None, "Chapter 360: Introduction"),
    ("01. Operations.docx", "OP", "Chapter 360: Operations"),
    ("02. Member Experience.docx", "MX", "Chapter 360: Member Experience"),
    ("03. Professional Development.docx", "PD", "Chapter 360: Professional Development"),
    ("04. Governance.docx", "GV", "Chapter 360: Governance"),
    ("05. Advocacy.docx", "AD", "Chapter 360: Advocacy"),
    ("06. Continuous Improvement.docx", "CI", "Chapter 360: Continuous Improvement"),
]

# Words that start a resource line ("Template: Budget"), so a resource is never
# mistaken for a practice that lost its number.
TOOL_TYPES = {"template", "list", "report", "presentation", "link", "weblink", "website link", "video",
              "sample", "isaca sop", "raci chart", "task checklist", "checklist", "guide", "toolkit",
              "document", "form", "policy", "tool", "survey", "webinar", "course", "article", "page",
              "reference", "example", "examples", "spreadsheet", "infographic", "plan", "process", "sop", "q&a"}


def tool_kind(kind):
    """'Website Link (Video)', '[Report]', 'Templates' -> 'website link', 'report', 'template'."""
    k = re.sub(r"\s*\(.*\)$", "", kind).strip().strip("[]").strip().lower()
    return k[:-1] if k.endswith("s") and k[:-1] in TOOL_TYPES else k


def clean(t):
    t = t.replace("​", "").replace(" ", " ")
    t = re.sub(r"\*(?=\s*(,|$|\)|\s))", "", t)   # the asterisk after a template name
    return re.sub(r"\s+", " ", t).strip()


def paragraphs(path):
    with zipfile.ZipFile(path) as z:
        root = ET.fromstring(z.read("word/document.xml"))
    for p in root.iter(W + "p"):
        st = p.find(f"{W}pPr/{W}pStyle")
        style = st.get(W + "val") if st is not None else ""
        numbered = p.find(f"{W}pPr/{W}numPr") is not None
        text = clean("".join(t.text or "" for t in p.iter(W + "t")))
        if text:
            yield style, numbered, text


def is_node(text):
    return text.startswith("[Node]")


def split_title(text):
    """'Title: rest' -> (Title, rest); no colon -> (text, '')."""
    m = re.match(r"^([^:]{2,90}):\s*(.*)$", text)
    return (m.group(1).strip(), m.group(2).strip()) if m else (text.strip(), "")


def tool(text):
    kind, name = split_title(text)
    if name and tool_kind(kind) in TOOL_TYPES:
        return {"type": kind.strip("[]"), "name": name}
    return {"type": "", "name": text}


# ---------------------------------------------------------------------------
# Library text: every paragraph in order, minus the parts listed above.
# ---------------------------------------------------------------------------
def library_text(fname, rows):
    rows = [r for r in rows if not is_node(r[2])]
    if fname == "Introduction.docx":
        kept, skipping, list_once = [], False, False
        for style, numbered, text in rows:
            if text.startswith("The Chapter 360 tool comprises"):
                kept.append((style, numbered, text)); skipping = True; continue
            if skipping:
                if text.startswith("The full view of Focus and Practice Areas"):
                    skipping = False
                    kept.append((style, numbered, text))
                elif text == "Practice Areas articulate:" and not list_once:
                    list_once = True
                    kept.append((style, numbered, text))
                    # its five items follow right after
                    i = rows.index((style, numbered, text))
                    for s2, n2, t2 in rows[i + 1:i + 6]:
                        kept.append(("ListParagraph", True, t2))
                continue
            kept.append((style, numbered, text))
        rows = kept

    sections = []
    cur = None
    title_line = True
    for style, numbered, text in rows:
        heading = None
        if title_line:
            # "OP. OPERATIONS" -> "Operations"
            heading = re.sub(r"^[A-Z]{2}\.\s*", "", text)
            heading = heading.title() if heading.isupper() else heading
            title_line = False
        elif re.match(r"^\d+\.\s+\S", text) and not re.match(r"^\d+\.\d", text) and style == "":
            heading = text
        elif style in ("ISACATitle",) or (fname == "Introduction.docx" and style == "" and len(text.split()) <= 5
                                          and not text.endswith(".")):
            heading = text
        if heading is not None:
            cur = {"heading": heading, "paragraphs": []}
            sections.append(cur)
            continue
        if cur is None:
            cur = {"heading": None, "paragraphs": []}
            sections.append(cur)
        cur["paragraphs"].append({"bullet": style == "ListParagraph", "text": text})
    return [s for s in sections if s["paragraphs"]]


# ---------------------------------------------------------------------------
# Focus Area structure, for the sample page.
# ---------------------------------------------------------------------------
def focus_area(code, rows):
    rows = [r for r in rows if not is_node(r[2])]
    area = {"code": code, "name": "", "intro": [], "why": [], "practiceNames": [], "practiceAreas": []}
    first = rows[0][2]
    area["name"] = re.sub(r"^[A-Z]{2}\.\s*", "", first).title().replace("&", "&")
    mode = "intro"
    pa = None
    pr = None
    anomalies = []
    for style, numbered, text in rows[1:]:
        low = text.lower().rstrip(":").strip()
        m_pa = re.match(r"^(\d+)\.\s+(.+)$", text)
        m_pr = re.match(r"^(\d+)\.(\d+)\s+(.+)$", text)
        if m_pr:
            title, rest = split_title(m_pr.group(3))
            pr = {"number": f"{m_pr.group(1)}.{m_pr.group(2)}", "title": title, "text": rest, "benefit": "", "tools": []}
            if pa is None:
                anomalies.append(f"practice before a practice area: {text[:60]}")
                continue
            pa["practices"].append(pr)
            mode = "practice"
            continue
        if m_pa and style == "":
            pa = {"number": int(m_pa.group(1)), "name": m_pa.group(2).strip(), "whatItIs": "", "why": [], "practices": []}
            area["practiceAreas"].append(pa)
            pr = None
            mode = "pa"
            continue
        if low in ("why it matters",):
            mode = "why" if pa is None else "pawhy"
            continue
        if low in ("practice areas",):
            mode = "names"
            continue
        if low in ("how to do it",):
            mode = "how"
            continue
        if low.startswith("tools & resources") or low.startswith("tools and resources"):
            mode = "tools"
            continue
        if text.lower().startswith("what it is:") and pa is not None:
            pa["whatItIs"] = split_title(text)[1]
            mode = "pa"
            continue
        if re.match(r"^benefits?:", text.lower()) and pr is not None:
            pr["benefit"] = split_title(text)[1]
            mode = "practice"
            continue

        if mode == "intro":
            area["intro"].append(text)
        elif mode == "why":
            t, r = split_title(text)
            area["why"].append({"title": t, "text": r} if r else {"title": "", "text": text})
        elif mode == "names":
            area["practiceNames"].append(text)
        elif mode == "pawhy":
            t, r = split_title(text)
            pa["why"].append({"title": t, "text": r} if r else {"title": "", "text": text})
        elif mode == "tools" and pr is not None:
            kind, rest = split_title(text)
            # A "Title: long description" line that isn't a resource is a practice
            # whose number is missing in the document; give it the next number.
            if rest and tool_kind(kind) not in TOOL_TYPES and len(rest.split()) > 5:
                last = int(pa["practices"][-1]["number"].split(".")[1]) if pa["practices"] else 0
                pr = {"number": f"{pa['number']}.{last + 1}", "title": kind, "text": rest, "benefit": "", "tools": []}
                pa["practices"].append(pr)
                mode = "practice"
                anomalies.append(f"numbered {pr['number']}: {kind}")
            else:
                pr["tools"].append(tool(text))
        elif mode in ("practice", "how") and pr is not None:
            pr["text"] = (pr["text"] + " " + text).strip()
        elif mode == "pa" and pa is not None:
            pa["whatItIs"] = (pa["whatItIs"] + " " + text).strip()
        else:
            anomalies.append(f"unplaced ({mode}): {text[:70]}")
    return area, anomalies


documents, areas = [], []
for fname, code, title in FILES:
    path = os.path.join(src, fname)
    if not os.path.exists(path):
        print(f"MISSING: {path}")
        sys.exit(1)
    rows = list(paragraphs(path))
    sections = library_text(fname, rows)
    words = sum(len(p["text"].split()) for s in sections for p in s["paragraphs"]) + \
        sum(len((s["heading"] or "").split()) for s in sections)
    documents.append({"id": "c360-" + (code or "intro").lower(), "title": title, "code": code,
                      "file": fname, "words": words, "sections": sections})
    if code:
        area, anomalies = focus_area(code, rows)
        areas.append(area)
        tools = sum(len(p["tools"]) for x in area["practiceAreas"] for p in x["practices"])
        practices = sum(len(x["practices"]) for x in area["practiceAreas"])
        print(f"{code}: {area['name']} - {len(area['practiceAreas'])} practice areas, {practices} practices, {tools} tools, {words} words")
        for a in anomalies:
            print("   note:", a)
    else:
        print(f"Introduction: {len(sections)} sections, {words} words")

with open(out, "w", encoding="utf-8") as f:
    json.dump({"_comment": "Built by onboarding/build_chapter360.py from content/Chapter 360. Do not edit by hand.",
               "documents": documents, "focusAreas": areas}, f, ensure_ascii=False, indent=1)
print(f"wrote {out}")
