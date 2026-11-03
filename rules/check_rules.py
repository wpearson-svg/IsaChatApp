"""Check Isa's rules files against their own tests.

A reference check for the sample rules in this folder (docs/Isa_Rules_Design.md).
It is NOT Isa's rule engine and Isa never runs it: Willis builds the real engine
in the app. This script only reads the files and reports, so the team can see
that every rule's tests pass, and that the rehearsed demo questions
(tests/rehearsed_demo.json) would get exactly the result they get today.

Run from the project folder:  python rules\\check_rules.py
"""

import csv
import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
CHAPTERS_CSV = HERE.parent / "directory" / "chapters.csv"

# The conflict order: the most careful outcome wins (design page, section 5).
SEVERITY = {"pass": 0, "answer": 0, "answer_then_point": 1, "hand_off": 2, "stop": 3, "safety": 4}
REQUIRED_HEADER = ["layer", "scope", "version", "approved_by", "approval_reference", "approved_on", "status", "rules"]


def clean(text):
    """Lower case, tidy quotes, and undo simple disguises ("t@xes", "t a x")."""
    t = text.lower().replace("’", "'").replace("‘", "'").replace("@", "a").replace("$", "s")
    # "t a x e s" -> "taxes": three or more single letters separated by spaces.
    return re.sub(r"\b(?:[a-z] ){2,}[a-z]\b", lambda m: m.group(0).replace(" ", ""), t)


def has(signal, text):
    """A signal matches at the start of a word: "tax" matches "taxes", not "syntax"."""
    return re.search(r"(?<![a-z0-9])" + re.escape(clean(signal)), text) is not None


def load_files():
    files = {}
    for path in sorted(HERE.rglob("*.json")):
        if path.parent.name == "tests":
            continue
        with open(path, encoding="utf-8") as f:
            files[path.relative_to(HERE).as_posix()] = json.load(f)
    return files


def load_chapters():
    with open(CHAPTERS_CSV, encoding="utf-8-sig", newline="") as f:
        return {row["Chapter ID"]: row for row in csv.DictReader(f)}


def files_for(chapter, files):
    """Which files apply to a chapter: global, its region, its country, itself."""
    wanted = {("global", ""), ("region", chapter["Region"]), ("country", chapter["Country"]),
              ("chapter", chapter["Chapter ID"])}
    return [d for d in files.values() if d["status"] == "active" and (d["layer"], d["scope"]) in wanted]


def severity(rule):
    if rule["outcome"] == "hand_off" and "urgent" in rule.get("flags", []) and rule.get("group") == "A":
        return SEVERITY["safety"]
    return SEVERITY[rule["outcome"]]


def fires(rule, text, role, parents):
    roles = rule.get("applies_to_roles", "all")
    if roles != "all" and role not in roles:
        return False
    topic, kind, unless = list(rule.get("topic_signals", [])), list(rule.get("kind_signals", [])), list(rule.get("unless", []))
    if "tightens" in rule:
        parent = parents[rule["tightens"]]
        topic += parent.get("topic_signals", [])
        kind += parent.get("kind_signals", [])
        unless += parent.get("unless", [])
    if not any(has(s, text) for s in topic):
        return False
    if kind and not any(has(s, text) for s in kind):
        return False
    return not any(has(s, text) for s in unless)


def decide(question, chapter_id, role, files, chapters):
    """Return (outcome, winning rule id, all fired rule ids) at the input checkpoint."""
    applicable = files_for(chapters[chapter_id], files)
    rules = [r for d in applicable for r in d["rules"]]
    parents = {r["id"]: r for r in rules}
    text = clean(question)
    fired = [r for r in rules if fires(r, text, role, parents)]
    if not fired:
        return "pass", None, []
    winner = max(fired, key=severity)
    return winner["outcome"], winner["id"], [r["id"] for r in fired]


def meets(expect, outcome, fired):
    if expect.startswith("not "):
        return expect[4:] not in fired
    if expect in ("answer", "pass"):
        return outcome == "pass"
    return outcome == expect


def main():
    files, chapters = load_files(), load_chapters()
    all_rules = {r["id"]: r for d in files.values() for r in d["rules"]}
    problems, checked = [], 0

    # 1. The files themselves: header, approval record, tighten-only.
    for name, d in files.items():
        missing = [k for k in REQUIRED_HEADER if k not in d]
        if missing:
            problems.append(f"{name}: missing header fields {missing}")
        if not d.get("approved_by") or not d.get("approval_reference"):
            problems.append(f"{name}: no approval record")
        for r in d["rules"]:
            if "tightens" in r:
                parent = all_rules.get(r["tightens"])
                if parent is None:
                    problems.append(f"{name} {r['id']}: tightens unknown rule {r['tightens']}")
                elif severity(r) < severity(parent):
                    problems.append(f"{name} {r['id']}: loosens {parent['id']} ({parent['outcome']} -> {r['outcome']})")

    # 2. Every rule's own tests, at both checkpoints.
    for name, d in files.items():
        for r in d["rules"]:
            for t in r.get("tests", {}).get("questions", []):
                checked += 1
                outcome, winner, fired = decide(t["text"], t["as_chapter"], t["as_role"], files, chapters)
                if not meets(t["expect"], outcome, fired):
                    problems.append(f"{r['id']}: \"{t['text']}\" as {t['as_chapter']}: expected {t['expect']}, got {outcome} ({winner})")
            for sentence in r.get("tests", {}).get("answers_must_be_stopped", []):
                checked += 1
                rule = all_rules.get(r.get("tightens"), r) if "tightens" in r else r
                if not any(has(p, clean(sentence)) for p in rule.get("answer_must_not", [])):
                    problems.append(f"{r['id']}: answer \"{sentence}\" would not be stopped by the output checkpoint")

    # 3. The rehearsed demo questions: exactly as today.
    with open(HERE / "tests" / "rehearsed_demo.json", encoding="utf-8") as f:
        demo = json.load(f)
    print("Rehearsed demo questions (as " + demo["as_chapter"] + ", " + demo["as_role"] + "):")
    for q in demo["questions"]:
        checked += 1
        outcome, winner, fired = decide(q["text"], demo["as_chapter"], demo["as_role"], files, chapters)
        ok = meets(q["expect"], outcome, fired) and (winner == q.get("rule"))
        print(f"  {'OK  ' if ok else 'FAIL'} {q['text']}  ->  {outcome}{' (' + winner + ')' if winner else ''}")
        if not ok:
            problems.append(f"Rehearsed: \"{q['text']}\": today {q['today']}, would now be {outcome} ({winner})")

    print(f"\n{checked} checks, {len(problems)} problems")
    for p in problems:
        print("  - " + p)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
