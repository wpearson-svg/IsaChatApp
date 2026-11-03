# Changes since the last commit

A running list of every change made since Matthew's last commit (November 3, 2026: 5490b45, merged into main and Willis-Pearson: "Ask the outgoing president", Listen on journey steps, the Chapter Affiliation Agreement in the Library, and the help guide). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **"Terms explained", a glossary in the Library** (November 3, Sprint 3 M4; ISACA's words only, Matthew's rule): a card at the top of the Library opens 26 terms that ISACA's documents define, A to Z, with search. Each shows ISACA's words exactly, with its document, page and part; "Read it" opens the document at that passage without playing, and Back returns to the term.
  - **onboarding/glossary.json** (new): the 26 terms, each with its quote and source. Read while Isa runs.
  - **onboarding/check_glossary.py** (new): checks every quote word for word against the text the Library shows. Run it after any change: `python onboarding\check_glossary.py` (now: 26 terms, 0 problems).
  - **Services/Onboarding/LibraryService.cs**: GetGlossary, and GetQuoteRange (finds a quote's sentences, on its page).
  - **Components/Shared/LibraryPanel.razor** (+ .css, .js): the card, the glossary view, "Read it" and Back. The message at the end of a part now reads "That is the end of that part. Press play to keep listening." (it was worded for journey steps only).
  - **onboarding/help.json**: the Library point mentions *Terms explained*.
  - **docs/Glossary_Terms_To_Request.md** (new): the terms ISACA's documents use but don't define, with where each appears and suggested wording, to send to ISACA. Written so it can be forwarded without mentioning the onboarding feature.
  - **ONBOARDING-FEATURE.md** (piece D and cut list item 6) and **ISA-PROJECT-CONTEXT.md** (the request, for a weekly update).
  - Checked on screen as Watanabe: the card, the list and search ("charter"), "Read it" on Charter (Affiliation Agreement, lands on the sentence, not playing) and on AGM (lands on guidebook page 8), and Back to the term. No pipeline change. Build: 0 warnings, 0 errors.

- **Board introduction and profile form** (November 3, Sprint 3 M4, Matthew's choice):
  - **Step J-19 moved into week 1** (days 6-7, beside "Meet your board"; Matthew: a president shouldn't wait a week to introduce themselves). **onboarding/journey_steps.csv** and **data/onboarding.db** (rebuilt; every day 1 to 90 still has a step). "Meet your board" now points to it as "Next".
  - J-19: "Write your introduction" opens a draft filled in from ISACA's records; the president edits and saves it (sign-in check and Input Compliance first); saving completes the step. Saved in Isa, never emailed.
  - Board members then see an invitation line at the top of their chat, and an optional four-question profile form. Only the president sees the answers; members can change or withdraw them. J-25 shows the replies so far, or "No reply yet" (never who said no).
  - **Services/Onboarding/BoardIntroService.cs** (new; registered in **Program.cs**), **Components/Shared/BoardProfileCard.razor** (+ .css, new), **Components/Shared/TodayPanel.razor** (+ .css), **Components/Layout/ChatWidget.razor** (+ .css).
  - Sign-in wording: a typed note, question, introduction or profile refused by the sign-in check now says "You're not signed in any more…" instead of blaming the words (**NotebookService.cs** too).
  - Checked on screen in a separate test copy (throwaway databases): Watanabe saved the introduction and J-19 was marked complete; Sato saw the invitation, filled in the form and got the "shared with" line; Watanabe's J-25 showed her answers and "No reply yet" for the other four. Build: 0 warnings, 0 errors.
- **Notebook reminders** (November 3, Sprint 3 M4, Matthew's choice): a check-in card at the top of the chat, for the chapter's president from week 2: "Anything from this week your successor should know?", or, if they already saved something, "You saved 1 note this week. Anything else your successor should know?" (Matthew's change: ask for more instead of skipping). One line and Save (saved to the notebook through the same checks as a note), "Nothing this week" / "That's everything", "Ask me later", and ×. How often (every week, two weeks or four weeks; weekly in the last three months of a term) in the notebook's ⋯ menu. Inside Isa only.
  - **Services/Onboarding/NotebookReminderService.cs** (new; registered in **Program.cs**), **Components/Shared/NotebookCheckInCard.razor** (+ .css, new), **Components/Layout/ChatWidget.razor**, **Components/Shared/NotebookPanel.razor** (the ⋯ choice; saving a note now updates the chat's check-in).
  - **onboarding/help.json**: "Saving decisions and notes" explains the check-in.
  - Checked on screen in a separate test copy: Watanabe (day 3) sees no check-in, so the rehearsed demo is unchanged; Ana (day 60) saw it, the wording changed after she saved a note, a line saved from the card went into her notebook (private) and the card went away; switching to every two weeks didn't ask again straight away. Build: 0 warnings, 0 errors.
- **docs/Supplement_Items.md** (new): a running list of what is designed but not built, for the ISACA supplement. It starts with the prompt after each board meeting (Matthew: not built), the answer checks, the role-specific rule, a chapter's own rules file, and meaning match.
- **ONBOARDING-FEATURE.md**: J-19's new days, the board introduction as built, and the reminder decisions (ask "Anything else…?" instead of skipping, starting in week 2; the meeting prompt to the supplement).

- **M3 closed** (Matthew, November 3): ONBOARDING-FEATURE.md's cut list now says what was built and what is planned (day 30/60/90 check-ins, "Coming up", progress sharing and the staff view, Listen in the notebook, other board roles); the planned items are added to **docs/Supplement_Items.md**.
- **Claude outputs/M3 and M4 - for Willis.md** (new): the M3 decisions and the six M4 builds, in Markdown for Willis's Claude, including what touches his pipeline (nothing) and two questions for him.

## Suggested commit message

```
Sprint 3 M3 and M4: Terms explained, the board introduction and profile form, notebook check-ins, J-19 in week 1, the supplement list and a note for Willis
```
