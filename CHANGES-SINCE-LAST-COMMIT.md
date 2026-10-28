# Changes since the last commit

A running list of every change made since Matthew's last commit (October 28, 2026: Matthew's-Branch at a3a20f2, "Added sample pages for Engage and Chapter 360..."; merged into main by Willis as pull request #35, main at 50bc9d5). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **data/notebook.db.bak-2026-10-23** still to be taken out of Git: it is still tracked after a3a20f2. Run `git rm --cached "data/notebook.db.bak-2026-10-23"` before the next commit. The file stays on Matthew's computer; .gitignore already ignores dated backups, so it won't come back.

- Not a code change (October 28): Docker Desktop had stopped again, which took Keycloak down ("The sign-in service isn't reachable"). Restarted both. Keycloak's container (isachatapp-keycloak-1) doesn't restart by itself when Docker starts; after starting Docker, run `docker start isachatapp-keycloak-1`.

- **"Worth saving?" label on the save button** (replaces the planned pop-up save offer; Matthew, October 28). When the section an answer cites is tagged "deadline" or "decision", the "Save to notebook" button carries a small label ("Deadline · worth saving?" with a calendar icon, or "Decision · worth saving?" with a signpost icon). Opening the save box says why: "Marked as a deadline because this answer cites ...". Nothing pops up. Willis's pipeline, the content database and the stages are not changed.
  - **onboarding/save_hints.csv** (new): the tag list, 13 of the 45 sections (7 deadline, 6 decision) in Isa's content, each with the reason the leader sees and a "Why tagged" note.
  - **onboarding/build_onboarding_db.py**: builds the list into data/onboarding.db (new save_hints table), checks it, and warns if a tagged section isn't in data/content.db. Run `python onboarding\build_onboarding_db.py` after pulling (done on Matthew's computer: all 13 match).
  - **Services/Onboarding/JourneyService.cs**: GetSaveHint looks up the cited source.
  - **Components/Shared/SaveToNotebook.razor and .razor.css**: the label, and the reason in the open box. "Save to notebook" no longer breaks onto two lines in the narrow chat; the label drops below it.
  - **ONBOARDING-FEATURE.md**: the decision and what was built.
  - Checked in the browser on a test copy (port 5068) as Watanabe Kenji: "When do we send in our yearly paperwork?" showed "Deadline · worth saving?"; "How many board meetings must a chapter hold each year?" showed "Decision · worth saving?". Builds with no warnings. Not checked live: an answer with no label (not tried, to avoid more hand-offs creating test cases; see below).
- Testing note: "When is the compliance submission window?" was passed to a person (the known answer-quality problem), and that hand-off created a real test Case in the team's Salesforce developer org (reference TKT-4524). Nothing was saved to the notebook during testing.

- **The Chapter 360 sample page now works in parts** (Matthew, October 28: it must look like it works). Components/Pages/SampleChapter360.razor (+ .razor.css, and a new .razor.js for scrolling).
  - Focus Area cards open each Focus Area: what it is, why it matters, and its Practice Areas, which open to their numbered practices, benefits and tools, all in ISACA's words.
  - "Listen to [Focus Area]" and a "Read & listen" tab read the whole document aloud in Isa's voice, with the Library's player and sentence highlighting.
  - The top tabs work: Overview, Self-assessment, Focus areas, Tools & templates (271 tools, searchable, filtered by kind; opening one shows a "not in the prototype" note) and Help (ISACA's Chapter 360 introduction, to read or listen to).
  - Self-assessment: rate each Practice Area 1 to 5; the chart reshapes as you go; Save updates the progress (5 of 6) and the overview. Nothing is stored; reloading starts over.
  - The browser's Back button works (each view has its own address), a new view opens at the top, and "Open the practice" scrolls to Chapter Leadership Continuity.
  - Ribbon changed to "some parts work, nothing is saved".
- **Chapter 360 page, second round (Matthew, October 28): the chapter's view from the whole board, "My Chapter 360", and a clickable chart.**
  - The self-assessment is now "Your rating of the chapter". Each board member rates on their own and the ratings are averaged (ISACA's own rule). In the sample, 3 of 6 board members have rated four Focus Areas. Watanabe Kenji's rating is averaged in, and each row shows the board average so far. Progress shows the chapter's rating and the leader's own ("Your rating 0 of 6").
  - The board card shows who has rated ("✓ Rated", "Not yet", "1 of 6 rated" for you), never anyone's scores.
  - "My Chapter 360" card: what the leader has opened, read and listened to, per Focus Area, with a "Suggested next" (the chapter's lowest-rated area they haven't opened). Private to the leader. The chart shows it as a dashed purple shape; cards say "✓ Explored".
  - The chart: hover over or focus a point for a card (level, ratings, lowest Practice Area, your exploring, Open and Add your rating); click a point to open the Focus Area, or in the self-assessment to rate it. On a phone the card sits under the chart.
  - Scores show to one decimal without ".0" (Member Experience showed "4.0" next to "Level 3").
  - Checked in the browser: hover card, Open from the card, exploring two Practice Areas and Read & listen (the dashed shape and "1 of 6 explored" update), rating Advocacy (5 of 6 rated, 4 of 6 board members, "1 of 6 rated" on the board card), the averaging for Governance (board 2.06 plus your 5 gives 2.8 from 4 ratings), clicking the chart in the self-assessment, and phone width (no sideways scrolling; the card fits). Builds with no warnings. Not checked: the chart's motion on a real screen.
- **The Engage sample page now works in parts** (Matthew, October 28). Components/Pages/SampleEngage.razor and .razor.css.
  - Home: the Isa announcement's example conversation steps through two real questions Isa answers (ISACA's words, with their sources); "What can Isa do?" opens a short, honest list; "Post a message" and "Following" work.
  - Tabs work: Discussion (threads, topic filters, search, New discussion; a thread opens with replies, Like and Reply), Library (folders and files), Events (each event opens; Register and Add to calendar), Members (members who chose to be listed, searchable).
  - The header search box searches discussions; Connect and Volunteer show a note that they're in ISACA's real Engage.
  - Every person, post, reply, event, file and number is invented. Anything typed stays until the page is reloaded; nothing is saved or sent. Ribbon: "some parts work, nothing is saved".
  - Checked in the browser on a test copy: both example conversations, "What can Isa do?", opening a thread, Like, Reply, Back, Post a message with a new post, a topic filter, a library folder, an event and Register ("47 going, including you"), member search, the header search, and no sideways scrolling at phone width (375px) in all views. Builds with no warnings.
- **Listen Library: a "Chapter 360" group** with ISACA's seven Chapter 360 documents, Read and Listen, and a note that Isa doesn't answer questions from them yet. The intro line now says "ISACA's guidance for chapter leaders" (it said all documents are ones Isa answers from).
  - **onboarding/build_chapter360.py** (new) builds **onboarding/chapter360.json** (new) from content/Chapter 360. Left out: ISACA's internal "[Node]" codes, a diagram's leftover text, and the asterisk after template names. Kept: "TBD" entries (word for word). Three practices that have no number in ISACA's documents were given the next number (MX 1.5, AD 4.9, CI 3.8).
  - **Services/Onboarding/Chapter360Service.cs** (new) reads it; **Program.cs** registers it (one line, next to the Library).
  - **Services/Onboarding/LibraryService.cs**: reads Chapter 360 documents as well as content.db ones; the content.db reading is unchanged, just moved into its own method.
  - **onboarding/library.json**: the seven Chapter 360 entries.
  - **Components/Shared/LibraryPanel.razor and .razor.css**: the group note and the new intro line.
  - Note for Willis: his voice warm-up now also pre-makes audio for the Chapter 360 documents (about 11,000 more words), once, in the background. His TTS code is unchanged.
- **ONBOARDING-FEATURE.md**: the Library's Chapter 360 group and a new "Chapter 360 sample page" section.
- Checked in the browser on a test copy (port 5068): every view of the Chapter 360 page; a Practice Area opening; a template note; Listen in Isa's voice (sentence highlighted, audio came back); the self-assessment (rate, the chart reshapes, Save, "Next"); search; Help; Back; "Open the practice"; no sideways scrolling at phone width (375px) in all six views; the Library's Chapter 360 group, a Chapter 360 document opening, and the President Guidebook still opening as before. Builds with no warnings. Not checked: how the chart's motion looks (the test browser freezes animations; worth a look on your screen), and a whole document read to the end.

## What pulling main brings in (Willis's work, October 26 to 27, for Matthew's information)

Matthew's-Branch has nothing main doesn't have, so the pull is a straight fast-forward to 50bc9d5. It brings:
- The presentation deck (/present): reworked slides, styles and keys; the dashboards now open as the deck's own panels during the presentation (Services/PresentPanels.cs); the Cases panel shows why each case was passed to a person (never the question text).
- Help moved from the bottom of the rail to a "Help" link in the chat header, so Backstage keeps its room at the bottom of the rail.
- The Ollama model now loads when Isa starts and stays loaded for 60 minutes (was 10), and is unloaded when Isa stops (OllamaLifetimeService.cs; stop-isa.bat also unloads it).
- ISA-PROJECT-CONTEXT.md: a new decision, translation will use Meta's NLLB-200 (self-hosted) instead of LibreTranslate/Argos; its license is non-commercial, so a real deployment needs a license check.
- The sample Chapter 360 and Engage pages are unchanged by Willis.

## Suggested commit message

```
Working Chapter 360 and Engage sample pages, Chapter 360 in the Library, "worth saving?" label

- Engage sample page: threads open with replies, like and reply;
  new posts; library folders; events with Register; a members
  directory; Isa's example conversation and "What can Isa do?"
- Chapter 360 sample page: Focus Areas, Practice Areas and practices
  open; read & listen in Isa's voice; searchable tools and templates;
  a live self-assessment that reshapes the 360 chart; Help
- Chapter 360: the leader's rating is averaged with the board's (who
  rated is shown, never scores); "My Chapter 360" tracks what the
  leader has explored; the chart is clickable with a details card
- Listen Library: a Chapter 360 group with ISACA's seven Chapter 360
  documents (built from the Word files into onboarding/chapter360.json)
- The save button shows "Deadline · worth saving?" or "Decision ·
  worth saving?" when the answer cites a tagged section, and the save
  box says why; replaces the planned pop-up save offer
- Add onboarding/save_hints.csv: 13 tagged sections, built into
  data/onboarding.db
- Remove data/notebook.db.bak-2026-10-23 from Git (kept locally)
```
