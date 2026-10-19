#!/usr/bin/env python3
"""Rebuild data/content.db from content/content.json + content/meaning_map.json. Safe to re-run (starts from empty)."""
import json, re, sqlite3, os, sys

# Table-of-contents text is not an answer to anything, but its headings
# match many searches ("Finances .......... 3"), so it pushed real answers
# out of the top 5 and showed up in the chat as rows of dots. Skip any
# passage that is a table of contents: it says "Table of Contents", or it
# has two or more lines ending in dot leaders and a page number.
DOT_LEADER_LINE = re.compile(r"\.{5,}\s*\d+\s*$", re.M)
def is_table_of_contents(text):
    return "table of contents" in text.lower() or len(DOT_LEADER_LINE.findall(text)) >= 2
here=os.path.dirname(os.path.abspath(__file__))
src=os.path.join(here,"content.json"); mm=os.path.join(here,"meaning_map.json")
out=sys.argv[1] if len(sys.argv)>1 else os.path.join(here,"..","data","content.db")
os.makedirs(os.path.dirname(out),exist_ok=True)
for ext in ("","-wal","-shm"):
    if os.path.exists(out+ext): os.remove(out+ext)
c=json.load(open(src,encoding="utf-8")); db=sqlite3.connect(out)
db.executescript("""
CREATE TABLE documents(id INTEGER PRIMARY KEY, title TEXT, source TEXT, folder TEXT);
CREATE TABLE sections(id INTEGER PRIMARY KEY, document_id INT, heading TEXT);
CREATE VIRTUAL TABLE passages USING fts5(id UNINDEXED, document, section, page, text, tokenize='porter unicode61');
CREATE TABLE meaning_map(question TEXT, passage_id INT);
""")
did={}
for i,d in enumerate(c["documents"],1):
    db.execute("INSERT INTO documents VALUES(?,?,?,?)",(i,d["document"],d["source"],d["folder"])); did[d["document"]]=i
secs={}
kept=[p for p in c["passages"] if not is_table_of_contents(p["text"])]
skipped=len(c["passages"])-len(kept)
for p in kept:
    k=(p["document"],p["section"])
    if k not in secs:
        secs[k]=len(secs)+1; db.execute("INSERT INTO sections VALUES(?,?,?)",(secs[k],did[p["document"]],p["section"]))
    db.execute("INSERT INTO passages(id,document,section,page,text) VALUES(?,?,?,?,?)",(p["id"],p["document"],p["section"],p["page"],p["text"]))
for m in json.load(open(mm,encoding="utf-8")):
    r=db.execute("SELECT id FROM passages WHERE document LIKE ? AND text LIKE ? LIMIT 1",(m["document"]+"%","%"+m["contains"]+"%")).fetchone()
    if not r: sys.exit("meaning_map: no passage for "+m["question"])
    db.execute("INSERT INTO meaning_map VALUES(?,?)",(m["question"],r[0]))
db.commit(); db.close(); print("built",out,len(kept),"passages (skipped",skipped,"table-of-contents passages)")
