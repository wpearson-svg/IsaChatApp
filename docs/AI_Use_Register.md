# AI-use register

Second Star Collective, for the Isa prototype. Started November 6, 2026 (Sprint 3, task P7; Promise_Inventory row R15).

The RFP (section 4, "AI Use in Delivery and Development") requires a register of every use of AI to do the work or make a deliverable. That covers code, settings, prompts, test cases, analyses, designs, documents, reports and content. For each use, the register has to name:

- the tool, provider and model;
- the purpose and the project activity;
- the information given to the tool;
- the resulting deliverable;
- whether a person reviewed and checked it.

**Part A** is that register. **Part B** lists the AI models inside Isa itself. The RFP doesn't ask for Part B in this register, but ISACA will want it, and it belongs in the supplement.

**Status: draft.** Matthew's Claude Code rows come from the project's own records. Every row marked **To confirm** needs its owner to fill it in before the register goes to ISACA. Don't guess on anyone's behalf.

---

## Part A: AI used to make the project

### A1. Matthew Brady: Claude Code

| | |
|---|---|
| **Tool, provider, model** | Claude Code (Anthropic), in the Claude desktop app. Model: Claude Opus 5.5 (`claude-opus-5-5`), in all 22 recorded sessions in this project, October 20 to November 6, 2026. |
| **Where it ran** | On Matthew's laptop, working in the IsaChatApp folder. Claude Code sends what it reads to Anthropic's service to produce its replies. |
| **Who decided** | Matthew. He asked for each piece, approved mockups before anything was built, and reviewed the results. Claude Code never commits, merges or pushes. Matthew does all Git work himself (project rule, October 20). |

What it was used for:

| Activity | What it produced | Information given to it | Human review and checks |
|---|---|---|---|
| **Code:** the onboarding feature | The 90-day journey and Today window, the Today note and calendar, the succession notebook, the handoff checklist, the contact step, "Meet your board", the board introduction and profile form, notebook check-ins, "Terms explained", the Library and Listen, the "worth saving?" label, "Add to my calendar" (Components/Shared/, Services/Onboarding/, onboarding/) | The project's source code; ISACA documents in the project (President Guidebook, Chapter Systems Overview, Core Roles, Chapter 360, the Affiliation Agreement template); invented test people and chapters | Matthew approved a mockup before each screen was built. Each build was checked in the running app (the change list records what was and wasn't checked). Quoted ISACA text is checked word for word by script: onboarding/check_glossary.py, and the checks in build_onboarding_db.py. |
| **Code:** the rest of Isa's screens | Chat and landing redesign in ISACA's brand, Show the work, the three panels, the rail, "Talk to a person", the welcome word, the presentation app (/present), the sample Engage and Chapter 360 pages | The same, plus Willis's pipeline code | Matthew approved the mockups (Claude outputs/*mockups.html). Checked in the running app. Changes to Willis's pipeline went to Willis first (project rule). |
| **Code:** test and set-up scripts | test-questions-candidates.csv (real test questions for every path), the settings inventory, services-config.ps1, launcher buttons, two Dev Dashboard sliders | Source code; the event log of test runs (counts and stage results, synthetic accounts only) | Each test question was run 5 of 5 times against the running Isa. The change list says which scripts were written but not yet run. |
| **Content and data** | onboarding/*.csv and *.json (journey steps, key dates, handoff checklist, contacts, save hints, glossary, help guide, sample notebook history), the rules files (rules/, with check_rules.py) | ISACA documents; Matthew's decisions | Matthew reviewed each step's wording and the rules. Every handoff item was checked against the guidebook. check_rules.py runs each rule's own tests (61 checks, 0 problems). All people are invented. |
| **Governance best-practice guides** | Nine guides in the team's own words (content/Governance Best Practice/) | 20 free public pages from ASAE, BoardSource, BoardEffect and Association Forum, which were read and summarized, never copied | Matthew read the guides. The ISACA copy was checked paragraph by paragraph against the source files (136 paragraphs). |
| **Designs** | Mockups for each feature (Claude outputs/*mockups.html, *.png) | Matthew's descriptions; ISACA's brand colors | Matthew chose and approved each one before it was built. |
| **Documents for ISACA** | Weekly update of November 4 (PDF), the governance guides copy (PDF), the list of terms ISACA's documents don't define (PDF), the hand-off and refusal rules proposal (PDF), the wording table | Project documents; Camille's emailed answers of October 21 | Matthew read each one before sending. He sends every email himself. The November 4 update was sent; the guides and term list were not. |
| **Notes for the team** | Notes and proposals for Willis (Claude outputs/*for Willis*.md), talking points and scripts, the M6 screen checks | Source code; event logs from test runs | Matthew read them before passing them on. Willis decides on anything in his code. |
| **Presentations** | Sprint 2 and Sprint 3 decks (presentation/*.json), the Sprint 3 outline, run of show, note cards | The Sprint 1 PowerPoint; the promise inventory; the RFP | Matthew reviewed them. Willis and Eliyas still need to confirm the "built" lines for their parts. |
| **Analysis** | The promise inventory, the RFP mapping, the ISACA question list, the M6 findings | The RFP; Camille's answers; source code; test logs | Matthew reviewed and decided each one. |

Claude Code was also used for the weekly ISACA update emails, with access to the team's GitHub repository. Matthew reviewed each draft, Willis reviewed it, and Matthew sent it himself from Outlook.

What was **not** given to it: no real chapter leader's or member's personal information (the project has none; every person in the data is invented). Passwords for test accounts stay in a git-ignored file. Claude Code read that file only to sign in to the local test app as invented users.

**To confirm (Matthew):** Claude Code was in use before October 20, but its records aren't on this laptop. The sign-in screen (October 16) and the commits of October 19 came from it. Where did those sessions run, and from what date?

### A2. Matthew Brady: Claude chat (claude.ai) and Claude Cowork (desktop app)

| | |
|---|---|
| **Tool, provider, model** | Claude (Anthropic): the claude.ai chat, and Claude Cowork in the Claude desktop app. The current Cowork session runs Claude Opus 5.5 (`claude-opus-5-5`). Models in earlier conversations: **To confirm (Matthew, from the claude.ai conversation history).** |
| **Dates** | September 8, 2026 (the first Isa conversation) to November 6, 2026. |
| **Earlier register** | Matthew compiled register rows on October 12, 2026 ("Matthew's AI register"), covering September 8 to October 12. **To do:** merge them into this file. |

What it was used for:

| When | Activity | What it produced | Human review |
|---|---|---|---|
| September 8 to 16 | Proposal and Sprint 1 presentation: RFP analysis, problem framing, the escalation model, naming "Isa", the speaker plan, scripts for slides 1 to 3, the pre-proposal question list, reviewing Willis's architecture diagram | A kickoff debrief, a client vocabulary sheet, a proposal skeleton, diagram notes, scripts, the master presentation document, likely-question lists, the ranked question list | Matthew edited, cut and reworded throughout. He locked the scripts after several rounds of his own changes. Willis reviewed the question list and adopted some of the diagram suggestions. Matthew made the key decisions (the name, speaker order, branding). |
| September 9 to 10 | Research from public websites: ISACA's site and job postings, and a competing product (Betty AI) and reviews of it | Research summaries | Used for ideas and framing only. |
| September 24 to October 5 | ISACA's brand guidelines; brainstorming features; Willis's Component Viability Checklist; how to present features that won't be built in time | Brand notes, feature ideas, a revised checklist (shared with the team) | Used for ideas. Matthew chose which features went ahead. |
| October 8 | Reviewing ISACA's source documents: gaps and cautions | A source-document review summary | Used as reference for design. |
| October 12 to 13 | Setting up this register; designing invented test users | The first register rows; the fields and a nine-person invented test group | Matthew reviewed them. All test people are invented. |
| October 15 to 20 | Weekly update emails; the sign-in screen; landing page design; designing the onboarding feature; planning the supplement | Email drafts; ONBOARDING-FEATURE.md (October 16, then updated with Matthew's later decisions); design notes | Willis reviewed the emails and the sign-in screen and committed it. Matthew made every decision recorded in ONBOARDING-FEATURE.md (for example the 90-day journey's structure). |
| October 27 onward | Design decisions on the ticketing hand-off and linking to Engage | Design notes | Matthew decided. |

Information given to it:
- the RFP, the kickoff meeting transcript (September 10), the pre-proposal Q&A notes (September 17) and i.c.stars class notes;
- teammates' materials: Willis's architecture diagrams, master context document and Component Viability Checklist; question lists from Willis, Eliyas and A'keyrah; the team roster and phase assignments;
- ISACA's brand guidelines (the public Visual Style page);
- the documents ISACA sent on October 8: Chapter 360, the Chapter Leader Processes documents (including the Annual Chapter Compliance Framework), the President Guidebook, the 2026 Chapter Needs Assessment Survey, the Highland chapter leader study and the Chapter Experience staff orientation deck;
- Camille's email about data use;
- public websites (above).

**Confidential material: yes.** ISACA's 2026 Chapter Needs Assessment Survey and the Highland chapter leader study were given to the tool, most likely as uploaded files, on October 8, 2026. Their findings informed later design conversations (October 13 and 20). Before October 20, Antonio Banks told the team that what it was doing was okay. ISACA then confirmed on October 20, 2026 that it was comfortable with the team using Claude with the documents it sent. Neither document is in the project's code, content, sample data or Git history. **To confirm (Matthew):** the date of Antonio's okay, and where ISACA's October 20 okay is recorded (an email is best).

**Model training:** "Help improve Claude" is switched off on Matthew's Claude account, so these conversations aren't used to train Anthropic's models.

**Personal information:** teammates' and ISACA contacts' names and work roles; Camille Sanders's work email address; a few work details about ISACA staff from the kickoff; teammates' notes from presentation planning. The Highland study's quotes include identifying details about the chapter presidents interviewed, and that study was given to the tool. No home addresses, phone numbers or ID numbers. The test users are invented.

### A3. Willis Pearson

**To confirm (Willis).** The project notes refer to "Willis's Claude", so Willis also works with an AI coding assistant. Needed: tool, provider and model; which parts it helped with (for example the pipeline, the rule engine, the answer checks, translation, vector search, the Dev Dashboard); the information given to it; and how he reviewed and tested the results.

### A4. Eliyas Eshta

**To confirm (Eliyas).** For example, the translation and embeddings services (ai-services/) and the original Postgres search. If no AI tool was used, write "None" here: that counts as a register entry too.

### A5. A'keyrah Stevenson

**To confirm (A'keyrah).** For example, test cases, the risk and success metrics work, and documents.

### A6. Other tools

**To confirm (all).** For example AI features in Microsoft 365, Teams, Canva or GitHub, or a writing assistant used on slides or emails. List them if they produced anything in a deliverable.

Considered but not used to make anything: Open Notebook (looked at for onboarding and for audio briefings on the roadmap). Ollama runs the model inside Isa, so it is in Part B.

---

## Part B: AI models inside Isa

Every model runs on the team's own machines (Matthew's laptop, or Willis's PC over the team's private Tailscale network). No question or ISACA document is sent to an outside AI service, and there are no paid services.

| Model | Provider and licence | What Isa uses it for | Information it sees | Controls |
|---|---|---|---|---|
| **phi4-mini (3.8B)**, through Ollama | Microsoft, MIT licence | Puts approved passages into a readable answer, only when ISACA's text can't answer directly | The leader's question (in English) and the approved passages Isa found | Input compliance before it, and output compliance after it: every sentence checked against its passage, plus checks on numbers, "must", contact details and answer shape, with the fallback ladder. Every call is logged. |
| **Vectara HHEM-2.1-Open**, run inside Isa (ONNX) | Vectara, Apache-2.0 | Scores whether each sentence of an AI answer is supported by its passage | The AI's answer sentences and the cited passage | A pass mark set on the Dev Dashboard. If the model files are missing, the check is skipped and logged as skipped. |
| **multilingual-e5-base** (`intfloat/multilingual-e5-base`) | Microsoft research, MIT licence | Search by meaning: finds approved passages that match a question | The question, and Isa's approved passages when the index is built | Only approved content is in the index. Answers need a score at or above the pass mark. A miss goes to a person. |
| **NLLB-200 (distilled 600M)** (`facebook/nllb-200-distilled-600M`) | Meta, CC-BY-NC-4.0 (**non-commercial**) | Translates Amharic and Spanish questions into English, and the reply back | The leader's question and Isa's finished reply | Checks run on the English text. Licence caution: a real deployment needs a licence decision (ISA-PROJECT-CONTEXT.md, October 26). |
| **Kokoro-82M** | hexgrad, Apache-2.0 | Isa's voice: reads ISACA's text and answers aloud, word for word | The text being read | Nothing is rewritten. When the voice is off, the leader's browser voice is used instead. |

Licences above are as published by each provider. **To confirm (Willis or Eliyas)** before the register goes to ISACA.

---

## Keeping it up to date

- Add a row whenever a new AI tool or model is used, or an existing one is used for a new kind of deliverable.
- Matthew keeps Part A1 and A2. Each teammate keeps their own row. Willis keeps Part B.
- For the supplement document: a one-page summary of this register, with the line "Every AI-assisted deliverable was reviewed by a team member before use".
