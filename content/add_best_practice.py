#!/usr/bin/env python3
"""Add the governance best-practice guides to content/content.json. Safe to re-run.

The guides in content/Governance Best Practice/*.md are general governance best
practice, written by the Isa team in its own words (Matthew, 22 October 2026)
from public pages by BoardSource, BoardEffect, ASAE and Association Forum,
the outside sources Camille accepted for broader governance questions. Nothing
from those pages is copied; each guide links to them under "Further reading".
They are NOT ISACA policy, and every document title starts with
"Best Practice Guide:" so Isa can label answers from them.

Each "## Heading" in a guide becomes one passage (section = the heading,
page = "n/a"). The paragraph under the "# Title" is a note for people
reading the file and is not loaded, and neither is "Further reading": its
lines are only links, and as passages they matched searches and turned up
as answers.

Re-running first removes any earlier copy of the guides, so editing a guide
and running this again keeps content.json in step. If content.json is ever
regenerated from the ISACA documents, run this again afterwards. Then
rebuild the database:

    python content\\add_best_practice.py
    python content\\build_content_db.py
"""
import glob, json, os, re

FOLDER = "Governance Best Practice"
here = os.path.dirname(os.path.abspath(__file__))
src = os.path.join(here, "content.json")

with open(src, encoding="utf-8") as f:
    content = json.load(f)

# Drop any earlier copy of the guides.
content["documents"] = [d for d in content["documents"] if d.get("folder") != FOLDER]
kept_titles = {d["document"] for d in content["documents"]}
content["passages"] = [p for p in content["passages"] if p["document"] in kept_titles]
next_id = max((p["id"] for p in content["passages"]), default=0) + 1

added_docs = added_passages = 0
for path in sorted(glob.glob(os.path.join(here, FOLDER, "*.md"))):
    with open(path, encoding="utf-8") as f:
        text = f.read()
    title = re.search(r"^# (.+)$", text, re.M).group(1).strip()
    content["documents"].append({
        "document": title,
        "source": FOLDER + "/" + os.path.basename(path),
        "folder": FOLDER,
    })
    added_docs += 1
    # Split on "## " headings; the first chunk (title and note) is skipped.
    for chunk in re.split(r"^## ", text, flags=re.M)[1:]:
        heading, _, body = chunk.partition("\n")
        body = " ".join(line.strip() for line in body.strip().splitlines() if line.strip())
        if not body or heading.strip().lower() == "further reading":
            continue
        content["passages"].append({
            "document": title,
            "section": heading.strip(),
            "page": "n/a",
            "text": body,
            "id": next_id,
        })
        next_id += 1
        added_passages += 1

# Same layout as the existing file (indent 1, no newline at the end), so
# the change shows up in Git as the guides only.
with open(src, "w", encoding="utf-8", newline="") as f:
    json.dump(content, f, ensure_ascii=False, indent=1)

print(f"added {added_docs} best-practice guides, {added_passages} passages, to {src}")
