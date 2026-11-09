# Changes since the last commit

A running list of every change made since Matthew's last commit (November 2, 2026: e3677f6, "hand-off to a person, some output compliance notes", pushed to Matthew's-Branch: "Talk to a person" and "Not quite" wired up, the promise inventory, and the role filter, output compliance and promise notes for Willis). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **The Chapter Affiliation Agreement in the Library** (November 3, Sprint 3 M4, Matthew's choice): a new "Your chapter and ISACA" group with ISACA's 2024 template, to read and listen to word for word, with the note that Isa doesn't answer questions from it yet (adding it to her approved content is a decision with Willis).
  - **onboarding/build_affiliation_agreement.py** (new): turns content/Chapter Affiliation Agreement-2024-template.docx into **onboarding/library_texts/affiliation-agreement.json** (new). Keeps ISACA's words and the template's blanks; puts back the clause numbers and letters Word shows, checked against the agreement's own cross-references; leaves out the address and signature tables and the header and footer. Run it again if ISACA sends a new version.
  - **Services/Onboarding/LibraryService.cs**: a new kind of Library document, "source": "text", read from onboarding/library_texts/.
  - **onboarding/library.json**: the new entry and a comment explaining "text".
  - **ONBOARDING-FEATURE.md**, Feature 5: a dated note on all of the above.
  - Checked on screen as Watanabe Kenji: the Library lists it (about 57 minutes), it opens and plays, and the 16 clauses and four exhibits show as headings. Willis's voice warm-up will also pre-make its audio, once, in the background.
  - A line with it, in the list and at the top of the document: it is ISACA's template, a chapter's signed agreement may differ (Exhibit D), and questions about a chapter's own agreement go to the Chapter Experience team. Shown, never read aloud (library.json "note"; LibraryPanel.razor and .css).
- **ISA-PROJECT-CONTEXT.md**: a question for the November 4 update to ISACA: may the Library include the Affiliation Agreement template? Worded about the Library only.

- **Listen on journey steps** (November 3, Sprint 3 M4): each step that cites an ISACA document in the Library has a "Listen to the source" box, one button per place it cites, with about how long it takes. It opens the Library beside Today, reads that guidebook page (or the whole document) word for word, and stops at the end of it; play carries on from there.
  - **onboarding/step_listen.json** (new): which step points to which document and page (23 buttons on 18 steps; none on steps from Isa's own onboarding plan, so the rehearsed day-3 screens don't change).
  - **Services/Onboarding/LibraryService.cs**: GetStepListens and GetRange; each guidebook section now knows its page.
  - **Components/Shared/TodayPanel.razor** (+ .css): the Listen box on a step.
  - **Components/Shared/LibraryPanel.razor** (+ .css, .js): opens at the part a step asks for, shows "From your journey step: ...", and stops at its end.
  - **Components/Layout/ChatWidget.razor**: passes the step's request to the Library and opens it.
  - Checked on screen as Ana Beatriz Souza (day 60): J-15's button opens the guidebook at page 14 and reads it; stopping at the end of a page checked on page 8. Build: 0 warnings, 0 errors.

- **"Ask the outgoing president"** (November 3, Sprint 3 M4): the president asks the president before them a question in the succession notebook; it waits there (nothing is emailed) until the former president opens Isa and answers. The question and answer stay in the notebook for the presidents after them.
  - Who: open while the former president is in the 30-day handoff window or holds any board role in the chapter. Only the president who asked can share it with the board (Matthew's rule, November 3); the former president answers but never shares, and sees only the questions asked of them. No earlier president to ask: the button is "Ask the Chapter Experience team", which opens the "Talk to a person" card.
  - Both the question and the answer pass the sign-in check and Input Compliance before they are saved (purposes notebook_question and notebook_answer; the event log records their length, not their words).
  - **Services/Onboarding/NotebookService.cs**: questions as a new kind of entry; GetAskTarget, AskAsync, AnswerAsync, GetWaitingQuestions; six new columns added to data/notebook.db without touching saved entries; a former president also sees the questions asked of them.
  - **Services/Onboarding/LeaderTermService.cs**: GetPreviousPresident.
  - **Components/Shared/NotebookPanel.razor** (+ .css): "Ask [name]" beside "Add a note", "Ask [name] about this" on entries, the question box, "Waiting for your answer", the answer box, a "Questions" filter, and the matching privacy and sharing lines. No print or email for questions.
  - **Components/Layout/ChatWidget.razor** (+ .css): a line at the top of a former president's chat when a question waits for them; the notebook's "Ask the Chapter Experience team" opens the "Talk to a person" card.
  - **ONBOARDING-FEATURE.md**: what was built, and cut list item 9 marked built.
  - Checked on screen in a separate test copy (port 5068, throwaway copies of the notebook, progress and event log, Salesforce off), so nothing was added to the real notebook: Watanabe asked about the online-elections decision; Sato saw the line in her chat and the waiting card, answered, and the line went away; Ana got the Chapter Experience team button and the card opened (nothing sent). Build: 0 warnings, 0 errors.
  - Demo note: Watanabe's notebook now shows "Ask Sato Yumiko" beside "Add a note", and "Ask Sato Yumiko about this" on each entry.

- **onboarding/help.json** ("How to use Isa"), so the guide matches what Isa does: "Your first 90 days" mentions the Listen button on steps; "Saving decisions and notes" explains asking the president before you (seen next time they open Isa, nothing emailed, the answer stays for later presidents, only you can share it); "What happens to your information" says such a question is seen by the two of you. Checked in the Help panel as Watanabe.

- **W3, answer wording (Willis's task, done):** direct answers now return a whole thought in ISACA's own words: lines the PDF broke mid-sentence are joined, a bullet gets its lead-in, a sentence that starts mid-thought gets the one before it, and a quote that is not a whole thought falls back to the whole bullet, then the whole passage. No words change. The rehearsed demo questions keep the old behaviour (`AnswerWording:ExemptQuestions` in appsettings.json); `AnswerWording:Enabled` false turns the whole change off. Files: Services/Orchestrator/Stages/AnswerExtract.cs, StubStages.cs (passes the config in), appsettings.json.

- **W2, NLI check (Willis's task, in progress):** output compliance's NLI check is now real: Vectara HHEM-2.1-Open (ONNX, Apache-2.0) scores each sentence of an AI answer against its cited passage, in Isa, with ONNX Runtime. New: Services/Orchestrator/Stages/HhemScorer.cs; changed: OutputChecks.cs, ResponseStages.cs (output compliance), Program.cs, IsaChatApp.csproj (Microsoft.ML.OnnxRuntime 1.23.2; the flan-t5 tokenizer is our own Services/Orchestrator/Stages/UnigramTokenizer.cs because Microsoft.ML.Tokenizers could not read spiece.model), appsettings.json (Hhem and NliThreshold settings), .gitignore (data/models/). The two model files are downloaded once on each computer: see docs/HHEM_Setup.md. Without them the check is skipped and logged as skipped. Not yet built or run: the package versions and the tokenizer are unchecked.

- **Dev Dashboard, Output Compliance settings (Willis's task):** the Compliance policy page now has a real **Answer-support (NLI) pass mark** slider (default 0.50, the cutoff below which an AI answer sentence fails) and the existing **Answer confidence threshold** slider is real too (default 0.70). **Save changes** writes them to output-compliance-settings.json (git-ignored); **Reset to defaults** deletes it and goes back to appsettings.json. The output compliance stage reads the values on every answer, so a change applies to the next question without restarting Isa. New: Services/OutputComplianceSettings.cs; changed: ResponseStages.cs, Program.cs, Components/Pages/DeveloperDashboard.razor, .gitignore. Not built or run yet. The other controls on that page are still illustrative.

- **Show the work, NLI score:** the Answer check step now shows the lowest sentence score and the pass mark (for example "Support score 0.61 (pass mark 0.50)") for AI-written answers, whether the check passed or sent the answer to a person. Only shown when the HHEM model actually scored the answer (not skipped, not a rehearsed demo question). Files: OutputChecks.cs, ResponseStages.cs, Services/ShowTheWork/WorkStepText.cs, Wording.cs.

- **W1, rule engine (Willis's task, built, not yet run):** Isa now reads ISACA's rules files (rules/, copied from Matthew's branch: global.json, region/europe.json, country/japan.json, tests/rehearsed_demo.json, check_rules.py, plus docs/Isa_Rules_Design.md and Isa_Rules_Draft.md) instead of the placeholder keywords. New: Services/Rules/RuleEngine.cs (layers global, region, country, chapter from the leader's chapter record; phrase signals with text clean-up and unless; tightens; conflict order; refuses a file with no approval record or that loosens a rule; re-reads files when they change; a self-check at start-up that runs every rule's own tests and the rehearsed demo questions and writes the result to the console); Components/Shared/RuleTester.razor (the rule tester: Rule tester section on the Developer Dashboard, same engine, read-only). Changed: InputComplianceStage in StubStages.cs (stop, hand off, and answer-then-point with the rule's point line; logs rule ID, file, version and checkpoint; Rules:Engine false switches back to the old placeholder), StageModels.cs, ResponseStages.cs (the output rule recheck is real now: answer_must_not phrases; the point line is added after the answer), Program.cs, appsettings.json. One change to Matthew's file: A-01 in rules/global.json also lists "keeps bothering" and "keep bothering", because the old placeholder handed those off (the Sprint 2 question "A colleague keeps bothering me"). Not built yet: the connect button (needs the Talk to a person hand-off from Matthew's branch), upload checks, retire and rollback.

- **W2, fixed output checks and the fallback ladder (Willis's task, built, not yet run):** an AI-written answer now also goes through: numbers (every date, number, amount and percentage must be in its passage; "Nov 1" = "1 November", "four" = 4), "must" stays "must" (word groups in content/answer_strength.json, edit the file to add a phrase), contact details (an email or phone number must be in the passage), answer shape ("how many" needs a number, "when" a date or period, "who" a role or team; a failure goes to a person), a source containing TBD (never reworded, goes to a person), and a confidence score built from search strength and the share of supported sentences (no longer a fixed 0.90). The ladder, from Matthew's proposal: Isa's wording; unsupported sentences removed (first claim sentence and at least half must survive); ISACA's exact words with the source (Show the work says "Shown in ISACA's own words"); a person. A rule failure follows the rule (hand off or stop), never a quote. The rehearsed questions keep the old fixed checks (AnswerWording:ExemptQuestions); OutputCompliance:FixedChecks false switches the new checks off. Every check writes its result to the event data (numbers, strength, contact, shape, confidence_parts, step_down, step_reason), counts and group names only. New: Services/Orchestrator/Stages/AnswerChecks.cs, content/answer_strength.json; changed: OutputChecks.cs, ResponseStages.cs, WorkStepText.cs, Wording.cs.

## Suggested commit message

```
Add Ask the outgoing president, Listen on journey steps and the Chapter Affiliation Agreement in the Library; update the help guide
```

## W4: "connect" button for answer-then-point rules
- When a rule says answer then point (`offer_handoff`), the answer now carries `OfferHandoff` (`AskResponse`), and `AnswerFeedback` shows a "Pass this to a person" button that opens Matt's card with door `answer_then_point`.
- Files: `Orchestrator.cs`, `ChatWidget.razor`, `AnswerFeedback.razor`, `Wording.cs` (`PassToPersonButton`). Not compiled or run yet.

## W7: best-practice guides back in the search order
- `DatabaseSearchStage` (StubStages.cs): search order is now ISACA's documents first, then the meaning map (rag stage), then the best-practice guides last. A guide answer is tagged `best_practice` so the existing wording and Show the work lines apply. A question that is in the meaning map never goes to the guides.
- `content/add_best_practice.py` was run (9 guides, 50 passages added to `content/content.json`) and `data/content.db` was rebuilt (131 passages, meaning map unchanged). Matt's three guide questions and the demo questions were checked against the rebuilt database by script; not yet in the running app.

## W2 follow-up: failed AI answers logged to the console
- `OutputComplianceStage` now logs, to the console only, the AI's answer and each sentence's score whenever an output check fails ("Output check failed ..."). Nothing new on screen, in Show the work or in the event data.

## E3/E1 ported to SQLite (no Postgres; decision November 3, 2026)
- Eliyas's search used Postgres with pgvector. It is now `Services/Search/VectorSearchService.cs`: vectors in `data/vectors.db` (SQLite), compared in memory, passage text read from `content.db` so only approved content comes back, clean miss (`Found=false` with a reason) on any problem. `Services/Search/EmbeddingClient.cs` calls the embedding service. Both are registered in `Program.cs`. **Not connected to the pipeline yet** (W5).
- `content/build_vectors.py` builds `data/vectors.db` from `content.db` through the embedding service. Tested against a stand-in service, not the real model.
- `ai-services/` holds Eliyas's translation and embeddings services unchanged, with a `docker-compose.yml` that has no database, and a README.
- E5 (events and dashboard totals) was not ported: Isa already has the privacy-safe store (`usage.db`, `UsageSummaryService`) and the Content Health Dashboard (`UsageReportService`), so Eliyas's Postgres version would duplicate them.
- Not compiled or run. `VectorSearchService` and `EmbeddingClient` need a restart of Isa to check they build.

## Services on Willis's desktop (digitalstorm-pc), used over Tailscale; voice and translation switches
- **Dev Dashboard > Services (this computer)** (new section in Components/Pages/DeveloperDashboard.razor): choose Local (services on this computer) or Remote (another computer's Tailscale IP or name; needs a restart), and switch the voice and translation on or off (applies at once). Shows the address of each service in use and a "Check services" button. Saved to **appsettings.Local.json** (git-ignored, this computer only); "Back to appsettings.json" deletes it. Same access as the rest of the dashboard.
  - **Services/LocalServiceSettings.cs** (new): reads and writes appsettings.Local.json, keeping anything else in it; the remote host must be an IP or a computer name. Also ServicesAtStart, what Isa started with.
  - **Program.cs**: reads appsettings.Local.json last (re-read when it changes). With Services:Location Remote, Keycloak:Authority, Ollama:Endpoint, Tts:Endpoint, Embeddings:BaseUrl and Translation:Endpoint keep their ports but point at Services:RemoteHost. Prints the choice and both switches at start.
  - **appsettings.json**: new Services (Location Local, RemoteHost 100.89.6.69), Translation (Enabled true, Endpoint) and Embeddings (BaseUrl) settings.
  - **Translation off** (TranslationStage in StubStages.cs): the step is skipped for every question ("skipped, off" in the event log; developer view: "Switched off on this computer (Translation:Enabled)", Wording.cs, WorkStepText.cs). The service is never called.
  - **Voice off** uses the existing Tts:Enabled: Isa never calls Kokoro, skips the warm-up and pre-made audio, and Listen uses the device's own voice.
  - **appsettings.Local.example.json** (new): an example of the file.
  - Checked in the running app on this PC: Local, all five services answer; switched voice and translation off and Remote 100.89.6.69, saved; after a restart the start-up line showed every address at 100.89.6.69, Check services showed Keycloak, Ollama and embeddings answering and voice and translation switched off, and a signed-in question (token from Keycloak at 100.89.6.69) passed access control with translation "skipped, off". No question in the test needed the model, so an AI-written answer over Remote was not seen. Then put back to appsettings.json. Build: 0 warnings, 0 errors.
- **appsettings.Development.json**: Keycloak also accepts sign-in tokens issued at this PC's Tailscale addresses (100.89.6.69, digitalstorm-pc, digitalstorm-pc.tail51548.ts.net), alongside the laptop's. Only matters for the Android app; web sign-in runs on the server through localhost.
- **tailscale-phone.ps1**: also forwards the voice (8880), translation (8091) and embeddings (8092), besides Isa (5067) and Keycloak (8080), so another computer's Isa can use this PC's services. It prints each address (http://100.89.6.69:...) and warns about any service that isn't running (fixed so Tailscale's own forwarder doesn't count as the service). Ollama is not forwarded: it already listens on the network itself.
- On this PC (nothing in the repo): data/content.db and data/directory.db were missing (git-ignored, never built in this clone), so every question was passed to a person; built with content/build_content_db.py (131 passages) and directory/build_directory_db.py. The voice container needed a restart after Docker's disk moved.
- Disk space on this PC (nothing in the repo): C: filled up during the translation/embeddings build. Ollama's models moved to D:\Ollama\models (user variable OLLAMA_MODELS); pip and NuGet download caches cleared. Docker's disk image (15 GB) moved to D:\Docker (Docker Desktop settings, Willis). Translation and embeddings then built and started; checked over 100.89.6.69 with a real /translate (English to Amharic, no fallback) and /embed (768 numbers). Keycloak 8080 and Ollama 11434 answer over the tailnet too. Isa (5067) is forwarded but was not started.
- Switched on (November 4): `-Mode On`. Checked over the tailnet address: Keycloak 8080 and Ollama 11434 answer; Isa, translation and embeddings forward correctly but weren't running yet.
- Set-up on this PC only (nothing in the repo changed): installed the .NET 10 SDK (10.0.403). IsaChatApp builds in Release with 0 warnings and 0 errors. The launcher in D:\Repos\IsaLauncherDbg builds too, opens, and found D:\Repos\IsaChatApp by itself. Python 3.15 is installed now (Willis), with pypdf for the content import; the rules self-check passes (61 checks, 0 problems). Docker Desktop installed (Willis).

## Launcher: one button pair per service
- New bats: start/stop-keycloak, start/stop-voice, start/stop-translation, start/stop-embeddings. Each touches only its own service (docker compose up -d / stop <service>).
- isa-launcher.ps1 and IsaLauncherDbg: Keycloak and voice are separate groups; new Helper services group (translation 8091, embeddings 8092, build search vectors); status shows both new ports.
- Not yet run: relaunch the launcher and press each pair.

## Translation hooked in (E1)
- TranslationStage now calls Translation:Endpoint /translate for Amharic (new "am" in LanguageCheck: Ethiopic script) and puts the English text in ctx.Question for the later stages. English is still skipped; other languages (es, fr...) still pass through, because the service only knows English and Amharic. Translation:Enabled false still switches it off.
- fallback=true or a service that does not answer: the question goes on as written, step says translated=no (note=fallback / service_unavailable). Never reported as translated. Question text is never logged.
- Show the work (developer detail): "Your question was translated to English" / fallback / not supported. ctx.Language stays "am" for tickets and the usage summary.
- NOT done: translating the answer back to Amharic (the stage only translates the question in), and the stage-contract extras row for translation (needs Willis written OK).
- Not yet compiled or run. Restart Isa to confirm.

## Translate the reply back (Amharic)
- New Services/Translation/TranslationClient.cs (shared by the stage and the orchestrator). Messages are split into short pieces before translating, because the model truncates long input.
- After all checks, the orchestrator translates the finished reply back into Amharic when the question was Amharic; if any piece fails the reply stays whole in English. Sources and ticket reference untouched. Logged as a second translation event: direction=out.
- Machine translation of approved wording is NOT checked by the NLI step (that runs on the English). Not yet compiled or run.

## Predetermined Amharic questions (test and presentation)
- content/translation-demo.json (empty until filled): per entry, the question as typed in Latin letters, the same question in Ethiopic script, and the reply in Latin letters. Read on each use, no restart.
- A question that matches an entry: the Ethiopic text goes to the translation service (real translation to English), the English question goes through the normal pipeline, and when it is answered the predetermined Latin reply is shown instead of the English one. A stop or hand-off stays English. Not machine-translated.
- Show the work (developer): translation step data has predetermined=yes. The reply is NOT produced by the translator; say so if asked in the presentation.
- Not yet compiled or run.
- Predetermined path also runs the English answer through the translator (English to Ethiopic) for real; the result is logged as back_translation=ok/fallback/service_unavailable with its time, but the person sees the predetermined Latin reply, not that output.

## Dev Dashboard: Translation test
- New section "Translation test" (dashboard_dev role): pick Amharic (Ethiopic script) or English, type a question, see (1) what you typed, (2) the English the service turns it into, (3) Isa answer in English through the real pipeline (status, sources, ticket, trace), (4) that answer translated back into Amharic. Plus a "Check the service" button (reachable, model loaded).
- Calls Translation:Endpoint directly, so it works against the home PC and even when Translation:Enabled is off. A run is real: event log rows, tickets on escalation.
- Not yet compiled or run.

## Spanish goes through the translation layer
- TranslationClient.ServiceCodes maps am->amh and es->spa; the stage and the reply-back step use it instead of Amharic only. Spanish questions are translated to English and the reply is translated back into Spanish (real calls, Latin letters, no predetermined text).
- Needs Eliyas to add spa to ai-services/translation/app.py (LANG map: "spa": "spa_Latn", and the eng|amh pattern on both language fields). Until then the service answers HTTP 422 and Isa passes Spanish through unchanged with note=language_not_supported_by_service.
- test-paths check 6 (Spanish) now requires translated=yes in the translation event. It fails until the service supports Spanish.
- Dev Dashboard Translation test: Spanish added to the language choice.
- Not yet compiled or run.

## Test scripts know when the services are remote
- New services-config.ps1 reads Services:Location / RemoteHost from appsettings.json and appsettings.Local.json (same rule as Program.cs). test-paths, test-failures, test-tokens, test-ask now get the Keycloak address from it (the home PC when Remote), and test-model gets the Ollama address. An explicit -Keycloak / -OllamaUrl still wins. They print "Services are Remote: using ..." when it applies.
- Not changed: test-tokens -Mode Restart / Reset (they restart or reset Keycloak in Docker on THIS computer, so they do nothing useful when Keycloak is remote); check-prereqs.ps1, reset-keycloak.ps1 and the launcher status lights still look at localhost.
- Not yet run.

## Vector search hooked into the rag stage (E3)
- RagStage: after the meaning map finds nothing, it asks VectorSearchService (embeddings on Embeddings:BaseUrl, vectors in data/vectors.db). Passages at or above VectorSearch:MinScore (ISACA documents only, top VectorSearch:TopK=3) go to complex_ai as before. A miss, or an embedding service that is down, escalates to a person exactly as before; the escalation event now adds vector_miss=<reason>. Step data: retrieval=vector; top_score=x.xx.
- MinScore default raised from 0.25 to 0.80 (multilingual-e5 scores unrelated text near 0.70, so 0.25 would match everything, including the cryptocurrency question). It is a starting point, NOT calibrated. Read each time, so an appsettings.json change applies without a restart. VectorSearch:Enabled false switches it off.
- Best-practice guides are not returned by vector search yet (their general-best-practice label is set by database_search).
- New Dev Dashboard section Vector search test: closest passages with scores for any question, and a table of best scores for the rehearsed questions to calibrate the mark.
- Not yet compiled or run.

## Test questions no longer exempt; forced accuracy and rule-recheck tests
- AnswerWording:ExemptQuestions is now empty in appsettings.json: the five rehearsed questions go through the same wording ladder, accuracy (NLI) check and fixed checks as every other question. Their results may change; test-paths expectations may need updating once re-run.
- test-paths.ps1 -ForcedCheck source|nli|rule|confidence (-ForcedFailure still means source): the forced case now expects the matching "ai check failed: ..." reason. Start Isa with OutputCompliance__ForceFail=<same word> first. Launcher (both): Start Isa (force NLI failure), Start Isa (force rule-recheck failure) and matching Test paths buttons.
- These are deterministic stand-ins. A question that fails the accuracy check or the rule recheck NATURALLY depends on what the model writes and has to be found by running questions in the Testing console; not done yet.
- Not yet run.

## Test scripts: fresh sign-in on a 401
- test-paths and test-failures remember the token they signed in with. If a request with that token gets a 401 (the token ran out during a long run), they sign in again once and retry; later requests use the new token. Made-up or tampered tokens are never retried, so the token tests still expect 401. The eight-at-once test signs in fresh just before it starts.
- Not yet run.
- test-paths: removed check 7 (English question). It repeated check 1; every case already checks the language and the translation step (English skips translation).
- IsaLauncherDbg Run all tests: the token step ran test-tokens.ps1 with no -User, which is a required parameter, so PowerShell sat waiting for someone to type a user name and the run never finished. It now passes -User d.whitfield. The three single Token tests buttons in IsaLauncherDbg had the same gap and now pass it too (isa-launcher.ps1 already did).

## IsaLauncherDbg: Run all tests runs all tests
- Now: test paths, test paths (model off), failure tests, forged-token tests, rules self-check, then the forced AI-check failures (source, nli, rule, confidence), stopping and starting Isa between them and starting it normally at the end. Excluded on purpose: token tests with Keycloak restart/reset (Docker on this computer, or need a person), Test Salesforce (creates a real Case), the model timing test, test paths with no token (needs DevBypass).
- Written without compiling the launcher: rebuild IsaLauncherDbg and watch the first run.

## Real failing questions instead of forced ones
- Run all tests no longer includes the forced AI-check failures or any Isa restarts (a forced check only proves a failure is caught). The forced buttons still exist on their own.
- New find-failing-questions.ps1 + test-questions-candidates.csv (13 candidate questions, intended check per question; guesses, none tried yet): sends each question to the running Isa several times, reads the event log, and reports which checks really failed and whether it is repeatable. Buttons in both launchers: Find failing questions.
- Next: run it with the model warm, keep the repeatable ones, add them to test-paths as real failure cases.
- Not yet run.

## Real test questions and the settings inventory (November 4)
- **test-questions-candidates.csv**: 52 rows added, each found by sending real questions to Isa (real sign-in, nothing forced, the path checked in the event log, the same path on 5 of 5 runs, 3 of 3 for the Spanish model-path questions). The `intended` column is the path number from Willis's test-path table: 1 direct hit, 2 model clean pass, 3 stopped, 4 no content, 5 sensitive, 6 Spanish (6-ai: Spanish through the model; replies take 10 to 25 seconds to translate back, and one question that took over 30 seconds came back in English, so it was left out), 8 trimmed, 9 ISACA's words after a failed check, 10 source check, 11 rule recheck, 12 answer shape, 13 contact check, 15 rule stop, 15-handoff (rule hand-offs instead of stops), 16 the AI says the passages don't answer. Path 14 (a failed check on a weak search match goes to a person) needs the strong-match mark at 0.85; its rows say so. The earlier rows are unchanged.
- **docs/Settings_Inventory.md** (new): every setting the code reads, where its value lives today, and whether it belongs on the Dev Dashboard, in the config file, or in code and tests only. Notes that StrongMatch and VectorSearch:MinScore aren't adjustable on the dashboard, that the Isa Configuration page settings reset when Isa restarts, and that several dashboard controls are still illustrative.
- Apart from the two sliders below, no code, rules or settings changed.
- **Dev Dashboard, two more real sliders** (Compliance policy, beside the NLI and confidence ones): **Strong-match mark** (OutputCompliance:StrongMatch, default 0.00) and **Search pass mark** (VectorSearch:MinScore, default 0.80). Save changes writes them to output-compliance-settings.json with the other two; Reset to defaults clears all four. They apply to the next question without a restart.
  - Strong-match mark: when an AI answer fails a check, a search score at or above the mark shows ISACA's own words; below it, the question goes to a person. At 0.00 (today's value) a failed check never goes to a person.
  - Files: Services/OutputComplianceSettings.cs (two new numbers, saved and loaded), Services/Orchestrator/Stages/ResponseStages.cs (reads the strong-match mark from it), Services/Search/VectorSearchService.cs (reads the search pass mark from it), Components/Pages/DeveloperDashboard.razor (the two sliders, saved with the others), Components/Shared/VectorSearchTest.razor (says where the pass mark is set).
  - Checked: build has 0 warnings and 0 errors. With the strong-match mark at 0.85 (saved in the file, then Isa restarted), questions with search scores of 0.81 to 0.84 that failed a check went to a person (failed=..., with a ticket), and 0.85 or above still got ISACA's words. 4 questions did this on 5 of 5 runs and were added to the CSV as path 14. The file was then put back as it was (NLI 0.40, confidence 0.70). Not checked on screen: the Dev Dashboard needs a sign-in, so the sliders still need a look in the browser.

## IsaLauncherDbg: a remote light for every service (November 5)
- The status strip now has two lights per service: "● Keycloak down" for this computer, as before, and a new "●R" after it for the same port on the remote host. Green: answering; red: not answering; grey: no remote host set.
- The remote host is Services:RemoteHost from the project's appsettings.json, then appsettings.Local.json (the Dev Dashboard's file wins), the same rule as Program.cs and services-config.ps1. Hovering a light shows the address it checked and whether Isa is set to use this computer's services or the remote ones (Services:Location).
- The remote light sends a small HTTP request and is green only when the service replies (any reply: 200, a redirect, even 404). An open port is not enough: tailscale-phone.ps1 forwards each port with "tailscale serve --tcp", so the home PC accepts a connection on 5067 even when Isa is not running there, then drops it. The first version only checked the port, so Isa showed green there while it was off (Willis spotted it). Checked by hand against 100.89.6.69: Isa no reply (down, correct), Ollama 200, Keycloak 302, voice, translation and embeddings 404 (all up). The light for this computer still checks the port, because the forwarder does not listen on localhost.
- All the checks run at the same time (remote ones wait up to 2 seconds), so the strip still refreshes every 3 seconds.
- File: IsaLauncherDbg\MainForm.cs (in the launcher's own folder, C:\Users\WillisPearson\IsaLauncherDbg, not in this repo). Build: 0 warnings, 0 errors. Not yet seen on screen: open the launcher to check the lights.

## test-paths.ps1 rebuilt around real questions (Nov 5)
- One real question per path (spares with -AllQuestions), each taken from test-questions-candidates.csv (found by real runs, same path 5 of 5): direct hit, model clean pass, stopped (O-01), no content, sensitive (A-01), Spanish direct and Spanish through the model (translated both ways), trimmed, ISACA words after an NLI / strength / numbers / confidence failure, source check, rule recheck, answer shape, contact check, weak match (path 14), rule stop (C-07), rule hand-off (C-03), and the AI saying the passages do not answer. Each case checks the event trail (stage data), not only the status.
- Strong-match mark: paths 9 and 13 need 0.00, path 14 needs 0.85. -MatchMode auto reads strongMatch from output-compliance-settings.json and skips the paths that do not fit; -MatchMode weak / default overrides.
- Warm-up question first (not counted), -Only <id or kind>, -AllQuestions. The model-off and forced-check extras remain as opt-in switches (the forced ones are stand-ins).
- Old rigged check questions removed: "Who writes down what the board decided?" (timed out), the English duplicate. The Spanish insurance question was left out on purpose (it matched the wrong passage).
- Checked: parses in PowerShell 7; replayed yesterday recorded real runs against a mock server (45 of 45 cases pass in default mode, path 14 passes in weak mode, and a wrong-mode run fails as it should). Not yet run against the live app. Spanish model-path answers take 10 to 30 s to translate back.
- Launchers: new buttons Test paths (all questions) and Test paths (weak match); descriptions updated.

- Translation test page: the note no longer says Spanish needs "spa" added (it is on the service).

## Dev Dashboard: the illustrative pieces are real now (Nov 5)
- Change log: new Services/SettingsChangeLog.cs writes settings-change-log.jsonl (git-ignored): when, who (signed-in name), area, setting, old and new value. Logged: the four compliance numbers (save and reset), the Services settings (location, host, voice, translation; save and reset) and the Salesforce settings (secret logged as "changed", never the value). View change log shows the last 50.
- Compliance policy page: the "Always route to a person" chips are replaced by the real rule list (RuleEngine.AllRules, rules that hand off or stop); the three do-nothing Output checks switches are replaced by a read-only list of the checks as they run (live settings); the simulated testing console and its random confidence are removed (a button opens the real Testing console). Slider hints for confidence and NLI now describe what actually happens.
- Role access tab: lists the roles Isa checks (chat_user, dashboard_dev) and says there are no content tiers. The fake role-by-tier grid is gone.
- Other tabs that are not built now say "Not built yet." Header comments of both dashboards corrected. docs/Settings_Inventory.md updated.
- Files: Services/SettingsChangeLog.cs (new), Services/Rules/RuleEngine.cs (AllRules, RuleSummary), Program.cs, .gitignore, Components/Pages/DeveloperDashboard.razor, ContentHealthDashboard.razor (comment only).
- Not compiled or looked at in the browser (no .NET here): restart Isa and open the Dev Dashboard.

## Content Health: four tiles wired
- Time to resolution: new Services/TicketCloseSyncService.cs polls Salesforce (SalesforceTicketService.ListClosedCasesAsync) every Ticketing:Salesforce:ClosedSyncMinutes (default 10) and records each closed Case's ClosedDate through UsageSummaryService.CloseTicketAsync(ref, closedUtc). Registered in Program.cs. Needs Ticketing:Salesforce:Enabled.
- Escalations by chapter: now also grouped by region, looked up from directory/chapters.csv at report time (works on existing rows).
- Question clusters: questions grouped by their best-matching passage (retrieval rank 1), 2+ questions, with people and passed-on counts. No question text stored.
- Content age: content/build_content_db.py adds documents.updated (source file's modified date); UsageReportService falls back to the file's date for an older content.db. ContentHealth:ReviewAfterDays (default 365) sets "overdue"; tile lists the oldest documents.
- Files: Services/UsageReportService.cs, UsageSummaryService.cs, SalesforceTicketService.cs, TicketCloseSyncService.cs, Program.cs, Components/Shared/ContentHealthSections.razor, content/build_content_db.py.
- Not compiled (no .NET here): restart Isa. Rebuild content.db to store the dates (not required; file dates are used otherwise).

- Salesforce Cases now carry RecordTypeId 012fj0000091Z3NAAU (Ticketing:Salesforce:RecordTypeId in appsettings.json; SalesforceTicketService.CreateCaseAsync) so support can close them. Restart Isa.

- Salesforce Cases now send "Type": Sensitive topic / Not covered / Answer didn’t pass checks / Leader request (SalesforceTicketService.TicketType). Restart Isa.

## Dev Dashboard: Front end and Health alerts tabs built
- New Services/DevHealthService.cs (read-only counts from events.db and usage.db), Components/Shared/DevHealthAlerts.razor and DevFrontEnd.razor (+ .css), wired into DeveloperDashboard.razor; DevHealthService registered in Program.cs.
- Health alerts: live check of Keycloak, Ollama, voice, embeddings, translation and Salesforce; recent trouble (model timeout/unavailable/empty, Salesforce Case failed, trimmed, ISACA's words) for the last hour and 24 hours; alerts when model errors reach DevHealth:ModelErrorsPerHour (3) in an hour or the last DevHealth:SalesforceFailuresInARow (3) Cases failed; ticket close sync last run (TicketCloseSyncService.LastRunUtc/LastRunOk).
- Front end: questions, languages, Yes/Not quite/Talk to a person clicks, median and slowest wait for found-in-text vs model answers, voice/translation switches.
- Not compiled (no .NET here): restart Isa and open both tabs.

- Ollama:IdleKeepAlive raised from 2m to 10m (appsettings.json). The model had been unloading 2 minutes after each question, so the next question after a pause loaded it cold and could hit the 45 s timeout. Restart Isa.

- IsaLauncherDbg (MainForm.cs AddGroup): group titles (Isa app, Keycloak, Voice...) had the tails of letters like p and y cut off by the buttons, which started at a fixed y of 20. The buttons now start at the title's measured height. (A first attempt that docked the buttons collapsed every group; it was reverted.) Not compiled: rebuild the launcher (run.bat, or publish.bat for the .exe).
