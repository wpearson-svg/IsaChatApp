"""Sprint 3 M2 testing: send a CSV of questions through Isa and record the path each one took.

Real runs only: signs in through Keycloak with a demo test account (keycloak/dev-passwords.local.csv),
sends each question to POST /api/ask, then reads GET /api/events/<traceId> to see which stages ran.
No bypass settings, no forced failures. Question text is written only to the results CSV here.

Input CSV columns:  id,lane,question,expected_path   (optional: user, to sign in as someone else;
                    expected_input, to check the rules' decision instead of the path:
                    stop | hand_off | answer_then_point | pass. Rules decide before any search,
                    so these rows count on this laptop even when the answer later needs Willis's lane.)
Output CSV columns: id,lane,user,expected_path,expected_input,input,path,match,rule,status,ticket,step_down,elapsed_s,trace_id,willis_lane,error

Paths:  stopped (input rule stop) | handed_off (input rule hand-off) | direct (exact search answer)
        guide (a governance best-practice guide found by word search)
        model (the AI wrote it) | words (ISACA's words after a failed check) | no_content (passed to a person,
        nothing approved found) | output_handoff (an answer check sent it to a person) | error
willis_lane = yes when the run touched vector search, the AI model or translation, which this laptop
              can't test reliably (Willis's lane). Those rows are recorded but not counted.

Usage (from the project folder, Isa running):
    python tests\\m2\\run_questions.py tests\\m2\\smoke.csv tests\\m2\\results\\smoke.csv [--user k.watanabe] [--repeat N]
"""
import argparse, csv, json, os, re, sys, time, urllib.error, urllib.parse, urllib.request, uuid

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
APP = "http://localhost:5067"
KEYCLOAK = "http://localhost:8080"

_tokens = {}


def token(user, fresh=False):
    if user in _tokens and not fresh:
        return _tokens[user]
    pw = {r["username"]: r["password"] for r in csv.DictReader(
        open(os.path.join(ROOT, "keycloak", "dev-passwords.local.csv"), encoding="utf-8-sig"))}
    body = urllib.parse.urlencode(dict(grant_type="password", client_id="isa-chat",
                                       client_secret="dev-only-placeholder-secret",
                                       username=user, password=pw[user])).encode()
    t = json.load(urllib.request.urlopen(f"{KEYCLOAK}/realms/isa/protocol/openid-connect/token", body))["access_token"]
    _tokens[user] = t
    return t


def ask(question, user):
    trace = str(uuid.uuid4())
    for attempt in (1, 2):
        req = urllib.request.Request(f"{APP}/api/ask", json.dumps({"question": question, "traceId": trace}).encode("utf-8"),
                                     {"Content-Type": "application/json; charset=utf-8",
                                      "Authorization": "Bearer " + token(user, fresh=attempt == 2)})
        try:
            return trace, json.load(urllib.request.urlopen(req, timeout=170)), ""
        except urllib.error.HTTPError as e:
            if e.code == 401 and attempt == 1:
                continue            # the token ran out during a long run: sign in once more
            return trace, None, f"http {e.code}"
        except Exception as e:      # timeouts and connection errors
            return trace, None, type(e).__name__
    return trace, None, "http 401"


def events(trace):
    return json.load(urllib.request.urlopen(f"{APP}/api/events/{trace}"))


def classify(res, ev):
    d = {e["stage"]: e["data"] for e in ev}
    inp = d.get("input_compliance", "")
    rule = (re.search(r"rule=([A-Z]+-\d+)", inp) or [None, ""])[1]
    out = d.get("output_compliance", "")
    rag = d.get("rag", "")
    ai = d.get("complex_ai", "")
    tr = d.get("translation", "")
    step = (re.search(r"step_down=([\w-]+)", out) or [None, ""])[1]
    if inp.startswith("result=stop"):
        decision = "stop"
    elif inp.startswith("result=escalate"):
        decision = "hand_off"
    elif "answer_then_point" in inp:
        decision = "answer_then_point"
    elif inp:
        decision = "pass"
    else:
        decision = ""
    status = res["status"] if res else ""

    willis = []
    if "model_called=yes" in ai:
        willis.append("model")
    if "retrieval=vector" in rag or "vector_miss" in rag:
        willis.append("vector search")
    if tr and "skipped, english" not in tr and "note=skipped" not in tr:
        willis.append("translation")

    if inp.startswith("result=stop"):
        path = "stopped"
    elif inp.startswith("result=escalate"):
        path = "handed_off"
    elif status == "answered" and "direct_hit" in d.get("database_search", ""):
        path = "direct"
    elif status == "answered" and "best_practice" in d.get("database_search", ""):
        path = "guide"            # a governance best-practice guide, found by plain word search (no AI)
    elif status == "answered" and step == "words":
        path = "words"
    elif status == "answered":
        path = "model"
    elif status == "escalated" and rag.startswith("result=escalate"):
        path = "no_content"
    elif status == "escalated":
        path = "output_handoff"
    else:
        path = status or "error"
    return path, rule, step, ",".join(willis), decision


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("infile")
    ap.add_argument("outfile")
    ap.add_argument("--user", default="k.watanabe")
    ap.add_argument("--repeat", type=int, default=1)
    # Where Keycloak is when the services are Remote (for example http://100.89.6.69:8080); default this computer.
    ap.add_argument("--keycloak", default=KEYCLOAK)
    a = ap.parse_args()
    globals()["KEYCLOAK"] = a.keycloak.rstrip("/")

    rows = list(csv.DictReader(open(a.infile, encoding="utf-8-sig")))
    os.makedirs(os.path.dirname(os.path.abspath(a.outfile)), exist_ok=True)
    cols = ["id", "lane", "user", "expected_path", "expected_input", "input", "path", "match", "rule", "status", "ticket", "step_down",
            "elapsed_s", "trace_id", "willis_lane", "error"]
    out = []
    for run in range(a.repeat):
        for r in rows:
            user = r.get("user") or a.user
            t0 = time.time()
            trace, res, err = ask(r["question"], user)
            elapsed = round(time.time() - t0, 2)
            path, rule, step, willis, decision = ("error", "", "", "", "")
            if res is not None:
                path, rule, step, willis, decision = classify(res, events(trace))
            exp = (r.get("expected_path") or "").strip()
            exp_in = (r.get("expected_input") or "").strip()
            # An expected value may name alternatives: "handed_off|no_content".
            if exp_in.startswith("not "):          # "not A-01": that rule must not be the one that decided
                match = "yes" if rule != exp_in[4:] else "no"
            elif exp_in:
                match = "yes" if decision in exp_in.split("|") else "no"
            else:
                match = "" if not exp else ("yes" if path in exp.split("|") else "no")
            out.append(dict(id=r["id"] + (f"#{run + 1}" if a.repeat > 1 else ""), lane=r.get("lane", ""),
                            user=user, expected_path=exp, expected_input=exp_in, input=decision, path=path, match=match, rule=rule,
                            status=res["status"] if res else "", ticket=(res or {}).get("ticketReference") or "",
                            step_down=step, elapsed_s=elapsed, trace_id=trace,
                            willis_lane=("yes: " + willis) if willis and not exp_in else "", error=err))
            print(f'{out[-1]["id"]:>10}  {decision:<17} {path:<14} {("OK" if match == "yes" else "MISMATCH" if match == "no" else ""):<8} '
                  f'{rule:<5} {elapsed:>6}s {("WILLIS LANE: " + willis) if out[-1]["willis_lane"] else ""} {err}', flush=True)
    with open(a.outfile, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=cols)
        w.writeheader()
        w.writerows(out)
    counted = [o for o in out if not o["willis_lane"] and o["match"]]
    print(f"\n{len(out)} runs; counted {len(counted)}; matched {sum(o['match'] == 'yes' for o in counted)}; "
          f"Willis lane {sum(bool(o['willis_lane']) for o in out)}; errors {sum(bool(o['error']) for o in out)}")
    sys.exit(0 if all(o["match"] == "yes" for o in counted) else 1)


if __name__ == "__main__":
    main()
