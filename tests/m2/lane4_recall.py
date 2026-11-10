"""Lane 4 recall test: one question per passage in data/content.db; does search by meaning find that passage?

Reproduces Isa's VectorSearchService exactly (Services/Search/VectorSearchService.cs): the question is embedded
by the embeddings service with mode=query, scored by dot product against the normalised vectors in
data/vectors.db, the top 3 kept, then those under the search pass mark (0.80) dropped, then the RAG stage drops
"Best Practice Guide:" passages. No model call, so it can run while the model is busy.
Writes tests/m2/lane4_recall.csv (the questions) and tests/m2/results/lane4_recall.csv (rank, score, found).
    python tests/m2/lane4_recall.py
"""
import array, csv, json, math, os, sqlite3, urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
TOPK, MARK = 3, 0.80

Q = {
1:"What does the Annual Chapter Compliance Framework explain?",
2:"What compliance activities happen throughout the year?",
3:"What do chapters do in the annual review from 1 November to 31 December?",
4:"What evidence shows we report activities, revenues and expenditures to members?",
5:"What financial report do we submit if this year's audit isn't finished by the compliance deadline?",
6:"Do we need a privacy policy and insurance for compliance?",
7:"What is the Common Tasks and Navigation Guide for?",
8:"How do I update our chapter leader roster when new officers are elected?",
9:"Does ISACA host chapter websites on Engage?",
10:"What is Event Manager on Engage?",
11:"What is cross-site configuration between ISACA Global, Engage and our chapter website?",
12:"Can we create member-only pages on our chapter website?",
13:"How many governance, financial and operational requirements do chapters confirm each year?",
14:"How do I gather and submit compliance documentation?",
15:"What happens in Q3 and Q4 of the compliance calendar?",
16:"How many educational events must a chapter hold and where are they posted?",
17:"What are the steps to upload CPE information?",
18:"Where can I find more information about chapter leader roles?",
19:"What does the Core Chapter Leader Roles document provide?",
20:"What are the chapter president's primary responsibilities?",
21:"What are the chapter president's common activities?",
22:"What are the vice president's primary responsibilities?",
23:"What are the vice president's common activities?",
24:"What are the chapter secretary's primary responsibilities?",
25:"What does the secretary do day to day?",
26:"What are the treasurer's primary responsibilities?",
27:"What are the treasurer's common activities?",
28:"What is the ISACA Chapter Systems Overview document for?",
29:"What is the Chapter Leader Portal?",
30:"What do chapter leaders use the Chapter Leader Portal for?",
31:"What is C360 (Chapter 360)?",
32:"What is Chapter 360 used for?",
33:"What is Engage?",
34:"What can chapter leaders do on Engage?",
35:"What reporting tools do chapters use?",
36:"What is chapter compliance reporting used for?",
37:"What is event reporting used for?",
38:"What does the CPE upload tool do?",
39:"How are board and leader rosters kept up to date?",
40:"Which system is used for which chapter function?",
41:"How do the Chapter Leader Portal, C360 and Engage relate to each other?",
42:"What is the chapter president's role as chief elected officer?",
43:"How does the president implement the board's policies and decisions?",
44:"What should the president do if the chapter struggles to meet standards?",
45:"What is the Regional Strategic Advisory Council?",
46:"Is the chapter an independent legal entity responsible for its own affairs?",
47:"What is the president's role in the strategic plan?",
48:"Who oversees nominations and elections and identifies risks?",
49:"Who is responsible for governance, the president or the whole board?",
50:"How should the president inspire volunteers and promote teamwork?",
51:"Who oversees reviews of the bylaws and chapter programs?",
52:"What is the president's role in succession planning and as spokesperson?",
53:"What is the president's role in chapter finances with the treasurer?",
54:"Should chapters require dual authorization for large disbursements?",
55:"What should the president do about bank reconciliations and unusual transactions?",
56:"Why does timely financial reporting matter?",
57:"Who is the chapter's main liaison with ISACA Global?",
58:"What must be submitted to ISACA within required timeframes after the fiscal year?",
59:"How should the president keep members informed about chapter activities?",
60:"What is the president's role in board and membership meetings?",
61:"What should the president review with the treasurer before a board meeting?",
62:"How should decisions and action items from meetings be documented?",
63:"What is the president responsible for in conducting board meetings?",
64:"Who sets the date and format of future board meetings and follows up on actions?",
65:"Must the chapter hold membership meetings, and who presides?",
66:"How should motions be handled at a membership meeting?",
67:"How do membership meetings differ from board meetings?",
68:"Who notifies ISACA of newly elected leaders after the AGM?",
69:"What else should the outgoing president do to ensure continuity?",
70:"How can we display chapter leadership information on our hosted website?",
71:"Can we publish a static board image on our chapter website?",
72:"Why don't leader profiles show on our chapter website?",
73:"How should the president support volunteer development and mentoring?",
74:"Should chapters have a succession plan?",
75:"Can we create a President-Elect position?",
76:"What records and credentials should be transferred at turnover?",
77:"What open items like invoices and contracts should be handed over?",
78:"What happens to chapter property and bank accounts when leaders change?",
79:"What correspondence goes to the incoming president, and should we recognise outgoing volunteers?",
80:"What should be in the orientation for incoming chapter leaders?",
81:"When should we begin recruiting volunteers for the following year?",
82:"Why do board meeting minutes matter?",
83:"What should board meeting minutes include?",
84:"What should be left out of minutes?",
85:"How are minutes approved, signed and shared?",
86:"How should we keep approved minutes safe?",
87:"What are common mistakes with minutes?",
88:"What does the board as a whole do?",
89:"What is every board member expected to do?",
90:"What are the typical duties of the board chair?",
91:"What does the vice chair do?",
92:"What are the secretary's typical duties on the board?",
93:"What are the treasurer's typical duties on the board?",
94:"Should we have written job descriptions for board roles, and can one person hold two roles?",
95:"What is the difference between a standing committee, an ad hoc committee and a task force?",
96:"How many committees does a chapter need?",
97:"How should we choose committee members?",
98:"What is a committee chair's job?",
99:"How do we check how committees are doing?",
100:"What is a conflict of interest?",
101:"What should a conflict of interest policy include?",
102:"What should a board member do when they have a conflict in a meeting?",
103:"How can we prevent conflicts of interest?",
104:"Why plan for leadership succession?",
105:"How do we build a pipeline of future leaders?",
106:"How should the outgoing president hand over to the next president?",
107:"What role do past presidents play?",
108:"What should a new leader do in their first months?",
109:"Why does orientation matter for new board members?",
110:"What should a new board member orientation cover?",
111:"What goes in a board handbook?",
112:"Who should lead new board member orientation?",
113:"How should an orientation session be run?",
114:"What happens after the orientation?",
115:"How do we find new volunteers and board members?",
116:"How do we set clear expectations for board members?",
117:"How do we keep volunteers engaged?",
118:"How should we recognise and thank volunteers?",
119:"How can the board improve how it works?",
120:"How should we welcome new chapter members?",
121:"When should we review how the board is set up?",
122:"Why agree on the problem before the solution in a governance review?",
123:"What are the steps for a governance review?",
124:"What pitfalls should we avoid in a governance review?",
125:"What is the board's role in planning?",
126:"How do we plan the board meeting agenda?",
127:"How early should we send board materials before a meeting?",
128:"What is a consent agenda?",
129:"How should the chair lead a board meeting?",
130:"What should happen after a board meeting?",
131:"What are common board meeting mistakes?",
}

def embed(text):
    body = json.dumps({"texts": [text], "mode": "query"}).encode()
    r = json.load(urllib.request.urlopen(urllib.request.Request("http://localhost:8092/embed", data=body,
                 headers={"Content-Type": "application/json"}), timeout=60))
    v = r["vectors"][0]; n = math.sqrt(sum(x * x for x in v)) or 1.0
    return [x / n for x in v]

vdb = sqlite3.connect(f"file:{os.path.join(REPO, 'data', 'vectors.db')}?mode=ro", uri=True)
tables = [t for (t,) in vdb.execute("select name from sqlite_master where type='table'")]
cols = {t: [c[1] for c in vdb.execute(f'pragma table_info("{t}")')] for t in tables}
vt = next(t for t in tables if "vector" in " ".join(cols[t]).lower() or "embedding" in " ".join(cols[t]).lower())
idc = next(c for c in cols[vt] if c.lower() in ("id", "passage_id"))
vc = next(c for c in cols[vt] if "vector" in c.lower() or "embedding" in c.lower())
index = []
for pid, blob in vdb.execute(f'select "{idc}", "{vc}" from "{vt}"'):
    a = array.array("f"); a.frombytes(blob)
    n = math.sqrt(sum(x * x for x in a)) or 1.0
    index.append((pid, [x / n for x in a]))
cdb = sqlite3.connect(f"file:{os.path.join(REPO, 'data', 'content.db')}?mode=ro", uri=True)
doc = {i: d for i, d in cdb.execute("select id, document from passages")}

with open(os.path.join(HERE, "lane4_recall.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f); w.writerow(["id", "lane", "language", "question", "expected_path"])
    for pid, q in Q.items(): w.writerow([f"L4-{pid:03d}", "4", "en", q, f"passage:{pid}"])

os.makedirs(os.path.join(HERE, "results"), exist_ok=True)
rows = []
for pid, q in Q.items():
    qv = embed(q)
    scored = sorted(((sum(a * b for a, b in zip(qv, v)), i) for i, v in index), reverse=True)
    rank = next((n + 1 for n, (_, i) in enumerate(scored) if i == pid), None)
    score = next(s for s, i in scored if i == pid)
    top = [(s, i) for s, i in scored[:TOPK] if s >= MARK]
    is_guide = doc[pid].startswith("Best Practice Guide:")
    kept = [i for s, i in top if not doc[i].startswith("Best Practice Guide:")]
    found = pid in kept
    rows.append({"id": f"L4-{pid:03d}", "passage": pid, "document": doc[pid], "guide": "yes" if is_guide else "no",
                 "rank": rank, "score": f"{score:.3f}", "top1": scored[0][1], "top1_score": f"{scored[0][0]:.3f}",
                 "found_by_vector": "yes" if found else "no",
                 "guide_slots_in_top3": sum(1 for s, i in top if doc[i].startswith("Best Practice Guide:"))})
with open(os.path.join(HERE, "results", "lane4_recall.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.DictWriter(f, fieldnames=list(rows[0])); w.writeheader(); w.writerows(rows)

from collections import defaultdict
per = defaultdict(lambda: [0, 0, 0])
for r in rows:
    p = per[r["document"]]; p[0] += 1; p[1] += r["rank"] <= TOPK; p[2] += r["found_by_vector"] == "yes"
print(f"{'document':<55} n  top3  found(ISACA, >=0.80)")
for d, (n, t3, fnd) in sorted(per.items()):
    print(f"{d[:55]:<55} {n:>2} {t3:>4}  {fnd if not d.startswith('Best Practice Guide:') else '-':>4}")
isaca = [r for r in rows if r["guide"] == "no"]
print(f"\nISACA passages: {len(isaca)}; in top 3: {sum(r['rank'] <= TOPK for r in isaca)}; "
      f"found (top 3 and >= {MARK}): {sum(r['found_by_vector'] == 'yes' for r in isaca)}")
print("ISACA questions where a guide passage took a top-3 slot:", sum(1 for r in isaca if r["guide_slots_in_top3"]))
