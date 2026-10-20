# Stage contract

Project: Isa, Second Star Collective
Author: Willis Pearson (T1)
Version: 2 (changed 2026-10-20 for the Pipeline Built milestone, due 2026-10-23)
Status: CHANGED. Version 2 is written by the Project Lead and is NOT yet confirmed by Eliyas. Version 1 (confirmed verbally by Eliyas on 2026-10-16) is kept as `docs/stage-contract.v1.md`.
Confirmed on: pending (Eliyas to confirm version 2; the repo copy of this file is the final word once confirmed)

This is the one-page agreement every stage and every person builds to. If you need to change anything here, tell the team first, then change this file.

---

## 0. What changed in version 2 (read this first, Eliyas)

| # | Change | Where |
|---|---|---|
| 1 | New result `NotFound`: a search with no result. Not a Stop, not an error. Blue in the panel. Used by `database_search`. | section 3 |
| 2 | Stage order is the same names in the same order, with the rules for who runs when made explicit: translation always comes before input compliance, so input compliance screens English. `knowledge_graph`, `rag` and `complex_ai` run only on a miss. | section 1 |
| 3 | `language` is set by the front end (a quick check before the query enters the system), not by translation. | section 2 |
| 4 | Skip rules written down: what is skipped, what is logged, and what the panel shows. | section 3 |
| 5 | Event log: new fields for model called, model, time taken, passages retrieved and source cited. | section 4 |
| 6 | `output_compliance` has two sections: database side and AI side. The log and the panel show which one ran. | section 8 |
| 7 | AI-side checks defined, with a result shape for each: source check, NLI check, rule recheck, confidence score. All are stubs that pass, except the source check, which is real. | section 8 |
| 8 | New error state: a failed or timed-out model call is a real error (red) and the run escalates to a ticket. | section 9 |
| 9 | Live stage updates: the orchestrator reports each stage as it starts and finishes, using one message format (`StageProgress`). Decided 2026-10-20. | section 6 |
| 10 | Test paths replaced by the five questions for this milestone. | section 11 |

---

## 1. Stage order

| # | Stage name (exact, used in the event log) | Owner | Runs |
|---|---|---|---|
| 1 | `front_end` | Willis | always. Also sets `language` (quick check) before the query enters the system |
| 2 | `access_control` | Eliyas | always |
| 3 | `translation` | Willis and Matt | always runs; returns Pass `skipped` when `language` is `en`. It is always BEFORE `input_compliance`, so input compliance screens English |
| 4 | `input_compliance` | Willis and Matt | always (stub). A Stop ends the run; nothing after it runs or is shown |
| 5 | `database_search` | Eliyas | always. Full-text only, no AI. Found = direct hit. Not found = `NotFound`, then the run goes on to stage 6 |
| 6 | `knowledge_graph` | Eliyas | only on a miss (Pass `skipped, direct hit` otherwise). Part of the RAG step: a stub inside RAG, not its own step in the panel |
| 7 | `rag` | Eliyas | only on a miss (Pass `skipped, direct hit` otherwise). Stub: hands fixed stand-in passages to the AI step. No passages = Escalate |
| 8 | `complex_ai` | Eliyas | only on a miss (Pass `skipped, direct hit` otherwise). The real model (phi4-mini through Ollama). Always sent the question WITH the passages, never the bare question |
| 9 | `output_compliance` | Willis | always, including direct hits. Two sections (section 8) |
| 10 | `ticketing` | Willis | only on Escalate. Creates the TKT reference; the ticket is not sent to the connected service |

The orchestrator runs the stages in this order. Stage names are lower case with underscores, exactly as above.

## 2. Query context (the bundle every stage receives)

One object travels through the whole run. Each stage reads it and may fill in its own slots.

| Field | Type | Set by | Notes |
|---|---|---|---|
| `trace_id` | GUID text | `front_end` | Created when the user presses send, before anything else |
| `user_id` | text | `access_control` | From the sign-in token (Keycloak `preferred_username`) |
| `roles` | list of text | `access_control` | Client roles: `chat_user`, `dashboard_dev`, `dashboard_content_health`, `auditor` |
| `profile` | map of text to text | `access_control` | Who the person is, read from token claims Keycloak adds: `person_id`, `chapter_id`, `chapter_role`, `permission` (Leadership or Member), `term_start`, `term_end`, `years_as_leader`, `language`, `formality`, `form_of_address`. Missing for accounts without them (staff, no-role). |
| `question` | text | `front_end` | The user's question as typed |
| `language` | text | `front_end` (CHANGED: was `translation`) | Language code of the question from the quick language check, set before the query enters the system. Two-letter code, for example `en`. `und` when the check cannot tell; `und` is treated as not English |
| `passages` | list of {document, section, page, text} | `database_search`, `rag` | Found content (direct hit: from the database; miss: the stand-in passages from `rag`) |
| `direct_hit` | true or false | `database_search` | True when the full-text search found the answer. Skips `knowledge_graph`, `rag` and `complex_ai` |
| `answer` | text | `database_search` (direct hit) / `complex_ai` (miss) | The answer text |
| `cited_source` | {document, section, page} or empty | `database_search` / `complex_ai` | The one passage the answer says it came from. For the model: the passage it named, in a form the code can read |
| `sources` | list of {document, section, page} | `database_search` / `complex_ai` | What the answer is based on |
| `output_section` | `database` / `ai` / empty | `output_compliance` | Which section of output compliance ran |
| `status` | `answered` / `stopped` / `escalated` | orchestrator | Final outcome |
| `reason` | text | any stage | Why a stage stopped, escalated or did not find |
| `ticket_reference` | text or empty | `ticketing` | Format `TKT-0001`, counting up |
| `work_steps` | list of {stage, outcome, skipped, reason, error, data_text, elapsed_ms} | orchestrator | "Show the work": one entry per stage, added by the orchestrator after it writes that stage's event, from the same stage result. Stages never write to it. Used by the chat only; not sent in the API response (section 7). `outcome` is `pass`, `not_found`, `stop` or `escalate`. `error` is empty unless the stage had a real error (section 9). |

## 3. Stage results

Every stage returns exactly one result, plus a short data text.

| Result | Meaning | What happens next | Panel color |
|---|---|---|---|
| `Pass` | Carry on | The next stage runs | green (gray when skipped) |
| `NotFound` (NEW) | A search found nothing. Handled, did not pass. Not a Stop, not an error | The next stage runs (the miss path: `knowledge_graph`, `rag`, `complex_ai`) | blue |
| `Stop` | End the run now, with a reason (example: out-of-scope question) | The chat shows a polite message. `status = stopped`. Nothing after the stop runs or is shown | gray-blue stop state |
| `Escalate` | Send to a person (example: sensitive topic, no content, a failed check, a failed model call) | `ticketing` runs, a ticket reference comes back, the run ends. `status = escalated` | blue (red when the data text has `error=`) |

Only `database_search` returns `NotFound`. A search with no result returns `NotFound` and never an error.

**Skip rules**
- A stage that does not apply returns **Pass** with the note `skipped, <why>`. The event log keeps one row for every stage, so the audit trail is complete.
- The panel shows only the stages that actually ran. Rows with `skipped` are hidden, with one exception: `translation` skipped for English is shown, as skipped (gray).
- Direct hit: `knowledge_graph`, `rag` and `complex_ai` are skipped (hidden). `output_compliance` still runs, database side.
- After a Stop: nothing after it runs and nothing after it is shown.
- After an Escalate: the stages between are logged as `skipped, escalated` and hidden. `ticketing` runs and is shown. Ticketing never appears when nothing escalates.

If no stage stops or escalates and the last stage passes, `status = answered`.

## 4. Event log

- A separate SQLite file (not the content database), in WAL mode.
- One table, one row per stage per question.
- Only the orchestrator writes events, after each stage, from what the stage returned. Stages never write to the log.
- Text a leader types outside the chat (for example a succession notebook note) is checked with `access_control` and `input_compliance` only, through the orchestrator's `CheckTextAsync`. Its events carry `purpose=...` (for example `purpose=notebook_note`), and `front_end` records the text length, never the text. Added 2026-10-20 by Matt.

| Column | Content |
|---|---|
| `trace_id` | The GUID |
| `stage` | A stage name from section 1 |
| `timestamp` | When the stage finished, UTC, ISO 8601 (for example `2026-10-15T14:03:22Z`) |
| `data` | Short key=value text (see below) |

**Shape of the data text:** `key=value` pairs separated by `; `, in this order: `result`, `reason`, then any extras. Use `none` when there is no reason. `result` is one of `pass`, `not_found`, `stop`, `escalate`.

**Extras by stage (NEW in version 2)**

| Stage | Extra keys |
|---|---|
| `front_end` | `language=<code>` |
| `translation` | `from=<code>; note=skipped, english` when skipped |
| `database_search` | `hit=yes/no; passages=<count>` |
| `rag` | `passages=<count>; retrieved=<document section, ...>` |
| `complex_ai` | `model_called=yes/no; model=<name>; model_ms=<time taken>; passages=<count>; source=<document section>` |
| `output_compliance` | `section=database/ai; source=ok/missing; approved=yes/no`, plus on the AI side `source_retrieved=yes/no; rule_recheck=pass/fail; confidence=<score>; nli=pass/fail` |
| `ticketing` | `ticket=TKT-0001` |

Examples:
- `result=pass; reason=none; note=skipped, direct hit`
- `result=not_found; reason=no match; hit=no; passages=0`
- `result=pass; reason=none; model_called=yes; model=phi4-mini; model_ms=4210; passages=3; source=Bylaws 4.1`
- `result=escalate; reason=model call failed; error=timeout; model_called=yes; model=phi4-mini; model_ms=30000`
- `result=stop; reason=out of scope`
- `result=escalate; reason=ai check failed: source not retrieved`
- `result=pass; reason=none; ticket=TKT-0001`

A direct hit logs `model_called=no` on `complex_ai` (skipped), so the demo can show that simple questions never use the model.

## 5. Trace ID

- Created by the front end the moment the user presses send, before anything else happens.
- Travels with the question through every stage and appears on every event.
- Shown to the user only on the escalation screen, as the "Reference #".

## 6. Live stage updates (decided 2026-10-20, task T2W)

**Decision: the orchestrator reports.** The chat never asks again and again. The orchestrator tells the chat as each stage starts and as each stage finishes. The chat calls the pipeline in the same process (the app is Blazor Server), so no new web call, polling loop or connection is needed. This builds on the `onStep` callback that is already in `Orchestrator.RunAsync`.

**Why not asking repeatedly:** polling would need a second web call and a timer, would show stages late and can miss a short one (the stubs finish in under a millisecond), and the order the stages ran would be guessed by the chat instead of reported.

**The message** (one record, `StageProgress`; the callback becomes `Func<StageProgress, Task>`):

| Field | Type | Notes |
|---|---|---|
| `trace_id` | text | The run this belongs to |
| `seq` | whole number | 1, 2, 3 ... in the order sent in this run |
| `phase` | `started` / `finished` / `done` | See below |
| `stage` | text | A stage name from section 1. Empty on `done` |
| `step` | work step or empty | On `finished`: the same entry added to `work_steps` (section 2). Empty on `started` and `done` |
| `status` | `answered` / `stopped` / `escalated` or empty | Only on `done` |

```json
{ "traceId": "3f2b8c1e-...", "seq": 7, "phase": "started",  "stage": "complex_ai" }
{ "traceId": "3f2b8c1e-...", "seq": 8, "phase": "finished", "stage": "complex_ai",
  "step": { "stage": "complex_ai", "outcome": "pass", "skipped": false, "reason": null,
            "error": null, "dataText": "model_called=yes; model=phi4-mini; model_ms=4210; passages=3; source=Bylaws 4.1",
            "elapsedMs": 4215 } }
{ "traceId": "3f2b8c1e-...", "seq": 15, "phase": "done", "status": "answered" }
```

**Rules**
- A stage that really runs sends `started`, then `finished`. The chat shows the `started` stage as the status message. When `finished` arrives, the step moves into the panel.
- A stage that is skipped (section 3) sends only `finished`, with `skipped = true`. There is no `started`, because nothing was running.
- After a Stop, nothing more is sent except `done` with `status = stopped`.
- After an Escalate, the skipped stages send `finished` with `skipped = true`; `ticketing` sends `started` and `finished`; then `done` with `status = escalated`.
- Every run ends with exactly one `done`. If a run throws before `done`, the chat shows the error state.
- The data in `step` is the same text the event log gets. It never contains the question text or the model's own reasoning.
- The minimum display time per stage (task T9W) is applied by the chat, not the orchestrator. The run and the event log are never slowed down.
- The `/api/ask` call (section 7) does not stream. It returns the finished response as before, and `test-paths.ps1` keeps using it.

## 7. API

`POST /api/ask`

Header: `Authorization: Bearer <sign-in token>`

Request:
```json
{ "question": "What is the deadline to file a reimbursement request?",
  "traceId": "3f2b8c1e-5d4a-4a7e-9b1c-0d2e6f7a8b90" }
```

Response (always the same shape):
```json
{ "status": "answered",
  "message": "Based on the approved content: ...",
  "sources": [ { "document": "Reimbursement Policy", "section": "3.2", "page": 4 } ],
  "ticketReference": null,
  "traceId": "3f2b8c1e-5d4a-4a7e-9b1c-0d2e6f7a8b90" }
```

| `status` | `message` | `sources` | `ticketReference` |
|---|---|---|---|
| `answered` | The answer | One or more | `null` |
| `stopped` | A polite stop message | empty | `null` |
| `escalated` | A "we've sent this to a person" message | empty | `TKT-0001` style |

Errors: `401` for a missing or bad token (the front end sends the user back to sign-in). `403` for a signed-in account with no role that allows chat. `400` for a missing question or trace ID.

## 8. Output compliance: two sections (stub)

Every answer passes through `output_compliance`, including direct hits. Which section runs depends on where the answer came from. `output_section` and the log show which one.

**Database side (direct hit)**
- The answer has a source (`cited_source`), and the source is an approved passage.
- No source, or a source that is not approved: Escalate, `reason=answer has no approved source`.

**AI side (answer written by the model)** does everything the database side does, plus:

| Check | Real or stub | Result shape | Pass rule |
|---|---|---|---|
| Source check | Real | `{ cited: {document, section, page}, retrieved: true/false }` | The passage the model named must be one of the passages `rag` retrieved. A source that was not retrieved: Escalate, `reason=ai check failed: source not retrieved` |
| NLI check (does the cited passage support the answer) | Stub | `{ verdict: "supported" / "not_supported" / "unclear", reason: text }` | `supported` passes. The stub returns `supported` with reason `stub: fixed pass` |
| Rule recheck (the output is checked again against the rules) | Stub | `{ passed: true/false, rule: text or empty, reason: text }` | `passed = true`. The stub returns passed |
| Confidence score | Stub | `{ score: 0.00 to 1.00, threshold: 0.70, passed: true/false }` | `score >= threshold`. The stub returns `0.90` |

A failure on any AI-side check returns Escalate with `reason=ai check failed: <check name>`, and the run goes to `ticketing`. Result shapes are fixed now so the real checks can be added later without changing the contract.

**Forcing a failure (Should):** a setting in the dev dashboard forces a chosen AI-side check to fail, so the demo and the tests can show a bad response being caught.

## 9. Errors

A **real error** is different from a Stop and from a NotFound. It is shown red in the panel. Today the one real error is a failed model call.

- If the model call fails (turned off, unreachable, empty reply) or takes longer than the time limit, `complex_ai` returns Escalate with `reason=model call failed` and `error=unavailable` or `error=timeout` or `error=empty` in the data text.
- The orchestrator copies the value into `work_steps[].error`, and the panel shows the step red.
- The run then goes to `ticketing`. A model that is turned off produces an error and a ticket, never a hang.
- The time limit for a model call is set after Eliyas measures phi4-mini on the 16 GB machine; until then it is 30 seconds.

## 10. Stub rules

- A stub returns the right shape every time. It does not have to be smart.
- A stub returns its result and its data text. The orchestrator writes the event.
- Fake means fake: invented passwords, accounts, and content only.
- For this milestone the stubs are: `rag` (with `knowledge_graph` inside it), `ticketing` (the ticket is created, not sent to the connected service), `input_compliance`, and `output_compliance` (the AI-side checks except the source check). Sign-in, roles, full-text search, the AI model, the event log and the panel are real.

## 11. The five test paths (every stub must support all five)

| # | Question | Expected result |
|---|---|---|
| 1 | "How many board meetings must a chapter hold each year?" | Direct hit. `answered`, with source. `knowledge_graph`, `rag` and `complex_ai` skipped. `output_compliance` runs, database side |
| 2 | "Who writes down what the board decided?" | `database_search` returns `NotFound`. `rag` hands over passages. `complex_ai` calls the real model and names a retrieved passage. `output_compliance` runs, AI side. `answered` |
| 3 | "Where's a good pizza place near the conference?" | `input_compliance` returns Stop. Nothing after it. `stopped` |
| 4 | "What is the cryptocurrency policy?" | No content found. Escalate. `ticketing` returns a TKT reference. `escalated` |
| 5 | "A board member keeps harassing me, what should I do?" | `input_compliance` returns Escalate (sensitive). `ticketing`. `escalated` |

Extra checks: one non-English question and one English question (translation is skipped for English only), a source not in the retrieved passages escalates, and a stopped model (turned off) gives an error and a ticket.

## 12. Open questions for Eliyas (version 2)

- [ ] `NotFound` as a fourth result: OK? (needs a new member in `StageOutcome`)
- [ ] The skip rules (section 3): the log keeps every stage, the panel hides skipped ones except translation for English: OK?
- [ ] `knowledge_graph` stays a back-end stage name but is not its own step in the panel: OK?
- [ ] Time limit for the model call: 30 seconds until measured?
- [ ] The result shapes in section 8: OK?
- [ ] Live updates (section 6): the callback changes from `Func<WorkStep, Task>` to `Func<StageProgress, Task>`, and the orchestrator also sends `started` and `done`. Chat side updated by Willis.

Version 1 questions (stage names and order, query context fields, data text shape, API, file location) were closed 2026-10-16, confirmed verbally.
