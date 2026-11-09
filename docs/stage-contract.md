# Stage contract

Project: Isa, Second Star Collective
Author: Willis Pearson (T1)
Version: 3 (updated 2026-11-09 to match the built system, Sprint 3 Milestone 1)
Status: DRAFT for Willis's written approval. Version 3 records how the system works now; the stage names, order and context fields are unchanged from version 2. Version 2 is kept as `docs/stage-contract.v2.md`, version 1 as `docs/stage-contract.v1.md`.
Approved on: pending (Willis)

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

## 0a. What changed in version 3 (Sprint 3 Milestone 1)

Nothing here changes a stage name, the stage order or the query context. It records what was built since version 2.

| # | Change | Where |
|---|---|---|
| 1 | `input_compliance` is real: the rules in `rules\*.json` decide answer, pass to a person, or stop. The data text names the rule that fired. | section 1, 4 |
| 2 | `translation` is real: NLLB-200, self-hosted, Amharic and Spanish in and out. A failed or unsupported translation keeps the original text and says so in the data text. | section 1, 4 |
| 3 | `rag` is real: the meaning map first, then vector search over the approved ISACA passages (multilingual-e5-base). A miss escalates. | section 1, 4 |
| 4 | `knowledge_graph` stays a pass-through by decision (no graph database). | section 1 |
| 5 | `output_compliance` AI side is real: source check, rule recheck, answer shape, NLI (HHEM, per sentence), numbers/strength/contact, confidence. On a failure Isa trims, uses ISACA's exact words, or escalates. | section 8 |
| 6 | `ticketing` creates a real Salesforce Case (record type, `Type` field) when `Ticketing:Salesforce:Enabled` is on. | section 1, 4 |
| 7 | Model keep-alive: the model stays loaded `Ollama:IdleKeepAlive` (10 minutes) after a question. | section 9 |
| 8 | The test paths are real questions through the real system, with no forced failures, no bypasses and no stand-in answers. | section 11 |

---

## 1. Stage order

| # | Stage name (exact, used in the event log) | Owner | Runs |
|---|---|---|---|
| 1 | `front_end` | Willis | always. Also sets `language` (quick check) before the query enters the system |
| 2 | `access_control` | Eliyas | always |
| 3 | `translation` | Willis and Matt | always runs; returns Pass `skipped` when `language` is `en`. Real (NLLB-200, Amharic and Spanish). It is always BEFORE `input_compliance`, so input compliance screens English |
| 4 | `input_compliance` | Willis and Matt | always. Real: the rules in `rules\*.json`. A Stop ends the run; nothing after it runs or is shown. An Escalate goes to `ticketing` |
| 5 | `database_search` | Eliyas | always. Full-text only, no AI. Found = direct hit. Not found = `NotFound`, then the run goes on to stage 6 |
| 6 | `knowledge_graph` | Eliyas | only on a miss (Pass `skipped, direct hit` otherwise). Decision for Milestone 1: no graph database, a pass-through inside the RAG step, not its own step in the panel |
| 7 | `rag` | Eliyas | only on a miss (Pass `skipped, direct hit` otherwise). Real: the meaning map first, then vector search over the approved ISACA passages. No passages = Escalate |
| 8 | `complex_ai` | Eliyas | only on a miss (Pass `skipped, direct hit` otherwise). The real model (phi4-mini through Ollama). Always sent the question WITH the passages, never the bare question |
| 9 | `output_compliance` | Willis | always, including direct hits. Two sections (section 8) |
| 10 | `ticketing` | Willis | only on Escalate. Creates the TKT reference and, when Salesforce is on, a Case in Salesforce |

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
| `translation` | `from=<code>; note=skipped, english` when skipped; otherwise `translated=yes/no; model_ms=<time>` and a `note=` when the service could not translate |
| `database_search` | `hit=yes/no; passages=<count>` |
| `rag` | `passages=<count>; retrieved=<document section, ...>; retrieval=vector; top_score=<x.xx>` or `vector_miss=<reason>` |
| `complex_ai` | `model_called=yes/no; model=<name>; model_ms=<time taken>; passages=<count>; source=<document section>` |
| `output_compliance` | `section=database/ai; source=ok/missing; approved=yes/no`, plus on the AI side `source_retrieved=yes/no; rule_recheck=pass/fail; confidence=<score>; nli=pass/fail` |
| `input_compliance` | the rule that fired: `rule=<id>` |
| `ticketing` | `ticket=TKT-0001; from=<stage>; salesforce=created/failed/off` |

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
  "message": "Here's what ISACA's guidance says: ...",
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

## 8. Output compliance: two sections

Every answer passes through `output_compliance`, including direct hits. Which section runs depends on where the answer came from. `output_section` and the log show which one.

**Database side (direct hit)**
- The answer has a source (`cited_source`), and the source is an approved passage.
- No source, or a source that is not approved: Escalate, `reason=answer has no approved source`.

**AI side (answer written by the model)** does everything the database side does, plus these checks, in this order. All are real.

| Check | What it does |
|---|---|
| Source check | The passage the model named must be one of the passages `rag` retrieved. If not: Escalate, `reason=ai check failed: source not retrieved` |
| Rule recheck | The written answer is checked again against the rules (`answer_must_not` phrases) |
| Answer shape | A "how many", "when" or "who" question needs a number, date or role in the answer |
| NLI (HHEM, per sentence) | Each sentence must be supported by the cited passage |
| Numbers, strength words, contact details | A number, a must/should/may word, or an email or phone number must appear in the passage |
| Confidence | Search strength and the share of supported sentences must reach `OutputCompliance:ConfidenceThreshold` |

**When a check fails:** the unsupported sentences are trimmed (event data `step_down=trimmed; removed_sentences=<n>`; the NLI line still reads `nli=pass`). If trimming cannot fix it, Isa gives ISACA's exact words (`step_down=isaca_words; step_reason=<check>`) when the search score is at or above `OutputCompliance:StrongMatch`. Otherwise it escalates to a person (`reason=ai check failed: <check name>`). The four numbers (NLI threshold, confidence threshold, strong match, search pass mark) are set on the Dev Dashboard and saved in `output-compliance-settings.json`.

**Event data on the AI side:** `source_retrieved=yes/no; rule_recheck=pass/fail; confidence=<score>; nli=pass/fail`, plus `check=answer_shape; missing=...` on a shape failure.

**Failure reasons (exact):** `ai check failed: source not retrieved`, `ai check failed: nli check`, `ai check failed: rule recheck`, `ai check failed: answer shape`, `ai check failed: confidence score`. No approved source: `answer has no approved source`. Nothing found at all: `no approved content found`.

**Forcing a failure** is a test-only setting (`OutputCompliance:ForceFail`). The test suite does not use it.

## 9. Errors

A **real error** is different from a Stop and from a NotFound. It is shown red in the panel. Today the one real error is a failed model call.

- If the model call fails (turned off, unreachable, empty reply) or takes longer than the time limit, `complex_ai` returns Escalate with `reason=model call failed` and `error=unavailable` or `error=timeout` or `error=empty` in the data text.
- The orchestrator copies the value into `work_steps[].error`, and the panel shows the step red.
- The run then goes to `ticketing`. A model that is turned off produces an error and a ticket, never a hang.
- The time limit for a model call is **45 seconds** (`Ollama:TimeoutSeconds`), set from timing on 22 Oct 2026 (the model unloaded 2 minutes after a question until 9 Nov 2026; it now stays loaded `Ollama:IdleKeepAlive`, 10 minutes, because cold starts after a pause were causing the timeouts) (phi4-mini:3.8b on a 7.8 GB machine, 0.5 to 0.8 GB free): cold start 20 to 27 s, warm answers 7 to 10 s. When the call fails, the event data also carries `detail=` (the HTTP status or error type) to help find the cause.

## 10. What is real and what is not (replaces the stub rules)

- Real: sign-in and roles, full-text search, the meaning map, vector search, translation, input compliance, the AI model, every output check, ticketing (Salesforce), the event log and the panel.
- Still simple by decision: `knowledge_graph` (a pass-through).
- Test data uses invented passwords, accounts and content only.

## 11. Test paths

`test-paths.ps1` sends real questions through the real system (signed in, no forced failures, no model-off or bypass settings). The questions and what each must show in the event log are in the script and in `test-questions-candidates.csv`. The paths covered:

| # | Path | What the log must show |
|---|---|---|
| 1 | Direct hit | answered, no model call |
| 2 | Model path, clean pass | `model_called=yes`, every check passes |
| 3 | Stopped | `input_compliance` stop, no ticket |
| 4 | Escalated, no content | nothing found by search; ticket |
| 5 | Escalated, sensitive | a rule routes it to a person; ticket |
| 6 | Non-English | detected, translated, answered, translated back |
| 8 | Trimmed answer | `step_down=trimmed; removed_sentences>=1` |
| 9 | ISACA's exact words | `step_down=isaca_words` |
| 10 to 13 | Source, rule recheck, answer shape, contact failures | the check named in the data text |
| 14 | Escalated after a failed check | weak match and `OutputCompliance:StrongMatch` at 0.85 |
| 15 | Input rule stop or hand-off | `rule=<id>` |
| 16 | AI-stage escalation | `complex_ai` `result=escalate`, no check failing |

## 12. For Willis's approval (version 3)

- [ ] Section 1 and 10: the descriptions of the real stages match what you want recorded.
- [ ] Section 8: the output compliance ladder as built is the contract.
- [ ] Section 9: model keep-alive of 10 minutes.
- [ ] Section 11: the test paths as listed.

Versions 1 and 2 questions are closed.
