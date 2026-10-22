# Changes since the last commit

A running list of every change made since Matthew's last commit (October 21, 2026: e6da781 "two word documents updated"; main is now at d73c41b, pulled October 22). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **Claude outputs/Block B check for Willis - October 22.md** (new): the Block B check of Willis's Wording.cs in the browser. Almost every state matches the approved warmer wording; two things to fix (the step line when a stage crashes still uses old words, and a few old words in developer mode), plus what couldn't be tested.
- **docs/Session_Handoff.md**: the Block B check is done; notes what is still untested.
- **Services/ShowTheWork/Wording.cs** (Willis's file, fixed with Matthew's go-ahead): if a stage ever crashes, its step line now uses the same words as the live status line (for example "Searching ISACA's guidance" instead of "Searching ISACA's approved content", "Finding the right person to help" instead of "Creating your ticket"). Also replaced the last old words: developer-mode skipped lines ("searching ISACA's guidance", "gathering the right sections", "double-checking the answer") and "ISACA's guidance isn't loaded yet". Builds with no errors or warnings; the crash state itself can't be triggered yet, so it wasn't seen in the browser.

- **Components/Layout/ChatWidget.razor**: on hand-offs, the "Reference # [trace ID]" line under the message now shows only for the Developer role. Leaders see just the TKT reference number in the message, so they aren't given two different numbers. Checked in the browser.
- **docs/Show_the_Work_Wording.md**: section 5 notes the change above.
- **Components/Shared/IsaRail.razor.css**: the number on the Today button's badge is now centered in a true circle (it was 20px wide by 18px tall, with the number 1px low). Checked in the browser: 18 by 18, number centered.

- **Components/Layout/ChatWidget.razor and ChatWidget.razor.js**: when the chat opens before the leader has asked anything, it now shows from the top (the Today note, then Isa's welcome) instead of opening pinned to the bottom with the Today note cut off. For two seconds after opening it stays at the top while the Today note loads, and stops as soon as the leader scrolls, taps or types. Once a question is asked, the chat behaves as before (replies open at the question). The same applies when the left rail is hidden or shown again: the chat's text rewraps as its width changes, which used to push the top line out of view; before the first question it now stays at the top. Checked in the browser on a short window (open, hide the rail, show it again: at the top each time).
- **Components/Layout/ChatWidget.razor, ChatWidget.razor.css and Components/Shared/IsaRail.razor.css**: hiding and showing the left rail is slower and smoother. The header's three-line menu button used to pop in all at once after the rail had gone, shoving the Isa ring sideways; now it fades in (and out) while the rail glides, and the Isa ring and name slide over to make room. The slide uses a transform rather than a changing width, so it stays smooth to the end instead of stuttering a pixel at a time as it settles (Matthew saw that stutter in the first version). The rail, the chat and the ring all move together over 0.7 seconds (was 0.5). Checked in the browser by recording the ring's position every frame, both ways: it moves smoothly and settles with no hop.
- **Components/Layout/ChatWidget.razor and ChatWidget.razor.css**: hiding or showing the left rail no longer changes any window's size (Matthew's choice, option 1). The chat, Today and the notebook always leave room for the rail, so the rail just slides out of the chat's left edge, and the header's big "C" graphic no longer shivers. With all three windows open and the rail hidden, a 76px strip at the far left stays empty. Phones are unchanged. Checked in the browser: the chat's width and the big "C" stayed exactly still on every frame, both ways; with three windows open, nothing moved.
- **ONBOARDING-FEATURE.md**: the panel rules note the rail decision above.
- **Components/Shared/TodayPanel.razor and TodayPanel.razor.js** (new): on the "Meet your Chapter Experience contact" step, clicking "Write message" now scrolls the Today window gently so the whole introduction form is in view, instead of opening it below the visible area. Only the Today window scrolls, never the page behind Isa; with reduced motion it moves at once. Checked in the browser: the form came fully into view and the page didn't move (the test draft was cancelled, so nothing was saved).
- **Claude outputs/Outside governance sources - 20 free documents.md** (new): 5 free documents each from BoardSource, BoardEffect, ASAE and Association Forum (the outside sources Camille approved for broader governance questions), with links, dates, what each covers, each site's terms of use, and three candidate demo questions. All four sites require written permission to copy their content, so nothing from them is copied.
- **content/Governance Best Practice/** (new, 9 guides): general governance best practice written in the team's own words from those 20 pages, by topic: orienting new board members, board and officer roles, running board meetings, minutes, conflicts of interest, committees, recruiting and engaging volunteers, succession and handover, reviewing governance. Each is labelled "not ISACA policy" and links to the original pages under Further reading.
- **content/add_best_practice.py** (new): adds the guides to content/content.json (one passage per section, links left out). Safe to re-run; run it again if content.json is ever regenerated, then build_content_db.py.
- **content/content.json**: 50 new passages (ids 82-131) for the guides; nothing else changed.
- **Services/Orchestrator/Stages/StubStages.cs** (search): ISACA's documents are searched first, then the meaning map, then the guides last, so a guide answers only when ISACA's documents have nothing. Without this, the guides pushed the President Guidebook out of the main demo answer.
- **Services/Orchestrator/Stages/ResponseStages.cs, Services/ShowTheWork/Wording.cs and WorkStepText.cs**: an answer from a guide opens "Here's what general governance best practice suggests (not ISACA policy):", its closed line reads "Answered from governance best practice", and its search step says "Not in ISACA's guidance; found general best practice".
- Checked in the browser: the orientation, minutes and conflict-of-interest questions answer from the guides with those labels, and "How many board meetings must a chapter hold each year?" still answers from the President Guidebook. The README's other test questions were checked by script and are unchanged. test-paths.ps1 was not run.
- **Claude outputs/Best practice guides - note for Willis.md** (new): what changed in his search and answer-check code, and why.
- **Succession notebook: index, entry pages and printing** (built from the approved mockup below):
  - **Components/Shared/NotebookPanel.razor and .razor.css**: the notebook no longer lists every entry in full. It opens as an index: search, filter chips with counts (Decisions, Saved answers, Unfinished, Contacts, What worked, Other, Shared), and one short line per entry grouped by month. Tapping a line opens the entry on its own page: the decision, when it was recorded (in the reader's time zone) and by whom, whether and when it was shared, its topic (the writer can change it), its record number, the question and Isa's cited answer, and its follow-ups. "+ Add a note" opens only when asked for. "+ Add a follow-up" adds a note under an entry (checked by Input Compliance like any note). Other board members see shared entries without the president's own question (the Board copy rule).
  - **Printing**: "Print or save PDF" on an entry and "Print all decisions" on the index offer Board copy (shared only, no private question) or My copy (presidents only), then open **Components/Pages/NotebookRecord.razor** (new) in a new tab: a clean decision record, or every decision as a summary table plus one record per page, printed or saved as a PDF from the browser's print window. Filled in by a fixed template from the notebook's data, never by the AI model. ("Print the term" from the mockup became "Print all decisions", since the notebook doesn't know each president's term dates.)
  - **Email this record**: To (prefilled with the leader's own address), Board copy or My copy, and an optional message (checked by Input Compliance). In the prototype nothing is sent: the request is saved in Isa, like the introduction on the contact step.
  - **Services/Onboarding/NotebookService.cs**: new columns topic, shared_on and parent_id (added automatically to an existing data/notebook.db, keeping its entries; older entries get a topic by the same rule) and a new notebook_emails table. The topic comes from a fixed, visible word rule (e.g. "election" = Elections, "budget" = Finances), not AI.
  - Checked in the browser: index, filters, search (including follow-up text), entry page, adding a follow-up, sharing (with its date), changing nothing on someone else's entry, the board-member view (shared entries only, no question, private follow-ups hidden), the print choice, the record and decisions pages (My copy and Board copy, and the refusal for a private entry), and the email form. Test changes were removed afterwards. Not checked: the actual paper/PDF output of the print window, and how it feels on Matthew's own screen.
- **Claude outputs/Notebook records mockups.html** (new): mockup, not built. The succession notebook as a short index with search and filters, each entry on its own page with follow-ups, and a decision record to print, save as PDF or email ("Board copy" without the leader's private question, or "My copy"), plus "Print the term" as a handover pack.

## Suggested commit message

```
Block B wording check, and fix the last old words in Wording.cs

- Wording.cs: a crashed stage's step line reuses the live status words;
  replace "approved content" and "passages" in the remaining lines
- Hand-offs: show the trace Reference # in developer mode only; leaders
  see just the TKT reference number
- Today badge: center the number in a true circle
- Chat: open at the top (Today note and welcome) before the first question
- Rail: slower, smoother hide and show (0.7s); the menu button fades in
  and the Isa ring slides over smoothly instead of jumping
- Rail: windows keep their size when it hides or shows (always leave room
  for it), so the header no longer shivers
- Contact step: "Write message" scrolls the introduction form into view
- Claude outputs: 20 free documents from the four outside governance
  sources, with their terms of use and candidate demo questions
- Content: nine governance best-practice guides in our own words
  (not ISACA policy), searched only when ISACA's documents have nothing,
  and labelled as best practice in every answer
- Notebook: index with search and filters, entry pages with follow-ups,
  topics and shared dates; decision records to print, save as PDF or
  email (Board copy / My copy); board members no longer see the
  president's question on shared entries
- Claude outputs: results of checking Wording.cs in the browser against
  the approved wording, and what wasn't tested
- docs/Session_Handoff.md: Block B check done
```
