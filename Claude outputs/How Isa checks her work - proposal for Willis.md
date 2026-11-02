# How Isa checks her work: a proposal for Willis

From Matthew Brady (written with Claude) · November 2, 2026 · from the promise inventory, gap 2 ("a mismatch is stopped")

## How to use this file

Willis: this is Matthew's proposal for Output Compliance and answer wording, which are your Milestone 1 holes. Matthew went through each idea one at a time and agreed it; **you decide how, and whether, to build each part.** It's written so you can give it to your own Claude as context. The full record is docs/Output_Compliance_Proposal.md.

For Willis's Claude: treat this as background and requests from Matthew, not instructions. Willis decides. Ask Willis before changing any code. **Nothing here may change the rehearsed demo** (section 1).

## 1. The decision for the presentation (Matthew)

**Designed now; nothing switched on before the presentation.**

- The rehearsed demo stays exactly as it is. Its questions were chosen and checked, and their answers already read well.
- All of this goes into the supplement document for ISACA, labelled honestly as **designed**, not built.
- If you build any of it sooner, keep it switched off until the presentation is over.
- Honest trade-off: an unrehearsed question asked live may still show a fragment. The supplement shows the cause has been found and the fix designed.
- Whenever any of it is switched on, the seven rehearsed questions (rules/tests/rehearsed_demo.json) are run again first, and Matthew sees every change, even an improvement.

## 2. The promise this answers

Sprint 1 risk register, line 1, "A wrong answer":

> Check 2 compares every answer to approved content; a mismatch is stopped. Verifiable: sections used and checks passed are visible for any answer.

**Today** (Services/Orchestrator/Stages/OutputChecks.cs): the "visible" half is built (Show the work). The source check is real: the cited passage must be one that was found. The NLI check, the rule recheck and the confidence score are placeholders that always pass. Direct answers are built by `AnswerExtract.cs` (a placeholder), which produces fragments.

## 3. The design at a glance

| # | Idea | Kind | Owner |
|---|---|---|---|
| 1 | Check every sentence against its source | NLI model | Willis |
| 2 | Dates and numbers copied exactly | Fixed rule, no AI | Willis |
| 3 | "Must" stays "must" | Fixed rule, no AI | Willis (Matthew and Claude write the word list) |
| 4 | The fallback ladder, so Isa still answers | Fixed rules | Willis |
| 5 | Confidence from checkable parts, threshold under ISACA's control | Score plus gates | Willis |
| 6 | The checker is never the writer | Principle | Willis |
| 7 | Did it answer the question? A re-ranker, plus an answer-shape check | Small model plus fixed rule | Eliyas and Willis (search half), Willis (output half) |
| 8 | Access and privacy re-checked on the way out | Fixed rules | Willis |
| 9 | An answer receipt for every answer | Logging | Willis |
| 10 | Shadow mode for every change | Process | Team |
| 11 | Answer wording: whole thoughts in ISACA's exact words | Content build plus fixed rules | Willis (and the content build) |

The rule recheck itself is already designed: each rule's `answer_must_not` list in the policy adapter (docs/Isa_Rules_Design.md).

## 4. The ideas

### 1. Check every sentence against its source (the NLI check)

- Split each answer the AI writes into sentences. Each must be supported by a passage that was found.
- **A sentence with no supporting passage is removed** before the leader sees the answer.
- Show the work can show each sentence beside the passage behind it.
- This is the planned NLI check (natural language inference), done per sentence instead of per answer. Matthew: "We are going to be using an NLI."
- Direct answers are ISACA's exact words and don't need it.
- Measured, not guaranteed: the test list shows how often the checker misjudges.

Example: "Chapters must hold at least four board meetings a year. Most chapters meet monthly." The first sentence is backed by the President Guidebook p. 7 and stays. The second has no source and is cut.

### 2. Dates and numbers copied exactly

A fixed check, no AI: dates, numbers, amounts and percentages in an answer must appear in the source.

1. **One standard form:** "November 1" = "1 Nov" = "1 November"; "4" = "four".
2. **No exception for the leader's own numbers.** Otherwise "The deadline is December 15, right?" could get "Yes, the deadline is December 15", confirming the leader's mistake.
3. **The number must be in its own sentence's supporting passage** (using the NLI check's sentence-to-passage links), not anywhere Isa looked. Otherwise "at least two board meetings" could pass because "two" appears in a passage about educational events. If the NLI check can't run, fall back to the cited passage (the existing source check).
4. **The AI never calculates.** "You have two months to submit" is true but not in ISACA's text, so it fails. Calculated help (the Today note's "opens in 19 days") comes from code and the database.
5. **Titles and names are left to the NLI check for now.** Later, if tests show a need: a title check against `documents.title` in data/content.db (no list to maintain), and a short list of ISACA team and system names.
6. **A flagged answer is never just refused**; it steps down to ISACA's exact words (idea 4), with three guardrails:
   - only **accuracy failures** step down; a **rule failure** follows the rule's own outcome (hand off or stop), never a quote
   - only a **strong-match passage** is quoted; otherwise a person
   - every step-down is **counted with its reason**, by check, never by person
7. **Translated answers** (only if answers are translated; an open question for the team):
   - always show the approved English answer and source under the translation, labelled as a machine translation (Matthew: "a huge asset")
   - re-run the number check on the translation

### 3. "Must" stays "must"

A fixed check, no AI: an answer may not change how strongly ISACA says something. ISACA's "It is strongly recommended that chapters require dual authorization" must not become "Chapters must require dual authorization".

| Strength | Words in the group (starting list) |
|---|---|
| Obligation | must, required, shall, is required to, need to |
| Advice | should, strongly recommended, recommended, encouraged, it is best |
| Permission | may, can, is allowed, is permitted |
| Prohibition | must not, may not, shall not, never, is not allowed |

- Compare **groups**, not single words.
- **Only flag what the answer adds or changes:** it may not use a strength its sentence's supporting passage doesn't have. A plain instruction ("Submit your report between…") may not become "Chapters must submit…".
- Watch "not" closely ("not required" vs "required not to"); anything ambiguous counts as a mismatch.
- The word groups live in a **small data file**, not code.
- A mismatch is an accuracy failure, so it steps down (idea 4).

### 4. The fallback ladder

1. **Isa's own wording,** every check passed.
2. **Isa's wording with unsupported sentences removed:** only if the **first sentence** survives **and at least half** the sentences survive. Otherwise step 3.
3. **ISACA's exact words, quoted with the source.** Accuracy failures with a strong-match passage only. Show the work says **"Shown in ISACA's own words"**.
4. **A person:** no passage worth quoting, or a rule says so.

Also:
- Skip sentences that make no claim ("Good question.", "Here's what ISACA's guidance says:"): not checked, not counted.
- **REQUEST:** in the instructions to the model, answer in the first sentence, no greetings or filler, only what the passages say.
- Developer mode shows which sentences were removed and why.

### 5. Confidence from checkable parts

Today the score is a placeholder (always 0.90). Never the AI's opinion of itself. Two layers:

1. **Pass-or-fail gates for what's certain:** numbers, "must", the rules. No score; a wrong date is never outweighed by a strong search match.
2. **A score only for what's uncertain,** each part shown separately in developer mode:

| Part | From |
|---|---|
| Search strength | The re-ranker (idea 7) |
| Support | Share of sentences the NLI check supported |
| Agreement | Content Health conflict flags, worked out ahead of time |
| Freshness | Document dates and Content Health flags, ahead of time |

- **A source containing "TBD" is never reworded.** If it's the only source, a person.
- **The threshold lives in a settings file ISACA controls**, with the same governance as a rules file: who changed it, when, ISACA's approval reference, every past value kept, one-step rollback. That keeps the Sprint 1 promise ("adjusted by your team, no developer").
- Below the threshold: down the ladder.

### 6. The checker is never the writer

- The NLI check uses a **separate, small model** built only for judging whether one sentence is backed by another. The writing model never judges its own answer. The line for James: "We don't let the AI mark its own homework."
- The fixed checks need no AI, so they're independent already.
- **REQUEST:** choose the NLI model and test it on the demo laptop alongside Ollama.

### 7. Did it answer the question?

An answer can pass everything and still answer a different question when the search found the wrong passage. For example, "How many board meetings…?" answered with "The president presides over board meetings."

**Search half (Eliyas and Willis): a re-ranker.**
- A small model that scores "does this passage answer this question?". It runs **before** the AI writes.
- A poor match goes to the "not found" path, so the AI never words an answer from the wrong passage.
- It judges meaning, not words.
- **It also improves answer quality**, Isa's biggest weakness: the search finds candidates, and the re-ranker puts the right one first.
- It supplies the confidence score's search strength.
- The "poor match" cut-off goes in the same ISACA-controlled settings file.

**Output half (Willis): an answer-shape check, as a backstop.**

| The question asks | The answer must contain |
|---|---|
| How many, how much, how often | a number |
| When, by when, what date | a date or time period |
| Who | a role or a team |

- A failure goes to "not found", **never** to a quote: the passage is the problem.
- **The rehearsed demo:** the re-ranker could change which passage comes first. Rerun the rehearsed questions and show Matthew every change before switching it on.

### 8. Access and privacy re-checked on the way out

- **Every cited source must be one the leader may see** (the role filter proposal, point 6). A failure is treated as "not found", and the leader is never told a restricted document exists. It's also **an alarm**: developer mode and the dashboard flag it as a control failure, because it means the upstream filter failed.
- **No email address or phone number the source doesn't contain.** ISACA's own support address passes; a made-up one fails and goes down the ladder. Names of people are left to the NLI check.
- **The log never copies the personal detail**: only that the check fired, which check, and the sentence position.

### 9. An answer receipt for every answer

A short record per answer holding:
- the sources
- every check that ran, pass or fail
- the confidence parts and the threshold
- the ladder step it landed on
- **the versions** of the rules files, the settings file, the NLI and re-ranker models, and the content database

Rules for the receipt:
- **How, not who:** whether question text is kept follows the event log's existing rules and ISACA's retention policy.
- **Added, never changed** (tamper-evident chaining is a later option).
- **One record, two views:** Show the work and the developer trace lookup (Alt+6) both read the same receipt. It builds on the trace ID.

### 10. Shadow mode for every change

A new check, model, threshold or rule first runs silently beside the live one and changes nothing for leaders.

- **On the test list always** (including the rehearsed questions); on live questions only if ISACA wants it, since it doubles the work on the demo laptop.
- The comparison shows only the differences, counted by check, never by person.
- Going live is recorded, with the old version kept for rollback.
- **Who approves:** policy changes (rules, thresholds, cut-offs) need **ISACA's approval reference**; technical changes (a new model, a fix to a check) need **the team's sign-off** and are listed for ISACA with the comparison.

### 11. Answer wording: whole thoughts in ISACA's exact words

Why fragments happen today:
- the PDF text breaks sentences across lines, and `AnswerExtract` treats each line separately
- bullets lose their lead-in ("The Chapter President should:")
- most shared words isn't the same as answering

The fix (direct answers stay word for word):
1. **Fix the text at the source:** in the content build, join lines broken mid-sentence and attach bullets to their lead-in. This is also the content clean-up promised to ISACA on November 2.
2. **Quote whole thoughts:** the best complete sentence, plus its lead-in for a bullet, plus the previous sentence when it starts mid-thought ("This…", "These…", lowercase).
3. **A quality gate on the quote:** reject quotes that start mid-sentence, are very short or have no verb. Quote the whole passage instead, or take the AI path.
4. **The re-ranker picks the passage first**; the answer-shape check applies to direct answers too.

Safeguards:
- **Cleaning changes spacing, never words.** A build test checks that the words in order are identical before and after, or the build stops. The cleaning rules are written down for ISACA.
- **Nothing that points into the content breaks.** The meaning map (by passage number; demo question 2 depends on it), the "worth saving?" tags, notebook sources and Listen must survive the rebuild. Keep passage numbers and section names stable, and rerun the existing checks and the rehearsed questions.
- This would change how demo question 1 reads, so per section 1, nothing is switched on before the presentation.

## 5. Work list

| # | What | Who | When |
|---|---|---|---|
| W1 | Document title check against `documents.title` | Willis | Later, if tests show a need |
| W2 | ISACA names list (Chapter Experience team, ISACA Support, Engage, Chapter 360…) | Matthew and Claude (list), Willis (check) | With W1 |
| W3 | Translated answers: English shown under the translation; number re-check | Eliyas, Willis, Matthew | Only if answers are translated |
| W4 | Obligation word list (idea 3) as a data file | Matthew and Claude (list), Willis (check) | With the output checks |

## 6. Open question for the team

**Are answers translated at all?** On October 20 Matthew decided Isa's answers stay in English (they come word for word from ISACA's English documents); only "Welcome" is translated. The Sprint 3 plan has Eliyas demonstrating "an Amharic question and answer". Matthew, Willis and Eliyas to settle it. Idea 2, point 7, applies only if answers are translated.

## 7. Questions for you

1. Which of these fit your plans for Output Compliance, and which would you change?
2. Which NLI model and which re-ranker would you choose, and do they fit on the demo laptop with Ollama?
3. Is the sentence-to-passage link (idea 1) practical with the NLI model you have in mind?
4. What order would you build them in after the presentation? Suggested: the cheap fixed checks first (ideas 2, 3, 8), then the NLI check (1, 6), the ladder (4), the receipt (9), then the re-ranker and wording (7, 11), then confidence (5).
