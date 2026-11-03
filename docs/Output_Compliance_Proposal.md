# Output compliance: proposal

November 2, 2026 · Matthew Brady, written with Claude · from the promise inventory, gap 2 ("a mismatch is stopped", rows S1-14 and R7)

**COMPLETE: all eleven ideas agreed by Matthew on November 2, 2026.** Sent to Willis as Claude outputs/How Isa checks her work - proposal for Willis.md (Output Compliance and answer wording are his Milestone 1 holes; he decides how to build them).

**For the presentation (DECIDED, Matthew, November 2, 2026): designed now, nothing switched on before the presentation.** The rehearsed demo stays exactly as it is (its answers were chosen and checked and already read well). Everything here goes into the supplement document for ISACA, labelled honestly as **designed**, not built. The proposal still goes to Willis, marked "after the presentation"; anything he builds sooner stays switched off until the presentation is over. Honest trade-off: an unrehearsed question asked live may still show a fragment; the supplement shows the cause has been found and the fix designed.

**The promise (Sprint 1 risk register, line 1, "A wrong answer"):** "Check 2 compares every answer to approved content; a mismatch is stopped. Verifiable: sections used and checks passed are visible for any answer."

**Today:** the "verifiable" half is built (Show the work). Output Compliance checks that the cited source is approved and was found; the NLI check, the rule recheck and the confidence score are placeholders that always pass.

## 1. Check every sentence against its source: AGREED (November 2)

- Split each answer the AI writes into sentences; each must be supported by a passage that was found.
- A sentence with no supporting passage is removed before the leader sees the answer.
- Show the work can show each sentence beside the passage behind it.
- This is the planned NLI check (natural language inference: a small model judging whether one sentence is backed by another), designed per sentence instead of per answer.
- Measured, not guaranteed: the test list shows how often the checker misjudges.
- Direct answers are ISACA's exact words and don't need it.

Example: "Chapters must hold at least four board meetings a year. Most chapters meet monthly." The first sentence is backed by the President Guidebook p. 7 and stays; the second has no source and is cut.

## 2. Words that matter must be copied exactly: IN DISCUSSION

A fixed check, no AI: dates, numbers, amounts and percentages in an answer must appear in the source. Being worked through complication by complication:

- **2a. The same fact written differently: AGREED.** Dates and numbers are compared in one standard form, so "November 1", "1 Nov" and "1 November" are the same date, and "4" is the same as "four".
- **2b. Numbers from the leader's question: AGREED, no exception.** Every date and number in an answer must be in ISACA's text, whatever the leader typed. Otherwise "The deadline is December 15, right?" could get "Yes, the deadline is December 15", confirming the leader's mistake. Caught answers drop to ISACA's exact words.
- **2c. Which passage the number must be in: AGREED, framed around the NLI check (Matthew: "We are going to be using an NLI").** The NLI check (idea 1) links each sentence to the passage that supports it; every date and number must appear in **that sentence's own supporting passage**, not just anywhere Isa looked. Otherwise "at least two board meetings" could pass because "two" appears in a passage about educational events. Order: the NLI check runs first and produces the links; the number check (plain text matching, no model) then uses them. If the NLI check can't run, the number check falls back to the passage the answer cites (already built: the source check). Which NLI model to use is not chosen yet (Willis).
- **2d. Numbers the AI works out itself: AGREED, a failure.** "You have two months to submit" is true but not in ISACA's text, so the answer drops to ISACA's exact words ("between 1 November and 31 December"). The AI only rewords; it never calculates. Calculated help (for example the Today note's "opens in 19 days") comes from code and the database, never from the AI.
- **2e. Names and document titles: AGREED.** The number check starts with dates, numbers, amounts and percentages (clear patterns, the most harmful mistakes). Titles and names are left to the NLI check: "See the Treasurer Guidebook" isn't supported by a President Guidebook passage. Later (see the work list): a title check against the document titles already in data/content.db, and a short list of ISACA team and system names.
- **2f. Blocking good answers: AGREED, with three guardrails.** A flagged answer is never just refused: it steps down to ISACA's exact words with the source (this is most of idea 4, the fallback ladder). Guardrails:
  1. **Only accuracy failures step down** (an unsupported sentence, a wrong number). A **rule failure** (the answer crossed a line in the policy adapter, for example tax advice under C-03) follows the rule's own outcome, hand off or stop, never a quote.
  2. **Only a strong-match passage is quoted:** one the search ranked as a strong match for the question. Otherwise the question goes to a person. (Idea 7, "did it answer the question?", covers this more fully.)
  3. **Every step-down is counted** with its reason (which check, which number or sentence), by check, never by person. Developer mode shows the reason; leaders just get the quoted answer.
- **2g. Translation: AGREED (applies only if answers are translated).**
  1. **Always show the English beside the translation** (Matthew: "a huge asset"). The translated answer is labelled as a machine translation; the approved English answer and its English source sit right under it, one tap away. A translation is no longer ISACA's exact words, so the leader can always check against them. This also covers what the number check can't see (a dropped "not", a softened "must").
  2. **The number check runs again after translation:** dates and numbers in the translation must match the approved English answer. If they don't, the leader gets the English answer with a note that the translation couldn't be checked.
  3. Later option, not required: translate the answer back into English and run the NLI check on it (doubles the translation work on the demo laptop).

**Idea 2 is complete.**

## 3. "Must" stays "must": AGREED (November 2)

A fixed check, no AI: an answer may not change how strongly ISACA says something (turning advice into a rule, a rule into advice, or dropping a "not"). To an audit audience that is a real error even when every other word is right. Example: ISACA's "It is strongly recommended that chapters require dual authorization" must not become "Chapters must require dual authorization".

1. **Word groups, not single words:**

   | Strength | Words in the group (starting list) |
   |---|---|
   | Obligation | must, required, shall, is required to, need to |
   | Advice | should, strongly recommended, recommended, encouraged, it is best |
   | Permission | may, can, is allowed, is permitted |
   | Prohibition | must not, may not, shall not, never, is not allowed |

   "Chapters are required to submit" passes for ISACA's "must submit": both are obligations.
2. **Only flag what the answer adds or changes:** the answer may not use a strength its sentence's supporting passage (the NLI link) doesn't have. Many ISACA sentences are plain instructions ("Submit your chapter's compliance report between 1 November and 31 December"): "Chapters must submit…" is flagged, "Submit your report between…" passes.
3. **"Not" is watched closely:** "not required" and "required not to" mean opposite things. Anything ambiguous counts as a mismatch.
4. **The word groups live in a small data file, not in code** (work list W4), built from words that actually appear in ISACA's documents; a missed phrase is a one-line change.
5. **A mismatch is an accuracy failure:** it steps down to ISACA's exact words under the 2f guardrails.

Why it earns its place: the NLI check might accept "must" for "strongly recommended" because the meaning is close; this check is strict about exactly the words auditors read most carefully.

## 4. The fallback ladder, so Isa still answers: AGREED (November 2)

From best to last resort:

1. **Isa's own wording, every check passed.**
2. **Isa's wording with unsupported sentences removed** (idea 1). Only allowed when the answer's **first sentence** survives (usually the direct answer) **and at least half** the sentences survive. Otherwise go to step 3. Fixed limits; the test list can tune them later.
3. **ISACA's exact words, quoted with the source,** for accuracy failures with a strong-match passage only (2f guardrails). Show the work says **"Shown in ISACA's own words"**, a strength like the approved "Answer uses ISACA's exact wording", not an apology.
4. **A person:** no passage worth quoting, or a rule says so.

Refinements:

- **Sentences with no claim are skipped** ("Good question.", "Here's what ISACA's guidance says:"): not checked, not counted, never treated as the first sentence. Recognised by a short fixed list of openers.
- **The AI is told to answer in its first sentence,** with no greetings or filler and only what the passages say (REQUEST FOR WILLIS: in the instructions given to the model).
- **Developer mode shows which sentences were removed and why;** leaders just see the trimmed answer.

## 5. A confidence score built from checkable parts: AGREED (November 2)

Today the confidence score is a placeholder that always returns 0.90. The Sprint 1 proposal promised a "confidence threshold… adjusted by your team, no developer". The score is never the AI's opinion of itself (models are poor judges of their own accuracy). Two layers:

1. **Pass-or-fail gates for the checks that are certain:** numbers (idea 2), "must" stays "must" (idea 3), and the rules. Any failure goes down the ladder (idea 4). No score involved: a wrong date is never outweighed by a strong search match.
2. **A confidence score only for what is uncertain,** each part shown separately in developer mode so the reason is always visible:

   | Part | What it measures | Where it comes from |
   |---|---|---|
   | Search strength | How well the passages found match the question | The search |
   | Support | Share of sentences the NLI check found supported | Idea 1 |
   | Agreement | Whether a passage used is flagged as conflicting with another | Content Health flags, worked out ahead of time (fast; only knows conflicts someone has flagged) |
   | Freshness | Whether a source is out of date | Document dates and Content Health flags, worked out ahead of time |

Firm rules:

- **A source containing "TBD" is never reworded.** If it is the only source, the question goes to a person. Isa never guesses what "TBD" should say.
- **The threshold lives in a settings file ISACA controls,** like the rules files: no developer needed. It is set and checked against the test list, so a value like 0.75 means something measured.
- **A threshold change follows the same governance as a rules file:** who changed it, when, ISACA's approval reference; every past value kept; one-step rollback. The policy adapter then covers all of ISACA's controls the same way.
- **Below the threshold:** down the ladder, to ISACA's exact words if there is a strong-match passage, otherwise a person.

## 6. The checker is never the writer: AGREED (November 2)

- The NLI check uses a **separate, small model** built only to judge whether one sentence is backed by another. It never writes answers, and it is a different model from the one that writes them, so it doesn't share the writer's blind spots. Isa's writing model is never asked to judge its own answer.
- The line for James: "We don't let the AI mark its own homework."
- The fixed checks (numbers, "must" stays "must", rules) use no AI, so they are independent already; only the NLI check needs this rule.
- REQUEST FOR WILLIS: choose the NLI model and test it on the actual demo laptop alongside Ollama before committing to it.

## 7. Did it answer the question that was asked? AGREED (November 2)

An answer can pass every other check and still answer a different question, when the search found the wrong passage. Example: "How many board meetings must we hold each year?" answered from a passage on who presides: "The president presides over board meetings." Every word supported; wrong question.

One idea, two halves:

**Search half: a re-ranker as the main judge** (REQUEST FOR ELIYAS AND WILLIS, part of the move to search on meaning)

- A small model built to judge "does this passage answer this question?" It reads the question and each passage the search found, and scores how well the passage answers it. It never writes anything.
- It runs **before** the AI writes: a poor match goes to the "not found" path (ending with a person), so the AI never words an answer from the wrong passage.
- It judges meaning, not words: "How many times must the board meet?" matches the page 7 passage with no list of equivalents.
- **It also improves answer quality,** Isa's biggest weakness: the search finds candidates, the re-ranker puts the truly relevant one first. One piece doing two jobs.
- It supplies the **search strength** part of the confidence score (idea 5).
- The **"poor match" cut-off** lives in the same ISACA-controlled settings file as the confidence threshold, with the same approval, history and rollback.
- Another small model: test it on the demo laptop alongside Ollama and the NLI model. Measured, not guaranteed: the test list includes "wrong passage" questions.

**Output half: the answer-shape check as a backstop** (REQUEST FOR WILLIS; fixed, no AI)

| The question asks | The answer must contain |
|---|---|
| How many, how much, how often | a number |
| When, by when, what date | a date or time period |
| Who | a role or a team |

A failure goes to the "not found" path, **never** to a quote: the problem is the passage, so quoting it perfectly wouldn't help. The word-based subject check is optional, since the re-ranker does that job better.

**Protecting the rehearsed demo (Matthew):** a re-ranker changes which passage comes first, so it could change rehearsed answers (demo question 2, which goes through the AI, most of all). Before it is switched on, all seven rehearsed questions (rules/tests/rehearsed_demo.json) are run again; any change, even a better answer, is shown to Matthew first; it is switched on only when he is happy with every result or has re-rehearsed.

## 8. Re-check access and privacy on the way out: AGREED (November 2)

Two quick fixed checks on the finished answer, repeating protections that exist earlier ("defense in depth"). They back up two Sprint 1 promises: no leader sees content outside their role, and confidential information is protected.

- **8a. Every cited source must be one the leader may see:** its audience includes the leader's sign-in role (the role filter proposal, point 6). A failure is treated as "not found", so the leader is never told a restricted document exists. Changes nothing today (every document is open to all leaders); it protects once the role filter exists.
- **8b. No contact details the sources don't contain:** any email address or phone number in an answer must appear word for word in its source (ISACA's own support address passes; a made-up one fails and goes down the ladder). Names of people are left to the NLI check (an unsupported name makes the sentence unsupported); stated honestly as a limit.
- **The log never copies the personal detail.** It records that the check fired, which check, and the sentence position, never the address or number itself.
- **An 8a failure is an alarm, not just a refusal:** a restricted source reaching the end means the role filter failed. The leader gets the normal "not found" path; developer mode and the dashboard flag it clearly as a control failure. Leaders never see red; developers do.

## 9. An answer receipt for every answer: AGREED (November 2)

Every answer leaves a short record in the log, so "why did Isa say that on 3 March?" can always be answered. It holds:

- the sources cited
- every check that ran, pass or fail (NLI, numbers, "must" stays "must", rules, answer shape, access and privacy)
- the confidence parts and the threshold in force
- the step of the ladder it landed on (own wording, trimmed, ISACA's exact words, a person)
- **the versions** of everything that shaped it: rules files, settings file, NLI and re-ranker models, content database. Answers depend on what was in force at the time; the receipt shows it even after documents or rules change.

Rules:

- **How, not who.** The receipt records how the answer was checked, not who asked. Whether the question text is kept, and for how long, follows the event log's existing rules and ISACA's retention policy, so the receipt never undoes "your questions to Isa stay private" or "never a named individual".
- **Added, never changed.** The receipts only grow, like "retire, never delete". Later option if ISACA wants it: chain the receipts cryptographically so tampering would show.
- **One record, two views.** Show the work (the leader's view) and the developer trace lookup (Alt+6) both read from the same receipt, so they can never disagree. Builds on the trace ID every question already has.

## 10. Shadow mode for every change: AGREED (November 2)

Before any new check, model, threshold or rule goes live, it runs silently alongside the live one, decides what it *would* do, and changes nothing for leaders. The team compares, and switches over only when the differences are understood and wanted. One change process for everything in Isa; it generalizes the rules impact preview, meaning match's "shadow first", and the re-ranker's rehearsed-question rerun.

1. **On the test list always** (including the seven rehearsed questions); **on live questions only if ISACA wants it.** Shadowing live questions doubles the work, which would slow the demo laptop, so the prototype shadows the test list only.
2. **The comparison shows only the differences:** which questions would change, from what to what, and why; counted by check, never by person.
3. **Going live is recorded:** who approved it, when, what the comparison showed; the old version kept for one-step rollback.
4. **Who approves:**
   - **Policy changes** (rules files, the confidence threshold, the "poor match" cut-off): ISACA's decisions, so going live needs **ISACA's approval reference**, like a rules upload.
   - **Technical changes** (a new NLI or re-ranker model, a fix to a check): the team's work, so going live needs **the team's sign-off**, and the change is listed for ISACA with its shadow comparison.

The line for James: "Nothing changes in Isa without being tested against the old behaviour first, and recorded."

## 11. Answer wording: whole thoughts in ISACA's exact words: AGREED (November 2)

**Today** (Services/Orchestrator/Stages/AnswerExtract.cs, a placeholder): a direct answer is the one sentence, from the passages found, that shares the most words with the question, after "Here's what ISACA's guidance says:". No AI, so the words are ISACA's, but it produces fragments: the PDF text breaks sentences across lines and the code treats each line separately; bullets lose their lead-in ("The Chapter President should:"); and most shared words isn't the same as answering the question.

**The design** (direct answers stay word for word; "Answer uses ISACA's exact wording" stays a strength):

1. **Fix the text at the source:** when the content database is built, join lines broken mid-sentence and attach each bullet to its lead-in. This is also the content clean-up promised to ISACA on November 2.
2. **Quote whole thoughts:** the best complete sentence, plus its lead-in when it is a bullet, plus the sentence before when it starts mid-thought ("This…", "These…", a lowercase letter).
3. **A quality gate on the quote** (fixed rules): a quote that starts mid-sentence, is very short or has no verb is rejected; Isa quotes the whole passage instead, or takes the AI path, where ideas 1 to 10 apply.
4. **Pick the right passage first:** the re-ranker (idea 7) chooses the passage; the answer-shape check applies to direct answers too.

**Safeguards:**

- **Cleaning changes spacing, never words.** A build test: for every document, the words in order (ignoring spaces, line breaks and bullet marks) must be identical before and after cleaning, or the build stops. The cleaning rules are written down so ISACA can see what was done to its text.
- **Nothing that points into the content breaks.** The meaning map (by passage number; demo question 2 depends on it), the "worth saving?" tags (by document and section), saved notebook sources and the Library's Listen all refer to passages. Passage numbers and section names stay stable wherever possible; after the rebuild the existing checks run (the save-hints build warns about missing sections) plus the rehearsed questions; anything that moved is fixed before it goes live.
- **The rehearsed demo:** this would change how demo question 1 reads. Per the presentation decision at the top, nothing is switched on before the presentation.

## Open questions for the team

1. **Are answers translated at all?** Matthew decided on October 20 that Isa's answers stay in English (they come word for word from ISACA's English documents; only "Welcome" is translated). The Sprint 3 plan has Eliyas demonstrating "an Amharic question and answer". Matthew, Willis and Eliyas to settle it; 2g applies only if answers are translated.

## Work list (things to build that come out of these decisions)

| # | What | Who | When |
|---|---|---|---|
| W1 | **Document title check:** every document title in an answer must match a title in data/content.db (`documents.title`). No new list to write: the titles are already there, and the check reads them, so new documents are covered automatically | Willis (code) | Later, if tests show a need |
| W2 | **ISACA names list:** a short list of team and system names Isa may mention (for example Chapter Experience team, ISACA Support, Engage, Chapter 360), taken from ISACA's own documents (the Chapter Systems Overview), for the same kind of check | Matthew and Claude (the list); Willis (the check) | Later, with W1 |
| W3 | **Translated answers:** show the approved English answer and source under every translation, labelled as a machine translation; re-run the number check on the translation | Eliyas (translation), Willis (checks), Matthew (screen) | Only if answers are translated (open question 1) |
| W4 | **Obligation word list:** the four strength groups (obligation, advice, permission, prohibition) as a small data file, built from words that actually appear in ISACA's documents | Matthew and Claude (the list); Willis (the check) | With the output checks |
