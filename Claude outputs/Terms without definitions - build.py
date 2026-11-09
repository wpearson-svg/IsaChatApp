"""Builds the ISACA copy of docs/Glossary_Terms_To_Request.md (HTML, then printed to PDF with Edge).

Keeps only the term tables; the internal notes at the top and the suggested email wording are left out.
Run from the repository root: python "Claude outputs/Terms without definitions - build.py"
"""
import html, re

SRC = "docs/Glossary_Terms_To_Request.md"
OUT = "Claude outputs/Terms Isa's documents use without defining.html"

md = open(SRC, encoding="utf-8").read()
md = md.split("## Suggested wording")[0]
parts = re.split(r"^## ", md, flags=re.M)[1:]  # each starts with its section title

def cell(text):
    # The source note on quorum is written to the team; word it as a question to ISACA.
    text = text.replace("Ask ISACA whether to add a general definition, or to point leaders to their own bylaws.",
                        "Should Isa give a general definition, or point leaders to their own chapter's bylaws?")
    t = html.escape(text.strip())
    return re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", t)

sections, count = [], 0
for part in parts:
    title, _, body = part.partition("\n")
    rows = [l for l in body.splitlines() if l.startswith("|")]
    head = [cell(c) for c in rows[0].strip("|").split("|")]
    trs = []
    for l in rows[2:]:
        cells = [cell(c) for c in l.strip("|").split("|")]
        trs.append("<tr>" + "".join(f"<td>{c}</td>" for c in cells) + "</tr>")
        if len(cells) == 3:  # the quorum table has two columns: a term the documents don't use
            count += 1
    ths = "".join(f"<th>{h}</th>" for h in head)
    sections.append(f"<h2>{html.escape(title.strip())}</h2>\n<table><thead><tr>{ths}</tr></thead><tbody>{''.join(trs)}</tbody></table>")

page = f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<title>Terms Isa's documents use without defining</title>
<style>
@page {{ size: Letter; margin: 0.6in 0.7in; }}
:root {{ --ink:#1f2933; --muted:#52606d; --line:#d9e2ec; --accent:#1b7a66; --soft:#eef6f3; }}
body {{ font-family: "Segoe UI", Arial, sans-serif; color: var(--ink); background:#fff; font-size: 9.6pt; line-height: 1.4; margin:0; }}
h1 {{ font-size: 20pt; line-height:1.2; margin: 0 0 4pt; }}
h2 {{ font-size: 12.5pt; color: var(--accent); margin: 14pt 0 5pt; break-after: avoid; }}
p {{ margin: 0 0 6pt; }}
ul {{ margin: 0 0 6pt; padding-left: 16pt; }}
li {{ margin-bottom: 2pt; }}
.byline {{ color: var(--muted); margin-bottom: 12pt; }}
.box {{ background: var(--soft); border-left: 4px solid var(--accent); padding: 9pt 12pt; margin: 0 0 8pt; }}
.box p:last-child, .box ul:last-child {{ margin-bottom: 0; }}
table {{ width: 100%; border-collapse: collapse; }}
th {{ text-align: left; font-size: 8.5pt; text-transform: uppercase; letter-spacing: .04em; color: var(--muted); border-bottom: 2px solid var(--line); padding: 3pt 6pt; }}
td {{ vertical-align: top; border-bottom: 1px solid var(--line); padding: 5pt 6pt; }}
td:first-child {{ font-weight: 600; width: 24%; }}
td:nth-child(2) {{ width: 30%; color: var(--muted); }}
tr {{ break-inside: avoid; }}
</style></head>
<body>
<h1>Terms Isa's documents use without defining</h1>
<div class="byline">Second Star Collective · Isa team · November 4, 2026</div>
<div class="box">
<p>Isa explains terms only in ISACA's own words. While checking her documents, we found {count} terms that the documents use but don't define, so she can't explain them yet, and one term chapter leaders need that the documents don't use (quorum). Would ISACA be willing to provide or approve a short definition for any of them?</p>
<ul>
<li>Each row shows where the term appears and why a definition would help a chapter leader.</li>
<li>If there are other terms new leaders often ask you about, we'd be glad to add them.</li>
<li>Beyond the terms ISACA's documents already use, we left out tax and legal terms on purpose: their meaning changes from country to country, and Isa passes those questions to a person.</li>
</ul>
</div>
<p><strong>Where.</strong> "Guidebook" is the Chapter Leader Guidebook: President (August 2026). "Agreement" is the Chapter Affiliation Agreement (2024 template). Other names are the documents' own titles.</p>
{chr(10).join(sections)}
</body></html>
"""
open(OUT, "w", encoding="utf-8").write(page)
print("written", OUT, count, "terms")
