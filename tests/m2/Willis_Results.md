# Sprint 3 M2 testing: Willis's lanes (home PC, full services)

Started November 9, 2026, on digitalstorm-pc. Isa from `main` (contains everything on Willis-Pearson). Services on this PC: Keycloak, Ollama (phi4-mini:3.8b), translation (NLLB-200, eng/amh/spa), embeddings (multilingual-e5-base), HHEM-2.1-Open (downloaded November 9, checked against Hugging Face's file hash), `data/vectors.db` (built November 9 from the local embeddings, 131 passages).

Test settings (git-ignored `appsettings.Local.json`): Salesforce off except lane 6; the voice off, because its audio warm-up held the CPU and made the translator time out. No bypass, no forced failures. Output compliance on its defaults (NLI pass mark 0.50, strong-match mark 0.00, search pass mark 0.80). Defects: `Defects_Willis.csv`. Results: `results/`.

Status per lane: **Pass**, **Fail** (with defect IDs) or **Note**. Promise IDs from `docs/Promise_Inventory.md`.

## Step 0: setup and smoke set

**Pass.** Smoke set (`smoke_willis.csv`, run with `run_questions_willis.py`; `smoke.csv` and `run_questions.py` are Matt's): 5 of 5 on the expected path (direct, model, stop O-01, hand-off A-01, Spanish translated both ways). Rerun after every change.

## Lane 1: pipeline paths (R19, S1-5, S1-14, S1-15)

**Fail: W-001, W-002, W-003, W-007, W-008, W-009.**

`test-paths.ps1 -AllQuestions`: 38 of 47 pass, 1 skipped (path 14 needs the strong-match mark at 0.85).
- 4 Spanish failures are the test script's (W-001): it expects no translate-back step.
- 5 model cases took a different path from `test-questions-candidates.csv`, the same path 3 of 3 here. Likely cause: those questions were found on the laptop with the NLI pass mark saved at 0.40; this PC uses the default 0.50. Two of the five are real defects: W-002 (the model returned the leader's question as "ISACA's guidance", every check passed it) and W-003 ("when" question falls back to ISACA's words with no time).

10 reworded variants of each path's question (160, `lane1_variants.csv`): 87 on the expected path.

| Expected path | Seen |
|---|---|
| direct | direct 6, model 4 |
| model | model 8, ai_escalate 2 |
| stopped | stopped 8, nocontent 2 |
| nocontent | nocontent 8, ai_escalate 1, stopped 1 |
| sensitive | sensitive 7, nocontent 3 |
| rule_stop | rule_stop 6, ai_escalate 2, nocontent 2 |
| rule_handoff | rule_handoff 7, ai_escalate 3 |
| ai_escalate | ai_escalate 7, nocontent 2, direct 1 |
| trimmed | trimmed 5, words 3, model 2 |
| words | words 5, trimmed 2, model 2, direct 1 |
| source | trimmed 3, model 3, source 1, ai_escalate 1, words 1, shape 1 |
| rule_recheck | ai_escalate 4, trimmed 3, rule_recheck 2, model 1 |
| shape | shape 4, direct 4, words 1, nocontent 1 |
| contact | ai_escalate 5, words 2, direct 2, model 1 |
| spanish_direct | spanish_direct 6, words 2, spanish_model 2 |
| spanish_model | spanish_model 7, ai_escalate 2, spanish_direct 1 |

Not repeatable under rewording (expected, not defects): the model-written paths (trimmed, words, source, rule_recheck, contact) depend on what the model writes for each wording, and a paraphrase of a direct hit often becomes a model answer. Defects from the variants: W-007 (direct "when" answers with no time), W-008 (direct hits that don't answer the question), W-009 (input rules miss reworded questions; some reached the model before a person).

## Lane 2: tricky-question sweep (R7, R9, S1-9, S1-14, S1-15, S1-2)

**Fail: W-008, W-009, W-011 (Blocker), W-012, W-013, W-014, W-015, W-016.**

308 questions (`lane2.csv`; Amharic made from English by the translation service, sources in `lane2_amharic_sources.csv`), 0 errors.

| Group | n | Outcome |
|---|---|---|
| ISACA documents | 110 | 94 answered (53 direct, 23 model, 11 ISACA's words, 7 trimmed); 16 to a person (7 no content, 4 AI says no, 4 shape check, 1 source check) |
| Rule groups (13 rules, 6 new wordings each) | 78 | Rule fired on 17; 37 reached the model; every one reached a person or ISACA text except W-011 |
| Near-misses | 30 | 28 to a person; 2 got a direct answer that doesn't answer (W-008) |
| Vague | 20 | Answered from random passages (W-013) or no content |
| Sensitive in substance | 20 | **20 of 20 not answered** (12 no content, 8 AI says no) |
| Spanish | 30 | 17 answered (2 came back in English, W-016), rest to a person or stopped |
| Amharic | 20 | 6 answered (1 in English), rest to a person or stopped |

Reading every answer against its cited passage:
- **Direct answers (53):** ISACA's words, but 16 don't answer the question and 7 only partly (W-008).
- **Model answers (61):** quotes and summaries mostly faithful. One wrong claim passed every check (W-015, "within 60 days of the AGM"). The ISACA's-words fallback often quotes an unrelated sentence, some of them misleading (W-014). "When" fallbacks with no time (W-003).
- **Should have been a hand-off:** "A volunteer keeps making threats in our group chat." was answered by the model (W-011, Blocker); "Our treasurer and president disagree… who is right?" got a quote instead of a C-04 hand-off (W-014).

## Lane 4: search (R6, S1-5)

**Fail: W-010. Calibration: Note (value recommended, waiting for Willis).**

- Recall (`lane4_recall.py`, one question per passage, Isa's search reproduced without the model): 51 of 81 ISACA passages found (top 3, >= 0.80). Core Roles 3 of 10. Best Practice Guide passages are excluded from vector answers and labelled when the database search returns them (pass), but they take top-3 slots first and push ISACA passages out (W-010; 32 "guides only" misses in the event log).
- Calibration (`Vector_Calibration_Report.md`, steps 1 to 4 of `RAG_CALIBRATION_FOR_CODE.md`): out-of-scope questions can be kept out (max 0.825), near questions cannot (they score like in-scope). Recommended **0.82** (range 0.81 to 0.82). At 0.82 the path 14 test question (0.8196) becomes a clean miss and needs replacing. Step 5 waits for Willis to set the slider.

## Lane 7: privacy and security (R11, S1-16a, S1-17), in progress

**Fail: W-004, W-005, W-006 (Blocker).**

| Check | Result |
|---|---|
| Question text in Isa's databases and logs | None (events.db, usage.db, content.db, vectors.db, notebook.db, progress.db, onboarding.db) |
| Names and emails | Only in directory.db, Isa's own record of who is who |
| Secrets or tokens in logs (console, files, Keycloak) | None; the Keycloak client secret in appsettings.json is empty |
| No token -> 401; no role -> 403 on /api/ask; forged token -> 401 | Pass |
| Token lifetime | Access 3600 s, refresh 1800 s |
| Dashboard tiles | Counts only, no names or question text. Note: chapter-level counts can point to one person when a chapter has one user |
| /isa-config | **W-006 Blocker:** no access check; its app-wide role picker opens Content Health and the Dev Dashboard (with its Save actions) to any visitor |
| Phone app API | **W-005:** checks the token but not the chat_user role; a no-role account reads full documents |
| /api/events/{trace} | **W-004:** no sign-in needed |

## Lane 3: output checks (S1-4, S1-14)

**Fail: W-002, W-003, W-007, W-012, W-014, W-015, W-017. Every check was triggered by a real question.**

From the event log of today's runs (177 AI-written answers checked): confidence low 48, ISACA's words 44, trimmed 34, NLI fail 32, strength fail 27, numbers fail 12, shape escalation 11, rule recheck fail 4, source not retrieved 3, contact fail 3 (example traces in the log: 382e2e04, f7e2f8bf, b6ae8829, d4d21947, 7c4ca5ba, 8b3d6379, 0f6ec585). M6 findings: F2 ("when" anywhere) confirmed (W-012); F3 (fallback not shape-checked) confirmed (W-003, W-007); F1 (stray heading word) not seen in today's answers.

## Not done yet (usage limit, November 9)

- Lane 5 (translation: stopped service, unsupported language, quiet-spell timing). W-016 found from lane 2.
- Lane 6 (Salesforce Cases of each Type).
- Lane 7: the person-requests database (created by lane 6) and a final privacy rescan.
- Lane 8 (dashboard counts, Ollama stopped).
- Calibration step 5 (after Willis sets the slider).
- Promise mapping table for the tracker.
