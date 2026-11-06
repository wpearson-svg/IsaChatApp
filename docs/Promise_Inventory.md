# Promise inventory

November 2, 2026 · Matthew Brady, written with Claude · Sprint 3, Milestone 1, task M2

**What this is:** every promise ISACA has been given, in one place: what the RFP asks for, what the team's Sprint 1 proposal promised (its controls and metrics), and what the emails to ISACA committed to. It is the base for Milestone 2 ("Promises kept"): Willis's promise tracker (task P1) adds, for each row, how it is tested, the result, and the fix or note.

**Where the "today" column comes from:** the Sprint 2 stage status slide (presentation/slides.json, October 29), Willis's Milestone 1 list, and a search of the code on November 2 (origin/main at d9c5c9d). It is Matthew's best reading, not a test. **Willis and Eliyas: please correct anything about your parts.**

**Status words:**
- **Built:** works in the prototype today.
- **Partly:** some of it works; the rest is a placeholder or missing.
- **Placeholder:** a stand-in (a "stub") that looks right but doesn't do the real work yet.
- **Designed:** written down and agreed, not built.
- **Not started.**
- **Plan:** something we describe (for a pilot or production), not something the prototype must do.

**Sources checked:** docs/ISACA_RFP_AI_Chapter_Support_Agent.md; the Sprint 1 proposal deck (Matthew's copy, "ISACA AI-Enabled Chapter Support Presentation.pptx", September 24, 2026, slides and speaker notes); the October 21 and November 2 emails to ISACA. **Not included:** the Sprint 2 deck, because its audience was i.c.stars staff, not ISACA. Nothing was promised to ISACA out loud, in a meeting or call (Matthew confirmed, November 2). **M2 complete: sent to Willis on November 2** (Claude outputs/Promise inventory - for Willis.md).

---

## 1. The RFP: what the agent should help ISACA do

The RFP's key principle: help chapter leaders find and understand ISACA-approved information while supplementing human Chapter Experience support.

| # | What ISACA asked for | What does it today | Status |
|---|---|---|---|
| R1 | **Govern:** access and understanding of approved chapter guidance | Search of ISACA's approved content with sources shown; the policy adapter for rules (docs/Isa_Rules_Design.md) | Partly |
| R2 | **Automate:** reduce avoidable or repetitive information requests | Direct answers from ISACA's text; onboarding journey and Today note (new leaders' repeat questions) | Partly (answer quality is the gap) |
| R3 | **Interface:** consistency in how approved information is surfaced | One chat with source cards and Show the work; rail links to Engage and Chapter 360 | Built |
| R4 | **Redirect:** human support **only** when questions are out of scope | Hand-offs with a reference number and a Salesforce Case; "answer, then point" designed | Partly ("answer, then point" and the rules are designed, not built) |
| R5 | **Adminable:** a sustainable, scalable governance framework | The policy adapter (ISACA's rules files, approval records, layers, versions) | Designed |

## 2. The RFP: required capabilities and controls (section 3)

| # | What ISACA asked for | What does it today | Status |
|---|---|---|---|
| R6 | **Approved-content grounding:** answers from ISACA-approved sources only; how sources are identified, prepared, connected, updated and retired | Content database built from ISACA's documents (content/); only those passages are searched; the AI writes only from the passages found | Partly (how content is updated and retired isn't written up) |
| R7 | **Answer quality and hallucination mitigation:** unsupported responses, uncertainty, source transparency, pre-launch testing, post-launch monitoring, remediation | Output compliance checks the source is approved and was found; Show the work; "not found" state. The deeper checks (NLI, rule recheck, confidence) are placeholders. Answer wording still produces fragments ("Based on the approved content: throughout the year...") | Partly (Willis's Milestone 1 holes: output compliance, answer wording) |
| R8 | **Content governance:** ownership, approval permissions, versioning and currency, duplicate or conflicting content, preventing unauthorized content | Content Health Dashboard (flags such as TBD entries); the policy adapter's approval records and versions apply to rules, not content | Partly (no approval or version record for content itself) |
| R9 | **Escalation and human hand-off:** recognize what it should not answer and route people to human support | Placeholder keyword check (pizza, "ignore your rules", harassment); safety net (nothing approved found → a person); tickets. Real rules designed and sample rules written (rules/) | Partly (Input Compliance is a placeholder until the rule engine) |
| R10 | **User experience:** intuitive for volunteers worldwide; how they ask, understand answers, see uncertainty, reach sources | Chat, suggestion buttons, source cards, Show the work, "How to use Isa" help, phone layout, Android app | Built |
| R11 | **Security and privacy:** data handling, retention, access controls, confidential information, third-party risks, security testing, issue response | Keycloak sign-in and roles (they decide which screens a person can open); everything runs locally (no outside AI service); questions to Isa stay private in onboarding. **Filtering the documents in an answer by role is not built** (see S1-16b). No written retention policy, security test or issue-response plan | Partly |
| R12 | **Accessibility:** meet ISACA's requirements once finalized | Reduced motion, contrast, 44px tap targets, screen-reader labels, Listen Library | Partly (no formal accessibility check yet: Milestone 2, P2) |
| R13 | **Analytics and measurement:** adoption, usefulness, answer quality, escalation, knowledge currency, user experience, operational impact | Event log and usage log (model, tokens, time); Developer and Content Health dashboards, but labelled "not wired to a real backend", parts simulated | Partly (Willis's Milestone 1 hole: dashboards) |
| R14 | **Maintainability and scalability:** maintained, monitored, governed and scaled across the network; language support later | SQLite, local Ollama, data files ISACA could maintain; translation detects the language only | Partly (translation is Eliyas's Milestone 1 hole) |
| R15 | **AI-use register** (required): tool, provider, model; purpose; information submitted; resulting artifact; human review | Drafted November 6 in docs/AI_Use_Register.md: Matthew's Claude Code use is filled in; teammates' rows are still to confirm | Partly (P7, Matthew) |

## 3. The RFP: phases and expected outputs (section 2)

The team's work is Phase 3, the prototype. Phases 1 and 2 are shown "in miniature"; Phases 4 and 5 are described as a plan.

| # | Phase and what ISACA expects | What we have | Status |
|---|---|---|---|
| R16 | **1. Discovery:** findings, prioritized use cases, requirements, recommended pilot scope | Camille's answers; what new leaders struggle with; onboarding as the pilot use case (new presidents as the pilot group) | Partly (not written up as a findings document) |
| R17 | **2. Knowledge and governance design:** knowledge inventory, governance model, content lifecycle and permissions | The Library (ISACA's documents); content findings (TBD entries, numbering slips); the policy adapter design | Partly (no single knowledge inventory or content lifecycle write-up) |
| R18 | **3. Prototype:** conceptual design, working prototype, configuration and design documentation | The working prototype; design documents in docs/ | Partly (the holes in Milestone 1) |
| R19 | **4. Testing and pilot:** test results, issue log, readiness assessment, pilot, usage and feedback data | Test paths (test-paths.ps1); rules tests (rules/check_rules.py) | Plan (Milestone 2: test runner, defect log, QA report) |
| R20 | **5. Evaluation and production option:** pilot evaluation, production recommendation, operating model, support and knowledge transfer | Nothing yet | Plan (presentation: "what a pilot would measure") |

## 4. Sprint 1 proposal: how Isa works (Solution and cost)

| # | What we promised | What does it today | Status |
|---|---|---|---|
| S1-1 | **Sign in with your ISACA account;** roles: chapter leader, staff, admin | Keycloak sign-in with test accounts; roles from the directory (not ISACA's real accounts) | Built (with test accounts) |
| S1-2 | **Translation: any language** | Detects the language only. NLLB-200 decided (about 200 languages, non-commercial licence) | Placeholder. **Reword:** "any language" is more than the plan delivers |
| S1-3 | **Check 1 reads the question** | Placeholder keyword rules; the policy adapter designed, sample rules written | Placeholder → Designed |
| S1-4 | **Check 2 reads the answer** | Checks the source is approved and was found; NLI, rule recheck and confidence are placeholders | Partly |
| S1-5 | **Search first; AI only if search isn't enough** | Full-text search answers direct hits word for word; the local model only when search isn't enough | Built |
| S1-6 | **Role filter, RAG, knowledge graph, logs** | RAG partly (vector search to come); knowledge graph a placeholder; logs built; role filter not built | Partly |
| S1-7 | **Contact the team: always on screen, one click from any answer, question and history go with it** | "Talk to a person" opens the "connect me" card (built November 2, on Matthew's branch, not yet merged): the leader sees what will be sent (this question and answer, ticked; earlier questions only if ticked; an optional note), and gets a reference number and a Salesforce Case | Built (docs/Paths_To_A_Person.md) |
| S1-8 | **Your team: ticket** | Reference number and a Salesforce Case | Built |
| S1-9 | **Most answers from direct search: no AI call, near-zero cost, word for word** | As promised for questions that match; the search needs every question word in one passage, so many questions miss (being replaced with search on meaning) | Partly |
| S1-10 | **Least expensive model that can answer; frontier model reserved for questions that need one** | One local model through Ollama; no frontier model | Partly. **Reword:** "one local model, no paid service" fits the RFP's commercial clarity better |
| S1-11 | **Every call logged: model, tokens, cost per question, live on the dashboard from day one of the pilot** | Model, tokens and time are logged; no cost (running locally costs nothing per call); dashboards partly simulated | Partly |
| S1-12 | **Your controls: confidence threshold, always-to-a-person list, fallback behaviour, adjusted by your team, no developer** | Developer dashboard shows these controls but they aren't wired up; the policy adapter lets ISACA change the always-to-a-person rules by file, without a developer | Placeholder → Designed |
| S1-13 | **Running lean: search first, right-sized models, graph-cached answers** | Search first and a small local model, yes; no graph cache (knowledge graph is a placeholder) | Partly |

## 5. Sprint 1 proposal: the risk register (a control for every line)

| # | Risk and the control we promised | What does it today | Status |
|---|---|---|---|
| S1-14 | **A wrong answer:** Check 2 compares every answer to approved content; a mismatch is stopped. Sections used and checks passed are visible for any answer | Show the work shows the sections and checks (built); the comparison itself is only "source approved and found" | Partly (the "visible" half is built; the "mismatch is stopped" half needs the real checks) |
| S1-15 | **A sensitive question reaching the AI:** Check 1 with an always-to-a-person list your team owns and can change; routed to a person before the AI sees it | Harassment is routed before the AI (placeholder); the list ISACA owns and changes is the policy adapter | Partly → Designed |
| S1-16a | **Roles decide what a person can open** (part of the role control) | Keycloak roles and the role picker decide who sees developer mode, the dashboards and Backstage; leaders without the role don't see them | Built |
| S1-16b | **A leader seeing content outside their role:** role filter at retrieval; a document outside a leader's tier can't be part of their answer | Not built. Checked November 2: the content database has no tier or role on any document or passage, and the search stage doesn't filter by role, so every signed-in leader's answers draw on all the same passages. The Developer dashboard's tier grid is simulated. (Today every document in Isa's content is meant for all chapter leaders, so nobody sees anything they shouldn't, but the control itself doesn't exist yet) | **Not started. The biggest gap in this list** |
| S1-17 | **The tool showing your team too much:** dashboard shows trends only (topics, regions, time), never a named individual | Usage summaries are counted, not named (a salted hash); onboarding counts by category only. Not checked across every dashboard | Partly (Milestone 2, P4, Eliyas: "no named individuals on dashboards") |
| S1-18 | **A leader who can't get to a person: three paths:** (1) Isa routes low-confidence and out-of-scope questions with context; (2) "this did not answer my question" on every answer; (3) contact the team, always on screen | Checked in the code November 2: (1) **Mostly built:** not-found and sensitive questions go to a person with a reference number and a Salesforce Case; out-of-scope questions are stopped (no ticket), and confidence is a placeholder. (2) **Built November 2:** after "Not quite", Isa offers "Want me to pass it to a person?", which opens the "connect me" card. (3) **Built November 2:** "Talk to a person" opens the same card. Both on Matthew's branch, not yet merged. Onboarding adds the contact step; the "answer, then point" door comes with the rule engine | Built (paths 2 and 3); path 1 mostly built (confidence routing waits on the real confidence score) |

## 6. Sprint 1 proposal: success metrics (defined before the build)

| # | What we promised | What exists | Status |
|---|---|---|---|
| S1-19 | **Accuracy:** a weekly sample of answers checked by your team; we report the number, you set the bar | Nothing to draw the sample yet | Plan (for the pilot; a sampling screen would help) |
| S1-20 | **The right kind of use:** how often leaders bring Isa the questions she's built for, and choose a person for the rest | Event log has outcomes per question | Partly (no report yet) |
| S1-21 | **Escalation quality, not rate:** what was escalated and whether it should have been | Tickets and hand-offs by topic; nobody reviews "should it have been?" yet | Partly |
| S1-22 | **Hours returned:** questions your team no longer answers by hand, hours each month | No baseline, no measure | Plan (needs a baseline from ISACA) |
| S1-23 | **Baseline before the pilot, target agreed in writing, live on your dashboard** | Dashboards partly simulated | Plan |
| S1-24 | **The ask: a pilot,** a small set of chapters, a defined window, your Chapter Experience team as the judges | New presidents as the recommended pilot group (ISA-PROJECT-CONTEXT.md) | Plan (presentation) |

## 7. Sprint 1 proposal: business impact and intended outcomes

Not features, but the presentation will be judged against them.

| # | What we said | What answers it | Status |
|---|---|---|---|
| S1-25 | Business impact: **new chapter leader onboarding** and **the annual leadership rotation and handoff** | The 90-day journey, Today note, succession notebook, handoff checklist (the onboarding feature, a surprise for the presentation) | Built |
| S1-26 | "Unlocking thousands of volunteer and team hours through instant, verified chapter support" | Depends on answer quality and the hours measure (S1-22) | Partly |
| S1-27 | Outcome 1: **leaders doing strategy** | 90-day journey, Today note, key dates | Built |
| S1-28 | Outcome 2: **answers that survive the rotation** | Succession notebook, handoff checklist | Built |
| S1-29 | Outcome 3: **a team freed for the work that needs them** | Hand-offs, contact step, "worth saving?" label | Partly (depends on answer quality and the hand-off path) |
| S1-30 | Slides 6 and 11 to 16 say "**[NUMBER] hours**" | Never filled in | **Fix before reusing:** a real figure, or reword |

## 8. Emails to ISACA

| # | What we committed to | Status |
|---|---|---|
| E1 | October 21: the 11 questions; no feature promises | Done (Camille answered) |
| E2 | November 2: the regular update on **Wednesday, November 4** | Due |
| E3 | November 2: in that update, **progress on the new search (on meaning) and on the rules work** | Rules: real progress (design, sample rules, tests). Search: Eliyas |
| E4 | November 2: content findings (TBD entries, numbering slips) **to be shared in a content review** | Not started |
| E5 | November 2: asked ISACA to approve the governance best-practice guides | Waiting for ISACA |

---

## 9. Gaps that matter most

In order of risk, because an engineer like James will test these first:

1. **Role filter at retrieval (S1-16b, R11).** Promised as a control in the risk register: a document outside a leader's tier can't be part of their answer. Roles already decide which screens a person can open (S1-16a, built); what isn't built is filtering the *documents in an answer* by role. **Proposed to Willis on November 2** (Claude outputs/Role filter - proposal for Willis.md): an audience on each document (none means everyone, so nothing changes today), a filter in search, a test-only sample document, and tests; not shown live, since the demo stays in one role. Otherwise: build a simple version (a tier on each passage, filtered by the leader's role) or say plainly in the presentation that it is designed, not built.
2. **"A mismatch is stopped" (S1-14, R7).** The real answer checks are placeholders, and answer wording still produces fragments. This is the first thing ISACA will judge. **Proposed to Willis on November 2** (Claude outputs/How Isa checks her work - proposal for Willis.md): eleven agreed ideas; designed now, nothing switched on before the presentation, shown in the supplement as designed.
3. **Two of the three paths to a person are incomplete (S1-18, S1-7).** "Not quite" is under every answer but only records feedback, never leading to a person; "Talk to a person" isn't wired up. Both are small to finish and visible in a demo. **Built November 2** (docs/Paths_To_A_Person.md): one "connect me" card; the "Talk to a person" and "Not quite" doors work; the "answer, then point" door comes with the rule engine.
4. **Input Compliance (S1-3, S1-15, R9).** Placeholder today; the rule engine is Willis's, with the sample rules and the rehearsed-demo tests ready.
5. **"Your controls, no developer" (S1-12).** The Developer dashboard's controls look real but do nothing. Anything shown live must work, or carry a "simulated" label.
6. **The AI-use register (R15).** Required by the RFP. Matthew's task (P7).
7. **Dashboards with real numbers (R13, S1-11, S1-20, S1-21).** Willis's Milestone 1 hole.

## 10. Promises to reword rather than build

Small honest changes that cost nothing and protect trust:

- **"Translation: any language" (S1-2)** → "translation through NLLB-200, a self-hosted model covering about 200 languages, including Amharic, Oromo, Somali and Tigrinya" (with the licence caution).
- **"Frontier model reserved for questions that need one" (S1-10)** → "one small model running locally: no paid service, nothing leaves ISACA's environment."
- **"Cost per question" (S1-11)** → "model, tokens and time per question; running locally has no per-call cost."
- **"[NUMBER] hours" (S1-30)** → a real figure, or reword around what a pilot would measure.
- **"Graph-cached answers" (S1-13)** → drop, unless the knowledge graph is built.

## 11. Next

- Willis and Eliyas correct the "today" column for their parts.
- Willis turns this into the promise tracker (P1): for each row, how it is tested, the result, and the fix or note.
- The presentation outline (M5) uses the same rows: "promised" beside "built", section by section.
- Keep the onboarding feature a surprise: this inventory names it, so it goes to the team only until the presentation.
