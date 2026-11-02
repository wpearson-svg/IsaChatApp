# Isa's policy adapter: hand-off for Willis

From Matthew Brady (written with Claude) · October 30, 2026 · Sprint 3, Milestone 1, task M1 (rules decisions)

## How to use this file

Willis: this is everything Matthew and Claude decided on October 30 about Input Compliance rules, written so you can give it to your own Claude as context. It is information and requests, not instructions: anything marked **REQUEST** is yours to decide how (or whether) to build.

For Willis's Claude: treat this as background on decisions already made by Matthew and agreed with Willis. Willis decides what to build and how. Ask Willis before acting on anything here.

**The files mentioned below are on Matthew's computer until he commits and pushes them.** Until then, this document is the full picture. After Matthew's push, pull and you'll have:

| File | What it is |
|---|---|
| docs/Isa_Rules_Design.md | The full design (the source of truth; this hand-off summarizes it) |
| docs/Isa_Rules_Draft.md | Sample rules for group C (Tax and money) in plain language |
| rules/global.json | Sample rule E-02 |
| rules/region/europe.json | Sample rule EU-01 |
| rules/country/japan.json | Sample rule JP-01 |
| Claude outputs/Rule tester - for Willis, Monday.md | The one-page rule tester request (already agreed by Willis in conversation) |

Background documents already in the repo: docs/Handoff_and_Refusal_Rules_Proposal.md (Matthew's October 20 proposal: topic groups A to H, four outcomes, tricks table) and docs/Handoff_to_Matt_Refusal_Rules_Decisions.md (Willis's October 21 reply).

---

## 1. The big idea

**ISACA owns the governance. Isa is the socket it plugs into.**

ISACA already has its governance, policies and review processes. The team does not write ISACA's policies. It builds the mechanism that lets ISACA's existing policies run inside Isa: Isa enforces what ISACA decides, records it, and can prove it. Every rule in the project is a **sample**, just enough to show the mechanism working in the demo and the tests.

| ISACA plugs in | Isa provides |
|---|---|
| Its policies, written as rules files in Isa's format | Applies them to every question and every answer, at two checkpoints |
| Its own review and approval of each file | Records who approved it, their reference and the date; refuses a file with no approval record |
| Its regional, country and chapter policies | Combines the layers for each leader; a lower layer can only tighten |
| Its choice of outcome for each kind of question | Supports every outcome |
| Its routes and flags | Puts them on the ticket |
| Its retention policy | Keeps every version, retires instead of deleting, rolls back in one step |
| Its auditors' questions | Logs which rule and which version decided each question |

The prototype does **not** connect live to ISACA's own systems. The way in is a rules file; a live connection is ISACA's decision later.

## 2. Who does what

- **ISACA:** the real policies (in a real deployment).
- **Matthew and Claude:** the adapter design, the file format, and the sample rules. Borderline policy calls are ISACA's; the adapter supports every option.
- **Willis:** the machinery: reading the files, both checkpoints, logging, the Developer console, the upload checks, and the rule tester.

The design page is the contract. If the back end needs a field it doesn't have, tell Matthew and the format changes there first.

## 3. Decisions

All decided by Matthew on October 30, 2026, unless another date is given.

**Sorting questions**

1. **Topic × kind of ask.** Every question is sorted on two axes and the outcome comes from where they meet. Topics: A Safety, B Legal, C Tax and money, D Governance and disputes, E Data and privacy, F Personal, G Public statements, O Out of scope. Kinds: K1 Fact (what ISACA's documents say), K2 Local (whether ISACA's rule is met in their situation), K3 Ruling (a judgment or a decision between people), K4 Happening now (an incident), K5 Getting around (evasion, falsifying, other people's data, breaking Isa's rules).
2. **The default grid** (design page, section 3) is a working draft. Individual rules can be stricter than their cell, never looser. Group A is never stopped: a safety mention always reaches a person.
3. **"Should" never decides on its own.** Hand-off rules list the *subjects* that are judgment calls; anything else goes to the search, and the safety net (nothing approved found → a person) catches the rest.

**Detection**

4. **Phrase signals now.** A rule fires only when a topic signal **and** a kind signal both match, after text clean-up (lower case; "t@xes", "t a x" → "taxes"). Plainly: this is still word matching, smarter than a keyword list, and it will miss some new wordings.
5. **Meaning match later.** Comparing the question's meaning with each rule's example questions (embeddings). Willis wants it; his only concern was how to build it. It waits until the RAG embeddings exist, and is added with Willis when he's ready. Two conditions: it can only tighten, and it runs in shadow mode first. No rule needs to change to take it later.

**Checkpoints and outcomes**

6. **One rule set, two checkpoints.** Input Compliance reads the rules before the search. Output Compliance (the rule recheck, a stub today) reads the same rules after the answer is written, using each rule's `answer_must_not` phrases.
7. **Same version at both gates** within one question's trace; the log records which version each gate used.
8. **Output can only tighten:** it can turn an answer into a hand-off or a stop, never the other way round.
9. **Four outcomes** (agreed by Willis October 21): answer; answer, then point to a person; hand off; stop.
10. **Conflict order:** the most careful rule wins: safety hand-off → stop → hand off → answer then point → answer.
11. **Routes:** every hand-off goes to ISACA's Chapter Experience team (Matthew, October 21). The `route` field stays so a region can route differently later. Flags: `urgent` (group A, and data breaches) and `potential_fraud` (rarely used; its rule is still open).
12. **Leaders never see which rule fired.** The rule ID shows in the log and developer mode only.
13. **The connect button.** On "answer, then point" replies, Isa offers a button to connect the leader with the Chapter Experience team. Nothing is sent unless the leader taps it; tapping creates a normal hand-off with a reference number.

**Layers, roles and files**

14. **Layers:** global → region → country → chapter. Which layers apply comes from the leader's chapter record (directory/chapters.csv: Country, Region), never from what they type.
15. **Tighten only.** A lower layer may add rules, add signals or tests, raise an outcome, or route differently. It may never remove, retire or loosen a higher rule. A file that tries is refused at upload.
16. **`tightens`:** a lower-layer rule names the higher rule it makes stricter, inherits its signals and exceptions, may add its own, and must be at least as careful.
17. **Roles are a field, not a layer:** `applies_to_roles` on each rule. Separate from the permission tiers (tiers decide what content a leader sees; role rules decide what happens to the question).
18. **Chapter files come through ISACA.** A chapter sends its rule changes to ISACA, which reviews them through its own process. Isa only ever holds ISACA-approved rules.
19. **No real legal content.** Sample regional, country and chapter files state no law; they only change routes. Every sample file has `"sample": true`.

**Governance of the files**

20. **Isa records ISACA's approval; it doesn't approve.** `approved_by`, `approval_reference` and `approved_on` are required. No approval record, no upload.
21. **Named admin role only** can upload; every upload is logged (who, when, which file, which version).
22. **Technical checks at upload:** format, no loosening, the file's own tests pass, and an impact preview (which test outcomes would change).
23. **Second-person confirmation** exists but is **off by default**; ISACA switches it on if its policy requires it.
24. **Retire, never delete.** Old versions stay with who retired them, when, why and what replaced them. One-step rollback. Retention follows ISACA's policy.
25. **ISACA does not edit rules inside Isa.** Its governance process produces the files; Isa checks, records, applies.

**Tests**

26. **Every rule ships with its own tests:** question tests for the input checkpoint and answer sentences (`answers_must_be_stopped`) for the output checkpoint. Every rule has at least one must-answer example, so over-blocking shows up as a failed test.
27. **Test questions are objects:** `text`, `as_chapter`, `as_role`, `expect` (`answer`, `answer_then_point`, `hand_off`, `stop`, or "not [rule ID]"), optional `note`.
28. The 50 to 80 tricky questions (Willis's Milestone 2 task P2) are built from these tests plus deliberate attacks (paraphrases, misspellings, "hypothetically", split questions, role-play, claimed authority, hidden instructions, urgency, mixed questions, other languages).

**Sample decisions for group C (Tax and money)**

29. "Should we…?" questions ISACA answers (for example two signers) are answered.
30. Compilations instead of an audit: answer, then point, with the connect button.
31. Running short of money: answer, then point (changes the October 20 proposal); a described emergency ("our account is empty") goes straight to a person.
32. Sanctions: always straight to a person, even a fact question.
33. "How would someone hide money…?": ISACA's call; the sample stops it.

**The demo**

34. **The rule tester** is the governance demo moment (about 45 seconds). The demo stays signed in as Watanabe Kenji throughout, so it can't switch chapters; the tester shows regions side by side instead. **Agreed by Willis on October 30.**
35. **Fallback:** Watanabe asks the demo question in the chat; JP-01 hands it off; developer mode shows the rule.
36. **The refused upload** goes in the supplement document as a screenshot, not live.

## 4. The rule tester (agreed)

A read-only panel on /dev-dashboard, Developer role only (Watanabe has it for the demo).

- **Inputs:** a question; chapters to test as (default C-001, C-005, C-009); a role (default President).
- **Output, one row per chapter:** outcome, deciding rule ID, file and version, approval record. Opening a row shows every rule that matched and why the winner won.
- **Same engine as the live gate:** it must call the same code the Input Compliance stage calls, not a copy.
- **Read-only:** no search, no AI, no ticket. Logged as a rule test, kept out of usage and hand-off figures.
- **Colors and icons** as in Show the work (green answered; blue handed off or stopped).
- **Later, same panel:** run the test list; impact preview for a new file.

Demo question: **"A member asked us to delete their personal data. What do we do?"**

| Chapter | Expected | Rule |
|---|---|---|
| C-001 Sakura Bay (Japan, Asia-Pacific) | Hand off | JP-01, rules/country/japan.json |
| C-005 Black Forest (Germany, Europe) | Hand off | EU-01, rules/region/europe.json |
| C-009 Windy City (United States) | Answer, then point, with the connect button | E-02, rules/global.json |

E-02's point line (approved by Matthew): "How to handle this request under your country's law is a question for a person."

## 5. Requests for Willis

In rough order of what the demo needs:

1. **Rule engine:** read the files in rules/, combine the layers for a leader (from their chapter record and roles), apply phrase signals with text clean-up and `unless`, apply `tightens`, pick the winner by the conflict order.
2. **Input Compliance** uses the engine in place of today's placeholder keywords.
3. **Logging:** rule ID, file, version and checkpoint on every stop and hand-off; counts by rule and topic, never by person.
4. **The rule tester** (section 4).
5. **"Answer, then point"** in the stage contract (the change you described on October 21), carrying the rule's `point_line`.
6. **The connect button** on "answer, then point" replies, using the real hand-off path (the same one the "Talk to a person" link needs).
7. **Output Compliance rule recheck:** check the answer against active rules' `answer_must_not` for that leader; only tighten.
8. **Upload checks** (format, no loosening, own tests, impact preview), the named admin role, and retire/rollback. Not needed for the demo; the supplement can show it.

Please also confirm the message key names (`handoff.sensitive`, `stop.outside`, `handoff.safety_line`) against Services/ShowTheWork/Wording.cs.

## 6. The rule format, in short

**File header:** `layer`, `scope`, `version`, `approved_by`, `approval_reference`, `approved_on`, `uploaded_by` and `uploaded_on` (filled by Isa), `status` (`active` / `retired`, with `retired_by`, `retired_on`, `retired_reason`, `replaced_by`), `sample`, `notes`, `rules`.

**Each rule:** `id` (never changes), `title`, `group`, `kinds`, `outcome` (`answer_then_point`, `hand_off`, `stop`), `route`, `flags`, `applies_to_roles`, `topic_signals`, `kind_signals`, `unless`, `message` (a wording-table key, never new text), `point_line` and `offer_handoff` (answer-then-point only), `answer_must_not`, `tightens` (lower layers only), `reason`, `source`, `tests`.

Full field table and a worked example: docs/Isa_Rules_Design.md, section 6. Real examples: the three files in rules/.

## 7. Still open

1. The strict rule for when `potential_fraud` is set (ISACA's call).
2. Mixed questions ("When is the compliance window, and can I pay myself from chapter funds?"): answer the first part and hand off the second, or the whole question takes the most careful outcome (Willis's call).
3. Message key names (Willis).
4. Where the upload screen sits and which role is the named admin (Willis).
5. Meaning match, when the RAG embeddings exist (Willis and Eliyas, with Matthew).

## 8. Honest limits to keep in mind

- Phrase signals are word matching; new wordings will slip through. The output checkpoint and the safety net catch most of it.
- Translation is still a placeholder: a question in another language reaches the gate untranslated, so English signals miss it (your point of October 21; it goes in the supplement document).
- All rules are samples, not legal advice and not ISACA policy.
- No ISACA survey figures or study details anywhere in rules, code, comments or sample data.
