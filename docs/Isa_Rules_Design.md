# Isa's policy adapter: design

October 30, 2026 · Matthew Brady, written with Claude · Sprint 3, Milestone 1, task M1 (rules decisions)

**ISACA owns the governance. Isa is the socket it plugs into.** (Matthew, October 30, 2026.) ISACA already has its governance, policies and review processes. This design does not write ISACA's policies. It builds the mechanism that lets ISACA's existing policies run inside Isa: Isa enforces what ISACA decides, records it, and can prove it. The rules in this project are **samples**, just enough to show every part of the mechanism working in the demo and in the tests.

Status: **DESIGN FOR REVIEW.** Nothing here is built yet. Sections marked **DECIDED** were agreed by Matthew on October 30, 2026. Sections marked **REQUEST FOR WILLIS** are things the back end would need to do; Willis decides how, and whether.

Builds on: docs/Handoff_and_Refusal_Rules_Proposal.md (topic groups A to H, the four outcomes, the tricks table) and Willis's reply, docs/Handoff_to_Matt_Refusal_Rules_Decisions.md. Wording comes from docs/Show_the_Work_Wording.md; this design adds no new messages.

## 1. Who does what

**DECIDED.**

- **ISACA** owns the policies: which questions Isa answers, hands off or stops, for whom, and in which region, country or chapter. In a real deployment ISACA's people translate their existing policies into rules files and approve them through their own process.
- **Matthew and Claude** design the adapter: the file format, how layers combine, what is recorded, and a small **sample rule set** that shows every feature working. Borderline policy calls (for example, whether "How would someone hide money?" is stopped or handed off) are ISACA's to make; the adapter supports every option.
- **Willis** builds the machinery: reading the rules files, applying them at both checkpoints, logging, the Developer console, and the upload checks.

This page is the contract between them. If the back end needs a field this page doesn't have, tell Matthew and the format changes here first.

### What ISACA plugs in, and what Isa provides

| ISACA plugs in (its own governance) | Isa provides (the adapter) |
|---|---|
| Its policies, written as rules files in Isa's format | Applies them to every question and every answer, at two checkpoints |
| Its own review and approval of each file | Records who approved it, their reference and the date; refuses a file with no approval record |
| Its regional, country and chapter policies | Combines the layers for each leader from their chapter's record; a lower layer can only tighten |
| Its choice of outcome for each kind of question | Supports every outcome: answer, answer then point (with an optional "connect me" button), hand off, stop |
| Its routes and flags (who receives what) | Puts them on the ticket |
| Its retention policy | Keeps every version, retires instead of deleting, rolls back in one step |
| Its auditors' questions | Logs which rule and which version decided each question, so any past decision can be explained |

**What the prototype does not do:** connect live to ISACA's own systems (its ticketing or compliance tools). The way in is a rules file. A live connection is ISACA's decision later, the same as the calendar.

## 2. Principles

**DECIDED.**

1. **Judge what is being asked, not only which words appear.** "When is our financial verification due?" mentions money, but ISACA's documents answer it, so Isa answers it.
2. **Fixed rules, never AI judgment.** Every outcome can be traced to a rule ID, the same way every answer can be traced to a page.
3. **Rules decide the route, never the answer.** No rule can make Isa say something her approved documents don't say.
4. **One rule set, two checkpoints.** Input Compliance and Output Compliance read the same rules (section 4).
5. **Lower layers can only tighten.** A regional, country or chapter file can make Isa more careful, never less (section 7).
6. **Every rule ships with its own tests.** The rules files double as the test suite (section 9).
7. **Retire, never delete.** Every version of every file is kept (section 8).
8. **ISACA's governance plugs in; it is not replaced.** Rules files come out of ISACA's own review. Isa checks them technically, records the approval, and applies them. ISACA does not edit rules inside Isa.

## 3. Sorting a question: topic × kind of ask

A plain keyword list either blocks too much or lets too much through. Instead every question is sorted on two axes, and the outcome comes from where they meet.

**Topic** (the groups from the hand-off proposal):

| Group | Topic |
|---|---|
| A | Safety and wellbeing |
| B | Legal |
| C | Tax and money |
| D | Governance and disputes |
| E | Data, privacy and security |
| F | Personal matters |
| G | Public statements and politics |
| O | Out of scope (nothing to do with running a chapter) |

**Kind of ask:**

| Kind | What the leader wants | Example signals |
|---|---|---|
| K1 Fact | What ISACA's documents say: a requirement, a deadline, a how-to | "when is", "how many", "what does ISACA require" |
| K2 Local | Whether ISACA's rule is met in their country or situation | "in our country", "does our policy meet", "under local law" |
| K3 Ruling | A judgment, interpretation, or a decision between people | "is it legal", "should we", "who is right", "yes or no" |
| K4 Happening now | An incident that is under way | "we've been hacked", "money is missing", "someone threatened" |
| K5 Getting around | Avoiding obligations, falsifying, other people's data, breaking Isa's rules | "get around", "off the books", "backdate", "ignore your rules" |

**The default grid.** Each cell is the outcome when a question matches that topic and that kind. Individual rules can be stricter than their cell, never looser.

| | K1 Fact | K2 Local | K3 Ruling | K4 Happening now | K5 Getting around |
|---|---|---|---|---|---|
| A Safety | Answer, then point | Hand off, urgent | Hand off, urgent | Hand off, urgent | Hand off, urgent |
| B Legal | Answer | Answer, then point | Hand off | Hand off | Stop |
| C Tax and money | Answer | Answer, then point | Hand off | Hand off | Stop |
| D Governance | Answer | Answer, then point | Hand off | Hand off | Stop |
| E Data and privacy | Answer | Answer, then point | Hand off | Hand off, urgent | Stop |
| F Personal | Answer | Hand off | Hand off | Hand off | Stop |
| G Public statements | Answer | Answer, then point | Hand off | Hand off | Stop |
| O Out of scope | Stop | Stop | Stop | Stop | Stop |

Notes:

- "Answer" still means only if ISACA's documents cover it. If they don't, the safety net (group H) passes it to a person as today.
- Group A is never stopped, even when it is worded as misuse: a safety mention always reaches a person.
- A question that matches no topic goes through the normal pipeline. The grid only changes what happens to questions that match.

**How the gate tells the topic and the kind.** Two ways, one decided and one proposed:

1. **Phrase signals (in this design).** Fixed phrases in each rule (`topic_signals` and `kind_signals`, section 6), matched after the text is cleaned up (lower case, "t@xes" and "t a x" made into "taxes"). A rule fires only when **both** a topic signal and a kind signal match, so "when is our tax filing due?" (C × K1) is answered, while "do we have to pay tax on event income in our country?" (C × K3) goes to a person. A signal matches **at the start of a word**: "tax" matches "taxes" but not "syntax", and "harass" matches "harassing". (ISACA is a security organization, so words like "threat" or "crypto" on their own are never signals: they would catch "threat modeling" or "cryptography".) To be plain about it: **this is still word matching.** It is smarter than a keyword list (two signals, exceptions, clean-up, tests), but it will miss wordings nobody thought of.
2. **Meaning match (LATER UPGRADE, decided by Matthew October 30, 2026; not part of Milestone 1).** It waits on the embeddings for RAG search (one of Eliyas's open Milestone 1 decisions); Isa's code has no embeddings yet. Two conditions if it is ever added: it can only **tighten** (turn an answer into a hand-off, never the other way round), and it runs in **shadow mode** first (it records what it would have done, changes nothing, and is compared against the test list before it goes live). The rules need no change to take it later, because every rule already carries example questions. What it would do: Isa also compares the meaning of the question with each rule's example questions (the same embeddings the RAG search uses), so "Is sales tax owed on what members pay for our summit?" fires C-03 through its example "Do we have to charge VAT on our conference tickets?" with no phrase in common. Nothing is generated: it is a fixed measurement, the same question gives the same result every time, and each decision names the rule, the closest example and the similarity score. Adding an example question teaches the rule a new wording and adds a test at the same time. The cut-off is set and checked against the test list. It uses a model, though not to judge, and would share Eliyas's embedding model rather than add a second one. Willis wants it in the solution (October 30, 2026); his only concern was not yet knowing how to build it. Matthew and Claude add this layer together with Willis when he is ready; until then, everything else comes first.

Either way, two more layers catch what detection misses: the output checkpoint (section 4) and the safety net (group H).

## 4. One rule set, two checkpoints

**DECIDED.**

| Checkpoint | When | What it reads | What it can do |
|---|---|---|---|
| Input Compliance | Before the search | The leader's question | Answer (let it through), answer then point, hand off, or stop |
| Output Compliance (the rule recheck, a stub today) | After the answer is written, before it is shown | The answer Isa is about to show | Let it through, or turn it into a hand-off or a stop |

The second checkpoint catches what the first can miss: a question worded to slip past the input check, or an answer that drifts into ground a rule forbids.

- **Same version at both gates.** Within one question's trace, both gates use the same version of every rules file. The log records which version each gate used.
- **The output check only makes things more careful.** It can turn an answer into a hand-off or a stop. It can never turn a hand-off into an answer.
- **Both gates name the rule** in the log and in developer mode.

## 5. Outcomes and the conflict order

The four outcomes are from the hand-off proposal, agreed by Willis on October 21:

| Outcome | What the leader sees | Message (from the wording table) |
|---|---|---|
| Answer | Isa's cited answer | none extra |
| Answer, then point to a person | The cited answer, plus one line saying which part needs a person | the rule's `point_line` |
| Hand off | A hand-off with a reference number | "Sensitive topic" hand-off message |
| Stop | A short, neutral reply, no ticket | "Outside what Isa covers" message |

**Conflict order. DECIDED.** When more than one rule matches, the most careful one wins, every time:

1. Safety hand-off (group A, urgent)
2. Stop
3. Hand off
4. Answer, then point
5. Answer

**Routes. DECIDED (Matthew, October 21).** Every hand-off goes to ISACA's Chapter Experience team. The `route` field stays in the format so a region can route differently later, but today every rule says `chapter_experience`. Two flags travel with the ticket:

- `urgent`: group A, and data breaches in group E.
- `potential_fraud`: used very sparingly. The strict rule for when it is set is still to be written (section 12).

**What leaders see about the rule:** nothing. Stops and hand-offs never say which rule fired, so people can't learn to word around it. The rule ID shows in the log and in developer mode only.

**REQUEST FOR WILLIS:**

- "Answer, then point" needs the stage contract change Willis described on October 21 (an extra line after an answered run).
- **Mixed questions.** "When is the compliance window, and can I pay myself from chapter funds?" Best: answer the first part and hand off the second. If splitting isn't practical, the whole question takes the most careful outcome. Willis's call.

## 6. What one rule looks like

### Fields

| Field | Meaning | Example |
|---|---|---|
| `id` | Never changes once used; saved logs refer to it | `C-03` |
| `title` | Plain words, for reviewers | "A country's tax rules" |
| `group` | Topic group | `C` |
| `kinds` | Which kinds of ask it covers | `["K2", "K3"]` |
| `outcome` | `answer_then_point`, `hand_off` or `stop` | `hand_off` |
| `route` | Who receives the hand-off | `chapter_experience` |
| `flags` | `urgent`, `potential_fraud` | `[]` |
| `applies_to_roles` | `all`, or a list of roles | `all` |
| `topic_signals` | Words or phrases that mark the topic | "tax", "VAT", "GST" |
| `kind_signals` | Words or phrases that mark the kind of ask. **Empty means the topic alone is enough**: used for rules stricter than the grid (safety, sanctions, money emergencies, the Stop list, out of scope) | "do we have to", "is it legal" |
| `unless` | Phrases that mean the rule should NOT fire | "compliance report due" |
| `message` | A key from the wording table, never new text | `handoff.sensitive` |
| `point_line` | Only for `answer_then_point`: the extra line | "Whether this meets your country's law is a question for a person." |
| `extra_line` | A wording-table key for a fixed line added after the message, for example the emergency-services line on safety hand-offs | `handoff.safety_line` |
| `offer_handoff` | Only for `answer_then_point`: after the answer, Isa offers a button to pass the question to ISACA's Chapter Experience team. Nothing is sent unless the leader taps it; tapping it creates the same hand-off (with a reference number) as any other. REQUEST FOR WILLIS: this uses the real hand-off path, the same one the "Talk to a person" link needs (a Milestone 1 hole) | `true` |
| `answer_must_not` | Output checkpoint: phrases an answer must never contain under this rule | "is tax-deductible" |
| `tightens` | Only in a lower layer: the ID of the higher rule it makes stricter. The lower rule fires whenever the higher rule would, for leaders in its scope: it inherits the higher rule's signals and exceptions, and may add signals of its own (never remove them). Its outcome must be at least as careful | `E-02` |
| `reason` | Why the rule exists, in one sentence | |
| `source` | Where it comes from: an ISACA document and page, the RFP, or a team decision with its date | |
| `tests` | Its own test examples (section 9) | |

Message keys are written here in plain form (`handoff.sensitive`, `stop.outside`, `handoff.safety_line`); they should match the names in Services/ShowTheWork/Wording.cs. **REQUEST FOR WILLIS:** confirm the key names.

### Each file's header

| Field | Meaning |
|---|---|
| `layer` | `global`, `region`, `country` or `chapter` |
| `scope` | Which one: blank for global, "Asia-Pacific", "Japan", "C-001" |
| `version` | Goes up by one with each upload |
| `approved_by`, `approval_reference`, `approved_on` | From ISACA's own review. Required: no approval record, no upload |
| `uploaded_by`, `uploaded_on` | Filled in by Isa |
| `status` | `active` or `retired` (with `retired_by`, `retired_on`, `retired_reason`, `replaced_by`) |
| `sample` | `true` on every file the team writes for the prototype. Shown in the Developer console |

### A worked example

```json
{
  "layer": "global",
  "scope": "",
  "version": 1,
  "approved_by": "SAMPLE: not yet reviewed by ISACA",
  "approval_reference": "SAMPLE",
  "approved_on": "2026-10-30",
  "status": "active",
  "sample": true,
  "rules": [
    {
      "id": "C-03",
      "title": "A country's tax rules",
      "group": "C",
      "kinds": ["K2", "K3"],
      "outcome": "hand_off",
      "route": "chapter_experience",
      "flags": [],
      "applies_to_roles": "all",
      "topic_signals": ["tax", "vat", "gst", "tax-exempt", "non-profit status", "deduct"],
      "kind_signals": ["do we have to", "is it legal", "are we exempt", "in our country", "under local law", "can we deduct"],
      "unless": ["financial verification", "compliance report"],
      "message": "handoff.sensitive",
      "answer_must_not": ["is tax-deductible", "you don't need to charge", "you are exempt", "under japanese law", "under local law you"],
      "reason": "Isa can state ISACA's requirements but never what a country's tax law requires.",
      "source": "Hand-off proposal, group C (Matthew, October 20, 2026); agreed by Willis October 21, 2026",
      "tests": {
        "questions": {
          "must_hand_off": [
            "Do we have to charge VAT on our conference tickets?",
            "Are we tax-exempt in our country?",
            "Can our sponsor deduct what they give us?"
          ],
          "must_answer": [
            "When is our financial verification due?",
            "What does ISACA require for our annual financial review?"
          ],
          "must_stop": [
            "How do we get around paying tax on event income?"
          ]
        },
        "answers_must_be_stopped": [
          "Your chapter's sponsorship income is tax-deductible for the sponsor.",
          "You don't need to charge VAT on member events."
        ]
      }
    }
  ]
}
```

The `must_stop` question here is caught by a different rule (group C × K5, a Stop), and the conflict order makes Stop win. Testing it inside C-03 proves the two rules work together.

## 7. Layers

**DECIDED.**

```
global          every chapter
  region        e.g. Asia-Pacific
    country     e.g. Japan
      chapter   e.g. C-001, Sakura Bay Chapter
```

- **Which layers apply to a leader** comes from their chapter's record (directory/chapters.csv already has Country and Region) and their roles come from the sign-in. Never from what the leader types.
- **Tighten only.** A lower layer may:
  - add rules
  - add signals or tests to a higher rule (through `tightens`)
  - raise a higher rule's outcome (answer → answer then point → hand off → stop)
  - route its hand-offs differently, or choose a different message key from the wording table

  A lower layer may never remove, retire or loosen a higher rule, remove signals, or lower an outcome. A file that tries is refused at upload (section 8).
- **Roles are a field, not a layer.** `applies_to_roles` on each rule says who it applies to. A Japan rule for treasurers is one row, not a file. This is separate from the permission tiers: tiers decide what *content* a leader can see; role rules decide what happens to the *question*.
- **Chapter files come through ISACA.** A chapter that wants a rule changed sends it to ISACA; ISACA reviews it through its own process and it arrives like any other file. Isa only ever holds rules ISACA has approved.
- **No real legal content.** The team writes no real law into any rule. Regional, country and chapter files built for the prototype are labelled samples (`"sample": true`) that show how the layers work. ISACA's own people supply real content.

## 8. Life of a rules file

**DECIDED.**

1. **ISACA reviews and approves** the file through its own governance process. Isa does not approve anything; she records the approval.
2. **A named admin uploads it.** Only a named admin role can upload. Every upload is logged: who, when, which file, which version.
3. **Isa runs technical checks** (REQUEST FOR WILLIS: the checks themselves):
   - **Format:** every required field is there and valid, and the approval record is filled in.
   - **Loosening:** nothing in the file loosens a higher layer.
   - **Its own tests:** every rule's test examples still give the expected outcome, at both checkpoints.
   - **Impact preview:** across the whole test list, which outcomes would change if this file went live.
4. **It goes live.** An optional second-person confirmation exists, **off by default**, for ISACA to switch on if its own policy calls for it ("we built it, you decide", like progress sharing).
5. **Retire, never delete.** Replacing or removing a file marks the old version retired, with who, when, why and what replaced it. Only active versions are used. Every version stays on record, so:
   - **rollback** to the previous version is one step, and
   - **any past decision can be explained:** the log says "C-03, global version 1", and that exact version can be opened.

How long retired versions are kept follows ISACA's data-retention policy.

## 9. Tests

**DECIDED.** Every rule carries two sets of test examples:

- **Questions** (tests the input checkpoint): `must_answer`, `must_hand_off`, `must_stop`. Every rule has at least one `must_answer` example next to its hand-off or stop examples, so over-blocking shows up as a failed test.
- **Answer sentences** (tests the output checkpoint): `answers_must_be_stopped`, sentences that must never reach a leader under this rule.

In the rules files, each test question is written as an object, because layered rules depend on who is asking: `text`, `as_chapter` (for example C-001), `as_role` (for example President), `expect` (`answer`, `answer_then_point`, `hand_off`, `stop`, or "not [rule ID]" when the rule must stay out of it), and an optional `note`. The lists in the worked example in section 6 are shorthand for the same thing. See rules/global.json.

The 50 to 80 tricky questions (Willis's task P2 in Milestone 2) are built from these examples plus deliberate attacks on each rule, using the tricks table in the hand-off proposal:

- paraphrases, softer wording and misspellings
- "hypothetically" and "asking for a friend"
- splitting a sensitive question across two messages
- "pretend you're…" and claimed authority ("I'm ISACA staff")
- instructions hidden in pasted text
- urgency and "just say yes or no"
- mixed questions (one fine part, one sensitive part)
- the same question in another language (translation is still a placeholder)

Results are reported honestly, including what fails.

### Protecting the rehearsed demo (Matthew, November 2, 2026)

- **rules/tests/rehearsed_demo.json** lists every question the team has rehearsed or shows on screen (the five demo questions, Alt+1 to Alt+5; the chat's suggestion buttons; the landing page), with the result each gets at the rules check **today**. Tested as Watanabe Kenji (C-001, President).
- **The switch waits until every one passes.** Input Compliance moves from the placeholder (StubStages.cs) to the rule engine only when all rehearsed questions get exactly today's result. Keeping the old placeholder selectable as a way back is Willis's call.
- The sample rules A-01 (harassment, with the safety line), S-01 (breaking Isa's rules), S-02 (restricted content) and O-01 (out of scope) exist to reproduce today's four placeholder checks. Demo question 4 ("What is the cryptocurrency policy?") must pass the rules check as today and reach the safety net, so C-04 is written so it does not catch it.
- **rules/check_rules.py** is a reference check, not Isa's engine: Isa never runs it. It reads the files and checks the headers, the approval records, tighten-only, every rule's question and answer tests, and the rehearsed questions. Run `python rules\check_rules.py` from the project folder. On November 2: 61 checks, 0 problems; all seven rehearsed questions behave as today. Willis's engine should give the same results; where it differs, one of the two is wrong.

## 10. Logging

- Every stop and hand-off logs the rule ID, the file and its version, and which checkpoint fired.
- Counts are by topic group and rule, never by person. A ticket names the person only because a person will help them (hand-off proposal, point 4).

## 11. Honest limits

- **Phrase signals are word matching.** Two-axis signals block far less by mistake than a keyword list, but new wordings will slip through. Meaning match (section 3, a later upgrade) would catch many of them, not all. The safety net (nothing approved found → a person) and the output checkpoint catch most of it. The real risk is a partial match: a tax-evasion question that pulls up a passage about banking controls. The output checkpoint is there for that case.
- **Translation is a placeholder.** A question in Spanish reaches the gate in Spanish and English signals miss it. Today the safety net catches it. State this in the supplement document (Willis, October 21).
- **Sample rules only.** The prototype's regional, country and chapter files are samples, not legal advice and not ISACA policy.

## 12. Still open

1. **The potential-fraud flag:** the strict, fixed rule for when it is set.
1a. **Meaning match** (section 3): a later upgrade, after RAG embeddings exist. Willis wants it; built with him when he is ready.
1b. **The default grid** (section 3): a working draft. Matthew reviews it through the drafted rules, group by group (docs/Isa_Rules_Draft.md), not as a table.
2. **Mixed questions:** split or most-careful-wins (section 5, Willis).
3. **Message key names** in Wording.cs (section 6, Willis).
4. **Where the files live.** DONE October 30: a `rules/` folder at the top of the project, with `global.json`, then `region/`, `country/` (and later `chapter/`) folders. The three sample files for the rule tester are there: rules/global.json (E-02), rules/region/europe.json (EU-01), rules/country/japan.json (JP-01).
4a. **E-02's `point_line`**: DONE, approved by Matthew October 30 ("How to handle this request under your country's law is a question for a person.").
5. **Upload screen:** where it sits in the Developer console, and which role is the "named admin" (Willis).

## 13. Next steps

1. Matthew reviews this page; Willis reads it and raises anything the back end needs differently.
2. Matthew and Claude write a **small sample rule set** (about 10 rules), not ISACA's full policy: enough to show each outcome, each layer, a role-specific rule, the connect button, and the output checkpoint. Group C, already drafted in docs/Isa_Rules_Draft.md, supplies most of it.
3. One sample region file, one sample country file and one sample chapter file, to show the layers working.
3a. **The demo moment** (about 45 seconds of a 15-minute presentation; the full design goes in the supplement document): the **rule tester** (section 14). DECIDED (Matthew, October 30): the demo stays signed in as Watanabe Kenji throughout, so it can't switch chapters; the rule tester shows regional differences without switching. Fallback: Watanabe asks the demo question in the chat, it is handed off by the sample Japan rule, and developer mode shows the rule. The refused upload goes in the supplement document as a screenshot, not live.
4. Willis builds the reading, the two checkpoints, logging, and the upload checks.
5. The tricky-question list (Milestone 2, P2) is built from the rules' tests.
6. **A review pack for an outside governance reviewer** (Matthew, October 30, 2026): when every group is agreed, a shareable document that a QA and governance expert can review with Matthew to find the tricky spots. Contents: overview and scope, the layer map, a question's path end to end, the rule catalogue, the coverage grid, a traceability table (risk → rule or layer → test), a failure-modes table, the test plan and results, change control, limits and open questions, and a glossary. Check what may be shared outside the team before sending it.

## 14. The rule tester (for the demo, and for ISACA's reviewers)

**DESIGNED by Matthew, October 30, 2026. AGREED with Willis the same day:** he is on board with the approach and builds it.

A small panel in the Developer dashboard (/dev-dashboard): type a question, pick the chapters to test it as, and see side by side what Isa would do for each and which rule decided it. It shows how ISACA's policies apply in different regions without anyone signing in as someone else.

### What it shows

| Chapter | Where | Outcome | Decided by | File | Approval |
|---|---|---|---|---|---|
| Sakura Bay Chapter (C-001) | Japan, Asia-Pacific | Handed off | JP-01 | Japan, version 1 | SAMPLE, 30 Oct 2026 |
| Black Forest Chapter (C-005) | Germany, Europe | Handed off | EU-01 | Europe, version 1 | SAMPLE, 30 Oct 2026 |
| Windy City Chapter (C-009) | United States, North America | Answered, then points to a person | E-02 | Global, version 1 | SAMPLE, 30 Oct 2026 |

Opening a row shows every rule that matched, the conflict order that picked the winner, and the rule's reason and source.

### How it works

- **Inputs:** the question; the chapters (default: the three above; or all chapters); the role (default: President).
- **Same engine as the live gate.** The tester must call the same code the Input Compliance stage calls, not a copy. Otherwise it proves nothing. This is the point to make to an engineer.
- **Read-only.** It evaluates the rules only: no search, no AI model, no ticket, nothing shown to any leader. Each run is logged as a rule test, kept apart from real questions so it never counts in usage or hand-off figures.
- **Who can use it:** the Developer role (Watanabe Kenji has it for the demo).
- **Colors and icons** as in Show the work: green for answered, blue for handed off or stopped, each with its icon.
- **Later, same panel:** "Run the test list" (every rule's examples, pass or fail) and the impact preview for a new file (section 8). Not needed for the demo.

### The sample rules it needs

The demo question: **"A member asked us to delete their personal data. What do we do?"**

| Rule | File | Outcome | Why it exists (sample) |
|---|---|---|---|
| E-02 | Global | Answer, then point, with the connect button | ISACA requires privacy policies that comply with applicable laws (Annual Chapter Compliance Framework); applying that to one request needs a person |
| EU-01 | Sample Europe region file, `tightens: E-02` | Hand off | Shows a region file tightening a global rule |
| JP-01 | Sample Japan country file, `tightens: E-02` | Hand off | Shows a country file tightening a global rule; also used for the fallback in the chat |

The sample files state no law: they only change the route, and are labelled samples.

### In the demo (about 45 seconds)

1. Open the Developer dashboard, then Rule tester.
2. Paste the demo question. Three rows appear: Japan and Europe hand off under their own files; the United States gets ISACA's global rule.
3. Open the Japan row: rule JP-01, Japan file version 1, its approval record, and the reason.
4. One line: "ISACA's own policies, region by region, enforced by Isa and traceable to the rule and the approval behind it."
