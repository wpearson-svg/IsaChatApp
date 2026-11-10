"""Prepare Isa for the presentation demo, in one step (Matthew, November 9, 2026).

The demo runs from one sign-in, Watanabe Kenji (P-0001), on day 3 of his term (demo date
July 3, 2026). See ONBOARDING-FEATURE.md, "The demo". This script makes every rehearsal,
and the presentation itself, start from exactly the same place:

  data/progress.db  (each leader's journey progress; local, git-ignored)
    - everything cleared: no steps done, no bookings, no check-ins, the Today note unseen
    - Kenji's board introduction saved (J-19 done), so his board can reply
    - Sato Yumiko's optional profile reply to it, so her board page has answers

  data/notebook.db  (the succession notebook; local, git-ignored)
    - every entry written while testing is removed; the invented history from
      onboarding/notebook_history.csv (Sato's 2025-26 year) is kept, and any of it that
      was hidden during testing is shown again
    - what Sato left Kenji before her term ended: a saved answer with the decision she
      recorded, a decision note, and an unfinished item

Nothing here changes Isa's content, rules, the event log or the usage counts, and nothing
is sent anywhere. All names are invented sample people.

Before running: stop Isa (the script refuses while it is running). Both files must exist:
if one is missing, start Isa once (it creates them), stop it, and run this again.

Run from the project folder:  python onboarding\\prepare_demo.py
"""

import datetime
import json
import os
import socket
import sqlite3
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, "data")
PROGRESS = os.path.join(DATA, "progress.db")
NOTEBOOK = os.path.join(DATA, "notebook.db")

CHAPTER = "C-001"                      # Sakura Bay Chapter
KENJI = ("P-0001", "Watanabe Kenji")   # incoming president, term from July 1, 2026
SATO = ("P-0012", "Sato Yumiko")       # outgoing president, term to June 30, 2026

NOW = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

# Kenji's introduction, as Isa drafts it from ISACA's records (BoardIntroService).
INTRODUCTION = (
    "Dear board,\n\n"
    "I'm Watanabe Kenji, the new president of the Sakura Bay Chapter. My term started on "
    "1 July 2026, and I'm looking forward to working with each of you this year.\n\n"
    "To help us share the work, I'd like to know a little about what you enjoy and how "
    "you'd like to help. If you'd like, please answer four short questions in Isa. "
    "It's optional and takes a few minutes.\n\n"
    "Best regards,\nWatanabe Kenji"
)

# Sato's reply to the optional profile form (only Kenji sees it).
SATO_PROFILE = {
    "good_at": "Running the AGM and handover; I know the chapter's history well.",
    "enjoy": "Mentoring new board members and planning the strategic session.",
    "time_per_month": "2 to 5 hours",
    "contact_by": "A message on Engage",
}

GUIDEBOOK = "Chapter Leader Guidebook - President (August 2026)"

# What Sato left in the notebook in her last days as president (June 2026).
# The saved answer is Isa's answer to demo question 1, word for word, with its sources.
SATO_ENTRIES = [
    {
        "kind": "saved_answer", "category": None, "topic": "Meetings", "on_date": "2026-06-26",
        "question": "How many board meetings must a chapter hold each year?",
        "answer": "Based on the approved content: throughout the year, with a minimum of four board meetings annually.",
        "sources": [
            {"Document": GUIDEBOOK, "Section": "The Chapter President should:", "Page": "7"},
            {"Document": GUIDEBOOK, "Section": "Guidebook page 8", "Page": "8"},
        ],
        "note": "We'll hold six board meetings this year, every second month.",
        "shared": 1,
    },
    {
        "kind": "note", "category": "decision", "topic": "Elections", "on_date": "2026-06-24",
        "note": "The board voted to hold this year's election online, so members abroad can vote.",
        "shared": 0,
    },
    {
        "kind": "note", "category": "unfinished", "topic": "Finances", "on_date": "2026-06-29",
        "note": "Sponsor renewal with the local university is still open. Their contact expects our proposal by the end of July.",
        "shared": 0,
    },
]


def isa_is_running() -> bool:
    """True when something answers on Isa's port (5067) on this computer."""
    try:
        with socket.create_connection(("127.0.0.1", 5067), timeout=1):
            return True
    except OSError:
        return False


def tables(conn) -> list[str]:
    return [r[0] for r in conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")]


def main() -> int:
    if isa_is_running():
        print("Isa is running (port 5067). Stop Isa first, then run this again.")
        return 1
    for path in (PROGRESS, NOTEBOOK):
        if not os.path.exists(path):
            print(f"{os.path.relpath(path, ROOT)} is missing. Start Isa once (it creates it), "
                  "stop it, then run this again.")
            return 1

    # ---- progress.db: start clean, then the board introduction and Sato's reply ----
    with sqlite3.connect(PROGRESS) as p:
        needed = {"board_introductions", "board_profiles", "step_progress", "step_history"}
        missing = needed - set(tables(p))
        if missing:
            print(f"progress.db has no {', '.join(sorted(missing))} table. "
                  "Start Isa once with the current code, stop it, then run this again.")
            return 1
        for t in tables(p):
            p.execute(f'DELETE FROM "{t}"')
        p.execute("DELETE FROM sqlite_sequence")

        p.execute("""INSERT INTO board_introductions(president_id, chapter_id, message, on_date, written_at)
                     VALUES (?, ?, ?, '2026-07-02', ?)""", (KENJI[0], CHAPTER, INTRODUCTION, NOW))
        p.execute("""INSERT INTO step_progress(person_id, step_id, how, completed_at)
                     VALUES (?, 'J-19', 'introduction', ?)""", (KENJI[0], NOW))
        p.execute("""INSERT INTO step_history(person_id, step_id, action, how, on_date, recorded_at)
                     VALUES (?, 'J-19', 'done', 'introduction', '2026-07-02', ?)""", (KENJI[0], NOW))
        p.execute("""INSERT INTO board_profiles(person_id, chapter_id, president_id, status, good_at, enjoy,
                                                time_per_month, contact_by, on_date, updated_at)
                     VALUES (?, ?, ?, 'answered', ?, ?, ?, ?, '2026-07-03', ?)""",
                  (SATO[0], CHAPTER, KENJI[0], SATO_PROFILE["good_at"], SATO_PROFILE["enjoy"],
                   SATO_PROFILE["time_per_month"], SATO_PROFILE["contact_by"], NOW))

    # ---- notebook.db: keep the sample history, then what Sato left ----
    with sqlite3.connect(NOTEBOOK) as n:
        cols = {r[1] for r in n.execute("PRAGMA table_info(notebook_entries)")}
        if "sample_id" not in cols:
            print("notebook.db is from older code. Start Isa once with the current code, stop it, then run this again.")
            return 1
        removed = n.execute("DELETE FROM notebook_entries WHERE sample_id IS NULL").rowcount
        n.execute("UPDATE notebook_entries SET removed_on = NULL WHERE sample_id IS NOT NULL")
        if "notebook_emails" in tables(n):
            n.execute("DELETE FROM notebook_emails")
        samples = n.execute("SELECT COUNT(*) FROM notebook_entries").fetchone()[0]

        for e in SATO_ENTRIES:
            n.execute("""INSERT INTO notebook_entries(chapter_id, author_id, author_name, author_role, kind, category,
                                                      question, answer, sources_json, note, shared, on_date,
                                                      recorded_at, topic, shared_on)
                         VALUES (?, ?, ?, 'President', ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                      (CHAPTER, SATO[0], SATO[1], e["kind"], e["category"], e.get("question"), e.get("answer"),
                       json.dumps(e["sources"]) if "sources" in e else None, e["note"], e["shared"],
                       e["on_date"], NOW, e["topic"], e["on_date"] if e["shared"] else None))

    print(f"progress.db: cleared; Kenji's board introduction (J-19) and Sato's profile reply added.")
    print(f"notebook.db: {removed} test entries removed; {samples} sample history entries kept; "
          f"{len(SATO_ENTRIES)} entries from Sato added (June 2026).")
    if samples == 0:
        print("Note: no sample history yet. Isa copies it in when it starts, so Kenji's 'Last year' row will appear then.")
    print("Ready. Start Isa, check the demo date is July 3, 2026 (Config page), and sign in as Kenji.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
