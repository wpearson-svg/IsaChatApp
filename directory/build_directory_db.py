#!/usr/bin/env python3
"""Rebuild data/directory.db from the CSV files in this folder (the master source).
Starts from empty every run. Checks the data and exits non-zero if anything is wrong.
Usage: python directory/build_directory_db.py [output.db]"""
import csv, os, re, sqlite3, sys
from datetime import date

here = os.path.dirname(os.path.abspath(__file__))
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(here, "..", "data", "directory.db")

def rows(name):
    with open(os.path.join(here, name), encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))

chapters, people, roles, accounts = (rows(n) for n in
    ("chapters.csv", "people.csv", "role_assignments.csv", "keycloak_accounts.csv"))

problems = []
chapter_ids = {c["Chapter ID"] for c in chapters}
person_ids = [p["Person ID"] for p in people]
if len(set(person_ids)) != len(person_ids): problems.append("duplicate Person ID")
emails = [p["Email"].lower() for p in people]
if len(set(emails)) != len(emails): problems.append("duplicate email")
for p in people:
    if not re.fullmatch(r"P-\d{4}", p["Person ID"]): problems.append(f"bad Person ID {p['Person ID']!r}")
    if not re.fullmatch(r"[^@\s]+@example\.org", p["Email"]): problems.append(f"{p['Person ID']}: email not a clean example.org address")
    if not p["Years as leader"].isdigit(): problems.append(f"{p['Person ID']}: Years as leader not a number")
    if not re.fullmatch(r"[a-z]{2,3}-[A-Z]{2}", p["Language"]): problems.append(f"{p['Person ID']}: odd language tag {p['Language']!r}")
    if p["Time zone"] != p["Time zone"].strip() or " " in p["Time zone"]: problems.append(f"{p['Person ID']}: time zone has spaces")
for r in roles:
    if r["Person ID"] not in person_ids: problems.append(f"{r['Assignment ID']}: unknown person {r['Person ID']!r}")
    if r["Chapter ID"] not in chapter_ids: problems.append(f"{r['Assignment ID']}: unknown chapter {r['Chapter ID']!r}")
    try:
        if date.fromisoformat(r["Term end"]) < date.fromisoformat(r["Term start"]): problems.append(f"{r['Assignment ID']}: term ends before it starts")
    except ValueError:
        problems.append(f"{r['Assignment ID']}: bad date")
names = [a["username"] for a in accounts]
if len(set(names)) != len(names): problems.append("duplicate Keycloak username")
for a in accounts:
    if a["person_id"] and a["person_id"] not in person_ids: problems.append(f"account {a['username']}: unknown person {a['person_id']}")
    if not re.fullmatch(r"[a-z0-9._-]+", a["username"]): problems.append(f"account {a['username']}: username must be lowercase Latin")
if problems:
    print("DATA PROBLEMS:"); [print(" -", x) for x in problems]; sys.exit(1)

os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
for ext in ("", "-wal", "-shm"):
    if os.path.exists(out + ext): os.remove(out + ext)
db = sqlite3.connect(out)
db.executescript("""
CREATE TABLE chapters(chapter_id TEXT PRIMARY KEY, chapter_name TEXT NOT NULL);
CREATE TABLE people(
  person_id TEXT PRIMARY KEY, full_name TEXT, latin_name TEXT, short_name TEXT, form_of_address TEXT,
  title TEXT, formality TEXT, language TEXT, country TEXT, time_zone TEXT,
  years_as_leader INTEGER, user_type TEXT, email TEXT UNIQUE NOT NULL);
CREATE TABLE role_assignments(
  assignment_id TEXT PRIMARY KEY, person_id TEXT NOT NULL REFERENCES people(person_id),
  chapter_id TEXT NOT NULL REFERENCES chapters(chapter_id), role TEXT NOT NULL,
  term_start TEXT NOT NULL, term_end TEXT NOT NULL, permission TEXT NOT NULL);
CREATE VIEW current_roles AS
  SELECT * FROM role_assignments WHERE term_start <= date('now') AND term_end >= date('now');
""")
db.executemany("INSERT INTO chapters VALUES(?,?)", [(c["Chapter ID"], c["Chapter name"]) for c in chapters])
db.executemany("INSERT INTO people VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?)", [
    (p["Person ID"], p["Full name as written"], p["Latin letters"], p["Short name"], p["Form of address"], p["Title"],
     p["Formality"], p["Language"], p["Country"], p["Time zone"], int(p["Years as leader"]), p["User type"], p["Email"]) for p in people])
db.executemany("INSERT INTO role_assignments VALUES(?,?,?,?,?,?,?)", [
    (r["Assignment ID"], r["Person ID"], r["Chapter ID"], r["Role"], r["Term start"], r["Term end"], r["Permission"]) for r in roles])
db.commit(); db.close()
print(f"built {out}: {len(chapters)} chapters, {len(people)} people, {len(roles)} role assignments")
