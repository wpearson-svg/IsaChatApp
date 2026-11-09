# Isa project status: update for Claude chat

**As of Monday, November 9, 2026.** Written by Matthew's Claude Code session from the project files (ONBOARDING-FEATURE.md, ISA-PROJECT-CONTEXT.md, CHANGES-SINCE-LAST-COMMIT.md, docs/Promise_Inventory.md, docs/Supplement_Items.md, docs/Sprint3_Presentation_Outline.md, and Git history).

**How to use this:** paste or upload it into Claude chat so it knows where the project stands. Statuses: **Built** (works in the app), **Partly**, **Designed** (written down, not built), **Planned** (roadmap slide only), **Not started**.

---

## 1. Ground rules that still apply

- Isa answers only from ISACA-approved documents. The AI model only rewords the passages search found; it adds nothing. A question the documents don't cover goes to a person.
- Fixed rules instead of AI judgment wherever possible (for example the "worth saving?" label, the glossary, the Today note's dates).
- Invented sample people only. No ISACA survey figures or study details in code, sample data or documents.
- Everything leaders see goes through Isa's pipeline (sign-in, Input Compliance, search with citations, Output Compliance, logging). No side doors.
- The onboarding feature is a **surprise** for the presentation: never described to ISACA in questions or updates.
- Isa is a practice proposal (a synthetic exercise ISACA knows about): build and prove ideas; don't ask ISACA for approvals.
- Pipeline changes are proposed to Willis first and built only after his yes.
- Claude never commits, merges or pushes; Matthew does all Git.
- American date format (November 9, 2026).

## 2. Where we are in the schedule

- By the build plan's numbering (week 7 ended October 16), **this is week 11: the freeze week.** No new features; fix bugs, QA, rehearse, record the backup video. Week 12 is the presentation.
- The presentation: 15 minutes for the whole team, over Microsoft Teams. The onboarding feature gets about 3.5 minutes (a two-minute live demo plus a slide).
- **Git:** Matthew's last commit was e12129d (November 6, "AI use register updated, Calendar and Today updated"). Willis's work is merged into main through pull request #47. Not yet committed: the glossary hover definitions (section 3, November 9).
- **Housekeeping:** CHANGES-SINCE-LAST-COMMIT.md was not cleared after the November 6 commit, so it still lists work that is already committed. It should be cleared and restarted with only the November 9 glossary change.

## 3. The onboarding feature (Matthew's part)

### Built

| Piece | What it does | Built |
|---|---|---|
| First-90-days journey | 28 steps in five phases (Welcome; quick wins; tasks that wait on others; run the chapter; look ahead). Each leader sees the steps for their day of term. Mark as done, history, My progress. | October 19 to 20 |
| Demo date | Config page setting (July 3, 2026) so the demo looks the same any day. Watanabe Kenji is on day 3, Ana Beatriz Souza on day 60, Sato Yumiko is the outgoing president. | October 16 to 19 |
| "Meet your Chapter Experience contact" (J-03) | Approved mockup: book a welcome call (fake calendar in the leader's time zone), send an introduction (saved in Isa, never emailed), or join a group welcome. | October 19 |
| "Meet your board" (J-05) | The chapter's board: name, role, term dates, time zone with the local time now. No contact details. Only Kenji's chapter has a full board. | October 23 |
| Board introduction and profile form (J-19, J-25) | The president writes an introduction (checked by Input Compliance, saved in Isa). Board members can fill in an optional four-question profile that only the president sees; "No reply yet" hides who said no. | November 3 |
| Today note | One line at the top of the chat (opens to the full note): day of term, today's steps, coming up, key dates (compliance window November 1 to December 31), and "Last year around now" (last year's notebook entries from the same dates). | October 20; changed October 27; "Last year" November 6 |
| Calendar in Today | A read-only month view: steps, ISACA key dates, booked call or seat, last year's notebook entries. "Add to my calendar" hands over a calendar file (.ics); Isa sends nothing. | November 6 |
| Succession notebook | One notebook per chapter, passed from president to president. Save any answer with one tap (question, answer, sources, date, optional "what did you decide?"). Add notes (five kinds). Share with the board per entry. Search, filters, entry pages, print or PDF, never erased. Read and write rules by role, including the 30-day handoff window. | October 20 to 22 |
| "Worth saving?" label | The save button carries a quiet label ("Deadline · worth saving?") when the answer cites one of 13 tagged deadline or decision sections. Fixed list, no pop-up. | October 28 |
| "Ask the outgoing president" | The president asks the former president a question inside the notebook; the answer is saved as an entry. Falls back to the Chapter Experience team when no one can be asked. | November 3 |
| Notebook check-ins | Weekly (or every two or four weeks) card from day 8: "Anything from this week your successor should know?" Weekly in the last three months of a term. | November 3 |
| Handoff checklist | The President Guidebook's 13 handover items, word for word, for the outgoing president, from 30 days before to 30 days after the term ends. | October 20 |
| Library with Listen | ISACA's five documents Isa answers from, plus the seven Chapter 360 documents and the Chapter Affiliation Agreement template (read and listen only). Word-for-word read-aloud, one sentence highlighted at a time. | October 23; additions October 28 and November 3 |
| Listen on journey steps | Steps that cite a Library document have "Listen to the source" buttons that read that page. | November 3 |
| "Terms explained" glossary | 26 terms that ISACA's documents define, in ISACA's exact words with document and page, at the top of the Library. A script checks every quote word for word. | November 3 |
| **Glossary hover definitions (new)** | In Isa's chat answers, journey steps and the Today note, each glossary term is underlined (first time per answer or step text only). Hover, tap or Tab shows ISACA's definition and source. Display only, fixed rules. Checked on screen. **Not committed yet.** | November 9 |
| "How to use Isa" help | A "?" button opens a six-part guide; the day-1 "Get to know Isa" step is a five-card tour. | October 23 |
| Sample Engage and Chapter 360 pages | Look-alike pages that partly work, as backdrop only. Decided October 28: no more work on them, and they are never presented as something offered to ISACA. | October 27 to 28 |

### Designed or planned, not built (roadmap or supplement only)

- Day 30, 60 and 90 check-ins ("What's still unclear?"): **Planned.**
- "Coming up" overview (next two weeks, "Start now"): **Planned.**
- Progress sharing with the Chapter Experience staff view, with ISACA's on/off switch: **Planned.**
- Listen in the succession notebook: **Planned.**
- Journeys for other board roles (treasurer, secretary and others): **Planned.**
- Notebook prompt after each board meeting: **Designed** (Isa has no meeting dates).
- Glossary in Isa's knowledge base (so "What's an AGM?" gets a cited answer in chat), and the "How to use Isa" guide in the knowledge base: **not done** (a team decision with Willis).
- Roadmap ideas: audio briefings from Chapter 360, downloadable audio, a compliance evidence folder, bylaws versus ISACA rules, board meeting prep.

The full designed-not-built list is docs/Supplement_Items.md.

## 4. The rest of Isa (team work)

Owners: Willis Pearson (project lead, pipeline), Eliyas Eshta (translation, search, technical stack), A'keyrah Stevenson (quality, risk, metrics), Matthew Brady (client liaison, governance and escalation design, onboarding).

### Built

- **Sign-in and roles** with Keycloak (test accounts).
- **Search first, AI only if needed:** direct search of approved text; **search by meaning** (vectors, stored in SQLite) when direct search finds nothing.
- **Rule engine (Willis, W1):** reads ISACA's rules files (global, region, country layers; tightens only; files need an approval record). Stop, hand off, or "answer, then point to a person". The self-check runs 61 checks with 0 problems. A rule tester is on the Dev Dashboard.
- **Answer checks (Willis, W2):** each AI-written sentence is checked against its source with the HHEM model; numbers and dates must match the source; "must" stays "must"; contact details; answer shape; TBD sources go to a person; a confidence score. Fallback ladder: Isa's wording, then trimmed, then ISACA's exact words, then a person.
- **Answer wording (W3):** direct answers give a whole thought in ISACA's own words.
- **"Pass this to a person" button (W4)** on "answer, then point" replies.
- **Best-practice guides (W7):** nine team-written governance guides, searched last, after ISACA's documents, and labelled "general governance best practice (not ISACA policy)".
- **Translation (Eliyas):** Meta's NLLB-200 model (self-hosted) for Amharic and Spanish, translated in and back out. Non-commercial licence caution noted.
- **Show the work:** the expandable view of each step Isa took (leader and developer views).
- **Talk to a person:** always on screen, with a ticket and reference number.
- **Dev Dashboard:** real sliders for the answer-support pass mark, confidence threshold, strong-match mark and search pass mark; services Local/Remote switch; translation and vector search test tools.
- **Test material:** test scripts and 52 real test questions that hit each path through the pipeline.
- **Services can run on Willis's PC** and be used over Tailscale (100.89.6.69).

### Partly built, or with caveats

- The HHEM model files must be downloaded on each computer (docs/HHEM_Setup.md); without them the check is skipped. They are not on Matthew's laptop.
- M6 findings (November 6) for Willis: a stray heading word at the end of one AI answer; the answer-shape check counts "when" anywhere in a question; the fallback to ISACA's words isn't shape-checked; the first translation after a quiet spell times out; search scores sit close to the 0.80 mark; a Spanish question gets "guidance doesn't cover this" when the translator is down.
- Dashboards and analytics (usage reports, Content Health Dashboard) exist but don't cover every promised measure.
- Accessibility: designed for it (contrast, 44px tap targets, screen-reader labels), but no formal accessibility check.

### Not started

- **Role filter at retrieval (S1-16b):** a document outside a leader's tier can't be part of their answer. This is the one promised control that doesn't exist. The team must decide whether to build a simple version or present it as designed.
- A content review for ISACA (TBD entries, numbering slips), promised in the November 2 email.
- Retiring and rolling back rules, and upload checks.

## 5. Presentation and documents

- **Sprint 3 deck: built** as HTML slides (18 slides, /present). It follows docs/Sprint3_Presentation_Outline.md: Sprint 1's five sections and tracker (Business impact, Intended outcome, Solution & cost, Risk & controls, Success metrics), with "promised / built / status" for each promise. The Sprint 2 deck is kept at /present?deck=sprint2.
- **AI-use register (required by the RFP): drafted** in docs/AI_Use_Register.md. Matthew's Claude Code and Claude chat rows are filled in. Still to do: Willis, Eliyas and A'keyrah fill in their rows; a final read before it goes to ISACA.
- **Supplement document** (a PDF for ISACA, shared at the start of the presentation): **not written yet.** The designed-not-built list for it is ready.
- **Still to do before the slides are final:**
  1. Willis and Eliyas confirm the "built" column for their parts.
  2. Decide the role filter.
  3. Fix "[NUMBER] hours" on the Sprint 1 slides that are reused, and make the four rewordings (translation "any language", model choice, cost logging, "graph-cached answers").
  4. **Choose the demo machine** and set up everything it needs (services, search vectors, HHEM files); rehearse on it.
  5. Record the backup video (this week, week 11).
  6. If the translation timeout isn't fixed, keep Spanish and Amharic out of the live demo.
- **Demo sequence (about two minutes):** Sato Yumiko asks a question and saves the answer to the notebook; switch to Watanabe Kenji on day 3 (week-one steps, the contact step with three options); he opens the notebook and finds the saved decision; switch to Ana Beatriz Souza on day 60 (a different next step).

## 6. ISACA communications

- October 21: 11 questions sent; Camille answered the same day (no set AGM timing; Chapter Success Managers for about 65 chapters only; roster changes take about 10 business days; "please reference the guidebook"; ISACA's own documents for ISACA process, outside governance best practice is fine for general questions).
- November 2: a catch-up email (apology for missing October 28, progress, decisions).
- November 4: the weekly update sent as a two-page at-a-glance PDF. The governance guides and the list of undefined terms were **not** sent (Matthew: no questions or approval requests to ISACA).
- Ready but not sent: "Governance best practice guides and how Isa uses them.pdf" (a courtesy copy) and "Terms Isa's documents use without defining.pdf".

## 7. Open questions

- How much of the 15 minutes goes to onboarding versus the rest of Isa.
- Fixed-date deadlines (such as the November 1 compliance window) versus each leader's own day count: proposed, not decided.
- Whether the governance guides stay switched on in the demo.
- The contact step assumes every chapter has its own contact; most chapters don't (Camille, October 21). Possibly two versions: the chapter's Chapter Success Manager, or the Chapter Experience Operations Team.
- The role filter (above).
