"""Summarises a results CSV against its question CSV (counts by path, mismatches with trace IDs).

    python tests/m2/analyse.py tests/m2/lane1_variants.csv tests/m2/results/lane1_variants.csv
Group expectations (lane 2): answer, answer:<rule>, person, never_answer, any; anything else is an exact path.
"""
import csv, sys
from collections import Counter, defaultdict

ANSWERED = {"direct", "model", "trimmed", "words", "spanish_direct", "spanish_model", "amharic_direct", "amharic_model"}
PERSON = {"nocontent", "ai_escalate", "source", "rule_recheck", "shape", "weak", "model_error", "sensitive", "rule_handoff"}

def ok(exp, r):
    p = r["path"]
    if r["error"]: return None
    if exp == "any": return True
    if exp == "answer": return p in ANSWERED
    if exp.startswith("answer:"): return p in ANSWERED and r["rule"] == exp.split(":", 1)[1]
    if exp == "person": return p in PERSON
    if exp == "never_answer": return p not in ANSWERED
    return p == exp

qs = {r["id"]: r for r in csv.DictReader(open(sys.argv[1], encoding="utf-8-sig"))}
res = list(csv.DictReader(open(sys.argv[2], encoding="utf-8")))
by_exp = defaultdict(Counter); bad = []; errs = []
for r in res:
    exp = qs[r["id"]]["expected_path"]
    v = ok(exp, r)
    if v is None: errs.append(r); continue
    by_exp[exp][r["path"]] += 1
    if not v: bad.append((exp, r))
print(f"{len(res)} run, {len(errs)} errors, {len(bad)} not as expected\n")
print(f"{'expected':<16} {'n':>3}  paths seen")
for exp, c in sorted(by_exp.items()):
    print(f"{exp:<16} {sum(c.values()):>3}  " + ", ".join(f"{k} {v}" for k, v in c.most_common()))
if bad:
    print("\nNot as expected:")
    for exp, r in bad:
        print(f"  {r['id']:<22} exp={exp:<14} got={r['path']:<14} rule={r['rule']:<5} {r['step_reason'][:40]:<40} {r['trace_id'][:8]}  | {qs[r['id']]['question'][:70]}")
if errs:
    print("\nErrors (rerun warm, not counted):")
    for r in errs: print(f"  {r['id']} {r['error']} {r['elapsed_s']}s {r['trace_id'][:8]}")
