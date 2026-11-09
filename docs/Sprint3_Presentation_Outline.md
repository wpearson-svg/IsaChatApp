# Sprint 3 presentation outline: promised and built, side by side

November 6, 2026 · Matthew Brady, written with Claude · Sprint 3, task M5

**What this is:** a slide-by-slide outline for the final presentation to ISACA (15 minutes for the whole team, over Teams, internship week 12). It follows the plan saved on October 28 (ISA-PROJECT-CONTEXT.md, "Plan for Sprint 3"). It brings back Sprint 1's five sections and the tracker across the top of the slides, and closes the loop: in each section, what we **promised** in September beside what we **built**.

**Row numbers** (R1, S1-14 and so on) are the ones in docs/Promise_Inventory.md, so this outline, the promise inventory and Willis's promise tracker (P1) all line up.

**The "built" column is updated to November 6** from the inventory (November 2), Willis's merges #43 to #47 and the M6 screen check. It is Matthew's best reading, not a test. **Willis and Eliyas: please correct anything about your parts.**

**Team only until the presentation:** this outline names the onboarding feature, which is a surprise for ISACA.

**Built as slides (November 6, 2026):** the team chose to build Sprint 3 on the Sprint 2 HTML deck. The slides are in presentation/slides.json (open /present). The Sprint 2 deck is kept at /present?deck=sprint2. The slides follow this outline in 18 slides; where they differ, the slides are newer.

Status words, as in the inventory: **Built**, **Partly**, **Designed** (written down, not built), **Plan** (for a pilot), **Reword** (change the promise, not the product).

---

## The story in one breath

"In September we named the problem: questions with documented answers eat volunteer and staff hours, and knowledge is lost at every yearly handover. We promised an agent that answers only from ISACA's approved content, shows its work, and hands off to a person when it should. Here is what we built against each promise, what we didn't, and what a pilot would measure."

The RFP framing (ISA-PROJECT-CONTEXT.md, "Framing the work in the RFP's terms"): in miniature, we did the RFP's first three phases (found the use cases, checked the content, built a working prototype), and we describe what Phases 4 and 5 would test and decide.

## Time plan (15 minutes)

| # | Section (the tracker) | Minutes | Suggested speaker |
|---|---|---|---|
| 0 | Opening and "what we promised" | 1 | Willis (Project Lead) |
| 1 | Business impact | 1 | Matthew (Client Liaison) |
| 2 | Intended outcomes, with the onboarding demo | 3.5 | Matthew |
| 3 | Solution and cost, with the Isa demo | 4 | Willis and Eliyas |
| 4 | Risk and controls | 2.5 | A'keyrah (Quality, Risk) and Matthew (Governance) |
| 5 | Success metrics and the pilot | 1.5 | A'keyrah |
| 6 | Roadmap and the ask | 1 | Willis |
| | Buffer for screen-share switching | 0.5 | |

Speakers follow the team roles on Sprint 1's slide 2. The team decides. Still open: how much of the 15 minutes onboarding gets. This plan gives it about 3.5 minutes inside section 2.

---

## 0. Opening (1 slide, 1 minute)

**Slide: "In September, we made you promises. Here's what we built."**
- One line per section of the tracker, each with a count, for example "Risk and controls: 5 controls promised, 4 built, 1 designed".
- The supplement document goes into the Teams chat now, so ISACA can follow along (ISA-PROJECT-CONTEXT.md, "The supplement document").

---

## 1. Business impact (1 slide, 1 minute)

**Slide: the same problem as in September, one sentence each.**

| Promised (Sprint 1) | Built | Status |
|---|---|---|
| S1-25 Business impacts named: **new chapter leader onboarding** and **the annual leadership rotation and handoff** | The 90-day journey, Today note and calendar, succession notebook, handoff checklist, "Ask the outgoing president" | Built |
| S1-26 "Unlocking thousands of volunteer and team hours" | Direct answers from ISACA's text; the hours measure needs a pilot baseline | Partly |
| S1-30 Sprint 1 slides 6 and 11 to 16 said "[NUMBER] hours" | | **Fix:** a real figure from ISACA's own numbers, or reword to "what a pilot would measure". Don't reuse the placeholder. |

Say: "We kept the same five sections, so you can check every promise."

---

## 2. Intended outcomes, with the onboarding demo (2 slides plus a demo, 3.5 minutes)

**Slide A: Camille's three outcomes, "in your words".**

| Outcome (S1-27 to S1-29) | Built | Status |
|---|---|---|
| Leaders doing strategy | 90-day journey (28 steps, quick wins in week 2), Today note, key dates, month calendar with "Add to my calendar", "Meet your board", board introduction and profile form | Built |
| Answers that survive the rotation | Succession notebook (save any answer with one tap, "worth saving?" label from fixed tags), handoff checklist (the guidebook's 13 items, word for word), "Ask the outgoing president", "Last year around now" | Built |
| A team freed for the work that needs them | The contact step, "Talk to a person", hand-offs with a reference number and a Salesforce Case | Partly (depends on answer quality and a pilot measure) |

**Live demo (about 2 minutes, the sequence in ONBOARDING-FEATURE.md, "The demo"):**
1. Sato Yumiko (outgoing president) asks a question, gets a cited answer and saves the decision with one tap.
2. Watanabe Kenji (incoming, day 3): week-one steps, the contact step with three options, and the Today note's "Last year" row.
3. Kenji opens the notebook and finds Sato's decision waiting.
4. Ana Beatriz Souza (day 60) sees a different next step.

**Slide B (shown for a few seconds): "What's built into the journey".** One slide listing what isn't demoed: Library with Listen, Terms explained, Meet your board, notebook check-ins, handoff checklist, calendar. RFP terms: "Automate" (fewer repeat questions from new leaders) and supporting the Chapter Experience team, not replacing it.

Say: "This isn't in the RFP. It's where your own business impacts pointed: the yearly handover."

---

## 3. Solution and cost, with the Isa demo (2 slides plus a demo, 4 minutes)

**Slide A: the Isa diagram from Sprint 1, with a "built" tick on each box.**

| Promised (Sprint 1) | Built | Status |
|---|---|---|
| S1-1 Sign in with your ISACA account; roles | Keycloak sign-in and roles (test accounts, not ISACA's real accounts) | Built |
| S1-2 "Translation: any language" | NLLB-200, self-hosted; Amharic and Spanish wired in, both ways (Willis's PC) | **Reword:** "NLLB-200, about 200 languages; two wired in today", with the licence caution |
| S1-3 Check 1 reads the question | Rule engine reading ISACA's rules files: layers (global, region, country), tighten only, approval records, versions, 61 self-checks | Built |
| S1-4 Check 2 reads the answer | Sentence-by-sentence support score (HHEM), numbers and dates, "must" stays "must", contact details, answer shape, TBD sources, confidence score, and the fallback ladder (trim, then ISACA's own words, then a person) | Built (needs the HHEM model files on the demo machine) |
| S1-5 Search first; AI only if search isn't enough | Direct answers word for word; search by meaning; the local model only after that | Built |
| S1-6 Role filter, RAG, knowledge graph, logs | RAG with search by meaning: built. Logs: built. Knowledge graph: placeholder. **Role filter: not built** (see section 4) | Partly |
| S1-7 Contact the team, always on screen, one click | "Talk to a person", "Not quite → pass it to a person", and "Pass this to a person" after answer-then-point all open one card that shows what will be sent | Built |
| S1-8 Ticket | Reference number and Salesforce Case | Built |

**Live demo (about 1.5 minutes):** one direct answer with Show the work; one "answer, then point" ("What insurance must we have?"); one hand-off with a reference number. Use the demo questions the team already chose and checked.

**Slide B: "What it costs to run".**

| Promised | Built | Status |
|---|---|---|
| S1-9 Most answers from direct search: no AI call, word for word | Yes for direct hits; search by meaning catches more | Built |
| S1-10 Least expensive model; frontier model when needed | One small local model (phi4-mini) through Ollama; no paid service | **Reword:** "one small model running locally: nothing leaves ISACA's environment" |
| S1-11 Every call logged: model, tokens, cost per question | Model, tokens and time logged | **Reword:** "running locally has no per-call cost" |
| S1-12 Your controls, adjusted by your team, no developer | Four real sliders (confidence, support score, strong match, search pass mark); the rules files, changed without a developer. Other dashboard controls are illustrative | Partly: show only the real sliders live |
| S1-13 Running lean: search first, right-sized models, graph-cached answers | Search first and a small model, yes; no graph cache | **Reword:** drop "graph-cached answers" |

---

## 4. Risk and controls (1 to 2 slides, 2.5 minutes)

**Slide: Sprint 1's risk register, with "built" beside each control.** This is the slide an engineer like James will test first.

| Risk and promised control | Built | Status |
|---|---|---|
| S1-14 A wrong answer: Check 2 compares every answer with approved content; a mismatch is stopped; the work is visible | Show the work, plus the checks and the fallback ladder in section 3 | Built |
| S1-15 A sensitive question reaching the AI: a list your team owns and changes | Rules files ISACA owns (approval record, version, tighten only); sensitive and urgent topics are handed off before search, with the safety line | Built |
| S1-16a Roles decide what a person can open | Keycloak roles decide who sees the dashboards and developer mode | Built |
| S1-16b A leader seeing content outside their role: role filter at retrieval | **Not built.** Today every document in Isa's content is meant for all chapter leaders, so nobody sees anything they shouldn't, but the control itself doesn't exist. Proposed to Willis November 2 (Claude outputs/Role filter - proposal for Willis.md) | **Designed.** Say so plainly, or build the simple version before the freeze |
| S1-17 The tool showing your team too much: trends only, never a named person | Usage counted with a salted hash; onboarding counts by category only. Not checked across every dashboard | Partly (P4, Eliyas) |
| S1-18 A leader who can't get to a person: three paths | All three built: Isa routes not-found and sensitive questions with a ticket; "Not quite" offers a person; "Talk to a person" is always on screen | Built |

Also on this slide or in the supplement (RFP section 3):
- **R11 Security and privacy:** everything runs locally; questions to Isa stay private; no written retention policy or security test yet (Partly).
- **R12 Accessibility:** reduced motion, contrast, 44px tap targets, screen-reader labels, Listen. No formal check yet (Partly; P2).

---

## 5. Success metrics and the pilot (1 slide, 1.5 minutes)

**Slide: "What a pilot would measure", the same four metrics as September, each with what already exists.**

| Promised metric | What exists | Status |
|---|---|---|
| S1-19 Accuracy: a weekly sample checked by your team | 52 real test questions, each tied to a known path (test-questions-candidates.csv); the rules self-check (61 checks) | Plan (a sampling screen would help) |
| S1-20 The right kind of use | Outcome per question in the event log | Partly (no report yet) |
| S1-21 Escalation quality, not rate | Hand-offs by rule and topic, with the rule ID logged | Partly |
| S1-22 Hours returned | No baseline | Plan (needs ISACA's baseline) |
| S1-23 Baseline before the pilot, target agreed in writing | | Plan |

Pilot scope (R16, S1-24): **new chapter presidents**. They change every year, need the most help early, and are a clear group to measure: time to first contact with the Chapter Experience team, repeat questions in the first 90 days, steps where leaders get stuck, hand-offs by topic.

Also name: the **testing and QA report** (R19, Milestone 2) and the **AI-use register** (R15, required by the RFP; Matthew's task P7, **not started as of November 6**).

---

## 6. Roadmap and the ask (1 slide, 1 minute)

**Roadmap (labelled planned):** day 30, 60 and 90 check-ins; the Chapter Experience staff view with ISACA's on/off switch; journeys for other board roles; Listen in the notebook; downloadable audio; the role filter (if not built); content approval and versioning for content itself (R8); Android app and Chrome extension.

**The ask, the same as September (S1-24):** "A pilot: a small set of chapters, a defined window, your Chapter Experience team as the judges."

---

## Before the team builds the slides

1. **Willis and Eliyas confirm the "built" column** for their parts: translation, search by meaning, the answer checks, dashboards.
2. **Decide the role filter (S1-16b):** build the simple version before the freeze, or present it as designed. It's the one promised control that doesn't exist.
3. **Fix "[NUMBER] hours" (S1-30)** and make the four rewordings (S1-2, S1-10, S1-11, S1-13).
4. **The demo machine.** On November 6, Willis's PC (all of Isa's services) wasn't answering, and on Matthew's laptop the AI-model paths couldn't run (no translation, no search vectors, no HHEM model). Decide which machine runs the demo and set up everything it needs there. Rehearse on it, and record the backup video in week 11.
5. **docs/Supplement_Items.md: done (November 6).** The answer checks have moved out of the "designed" list and are marked built, with caveats.
6. **The AI-use register (R15, P7): drafted November 6** in docs/AI_Use_Register.md. Matthew's Claude Code rows are filled in. Matthew still has to confirm two points (survey material, the chat app's model), and Willis, Eliyas and A'keyrah each fill in their own row.
7. **Matthew's M6 findings** (Claude outputs/M6 findings - for Willis's Claude.md): if the translation fallback isn't fixed, keep Spanish and Amharic out of the live demo.
