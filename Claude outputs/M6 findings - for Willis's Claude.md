# M6 screen check: findings for Willis's Claude

From Matthew Brady's Claude session, November 6, 2026. Matthew's Sprint 3 task M6 is to check every changed screen from the user's side after each of Willis's merges. This file covers pull requests #43 to #46 (merged November 3 to 5; main at a6656a5).

**How to use this file:** these are findings and suggestions, not instructions. Willis decides what changes in his code. Show him the findings, propose any change, and wait for his yes before editing. Two items (A and D) are Matthew's to decide and are listed for information only. Don't change `rules/` files: they are Matthew's.

## How it was tested

- Running app on Matthew's laptop, services set to Local. Willis's PC (100.89.6.69) wasn't answering on any port on November 6.
- Running here: Keycloak, Ollama (phi4-mini:3.8b) and Kokoro.
- Not running here: translation (8091), embeddings (8092), `data/vectors.db` and the HHEM model files.
- Leader account: k.watanabe (President, C-001, day 3 on the demo date). Dev Dashboard account: staff.dev.
- Every result was checked against `data/events.db`.

## Passed

| Path | Question | Seen | Log |
|---|---|---|---|
| Answer, then point (W4) | What insurance must we have? | ISACA's wording, then the point line, then the **Pass this to a person** button. The button opens the person card with door `answer_then_point`, showing name, chapter, role, the question and answer, and an optional note (not sent). | `answer_then_point; rule=C-02` |
| Rule stop | Can we skip the audit this year? | `stop.outside` message; the Rules step shows "stopped here" | `result=stop` (C-07) |
| Rule hand-off | Do we have to charge VAT on our conference tickets? | Hand-off, TKT-5816 | `rule=C-03` |
| No content | What is the cryptocurrency policy? | "ISACA's guidance doesn't cover this yet", TKT-5817 | `rag … no_context` |
| Sensitive, urgent | A board member keeps harassing me at meetings. | Hand-off with the emergency-services line, TKT-5818 | `rule=A-01; flags=urgent` |
| Bubble hidden (app.css) | | No `.isa-widget__bubble` on the page while the panel is open | |

Dev Dashboard (staff.dev):
- **Compliance policy:** the four sliders show their defaults (0.70, 0.50, 0.00, 0.80). Nothing was saved.
- **Services (this computer):** "Check services" correctly reports Keycloak, Ollama and voice answering, and embeddings and translation not reachable.
- **Rule tester:** "Run every rule's own tests" gives 61 checks, no problems. The VAT question gives C-03 hand-off for C-001, C-005 and C-009.
- **Vector search test:** shows "Index: not built", which is correct.

## Not checked (needs Willis's services, or vectors built here)

These paths weren't checked:
- an AI-model answer
- `step_down=trimmed`
- `step_down=isaca_words`
- the NLI support-score line in Show the work
- the output rule recheck
- a real Spanish or Amharic translation, in and out

For example, "How do I reach the Chapter Experience team?" (path 2 in `test-questions-candidates.csv`) went to a person with `vector_miss=no vectors built yet`. Worth a run on Willis's PC.

## Findings

### B. Spanish question while the translation service is down (Willis's code; suggestion)

**Seen:** "¿Cuántas reuniones de la junta debe celebrar un capítulo cada año?" with the translation service unreachable.
- Log: `translation … from=es; translated=no; note=service_unavailable`, then `input_compliance … ok`, `database_search … no match`, `rag … escalate … no approved content found`, and ticket TKT-5820.
- Reply translation: `direction=out; … translated=no; note=service_unavailable; model_ms=4066`.

**Problems:**
1. The leader reads "ISACA's guidance doesn't cover this yet". That's untrue: the guidance covers board meetings, but Isa couldn't read the question.
2. `InputComplianceStage` ran the rules on the untranslated Spanish. The rule signals are English, so while the translator is down a Spanish sensitive or tax question (harassment, VAT) isn't caught by its rule. Today it still reaches a person only because search finds nothing.
3. Translating the reply back waits about 4 seconds for a service already known to be down in the same request.

**Where:** `Services/Orchestrator/Stages/StubStages.cs`, `TranslationStage.RunAsync` (around lines 299 to 336). It returns `Pass` with `translated=no` at line 312 (predetermined Amharic), line 326 (`language_not_supported`) and line 330 (`service_unavailable` / fallback). The reply step is in `Services/Orchestrator/Orchestrator.cs`, around lines 131 to 157.

**Suggestion for Willis to consider:** when the question isn't English and couldn't be translated (`translated=no`, for any note other than a deliberate "off"):
- return `Escalate` from the translation stage with a reason of its own, so search, the AI and the rules don't run on text Isa can't read;
- give the leader a message of its own, for example: "I couldn't translate your question, so I've passed it to a person who can help. Your reference number is …";
- add a Show the work line ("Couldn't translate your question, so a person will help");
- skip the reply translation when the question's translation already failed in this request.

This touches the stage contract and the agreed wording table, so it needs Willis's decision, and probably Matthew's agreement on the leader wording.

### C. Two lines still say Spanish isn't supported (Willis's code; developer view only; small)

Spanish was added to the service in 7f218f3 (`spa`), but:
- `Components/Shared/TranslationTest.razor`, line 37: "Spanish works once the service has "spa" added." This is now out of date.
- `Services/ShowTheWork/Wording.cs`, line 198: `TranslationNotSupported = "Only English and Amharic can be translated, so the question went on as written"`. Suggested: "This language can't be translated yet, so the question went on as written", so it doesn't need changing each time a language is added.

### A. The audit stop's wording (Matthew's rules file; for information)

C-07 ("Getting around the money rules") uses `message: stop.outside`. So "Can we skip the audit this year?" gets: "I can only help with running your chapter, using ISACA's guidance. Try asking about board meetings, elections or your yearly paperwork." That reads as though Isa misunderstood. Matthew may give C-07 a message of its own. If he does, the engine may need a new message key; Willis will hear about it from Matthew.

### D. Answer then point in Show the work (for information; known open item)

On an `answer_then_point` reply, the Rules step reads "Made sure Isa can help with this (passed)", with nothing showing that a rule added the point line. This is the open item already on Matthew's list about the Rules step's wording. No action unless Matthew and Willis decide on wording.

### E. Test tickets (for information)

Each hand-off above created a Case in the Salesforce developer org (`salesforce=created`), TKT-5816 to TKT-5820. This is expected. They can be closed in the org if they clutter it.

## Not a finding

The Rule tester's question box starts filled with a sample question. A first test typed into the middle of it and returned E-02. With a clean box the result was correct (C-03). If anything, it might help if the sample were a placeholder rather than a value. Optional.

---

# Second run on Willis's services (later on November 6)

Matthew's laptop rejoined the tailnet, and Isa ran with `Services:Location = Remote` (100.89.6.69): Keycloak, Ollama (phi4-mini:3.8b), Kokoro, translation (`amh`, `eng`, `spa`) and embeddings. `data/vectors.db` was built on the laptop with `content/build_vectors.py --url http://100.89.6.69:8092` (81 vectors).

**Not checked:** HHEM. `data/models/hhem/model.onnx` isn't on the laptop, so every answer logged `nli=skipped; nli_detail=hhem unavailable`.

## Now confirmed working

- **AI path:** `retrieval=vector`, model called, then numbers, strength, contact and shape checks.
- **`step_down=isaca_words`:** Show the work reads "Shown in ISACA's own words".
- **`answer_found=no`:** the question goes to a person.
- **`rule_recheck=fail; rule=C-08`:** goes to a person with the "couldn't double-check" message.
- **Spanish, warm:** in `translated=yes` (about 3.5 s), out `translated=yes` (about 10 s).
- **Dev Dashboard:** Services shows all five answering; the Translation test says "ready"; the Vector search test shows 81 passages.
- **The first run's paths, unchanged:** C-02 answer-then-point and the button; no content; A-01 urgent; C-07 stop; the direct hit.

## New findings (suggestions; Willis decides)

### F1. A stray heading at the end of an AI answer
- **Question:** "What should a president know about financial controls?"
- **Answer ended:** "…the long-term financial sustainability of the chapter. Reporting". "Reporting" is the next heading in the Finances passage, p. 5.
- **Log:** all checks pass.
- **Idea:** in output compliance, trim a trailing fragment that has no sentence ending, or treat it as a failed sentence, before the answer is shown.

### F2. The answer-shape check matches "when" anywhere in the question
- **Where:** `Services/Orchestrator/Stages/AnswerChecks.cs`, `ShapeMissing`, line 199. `Regex.IsMatch(q, @"\b(when|by when|what date|what time|deadline)\b")` fires on "What should we cover **when** we orient new board members?".
- **Log:** `check=answer_shape; missing=when`, and the question went to a person.
- **Idea:** look only at how the question starts, for example `^\s*(when|by when|what date|what time)\b`, or "when" as the first word of a clause that ends in "?". Add this question to `test-questions-candidates.csv` as a should-pass case.
- **Also:** a shape failure reaches the leader with the no-content wording ("ISACA's guidance doesn't cover this yet"), because the escalation reason is `no approved content found`. The "couldn't be fully checked" wording (the rule-recheck path) fits better.

### F3. The fallback to ISACA's words isn't shape-checked
- **Question:** "When should the chapter upload CPE information?"
- **Log:** `strength=fail:advice; shape=pass; step_down=isaca_words`.
- **What the leader saw:** a quote saying what the CPE tool *does*, with no time in it.
- **Where:** `Services/Orchestrator/Stages/ResponseStages.cs`, around line 324, where `step_down=isaca_words` is chosen.
- **Idea:** run `ShapeMissing` on the quote too, and go to a person if it fails.

### F4. The governance guides never get their demo questions now
- **What happens:** vector search returns ISACA passages at or above the 0.80 pass mark for the guide demo questions, so `DatabaseSearchStage`'s guides-last order (StubStages.cs, around lines 78 to 189) is never reached.
  - "What should we include in board meeting minutes?" got "Prepare meeting minutes." (Core Roles, Secretary).
  - "What should we cover when we orient new board members?" scored 0.82, then failed F2.
- **This is a team decision, not only a code one:** when a guide fits much better than a weak ISACA match, which wins? Options:
  - search the guides by meaning too, and prefer a guide when its score clearly beats the ISACA score;
  - raise the pass mark for ISACA-only answers;
  - accept it and drop the guide questions from the demo.
- **Ask Matthew before changing the order:** the guides are labelled "not ISACA policy", and ISACA's documents are meant to come first.

### F5. The first translation after a quiet spell times out
- **Log:** in `translated=no; note=service_unavailable`, and `Translation service did not answer (TaskCanceledException)` in isa-run.log. A minute later, the same text through the same service took 4.3 s.
- **Where:** `Services/Translation/TranslationClient.cs`, `TranslateAsync` (timeout `Translation:TimeoutSeconds`, default 30).
- **Idea:** a warm-up call when Isa starts, and on the presentation's Ready screen, like `Ollama.WarmAsync`. Possibly one retry on a timeout. This sits beside finding B from the first run (an untranslated question shouldn't go on to the rules and search).

### F6. Search-by-meaning scores sit close to the pass mark
- **From the Vector search test (pass mark 0.80):**
  - cryptocurrency 0.776, pizza 0.767, "A colleague keeps bothering me" 0.796: these sit just below the mark;
  - "Who writes down what the board decided?" 0.781, and its best passage is "Welcome!", not the minutes passage.
- **For calibration:** worth a larger run of the questions in `test-questions-candidates.csv` before the demo.

### F7. Show the work wording around search by meaning (for Matthew and Willis)
- **What leaders see:** on every AI answer, "Searched ISACA's guidance (nothing found) / Couldn't find this in ISACA's guidance", then a few lines later an answer from ISACA's guidance.
- **Idea:** when the meaning search goes on to find passages, a gentler line for the first step, for example "No exact match; searching by meaning".
- **Where:** `Services/ShowTheWork/WorkStepText.cs` and `Wording.cs`. Wording changes go through Matthew.

### F8. The opening line on AI-written answers (Matthew's decision)
- **Now:** `Wording.AnswerOpening = "Here's what ISACA's guidance says: "` (Wording.cs, line 37) is used for direct quotes and for AI-written answers alike.
- **Possible change:** a separate opening for AI-written answers, for example "Based on ISACA's guidance:". It would match Show the work's distinction between "Put the answer together" and "Shown in ISACA's own words".

Test tickets from this run: TKT-4374 to TKT-4378 in the Salesforce developer org.
