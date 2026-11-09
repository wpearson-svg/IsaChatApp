# Sprint 3, M3 and M4: what Matthew decided and built

November 3, 2026 · Matthew Brady, written with Claude · for Willis (and his Claude)

**In one line.** M3 is decided and M4 is built. Six small onboarding pieces were added before the presentation, all inside the onboarding feature, with **no change to the pipeline**. Everything else not started goes on the roadmap, labelled planned.

The files named here reach you only after Matthew commits and pushes. His change list (CHANGES-SINCE-LAST-COMMIT.md) has every file.

## M3: the decisions

| Item | Decision |
|---|---|
| Sample Engage and Chapter 360 pages | Stay as they are, no more building (decided October 28, after your point about the RFP). |
| "Ask the outgoing president" | **Built** |
| Listen on journey steps | **Built** |
| Chapter Affiliation Agreement in the Library | **Built** (read and listen only) |
| ISACA terms glossary ("Terms explained") | **Built** (ISACA's words only) |
| Board introduction and profile form | **Built** |
| Notebook check-ins | **Built** (the prompt after each board meeting is not: Isa has no meeting dates) |
| Day 30/60/90 check-ins | Planned (roadmap) |
| "Coming up" overview | Planned (roadmap) |
| Progress sharing and the Chapter Experience staff view | Planned (roadmap); may overlap the client health dashboard |
| Listen in the succession notebook | Planned (roadmap) |
| Journeys for other board roles | Planned (roadmap, since October 19) |

Everything designed but not built is now listed in **docs/Supplement_Items.md**, for the ISACA supplement document.

## M4: what was built

All of it is in the onboarding feature (Services/Onboarding, Components/Shared, onboarding/). Every piece of typed text goes through `Orchestrator.CheckTextAsync`: the sign-in check and Input Compliance, logged by purpose, with the length but never the words. That is the same route notebook notes already use, so nothing is a side door around the pipeline. Nothing calls the AI model.

1. **"Ask the outgoing president".**
   - The president asks the president before them a question in the succession notebook. It waits there; nothing is emailed. The former president sees a line at the top of their chat and answers in the notebook.
   - Open while the former president is in the 30-day handoff window or holds a board role. Only the asker can share it with the board.
   - No earlier president to ask: the button becomes "Ask the Chapter Experience team", which opens your existing "Talk to a person" card.
   - Purposes `notebook_question` and `notebook_answer`. Six new columns in data/notebook.db, added automatically and leaving saved entries untouched.
2. **Listen on journey steps.**
   - Steps that cite an ISACA document get "Listen to the source" buttons. Each opens the Library at the cited guidebook page (or the whole document), reads it word for word and stops.
   - The list of which step points where is onboarding/step_listen.json.
3. **The Chapter Affiliation Agreement in the Library.**
   - onboarding/build_affiliation_agreement.py turns Camille's Word file into onboarding/library_texts/affiliation-agreement.json, word for word, with the clause numbers put back. The address and signature tables are left out.
   - Read and listen only, with a note that it's ISACA's template and a chapter's signed agreement may differ.
   - **Isa doesn't answer from it.** Adding it to data/content.db (your search) is your call.
4. **"Terms explained".**
   - 26 terms that ISACA's documents define, quoted word for word in the Library. onboarding/check_glossary.py proves every quote against the source text.
   - Nothing new goes into the search.
   - docs/Glossary_Terms_To_Request.md lists the terms ISACA's documents use without defining, to ask ISACA about.
5. **Board introduction and profile form.**
   - Step J-19 moved into week 1 (days 6–7). data/onboarding.db is rebuilt and committed.
   - The president saves an introduction from a draft filled in from ISACA's records; nothing is emailed.
   - Board members then get an optional four-question profile form. Only the president sees the answers, and J-25 shows them.
   - Purposes `board_introduction` and `board_profile`. Stored in data/progress.db.
6. **Notebook check-ins.**
   - From week 2, a card at the top of the president's chat: "Anything from this week your successor should know?", or "Anything else…" if they already saved something.
   - How often is chosen in the notebook's ⋯ menu. Saved lines go through the notebook's normal note check.

**One small fix in shared wording.** When the sign-in check refuses a typed note, question, introduction or profile, the message now says "You're not signed in any more…" instead of blaming the words. This is in NotebookService and BoardIntroService only, not the pipeline.

## The rehearsed demo

- Signed in as Watanabe Kenji on day 3, the screens are the same as before, with one exception: his notebook now shows "Ask Sato Yumiko" beside "Add a note" and "Ask Sato Yumiko about this" on each entry.
- No check-in card appears on day 3.
- The demo questions and the pipeline are untouched.

## Checked

- Each piece was checked on screen.
- Anything that saves data was tested on a separate copy of the app with throwaway databases and Salesforce off, signed in as Watanabe, Sato and Ana Beatriz Souza.
- Build: 0 warnings, 0 errors.

## What Matthew may ask you

- Whether the Affiliation Agreement (and later the glossary terms ISACA approves) should go into Isa's approved content, so she answers from them.
- Whether the planned Chapter Experience staff view should share anything with the client health dashboard.
