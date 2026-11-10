"""M2 test runner: sends each question in a CSV to the running Isa with a real sign-in, reads the event log
for its trace ID, and writes one results row per question.

    python tests/m2/run_questions.py tests/m2/smoke.csv
    python tests/m2/run_questions.py tests/m2/lane2.csv --out tests/m2/results/lane2.csv --user d.whitfield

Input CSV columns:  id,lane,language,question,expected_path
Output CSV columns: id,trace_id,expected_path,path,match,status,rule,step_down,step_reason,search_score,
                    source,ticket,elapsed_s,error,answer_text

Rules (M2 brief): real sign-in (Keycloak password grant, test passwords from keycloak/dev-passwords.local.csv),
no bypass, no forced failures. Nothing is retried. A question whose request times out is written with
error=timeout so it can be rerun warm; it is not counted. A fresh token is fetched before each question
(a token can expire during a slow run); that is not a retry of the question.
Question text is written only to the results CSV under tests/m2/, never anywhere else.
No-model questions (expected_path in NO_MODEL) run 4 at a time; everything else runs one at a time.
"""
import argparse, csv, json, os, sys, time, uuid, urllib.request, urllib.error, urllib.parse
from concurrent.futures import ThreadPoolExecutor

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))

# Paths that never call the model, so they can safely run in parallel.
NO_MODEL = {"direct", "stopped", "rule_stop", "sensitive", "rule_handoff", "nocontent", "spanish_direct",
            "amharic_direct", "access"}


def read_password(user):
    with open(os.path.join(REPO, "keycloak", "dev-passwords.local.csv"), encoding="utf-8-sig") as f:
        for row in csv.DictReader(f):
            if row["username"] == user:
                return row["password"]
    raise SystemExit(f"no test password for {user} in keycloak/dev-passwords.local.csv")


def get_token(keycloak, user, password):
    body = urllib.parse.urlencode({"grant_type": "password", "client_id": "isa-chat",
                                   "client_secret": "dev-only-placeholder-secret",
                                   "username": user, "password": password}).encode()
    req = urllib.request.Request(f"{keycloak}/realms/isa/protocol/openid-connect/token", data=body,
                                 headers={"Content-Type": "application/x-www-form-urlencoded"})
    with urllib.request.urlopen(req, timeout=15) as r:
        return json.load(r)["access_token"]


def ask(app, token, question, trace, timeout):
    body = json.dumps({"question": question, "traceId": trace}).encode("utf-8")
    headers = {"Content-Type": "application/json; charset=utf-8"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(f"{app}/api/ask", data=body, headers=headers)
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.status, json.load(r)


def events(app, trace):
    with urllib.request.urlopen(f"{app}/api/events/{trace}", timeout=15) as r:
        return json.load(r)


def field(data, key):
    """Value of 'key=value' inside an event's data text ('result=pass; reason=...; key=value; ...')."""
    if not data:
        return ""
    for part in data.split(";"):
        part = part.strip()
        if part.startswith(key + "="):
            return part[len(key) + 1:].strip()
    return ""


def classify(status, evs):
    """The path a question took, read from its event trail (the same rules test-paths.ps1 checks)."""
    by = {}
    for e in evs:
        by.setdefault(e.get("stage"), e.get("data") or "")
    ic, dbs, rag, ai, oc = (by.get(s, "") for s in ("input_compliance", "database_search", "rag", "complex_ai", "output_compliance"))
    if ic.startswith("result=stop"):
        return "rule_stop" if field(ic, "rule").startswith("C-") else "stopped"
    if ic.startswith("result=escalate"):
        return "sensitive" if field(ic, "rule").startswith("A-") else "rule_handoff"
    if dbs.startswith("result=pass") and "hit=yes" in dbs:
        return "direct"
    if rag.startswith("result=escalate"):
        return "nocontent"
    if ai.startswith("result=escalate"):
        return "ai_escalate" if "answer_found=no" in ai else "model_error"
    if oc.startswith("result=escalate"):
        r = field(oc, "reason")
        if "source" in r: return "source"
        if "rule" in r or "rule_recheck=fail" in oc: return "rule_recheck"
        if "check=answer_shape" in oc: return "shape"
        return "weak"
    if oc.startswith("result=pass"):
        sd = field(oc, "step_down")
        if sd == "trimmed": return "trimmed"
        if sd == "isaca_words": return "words"
        if "section=ai" in oc: return "model"
    return f"other:{status}"


def run_one(row, args, password):
    trace = str(uuid.uuid4())
    out = {"id": row["id"], "trace_id": trace, "expected_path": row.get("expected_path", ""), "path": "", "match": "",
           "status": "", "rule": "", "step_down": "", "step_reason": "", "search_score": "", "source": "",
           "ticket": "", "elapsed_s": "", "error": "", "answer_text": ""}
    try:
        token = get_token(args.keycloak, args.user, password)
    except Exception as ex:
        out["error"] = f"signin:{type(ex).__name__}"
        return out
    t0 = time.time()
    try:
        code, res = ask(args.app, token, row["question"], trace, args.timeout)
    except urllib.error.HTTPError as ex:
        out["error"] = f"http_{ex.code}"
        out["elapsed_s"] = f"{time.time() - t0:.1f}"
        return out
    except Exception as ex:   # socket timeout or connection failure: marked, never retried
        out["error"] = "timeout" if "timed out" in str(ex).lower() else f"request:{type(ex).__name__}"
        out["elapsed_s"] = f"{time.time() - t0:.1f}"
        return out
    out["elapsed_s"] = f"{time.time() - t0:.1f}"
    out["status"] = res.get("status", "")
    out["ticket"] = res.get("ticketReference") or ""
    out["answer_text"] = (res.get("message") or "").replace("\r", " ").replace("\n", " ")
    srcs = res.get("sources") or []
    if srcs:
        s = srcs[0]
        out["source"] = " | ".join(str(s.get(k, "")) for k in ("document", "section", "page") if s.get(k))
    try:
        evs = events(args.app, trace)
    except Exception as ex:
        out["error"] = f"events:{type(ex).__name__}"
        return out
    by = {}
    for e in evs:
        by.setdefault(e.get("stage"), e.get("data") or "")
    out["path"] = classify(out["status"], evs)
    lang = field(by.get("front_end", ""), "language")
    if lang and lang != "en" and out["path"] in ("direct", "model"):
        out["path"] = {"es": "spanish", "am": "amharic"}.get(lang, lang) + "_" + out["path"]
    out["rule"] = field(by.get("input_compliance", ""), "rule") or field(by.get("output_compliance", ""), "rule")
    oc = by.get("output_compliance", "")
    out["step_down"] = field(oc, "step_down")
    out["step_reason"] = field(oc, "step_reason") or field(oc, "failed") or field(oc, "reason")
    out["search_score"] = field(by.get("rag", ""), "top_score") or field(by.get("database_search", ""), "score")
    if out["expected_path"]:
        out["match"] = "yes" if out["expected_path"] == out["path"] else "no"
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("csv")
    ap.add_argument("--out")
    ap.add_argument("--app", default="http://localhost:5067")
    ap.add_argument("--keycloak", default="http://localhost:8080")
    ap.add_argument("--user", default="d.whitfield")
    ap.add_argument("--timeout", type=float, default=170)
    ap.add_argument("--warm", action="store_true", help="send one throwaway model question first (not written)")
    args = ap.parse_args()

    with open(args.csv, encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))
    password = read_password(args.user)
    out_path = args.out or os.path.join(HERE, "results", os.path.basename(args.csv))
    os.makedirs(os.path.dirname(out_path), exist_ok=True)

    if args.warm:
        try:
            ask(args.app, get_token(args.keycloak, args.user, password),
                "How should the Chapter President prepare the agenda for a board meeting?", str(uuid.uuid4()), args.timeout)
        except Exception as ex:
            print(f"warm-up failed: {ex}", file=sys.stderr)

    fast = [r for r in rows if r.get("expected_path") in NO_MODEL]
    slow = [r for r in rows if r.get("expected_path") not in NO_MODEL]
    results = {}
    with ThreadPoolExecutor(max_workers=4) as pool:
        for r, res in zip(fast, pool.map(lambda r: run_one(r, args, password), fast)):
            results[r["id"]] = res
            print(f"{res['id']:>10}  {res['path']:<14} {res['match']:<3} {res['elapsed_s']:>6}s {res['error']}", flush=True)
    for r in slow:
        res = run_one(r, args, password)
        results[r["id"]] = res
        print(f"{res['id']:>10}  {res['path']:<14} {res['match']:<3} {res['elapsed_s']:>6}s {res['error']}", flush=True)

    cols = ["id", "trace_id", "expected_path", "path", "match", "status", "rule", "step_down", "step_reason",
            "search_score", "source", "ticket", "elapsed_s", "error", "answer_text"]
    with open(out_path, "w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=cols)
        w.writeheader()
        for r in rows:
            w.writerow(results[r["id"]])
    done = list(results.values())
    print(f"\n{len(done)} run, {sum(1 for d in done if d['match'] == 'yes')} matched, "
          f"{sum(1 for d in done if d['match'] == 'no')} differed, {sum(1 for d in done if d['error'])} errors -> {out_path}")


if __name__ == "__main__":
    main()
