#!/usr/bin/env python3
"""Rebuild data/onboarding.db from the CSV files in this folder (the master source).

Onboarding feature (see ONBOARDING-FEATURE.md). Same pattern as
directory/build_directory_db.py: the CSV is the master copy, this script
checks it and builds the database from empty every run.

This database holds only CONTENT that comes from the CSV files: the
journey steps (journey_steps.csv) and the invented sample Chapter
Experience contacts (contacts.csv). Things leaders do in Isa, such as marking a step
complete, will be stored in a separate database that this script never
touches, so rebuilding never erases anyone's progress.

Usage (from the IsaChatApp folder): python onboarding/build_onboarding_db.py [output.db]"""
import csv, os, re, sqlite3, sys

here = os.path.dirname(os.path.abspath(__file__))
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "data", "onboarding.db")

with open(os.path.join(here, "journey_steps.csv"), encoding="utf-8-sig", newline="") as f:
    steps = list(csv.DictReader(f))

# Invented sample contacts only -- never real ISACA staff.
with open(os.path.join(here, "contacts.csv"), encoding="utf-8-sig", newline="") as f:
    contacts = list(csv.DictReader(f))

# ---- Check the data before building -------------------------------------
problems = []
ids = [s["Step ID"] for s in steps]
if len(set(ids)) != len(ids):
    problems.append("duplicate Step ID")
for s in steps:
    sid = s["Step ID"]
    if not re.fullmatch(r"J-\d{2}", sid):
        problems.append(f"bad Step ID {sid!r} (use J- and two digits, like J-07)")
    for field in ("Role", "Title", "What to do", "Source"):
        if not s[field].strip():
            problems.append(f"{sid}: {field} is empty")
    try:
        day_from, day_to = int(s["Day from"]), int(s["Day to"])
        if not (1 <= day_from <= 90 and 1 <= day_to <= 90):
            problems.append(f"{sid}: days must be between 1 and 90")
        if day_to < day_from:
            problems.append(f"{sid}: Day to is before Day from")
    except ValueError:
        problems.append(f"{sid}: Day from and Day to must be whole numbers")
contact_ids = [c["Contact ID"] for c in contacts]
if len(set(contact_ids)) != len(contact_ids):
    problems.append("contacts.csv: duplicate Contact ID")
seen_chapters = {}
for c in contacts:
    cid = c["Contact ID"]
    for field in ("Name", "Team", "Region label", "Time zone", "Email", "Chapter IDs"):
        if not c[field].strip():
            problems.append(f"contacts.csv {cid}: {field} is empty")
    if not c["Email"].strip().endswith("@example.org"):
        problems.append(f"contacts.csv {cid}: sample contacts must use an example.org email")
    for ch in [x.strip() for x in c["Chapter IDs"].split(";") if x.strip()]:
        if ch in seen_chapters:
            problems.append(f"contacts.csv: chapter {ch} has two contacts ({seen_chapters[ch]} and {cid})")
        seen_chapters[ch] = cid

if problems:
    print("DATA PROBLEMS:")
    for p in problems:
        print(" -", p)
    sys.exit(1)

# ---- Build from empty ----------------------------------------------------
os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
for ext in ("", "-wal", "-shm"):
    if os.path.exists(out + ext):
        os.remove(out + ext)
db = sqlite3.connect(out)
db.executescript("""
CREATE TABLE journey_steps(
  step_id  TEXT PRIMARY KEY,
  role     TEXT NOT NULL,     -- which role sees this step, e.g. President
  day_from INTEGER NOT NULL,  -- first day of term the step is shown (day 1 = term start)
  day_to   INTEGER NOT NULL,  -- last day of term the step is shown
  title    TEXT NOT NULL,
  details  TEXT NOT NULL,     -- the "What to do" text
  source   TEXT NOT NULL      -- where the step comes from, e.g. a guidebook page
);
CREATE TABLE contacts(
  contact_id   TEXT PRIMARY KEY,
  name         TEXT NOT NULL,   -- invented sample person, never real staff
  team         TEXT NOT NULL,   -- e.g. Chapter Experience
  region_label TEXT NOT NULL,   -- e.g. Asia-Pacific chapters
  time_zone    TEXT NOT NULL,   -- e.g. Asia/Singapore
  email        TEXT NOT NULL
);
CREATE TABLE contact_chapters(
  chapter_id TEXT PRIMARY KEY,  -- each chapter has one contact
  contact_id TEXT NOT NULL REFERENCES contacts(contact_id)
);
""")
db.executemany("INSERT INTO journey_steps VALUES(?,?,?,?,?,?,?)", [
    (s["Step ID"], s["Role"].strip(), int(s["Day from"]), int(s["Day to"]),
     s["Title"].strip(), s["What to do"].strip(), s["Source"].strip()) for s in steps])
db.executemany("INSERT INTO contacts VALUES(?,?,?,?,?,?)", [
    (c["Contact ID"], c["Name"].strip(), c["Team"].strip(), c["Region label"].strip(),
     c["Time zone"].strip(), c["Email"].strip()) for c in contacts])
db.executemany("INSERT INTO contact_chapters VALUES(?,?)", list(seen_chapters.items()))
db.commit()
db.close()
print(f"built {out}: {len(steps)} journey steps, {len(contacts)} sample contacts")
