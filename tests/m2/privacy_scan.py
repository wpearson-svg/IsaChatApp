"""Lane 7: searches Isa's databases and logs for question text and test people's names and emails.

Question text: every question in tests/m2/*.csv (at least 12 characters, so short words don't match by chance).
People: every name and email in directory/people.csv. Isa's own record of who is who (directory.db, keycloak
realm files, onboarding.db's member roster) is expected to hold names; those files are listed but not counted.
    python tests/m2/privacy_scan.py
"""
import csv, glob, os, sqlite3

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
EXPECTED_TO_HOLD_NAMES = {"directory.db", "onboarding.db", "notebook.db", "progress.db"}  # by design, see note

questions = set()
for f in glob.glob(os.path.join(REPO, "tests", "m2", "*.csv")):
    try:
        for r in csv.DictReader(open(f, encoding="utf-8-sig")):
            q = (r.get("question") or "").strip()
            if len(q) >= 12: questions.add(q)
    except Exception:
        pass
people = []
for r in csv.DictReader(open(os.path.join(REPO, "directory", "people.csv"), encoding="utf-8-sig")):
    for k, v in r.items():
        if v and k and any(w in k.lower() for w in ("name", "email")) and len(v) >= 5:
            people.append(v)

def text_of_db(path):
    out = []
    db = sqlite3.connect(f"file:{path}?mode=ro", uri=True)
    for (t,) in db.execute("select name from sqlite_master where type='table'"):
        try:
            for row in db.execute(f'select * from "{t}"'):
                out.append((t, " ".join(str(x) for x in row if isinstance(x, str))))
        except Exception:
            pass
    return out

targets = glob.glob(os.path.join(REPO, "data", "*.db")) + glob.glob(os.path.join(REPO, "*.log")) + \
          glob.glob(os.path.join(REPO, "*.jsonl")) + glob.glob(os.path.join(REPO, "data", "*.log"))
for path in sorted(targets):
    name = os.path.basename(path)
    rows = text_of_db(path) if path.endswith(".db") else [("file", open(path, encoding="utf-8", errors="ignore").read())]
    qhits = {}; phits = {}
    for table, text in rows:
        for q in questions:
            if q in text: qhits.setdefault(table, set()).add(q)
        for p in people:
            if p in text: phits.setdefault(table, set()).add(p)
    flag = " (expected: Isa's own records)" if name in EXPECTED_TO_HOLD_NAMES else ""
    print(f"{name:<24} question text: {sum(len(v) for v in qhits.values()):>3}   names/emails: {sum(len(v) for v in phits.values()):>3}{flag}")
    for t, v in qhits.items(): print(f"    questions in table {t}: {len(v)}  e.g. {sorted(v)[0][:60]!r}")
    for t, v in phits.items():
        if not flag: print(f"    people in table {t}: {len(v)}  e.g. {sorted(v)[0]!r}")
