# Claude Code Hand-off: where the Isa build stands (written Wed 21 Oct 2026)

For a Claude Code session opened in this repo by **Willis Pearson** (Project Lead). It was written by the Claude chat session Willis has been working with, so you start with the same picture. Read it fully before touching anything. Where this file and the code disagree, the code wins: check, then tell Willis.

## 1. The project in four lines

- **Isa** is a test build of an AI chat agent that helps chapter leaders of **"The Client"** (always call ISACA "The Client" in documents and code comments) run their chapters. Team: **Second Star Collective** (internship project).
- It is an **isolated test build on a fixed set of 81 approved passages**. It is not a real product. Do not frame questions or tasks as launch requirements, and do not suggest documents beyond the 81 unless Willis asks.
- Stack: Blazor Server (.NET 10), SQLite (events, content, directory, onboarding, progress, notebook, usage), Keycloak (Docker) for sign-in, Ollama for models (phi4-mini planned for the AI step).
- **Current milestone: "Pipeline Built", due Fri 23 Oct 2026.** Plans are in `../Pipeline/` (`Pipeline_Built_Task_List.md`, `Task_List_Willis.md`, `Task_List_Matt.md`, `Task_List_Eliyas.md`). Willis works his list one task at a time.

## 2. Rules you must follow (from Willis, and already in CLAUDE.md)

1. **Never `git commit`, `push`, `merge` or anything that creates commits or moves a branch.** Willis commits from his terminal. Read-only git (status, diff, log, fetch) is fine. After a change, make sure it builds, then tell Willis what to commit and give a suggested commit message. Willis is currently on the **`Backup`** branch.
2. **Never read, print, type or store credentials**: `keycloak\dev-passwords.local.csv`, `dev-keys.json`, tokens, passwords. Use placeholders. Compare programmatically if you must. Test scripts read the password file themselves.
3. **CLAUDE.md in this repo was written for Matthew's session** and tells you to maintain `CHANGES-SINCE-LAST-COMMIT.md`. That file is Matthew's running list, so appending to it from Willis's branch will cause merge conflicts. **Recommendation: log Willis's changes in a new `CHANGES-WILLIS.md` instead**, and ask Willis once to confirm.
4. Matt's onboarding work (`ONBOARDING-FEATURE.md`, `Services/Onboarding/*`, journey, notebook, Today panel) is the **baseline**. Improve it only as additions to his work, never instead of it.
5. Do not list **A'keyrah** as a team member (she left). Code comments that mention her schema are history only.
6. Style Willis wants: answer, then build on confirm for big jobs; **give a recommendation, not a menu of options**; keep wording short when he says "smaller"; say **"restart Isa"** (the launcher rebuilds), never "rebuild"; deliverables as files he can use (.docx, .md, .txt, .xlsx), not in-interface docs.
7. Never put survey figures or study details in code, comments or sample data. Use invented sample people only.
8. Do not run `docker compose down -v` or the Keycloak reset scripts without asking. They wipe sign-in data.

## 3. People and ownership

| Person | Owns |
|---|---|
| **Willis** | Front end, connections between the chat and the back end, back-end testing, and the stubs for `complex_ai`, `output_compliance` and `ticketing`. Content Health logging and dashboard sections. |
| **Matt** (Matthew, `mbrady-SSC`) | Onboarding feature, wording table and panel styles, presentation. Waiting on his final wording file (task T11W) and his yes on the "Questions Are Screened" / "Answers Are Checked" work split (T8W). |
| **Eliyas** | Back end: database search (full-text, NotFound), RAG stub (meaning-map), the real AI step (phi4-mini via Ollama), event-log model fields, the orchestrator reporting each stage. |

**Eliyas situation:** he missed today's team meeting and has sent no update. The only commits from his account (`Ella-tec`, which appears to be his) are from 15 and 18 Oct; none touch this milestone. Willis expects to see what he submits **by tomorrow (Thu 22 Oct)** and then **patch the holes** (section 7). Do not start rebuilding his pieces before checking `git fetch` and `git log origin/Eliyas-Branch`. If he has pushed, build on it.

## 4. Architecture you need

Read `docs/stage-contract.md` (version 2) first. Summary:

- `Services/Orchestrator/Orchestrator.cs` runs stages **in registration order** (`Program.cs`): `access_control`, `translation`, `input_compliance`, `database_search`, `knowledge_graph`, `rag`, `complex_ai`, `output_compliance`, `ticketing`. `front_end` is logged by the orchestrator itself (it also runs `LanguageCheck.Detect`). **Ticketing runs only after an Escalate.** After a Stop, nothing else runs.
- Stages return `StageResult(Outcome Pass|Stop|Escalate, Reason, DataText, Skipped)`. **Stages never write to the event log; only the Orchestrator does**, one event per stage, in the format `result=...; reason=...; extras`.
- **The question text is never logged.**
- Shared state is `QueryContext` (`Services/Orchestrator/StageModels.cs`): `Language`, `Passages`, `DirectHit`, `CitedSource`, `OutputSection` ("database" or "ai"), `SafetyTopic`, and the hooks `PromptTokens`, `CompletionTokens`, `SearchScore` (nothing sets these yet).
- Stubs: `StubStages.cs` (access/translation/input compliance, database_search, knowledge_graph, rag) and `ResponseStages.cs` (`ComplexAiStage`, `OutputComplianceStage`, `TicketingStage`). `OutputChecks.cs` holds the AI-side checks (source check is real; NLI, rule recheck and confidence are stubs returning the right shape). Config `OutputCompliance:ForceFail` = `source|nli|rule|confidence` forces a failure for testing.
- Input compliance stub: "pizza", "ignore your rules", "show me every document" -> Stop; "harass" -> Escalate with `SafetyTopic`.
- **All leader-facing wording lives in `Services/ShowTheWork/Wording.cs`.** Change strings there only. Matt's final wording file will be diffed against it.
- Show the work: `Components/Shared/ShowTheWork.razor`, `Services/ShowTheWork/*` (`PacedStageFeed`, `StageFeed`, `WorkStepText`). Live mode is built against a made-up feed. In `Components/Layout/ChatWidget.razor`, `RunPipeline` adapts the finished run into a feed. **When the orchestrator emits real `StageProgress` (started/finished/done), replace the adapter with `Pipeline.RunAsync(ctx, feed.Post)`** (a comment marks the spot).

## 5. How to run and test

- Start/rebuild: `isa-launcher.ps1` / `start-isa.bat` (the launcher rebuilds). Stop: `stop-isa.bat`. You may run `dotnet build` yourself to catch errors. Windows PowerShell 5.1 quirks apply (`Invoke-RestMethod` returns a nested array; curl.exe eats backslash-escaped quotes).
- `test-paths.ps1` runs the contract's test paths (flags: `-NoToken`, `-SkipSignIn`, `-ModelOff`, `-ForcedFailure`). An Excel plan of every test question, goal and intended outcome was delivered to Willis as `Isa_Test_Questions.xlsx` (not in the repo).
- The five test questions: direct hit ("How many board meetings must a chapter hold each year?"), model path ("Who writes down what the board decided?"), stopped ("Where's a good pizza place near the conference?"), escalated no-content ("What is the cryptocurrency policy?"), escalated sensitive ("A board member keeps harassing me, what should I do?"), plus a Spanish and an English check.
- Sign-in: Keycloak realm is imported **only on a fresh volume**. `KeycloakSignInService` keeps tokens **in memory per session**, so an app restart means sign in again. `AccessControl:DevBypass` is true in `appsettings.Development.json`, false in `appsettings.json`; override with env var `AccessControl__DevBypass=false`. Accounts (names only): `k.watanabe` (the **demo account** for multilingual features), `staff.dev` (has the `dashboard_dev` client role), `d.whitfield`, `norole.test` (expects 403). No token expects 401.
- Models: `ChatWidget.razor` knows two Ollama hosts (a remote tailnet host and `localhost:11434`). Eliyas's plan is phi4-mini; timing on 16 GB with Postgres, Keycloak and the app up is still unmeasured.

## 6. Status of Willis's milestone tasks (from `Task_List_Willis.md`)

**Done**
- Contract changes written (NotFound result, stage order, language field, skip rules, event fields, two-section output compliance, AI-check shapes, error state) and stage-reporting decision made. Contract marked changed (v2).
- Panel built against the made-up feed, all five questions covered; `LanguageCheck` at the front end (English skips translation); dev dashboard Testing section (`Components/Shared/DevTesting.razor`, limited to `dashboard_dev`); sign-in retest (401, 403 and a working account verified); `test-paths.ps1` rewritten (old copy `test-paths.v1.ps1.bak`); minimum display time setting (`MinStageMs`); user-safe/debug panel toggle; hand-off notes to Matt and Eliyas sent and merged.
- Output compliance two sections plus forced-failure setting; ticketing hand-off wording.
- Wording centralised in `Wording.cs` (task T11W). **Still waiting on Matt's final wording file**, then diff.

**Waiting**
- **Connect the chat to Eliyas's live stage updates** (swap the adapter, section 4).
- **Run updated `test-paths.ps1` and the sign-in test once Eliyas says the back end is ready; tell him what fails; pass or fail the milestone.**
- Mark contract changes confirmed; run the whole demo once with Matt.
- Matt's yes on the screening/checking split (hand-off md: `docs/Handoff_to_Matt_Refusal_Rules_Decisions.md`).

**Last known test-paths result (before Eliyas's work):** 3 of 9 failed, all on his side (section 7).

## 7. Eliyas's open work: the holes to patch

From `Task_List_Eliyas.md`. Check what he actually pushed first. Whatever is still missing, in order of impact:

1. **`database_search` returns a proper NotFound** (not an error, not an empty pass) when full-text finds nothing. Today the meaning-map fallback lives inside `DatabaseSearchStage` (`StubStages.cs`).
2. **Move the meaning-map stand-in out of `database_search` into the RAG stub**; the RAG stub returns stand-in passages and logs `retrieved=N`. A **direct hit skips RAG and `complex_ai` but still goes to output compliance**. `knowledge_graph` should show as skipped on a direct hit.
3. **Real `complex_ai`** (name unchanged): runs only after a miss; calls phi4-mini through Ollama with the question **plus the RAG passages, never the bare question**; no passages means no model call and the run escalates; a failed or timed-out call reports an error and escalates to a ticket the same way other escalations do; names the passage it used (`CitedSource`) in a form code can read; says so when the passage doesn't answer. Set `ctx.PromptTokens` / `ctx.CompletionTokens` from Ollama's `prompt_eval_count` / `eval_count`.
4. **Event-log extras on the AI step:** `model_called`, `model` (name), `model_ms`, passages retrieved, `source=`. test-paths expects `model_called/model_ms/source=`.
5. **Orchestrator emits `StageProgress`** (started, finished, done) for the live panel. It already has an `onStep` callback and `StageProgress` exists in `StageModels.cs`.
6. Timing numbers and the time limit for the model call, handed to Willis (and Matt for demo pacing).
7. Set `ctx.SearchScore` in the real search for the dashboard.

Already on our side, so do not redo: stage order (translation before input compliance), two-section output compliance, source check, forced-failure setting.

## 8. Content Health Dashboard and logging (built 21 Oct; committed in `96009e4` on Backup as far as git shows)

Written in the chat session **without a compile**, so first confirm `dotnet build` is clean and the dashboard page loads.

- `Services/UsageSummaryService.cs`: writes `data/usage.db`. `request_summary` (one row per question, written by the Orchestrator at the end of a run: outcome, route direct|ai|none|blocked, stopped_by, reason, retrieved_count, cited, category, salted question fingerprint, confidence, tokens, ticket fields), `retrieval` (one row per passage found), `feedback` (resolved|not_quite|talk_to_person), `content_conflicts`. Never stores question text; people appear only as a salted hash (the salt is random per start, kept in memory only, never stored or configurable, so repeats are counted within one run only). Failures are swallowed so logging never breaks an answer. Hooks: `CloseTicketAsync`, `FlagConflictAsync`, the three `QueryContext` token/score fields.
- `Services/UsageReportService.cs`: read-only report snapshot. `Components/Shared/ContentHealthSections.razor` (+ `CountBars.razor`) render SUPPORT / DATA / TRAINING on `/content-health-dashboard` (Executive or Manager role). Metrics that cannot be measured yet show as grey "Not collecting yet" tiles. Totals and rates only.
- `Components/Shared/AnswerFeedback.razor`: "Yes, this answered my question" / "Not quite" buttons under answered questions.
- `data/content_categories.csv`: document -> Governance / Operations / Programs / Products.
- Open points: the question fingerprint should be cleared with Matt (the event log promises no question text); region is empty (the directory has none); content age needs a last-updated date; "Talk to a person" in the chat footer is an unwired placeholder; question clusters wait on real embeddings.

## 9. The Client's answers (received 21 Oct) that affect the build

- AGM timing and when officers take office vary by chapter bylaws; the compliance window does not dictate term start. Supports counting journey days from term start with the compliance window as an alert only when inside the 90 days (Matt's decision).
- **Most chapters have no named Chapter Experience contact.** About 65 have a Chapter Success Manager (chosen by a risk profile that **must never appear in Isa**); about 165 are supported by the Chapter Experience Operations Team as needed. Matt's contact step assumes one named contact and needs rework.
- New-leader system access takes about **10 business days** after the chapter submits a leader roster through the Support ticketing system (affects step J-20).
- Presidents may see everything on the member roster (sample header coming separately).
- For The Client's own process and policy, answer from their documents only; for broader governance questions they want best practice from ASAE, BoardSource, BoardEffect and Association Forum. This conflicts with "approved content only" and needs a team decision before any build.
- Guidebooks for the other board roles and the Chapter Affiliation Agreement exist but are not in the 81-passage set. Whether to add them is a team decision.

## 10. Known issues and gotchas

- **"Not signed in" in the chat right after signing in:** a hard refresh (Ctrl+F5) fixes it. Cause unproven (stale interactive connection with old anonymous state). Mitigation added: `data-enhance-nav="false"` on both Sign in links in `Home.razor`. Not yet verified. Demo checklist: sign in after the last app restart, Ctrl+F5 once, ask one test question first.
- Pages with buttons need `@rendermode InteractiveServer`. Inside `RenderFragment` lambdas use plain `if`, not `@if`. Scoped CSS lives in `*.razor.css`.
- `/api/events/{traceId}` has no auth (known, test build).
- `.gitignore` leaves out `data/*.db`; `data/progress.db` deletion resets journey progress, `data/usage.db` deletion resets the dashboard.
- `bin/`, `publish/`, `.bak` files and `keycloak.zip` are clutter, not part of the design.

## 11. Suggested first steps for you

1. `git status`, `git log -5`, `git fetch`, then `git log origin/Eliyas-Branch -5`. Report to Willis what Eliyas pushed.
2. `dotnet build`. Fix compile errors in the Content Health files if any.
3. Read `docs/stage-contract.md`, `Services/Orchestrator/Orchestrator.cs`, `StubStages.cs`, `ResponseStages.cs`.
4. Start Isa, run `test-paths.ps1`, and show Willis the pass/fail list before changing anything.
5. Patch Eliyas's gaps in the order in section 7, one at a time, building and re-running the paths after each. Tell Willis what to commit.

### Eliyas: complex_ai must catch model failures itself
The Orchestrator turns any exception thrown inside a stage into `Stop` (plain message, no ticket). A failed model call (unavailable, timeout, empty) must instead be caught inside complex_ai and returned as `Escalate` with `reason=model call failed; error=unavailable|timeout|empty` (stage contract section 9), so the leader gets a ticket. Test: `test-paths.ps1 -ModelOff` expects escalated + ticket.

### Adding content
`python content\import_content.py <file or folder>` (launcher: Content database group) reads .docx/.pptx/.pdf, appends to content/content.json and rebuilds data/content.db. Stop Isa first. New documents land in data/content_categories.csv as "Uncategorized"; set a category. This grows the fixed 81-passage test set, so the test questions workbook and counts change.

### Content import in the app
Dev Dashboard > Content import (dashboard_dev role). `Services/ContentImportService.cs` reads .docx/.pptx/.pdf (PdfPig package for PDFs), adds passages to the LIVE data/content.db and to content/content.json, and lists the document in data/content_categories.csv. Written without a compile; check the build first. Same chunking rule as content/import_content.py.

### Update 22 Oct: Eliyas's AI step ported (written without a compile)
Eliyas's package (`ISA_Pipeline_Built_VSCode`, no git history, never built) was ported onto this code, not copied over it. Done: `Services/Orchestrator/OllamaChatService.cs`; the real `complex_ai` (question + numbered passages, model names `SOURCE: PASSAGE n`, failed/timeout/empty call = Escalate `model call failed` with `error=`, tokens set); `database_search` returns `NotFound` (new `StageOutcome.NotFound`, tag `not_found`), full-text only, search score set, real DB errors no longer swallowed; the meaning-map stand-in moved into `rag` (real passages from content.db; none = Escalate); `knowledge_graph` skipped on a direct hit; event data uses the contract names (`model_called=yes`, `model_ms=`, `source=`, `retrieved=`). His output-compliance and PipelineMonitor were NOT taken (ours is more complete / the dev dashboard already has trace lookup). Still open: model timing numbers and the chosen time limit (`test-model.ps1`, default 30 s in `Ollama:TimeoutSeconds`), prompt tuning against real phi4-mini replies, `dotnet build`, then `test-paths.ps1` (also `-ModelOff`) and `test-failures.ps1`.
