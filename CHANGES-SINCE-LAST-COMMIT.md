# Changes since the last commit

A running list of every change made since Matthew's last commit (October 26, 2026: Matthew's-Branch at d17d23a, "Tightend up the visuals in the app"; merged into main by Willis as cb09d5a, then Willis's "Update to presentation", main at 719305c). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **data/notebook.db.bak-2026-10-23** still to be taken out of Git: it is still tracked after d17d23a. Run `git rm --cached "data/notebook.db.bak-2026-10-23"` before the next commit. The file stays on Matthew's computer; .gitignore already ignores dated backups, so it won't come back.

- **Claude outputs/Presentation script cards - Matthew.pdf** (new): Matthew's script printed as eight note cards, four to a landscape letter page (two pages, with dashed cut lines). One card per slide, plus a "Keep in mind" card with the reminders, cuts and timing.

- **Claude outputs/Run of show - Sprint 2 presentation.md** (new): the whole presentation in order, with start times, presenter, length and hand-offs for every slide; a before-we-start checklist and the deck's keyboard shortcuts; the six demo steps with the questions built into the deck (Alt+1 to Alt+5); and where the old talking-point sheets no longer match the deck on main (ticketing now built, slide 8's new layout, slide 9's dashboard, slide 11's six items, slide 12 now Matthew's). Also as a printable PDF, **Run of show - Sprint 2 presentation.pdf** (five portrait letter pages: the timeline, the setup checklist and keys, then slide by slide).

- Not in Git (local only): data/content.db was rebuilt from content/content.json on 26 October (the old one, which still had the best-practice guides, is backed up as data/content.db.bak-2026-10-26 and ignored by Git).

- **Claude outputs/Note cards for review - Matthew.md** (new): Matthew's eight note cards as one document for review in another AI program: what the presentation is and who it's for, the team's rules for the talk, facts to check the cards against, and for each card what's on the slide and the words on the card.

- **Components/Shared/TodayNote.razor**: the Today note now always opens as one line ("Day 3 · 2 steps for today", with Show and Open Today); Show opens the full note and "Got it" folds it back. Before, it opened in full the first time each day unless that computer's data/progress.db remembered a "Got it", so it looked different on Willis's copy than on Matthew's. "Got it" no longer saves anything. Checked in the browser as Ana Beatriz Souza (no "Got it" on record): opens as one line, Show opens it, Got it folds it.
- **ONBOARDING-FEATURE.md**: Feature 4 notes the change above.
- **Components/Pages/SampleChapter360.razor and .razor.css**: the Chapter 360 button's sample page, built out (was a "still being built" placeholder). A Chapter 360 dashboard for the sample Sakura Bay Chapter: a "360 view" chart of maturity across the six Focus Areas, self-assessment progress (4 of 6), a card for each Focus Area (code, purpose, level, Practice Areas), recommended templates, a leadership-continuity tip and the chapter's board. The Focus Areas, Practice Areas and template names come from ISACA's own Chapter 360 documents (content/Chapter 360); the levels, progress and people are invented sample data.
- **Components/Pages/SampleEngage.razor and .razor.css**: the Engage button's sample page, built out. The Sakura Bay Chapter community on an Engage-style page (header, teal banner, tabs) with a "Say hello to Isa" announcement (Isa's logo, a little confetti, a sample question and its cited answer, and an "Ask Isa a question" button back to Isa), latest discussions, the library, upcoming events, chapter leaders and community numbers. Every person, post, event and number is invented.
- **Components/Shared/IsaRail.razor**: the rail's Library button now reads "Listen Library" (on two lines, under its icon), with matching hover text and screen-reader label. Checked in the browser: it fits inside the rail.
- Both pages: a ribbon says they are samples and nothing works; no ISACA logo, no sign-in fields; they open without Isa's chat bubble; they work at phone width (checked: no sideways scrolling at 375px) and respect "reduce motion". Checked in the browser and with full-page screenshots at desktop and phone sizes.

## Suggested commit message

```
Add the run of show and Matthew's script cards

- Remove data/notebook.db.bak-2026-10-23 from Git (kept locally)
- Add a run of show for the sprint 2 presentation (Markdown and PDF)
- Add Matthew's script as printable note cards
- Add Matthew's note cards as a document for review
- Today note: always open as one line, with Show to open it
- Build out the Chapter 360 and Engage sample pages, with an
  "Isa is live" announcement on Engage
- Rename the rail's Library button to "Listen Library"
```
