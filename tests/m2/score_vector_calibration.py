"""Scores tests/m2/test-vector-calibration.csv the way Isa's search by meaning does, and analyses pass marks.

Two ways of choosing the top 3 are scored:
  isaca_only  guides removed BEFORE the top 3 (what RAG_CALIBRATION_FOR_CODE.md describes)
  as_built    top 3 of all passages, THEN guides removed (what VectorSearchService + RagStage do today; W-010)
The top-1 score that meets the pass mark is the best ISACA score that survives each way.
    python tests/m2/score_vector_calibration.py [--check "question" ...]
Writes tests/m2/results/vector_calibration.csv and prints the report sections.
"""
import argparse, array, csv, json, math, os, sqlite3, statistics as st, urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
GUIDE = "Best Practice Guide:"

def embed(text):
    body = json.dumps({"texts": [text], "mode": "query"}).encode()
    for attempt in (1, 2):   # one reconnect on a dropped socket (not a re-score: the same text, the same vector)
        try:
            r = json.load(urllib.request.urlopen(urllib.request.Request("http://localhost:8092/embed", data=body,
                         headers={"Content-Type": "application/json"}), timeout=60))
            break
        except ConnectionResetError:
            if attempt == 2: raise
    v = r["vectors"][0]; n = math.sqrt(sum(x * x for x in v)) or 1.0
    return [x / n for x in v]

vdb = sqlite3.connect(f"file:{os.path.join(REPO, 'data', 'vectors.db')}?mode=ro", uri=True)
index = []
for pid, blob in vdb.execute("select passage_id, embedding from passage_vectors"):
    a = array.array("f"); a.frombytes(blob); n = math.sqrt(sum(x * x for x in a)) or 1.0
    index.append((pid, [x / n for x in a]))
cdb = sqlite3.connect(f"file:{os.path.join(REPO, 'data', 'content.db')}?mode=ro", uri=True)
P = {i: (d, s, p) for i, d, s, p in cdb.execute("select id, document, section, page from passages")}

def search(q):
    qv = embed(q)
    scored = sorted(((sum(a * b for a, b in zip(qv, v)), i) for i, v in index), reverse=True)
    isaca = [(s, i) for s, i in scored if not P[i][0].startswith(GUIDE)]
    built = [(s, i) for s, i in scored[:3] if not P[i][0].startswith(GUIDE)]
    return isaca[:3], built

ap = argparse.ArgumentParser(); ap.add_argument("--check", nargs="*")
args = ap.parse_args()
if args.check:
    for q in args.check:
        top, built = search(q)
        print(q, "| isaca_only:", [(round(s, 4), P[i][1][:30]) for s, i in top], "| as_built:", [(round(s, 4), P[i][1][:30]) for s, i in built])
    raise SystemExit

rows = list(csv.DictReader(open(os.path.join(HERE, "test-vector-calibration.csv"), encoding="utf-8")))
res = []
for r in rows:
    top, built = search(r["question"])
    exp = {int(x) for x in r["note"].split("=")[1].split("+")} if r["note"].startswith("pids=") else set()
    exp_ds = {(P[e][0], P[e][1]) for e in exp}
    def correct(i): return (P[i][0], P[i][1]) in exp_ds
    res.append({"kind": r["kind"], "question": r["question"],
                "top1_score": f"{top[0][0]:.4f}", "top1_passage": f"{top[0][1]}: {P[top[0][1]][0]} | {P[top[0][1]][1]}",
                "expected_passage": r["note"].replace("pids=", ""),
                "correct": ("yes" if correct(top[0][1]) else "no") if exp else "",
                "expected_in_top3": ("yes" if any(correct(i) for _, i in top) else "no") if exp else "",
                "top3_scores": " ".join(f"{s:.4f}" for s, _ in top),
                "as_built_top1_score": f"{built[0][0]:.4f}" if built else "",
                "as_built_expected_in_top3": ("yes" if any(correct(i) for _, i in built) else "no") if exp else ""})
os.makedirs(os.path.join(HERE, "results"), exist_ok=True)
with open(os.path.join(HERE, "results", "vector_calibration.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.DictWriter(f, fieldnames=list(res[0])); w.writeheader(); w.writerows(res)

def pct(xs, p): return st.quantiles(xs, n=100, method="inclusive")[p - 1]
print(f"questions embedded: {len(res)}\n")
print("1. Top-1 score by kind (isaca_only)")
print(f"{'kind':<13} {'n':>3} {'min':>6} {'p10':>6} {'p25':>6} {'med':>6} {'p75':>6} {'p90':>6} {'max':>6}")
for k in ("in_scope", "out_of_scope", "near"):
    xs = [float(r["top1_score"]) for r in res if r["kind"] == k]
    print(f"{k:<13} {len(xs):>3} {min(xs):6.3f} {pct(xs,10):6.3f} {pct(xs,25):6.3f} {st.median(xs):6.3f} {pct(xs,75):6.3f} {pct(xs,90):6.3f} {max(xs):6.3f}")

ins = [r for r in res if r["kind"] == "in_scope"]
print(f"\n2. In scope ({len(ins)}): top-1 correct {sum(r['correct']=='yes' for r in ins)}, expected in top 3 "
      f"{sum(r['expected_in_top3']=='yes' for r in ins)} (isaca_only); as built, expected in top 3: "
      f"{sum(r['as_built_expected_in_top3']=='yes' for r in ins)}")
print("   High score on the wrong passage (top-1 wrong, score >= 0.80):")
for r in sorted(ins, key=lambda r: -float(r["top1_score"])):
    if r["correct"] == "no" and float(r["top1_score"]) >= 0.80:
        print(f"     {r['top1_score']}  {r['question'][:60]:<60} -> {r['top1_passage'][:60]} (expected {r['expected_passage']})")
print("   Low score (top-1 < 0.80):")
for r in sorted(ins, key=lambda r: float(r["top1_score"])):
    if float(r["top1_score"]) < 0.80:
        print(f"     {r['top1_score']}  {r['question'][:60]:<60} correct={r['correct']}")

print("\n3. Pass mark table (isaca_only | as built)")
print(f"{'mark':>5} {'in acc%':>8} {'in+exp%':>8} {'out acc%':>9} {'near acc%':>10} || {'in acc%':>8} {'in+exp%':>8} {'out%':>6} {'near%':>6}")
def rate(rs, key, m, need=None):
    ok = [r for r in rs if r[key] and float(r[key]) >= m and (need is None or r[need] == "yes")]
    return 100 * len(ok) / len(rs)
outs = [r for r in res if r["kind"] == "out_of_scope"]; near = [r for r in res if r["kind"] == "near"]
for m100 in range(70, 91):
    m = m100 / 100
    print(f"{m:5.2f} {rate(ins,'top1_score',m):8.1f} {rate(ins,'top1_score',m,'expected_in_top3'):8.1f} {rate(outs,'top1_score',m):9.1f} {rate(near,'top1_score',m):10.1f} || "
          f"{rate(ins,'as_built_top1_score',m):8.1f} {rate(ins,'as_built_top1_score',m,'as_built_expected_in_top3'):8.1f} {rate(outs,'as_built_top1_score',m):6.1f} {rate(near,'as_built_top1_score',m):6.1f}")

low_ok = min(float(r["top1_score"]) for r in ins if r["correct"] == "yes")
print(f"\n4. Overlap: out_of_scope / near scoring above the lowest correct in_scope ({low_ok:.4f})")
for r in sorted(res, key=lambda r: -float(r["top1_score"])):
    if r["kind"] != "in_scope" and float(r["top1_score"]) > low_ok:
        print(f"     {r['top1_score']}  {r['kind']:<12} {r['question'][:60]:<60} -> {r['top1_passage'][:50]}")
