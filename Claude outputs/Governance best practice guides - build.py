"""Builds the governance guides copy (HTML) for ISACA from content/Governance Best Practice/*.md.

Run from the repository root: python "Claude outputs/Governance best practice guides - build.py" . "Claude outputs/Governance best practice guides and how Isa uses them.html"
then print the HTML to PDF with Edge (msedge --headless --print-to-pdf).
"""
import html, os, re, sys

repo, out_html = sys.argv[1], sys.argv[2]
src = os.path.join(repo, "content", "Governance Best Practice")

ORDER = [
    "Orienting New Board Members",
    "Board and Officer Roles",
    "Running Effective Board Meetings",
    "Board Meeting Minutes",
    "Committees and Task Forces",
    "Conflicts of Interest",
    "Leadership Succession and Handover",
    "Recruiting and Engaging Volunteers",
    "Reviewing Your Chapter Governance",
]

def inline(text):
    t = html.escape(text)
    return re.sub(r"(https?://[^\s]+?)(?=\s|$)", r'<a href="\1">\1</a>', t)

guides = []
for name in ORDER:
    raw = open(os.path.join(src, name + ".md"), encoding="utf-8").read().strip()
    words = len(raw.split())
    body = []
    for block in re.split(r"\n\s*\n", raw):
        block = block.strip()
        if block.startswith("# "):
            continue  # title rendered separately
        if block.startswith("## "):
            body.append(f"<h3>{html.escape(block[3:])}</h3>")
        else:
            cls = ' class="note"' if not body else (' class="reading"' if body[-1] == "<h3>Further reading</h3>" else "")
            body.append(f"<p{cls}>{inline(block)}</p>")
    guides.append((name, words, "\n".join(body)))

total = sum(w for _, w, _ in guides)
toc = "\n".join(
    f'<li><span>{i}. {html.escape(n)}</span><span class="w">{w} words</span></li>'
    for i, (n, w, _) in enumerate(guides, 1))
sections = "\n".join(
    f'<section class="guide"><div class="kicker">Guide {i} of {len(guides)} · general best practice, not ISACA policy</div>'
    f"<h2>{html.escape(n)}</h2>\n{b}</section>"
    for i, (n, _, b) in enumerate(guides, 1))

page = f"""<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<title>Governance best practice guides and how Isa uses them</title>
<style>
@page {{ size: Letter; margin: 0.5in 0.7in; }}
:root {{ --ink:#1f2933; --muted:#52606d; --line:#d9e2ec; --accent:#1b7a66; --soft:#eef6f3; }}
body {{ font-family: "Segoe UI", Arial, sans-serif; color: var(--ink); background:#fff; font-size: 9.4pt; line-height: 1.38; margin:0; }}
h1 {{ font-size: 21pt; line-height:1.2; margin: 0 0 4pt; }}
h2 {{ font-size: 16pt; margin: 2pt 0 8pt; color: var(--ink); }}
h3 {{ font-size: 11pt; margin: 8pt 0 2pt; color: var(--accent); break-after: avoid; }}
p {{ margin: 0 0 6pt; }}
p.reading {{ font-size: 8.5pt; color: var(--muted); }}
a {{ color: var(--accent); word-break: break-all; }}
.byline {{ color: var(--muted); margin-bottom: 12pt; }}
.box {{ background: var(--soft); border-left: 4px solid var(--accent); padding: 9pt 12pt; margin: 0 0 12pt; break-inside: avoid; }}
.box h3 {{ margin-top: 0; }}
.box p:last-child, .box ul:last-child {{ margin-bottom: 0; }}
ul {{ margin: 0 0 7pt; padding-left: 16pt; }}
li {{ margin-bottom: 3pt; }}
.label {{ font-weight: 600; }}
.q {{ font-style: italic; }}
ol.toc {{ list-style: none; padding: 0; margin: 0; columns: 2; column-gap: 18pt; }}
ol.toc li {{ display: flex; justify-content: space-between; border-bottom: 1px solid var(--line); padding: 1.5pt 0; margin: 0; break-inside: avoid; }}
ol.toc .w {{ color: var(--muted); }}
.guide {{ break-before: page; }}
.kicker {{ text-transform: uppercase; letter-spacing: .06em; font-size: 8pt; color: var(--muted); }}
p.note {{ color: var(--muted); font-style: italic; border-bottom: 1px solid var(--line); padding-bottom: 8pt; margin-bottom: 4pt; }}
.cover {{ break-after: page; }}
</style></head>
<body>
<div class="cover">
<h1>Governance best practice guides<br>and how Isa uses them</h1>
<div class="byline">Second Star Collective · Isa team · November 4, 2026</div>

<div class="box">
<h3>Why we're sharing these</h3>
<p>We're sharing these as a courtesy, so you can see how the prototype puts your October 21 guidance into practice. Isa always answers from ISACA's own documents first, and turns to these guides only for broader governance questions those documents don't cover.</p>
<p>In a real deployment, everything Isa answers from would be ISACA-approved. If anything here doesn't match how you'd want this handled, we'd be glad to hear it.</p>
</div>

<h3>What these are</h3>
<p>Nine short guides on general board governance for volunteer chapter boards, about {round(total, -2):,} words in all. On October 21 you told us that for broader governance questions it is fine to draw on established best practice from ASAE, BoardSource, BoardEffect and Association Forum. Their websites don't allow their pages to be copied, so we wrote these guides in our own words from 20 of their free public pages. Each guide lists the pages it draws on under "Further reading".</p>

<h3>When Isa uses them</h3>
<ul>
<li>ISACA's documents are always searched first. If they answer the question, the guides are not used.</li>
<li>Next, Isa checks a short list of common questions that are matched to passages in ISACA's documents. A question on that list never goes to the guides.</li>
<li>Only when neither finds an answer does Isa look in the guides.</li>
</ul>

<h3>How Isa labels them</h3>
<ul>
<li>An answer from a guide begins: <span class="q">"Here's what general governance best practice suggests (not ISACA policy):"</span></li>
<li>Isa's step-by-step view of how she answered says: <span class="q">"Not in ISACA's guidance; found general best practice."</span></li>
<li>The source shown with the answer is the guide, and every guide opens by saying the Isa team wrote it, that it is not ISACA policy, and that ISACA's documents and the chapter's bylaws come first.</li>
</ul>

<h3>Example questions a guide answers</h3>
<ul>
<li class="q">What should we cover when we orient new board members?</li>
<li class="q">What should we include in board meeting minutes?</li>
<li class="q">What do we do if a board member has a conflict of interest?</li>
</ul>

<h3>The guides</h3>
<ol class="toc">
{toc}
</ol>
</div>
{sections}
</body></html>
"""
open(out_html, "w", encoding="utf-8").write(page)
print("written", out_html, total, "words")
