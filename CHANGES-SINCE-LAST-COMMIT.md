# Changes since the last commit

A running list of every change made since Matthew's last commit (November 2, 2026: e3677f6, "hand-off to a person, some output compliance notes", pushed to Matthew's-Branch: "Talk to a person" and "Not quite" wired up, the promise inventory, and the role filter, output compliance and promise notes for Willis). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **The Chapter Affiliation Agreement in the Library** (November 3, Sprint 3 M4, Matthew's choice): a new "Your chapter and ISACA" group with ISACA's 2024 template, to read and listen to word for word, with the note that Isa doesn't answer questions from it yet (adding it to her approved content is a decision with Willis).
  - **onboarding/build_affiliation_agreement.py** (new): turns content/Chapter Affiliation Agreement-2024-template.docx into **onboarding/library_texts/affiliation-agreement.json** (new). Keeps ISACA's words and the template's blanks; puts back the clause numbers and letters Word shows, checked against the agreement's own cross-references; leaves out the address and signature tables and the header and footer. Run it again if ISACA sends a new version.
  - **Services/Onboarding/LibraryService.cs**: a new kind of Library document, "source": "text", read from onboarding/library_texts/.
  - **onboarding/library.json**: the new entry and a comment explaining "text".
  - **ONBOARDING-FEATURE.md**, Feature 5: a dated note on all of the above.
  - Checked on screen as Watanabe Kenji: the Library lists it (about 57 minutes), it opens and plays, and the 16 clauses and four exhibits show as headings. Willis's voice warm-up will also pre-make its audio, once, in the background.
  - A line with it, in the list and at the top of the document: it is ISACA's template, a chapter's signed agreement may differ (Exhibit D), and questions about a chapter's own agreement go to the Chapter Experience team. Shown, never read aloud (library.json "note"; LibraryPanel.razor and .css).
- **ISA-PROJECT-CONTEXT.md**: a question for the November 4 update to ISACA: may the Library include the Affiliation Agreement template? Worded about the Library only.

- **Listen on journey steps** (November 3, Sprint 3 M4): each step that cites an ISACA document in the Library has a "Listen to the source" box, one button per place it cites, with about how long it takes. It opens the Library beside Today, reads that guidebook page (or the whole document) word for word, and stops at the end of it; play carries on from there.
  - **onboarding/step_listen.json** (new): which step points to which document and page (23 buttons on 18 steps; none on steps from Isa's own onboarding plan, so the rehearsed day-3 screens don't change).
  - **Services/Onboarding/LibraryService.cs**: GetStepListens and GetRange; each guidebook section now knows its page.
  - **Components/Shared/TodayPanel.razor** (+ .css): the Listen box on a step.
  - **Components/Shared/LibraryPanel.razor** (+ .css, .js): opens at the part a step asks for, shows "From your journey step: ...", and stops at its end.
  - **Components/Layout/ChatWidget.razor**: passes the step's request to the Library and opens it.
  - Checked on screen as Ana Beatriz Souza (day 60): J-15's button opens the guidebook at page 14 and reads it; stopping at the end of a page checked on page 8. Build: 0 warnings, 0 errors.

- **"Ask the outgoing president"** (November 3, Sprint 3 M4): the president asks the president before them a question in the succession notebook; it waits there (nothing is emailed) until the former president opens Isa and answers. The question and answer stay in the notebook for the presidents after them.
  - Who: open while the former president is in the 30-day handoff window or holds any board role in the chapter. Only the president who asked can share it with the board (Matthew's rule, November 3); the former president answers but never shares, and sees only the questions asked of them. No earlier president to ask: the button is "Ask the Chapter Experience team", which opens the "Talk to a person" card.
  - Both the question and the answer pass the sign-in check and Input Compliance before they are saved (purposes notebook_question and notebook_answer; the event log records their length, not their words).
  - **Services/Onboarding/NotebookService.cs**: questions as a new kind of entry; GetAskTarget, AskAsync, AnswerAsync, GetWaitingQuestions; six new columns added to data/notebook.db without touching saved entries; a former president also sees the questions asked of them.
  - **Services/Onboarding/LeaderTermService.cs**: GetPreviousPresident.
  - **Components/Shared/NotebookPanel.razor** (+ .css): "Ask [name]" beside "Add a note", "Ask [name] about this" on entries, the question box, "Waiting for your answer", the answer box, a "Questions" filter, and the matching privacy and sharing lines. No print or email for questions.
  - **Components/Layout/ChatWidget.razor** (+ .css): a line at the top of a former president's chat when a question waits for them; the notebook's "Ask the Chapter Experience team" opens the "Talk to a person" card.
  - **ONBOARDING-FEATURE.md**: what was built, and cut list item 9 marked built.
  - Checked on screen in a separate test copy (port 5068, throwaway copies of the notebook, progress and event log, Salesforce off), so nothing was added to the real notebook: Watanabe asked about the online-elections decision; Sato saw the line in her chat and the waiting card, answered, and the line went away; Ana got the Chapter Experience team button and the card opened (nothing sent). Build: 0 warnings, 0 errors.
  - Demo note: Watanabe's notebook now shows "Ask Sato Yumiko" beside "Add a note", and "Ask Sato Yumiko about this" on each entry.

- **onboarding/help.json** ("How to use Isa"), so the guide matches what Isa does: "Your first 90 days" mentions the Listen button on steps; "Saving decisions and notes" explains asking the president before you (seen next time they open Isa, nothing emailed, the answer stays for later presidents, only you can share it); "What happens to your information" says such a question is seen by the two of you. Checked in the Help panel as Watanabe.

## Suggested commit message

```
Add Ask the outgoing president, Listen on journey steps and the Chapter Affiliation Agreement in the Library; update the help guide
```
