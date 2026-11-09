# M6 screen check: Willis's merges of November 3 to 5

Checked November 6, 2026, from the user's side, in the running app on Matthew's laptop (main at a6656a5). Covers pull requests #43, #44, #45 and #46: NLI answer check, rule engine and fixed answer checks, the "connect" button, translation, vector search, and the Dev Dashboard sections.

**How:** signed in as Watanabe Kenji (president, Sakura Bay Chapter, day 3 on the demo date) and asked one question per path. Then signed in as staff.dev for the Dev Dashboard. Every result was checked against the event log (data/events.db).

**Limits:** Willis's PC (100.89.6.69) wasn't answering, so Isa ran on this laptop's services: Keycloak, Ollama (phi4-mini) and the voice. Translation and embeddings weren't running, there's no data/vectors.db, and there's no HHEM model here. So **the AI-model paths couldn't be checked**: the model's own answers, trimmed answers, ISACA's words after a failed check, the NLI score line, and Spanish or Amharic actually translated. They need Willis's services, or the services and vectors built on this laptop.

## Results

| # | What | Question | What the leader saw | Result |
|---|---|---|---|---|
| 1 | Answer, then point (W4, the connect button) | What insurance must we have? | ISACA's wording, then "Whether a policy meets your country's law is a question for a person." Buttons: Yes / Not quite / **Pass this to a person**. The button opens the card: "Pass this to ISACA's Chapter Experience team?" with name, chapter, role, the question and answer, and an optional note. (Not sent.) | Works |
| 2 | Rule stop (C-07) | Can we skip the audit this year? | "Not something Isa can help with." Steps: sign-in passed, "Made sure Isa can help with this" stopped. | Works as the rule says. See note A |
| 3 | Rule hand-off (C-03, tax) | Do we have to charge VAT on our conference tickets? | Passed to a person, TKT-5816, the agreed hand-off wording | Works |
| 4 | No content | What is the cryptocurrency policy? | "ISACA's guidance doesn't cover this yet", TKT-5817 | Works |
| 5 | Sensitive, urgent (A-01) | A board member keeps harassing me at meetings. | Hand-off, TKT-5818, with the safety line about emergency services | Works |
| 6 | Model path | How do I reach the Chapter Experience team? | Passed to a person, TKT-5819. The log shows `vector_miss=no vectors built yet`. | Can't check here (limits above) |
| 7 | Spanish, translator down | ¿Cuántas reuniones de la junta debe celebrar un capítulo cada año? | "ISACA's guidance doesn't cover this yet", TKT-5820 | Works as written, but see note B |
| 8 | Isa bubble hidden while the panel is open (app.css) | | No bubble on the page with the panel open | Works |

Dev Dashboard (signed in as staff.dev):

| Section | Result |
|---|---|
| Compliance policy | Four sliders show with their explanations and defaults (confidence 70%, NLI 0.50, strong match 0.00, search pass 0.80). Nothing was saved. |
| Services (this computer) | Local/Remote, voice and translation switches. "Check services": Keycloak, Ollama and voice answer; embeddings and translation not reachable. Correct. |
| Rule tester | "Run every rule's own tests": 61 checks, no problems. The VAT question: hand-off by C-03 for Japan, Germany and the United States. Correct. |
| Translation test | "Check the service": not reachable (correct). See note C. |
| Vector search test | "Index: not built (run Build search vectors in the launcher)." Correct. |

## Notes

**A. The audit stop sounds odd to a leader (Matthew's rules file, not Willis's code).** C-07 uses `stop.outside`, so "Can we skip the audit this year?" gets "I can only help with running your chapter… Try asking about board meetings, elections or your yearly paperwork." The audit is about running the chapter, so the reply reads as if Isa didn't understand. A message of its own for C-07 would fit better, for example one saying Isa can't help find ways around ISACA's requirements, and pointing to them. It's Matthew's wording to decide.

**B. When the translator is down, a Spanish question goes on untranslated (for Willis).**
- The leader is told "ISACA's guidance doesn't cover this yet". That isn't true: the guidance covers board meetings, but Isa couldn't read the question. The log has the real reason (`translation … translated=no; note=service_unavailable`), but the leader's view doesn't show it.
- The rules then ran on the Spanish text. They're English phrases, so a Spanish question about harassment or tax wouldn't be caught while the translator is down. Here it still ended with a person, but because nothing was found, not because of the rule.
- Suggestion: if a non-English question can't be translated, hand it off with its own message (for example "I couldn't translate your question, so I've passed it to a person"), and skip search and the AI.
- Also, translating the reply back waited about 4 seconds for the missing service before giving up (`model_ms=4066`).

**C. Two lines still say Spanish isn't supported, but it is now (for Willis; developer view only).**
- The Translation test page: "Spanish works once the service has 'spa' added". Spanish was added in 7f218f3.
- Show the work's developer detail `TranslationNotSupported` (Wording.cs): "Only English and Amharic can be translated". It should now name Spanish too, or say "This language can't be translated yet".

**D. Small, for Matthew (known open item).** On the answer-then-point reply, "Made sure Isa can help with this" shows only "passed". Nothing says a rule added the line about a person. This is the open item already noted (the Rules step's wording).

**E. Test tickets.** Each hand-off created a Case in the team's Salesforce developer org (TKT-5816 to TKT-5820), as designed. They can be closed or deleted there if they get in the way.

**Not a problem (my mistake while testing):** the Rule tester's question box starts with a sample question in it. My first test typed into the middle of that sample and got E-02. With a clean box, the result was correct.

---

# Second run, the same day, on Willis's services

After Matthew's laptop rejoined the tailnet, Isa ran with **Services: Remote (100.89.6.69)**: Keycloak, Ollama (phi4-mini), the voice, translation (NLLB-200, with Spanish) and embeddings, all on Willis's PC. The search-by-meaning index (data/vectors.db, 81 passages) was built on the laptop with Willis's embedding service. Signed in as Watanabe Kenji through Willis's Keycloak.

**Still not checked:** the sentence-support check (HHEM). It runs inside Isa on the laptop, and its model files (docs/HHEM_Setup.md) aren't downloaded there, so every answer logged `nli=skipped; nli_detail=hhem unavailable`.

## Results

| Path | Question | What the leader saw | Log | Result |
|---|---|---|---|---|
| AI answer | How do I reach the Chapter Experience team? | An answer from guidebook p. 8 (notify the team through ISACA Support) | `retrieval=vector; top_score=0.86`; model called; numbers, strength, contact, shape all pass | Works (the answer covers only part of the question) |
| AI answer | What should a president know about financial controls? | A good answer, but it **ends with a stray word, "Reporting"** | all checks pass | Works, see note 1 |
| ISACA's words after a failed check | What do chapters usually do about meetings? | "Shown in ISACA's own words" in Show the work | `numbers=fail:1; strength=fail:advice; step_down=isaca_words` | Works |
| ISACA's words, wrong shape | When should the chapter upload CPE information? | A quote saying what the CPE tool does, **not when** | `strength=fail; step_down=isaca_words` | See note 2 |
| The AI says the passages don't answer | How do I get help with Engage? | Passed to a person | `answer_found=no` | Works |
| Rule recheck | Is a compilation fine for the financial review if ISACA approves it in writing? | Passed to a person: "I couldn't double-check this answer well enough to share it" | `rule_recheck=fail; rule=C-08` | Works |
| Governance guide | What should we cover when we orient new board members? | Passed to a person: "ISACA's guidance doesn't cover this yet" | `check=answer_shape; missing=when` | See notes 3 and 4 |
| Governance guide | What should we include in board meeting minutes? | "Prepare meeting minutes." (from Core Roles) | vector search found ISACA passages first | See note 4 |
| Spanish, translator cold | ¿Cuántas reuniones de la junta debe celebrar un capítulo cada año? | Answered **in Spanish**: at least four board meetings a year | in: `translated=no; note=service_unavailable`; out: `translated=yes` | See note 5 |
| Spanish, translator warm | ¿Cómo se registran los eventos del capítulo? | Answered in Spanish from the Chapter Systems Overview | in: `translated=yes` (3.5 s); direct hit; out: `translated=yes` (10 s) | Works |
| Earlier paths again | insurance; cryptocurrency; harassment; audit; board meetings | Same as the first run: answer then point with the button; no content; hand-off with the safety line; the stop; the direct answer | | Unchanged |

Dev Dashboard:
- **Services:** all five services answer at 100.89.6.69.
- **Translation test:** "ready".
- **Vector search test:** 81 passages. See note 6 for the scores.

## New notes

1. **A stray heading at the end of an AI answer (for Willis).** The financial-controls answer ends "…the long-term financial sustainability of the chapter. Reporting". "Reporting" is the next section's heading in the passage. Every check passed. An answer ending in a word with no full stop could be trimmed, or flagged by a check.
2. **The fallback to ISACA's words isn't checked for shape (for Willis).** For "When should…", the AI's wording failed the "must" check, so Isa showed ISACA's words instead, but that quote has no time in it. The shape check (a "when" question needs a date or period) could also run on the quote, and go to a person if it fails.
3. **The shape check counts "when" anywhere in the question (for Willis).** "What should we cover **when** we orient new board members?" is a "what" question, but it failed as a "when" question with no date. The leader was then told "ISACA's guidance doesn't cover this yet", the no-content message, not the "couldn't be fully checked" one.
4. **The governance guides no longer get their demo questions (for the team).** Search by meaning now finds ISACA passages above the 0.80 mark (0.82 here), so the guides, searched last, are never reached. The October 22 demo questions now go to a person or get a thin answer ("Prepare meeting minutes."). Decide which wins (ISACA passages that match well, or a guide that fits better), then recheck the guide demo questions.
5. **The first translation after a quiet spell times out (for Willis).** The question went on in Spanish, untranslated (`TaskCanceledException`, 30-second timeout), while the reply was translated back. It still worked here only because the search is multilingual. The rules still ran on Spanish text (see note B in the first run). A translation warm-up when Isa starts, like the model's, would avoid the cold first call.
6. **Search scores sit close to the pass mark (for Willis).** Unrelated questions score 0.77 to 0.80 against a mark of 0.80: cryptocurrency 0.776, pizza 0.767, "a colleague keeps bothering me" 0.796. "Who writes down what the board decided?" scores 0.781, and its best passage is "Welcome!", not the minutes passage. The margins are thin, so the mark needs care.
7. **Show the work tells leaders "Couldn't find this in ISACA's guidance" before every AI answer (for Matthew and Willis).** The first search finds nothing, so that step says "(nothing found) Couldn't find this…", and then a few lines later Isa answers from ISACA's guidance. Leaders may read that as a contradiction. Suggest a gentler line for that step when search by meaning goes on to find passages, for example "No exact match; searching by meaning".
8. **Every AI answer starts "Here's what ISACA's guidance says:" (for Matthew).** That reads like a quote, even when the model wrote the sentence. Show the work does say "Put the answer together using only ISACA's guidance". A different opening for AI-written answers ("Based on ISACA's guidance:") would match the "Shown in ISACA's own words" distinction.

Test tickets from this run: TKT-4374 to TKT-4378, as Cases in the Salesforce developer org. Ticket numbers restarted when Isa restarted.
