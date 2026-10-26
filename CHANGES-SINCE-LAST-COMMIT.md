# Changes since the last commit

A running list of every change made since Matthew's last commit (October 23, 2026: Matthew's-Branch at 5f1bb21, "How to use Isa and a few other things", after 1605855 "Presentation Ready"; both pushed). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **.gitignore**: dated backup copies (names like `notebook.db.bak-2026-10-23`) are now ignored too; the old rule only caught names ending in `.bak`.
- **data/notebook.db.bak-2026-10-23** taken out of Git (Matthew runs `git rm --cached`): it was committed by accident in 5f1bb21. It is a backup of local test data (invented sample notebook entries only). The file stays on Matthew's computer; only Git stops tracking it.

- **Claude outputs/Backup file in commit - note for Willis.md** (new): explains to Willis what the backup file is (four invented test notebook entries for C-001, nothing real, nothing reads it), how it got in, and the fix.

- **Components/Shared/TodayPanel.razor.js**: "Write message" on the contact step now glides the Today window down to the introduction form slowly and gently (Matthew, 23 October 2026: "smooth, gentle, elegant, easy"), instead of the browser's quick smooth scroll, which looked jerky. The glide takes 0.9 seconds and starts and ends softly; scrolling by hand stops it; with reduced motion it jumps at once. If the browser pauses animations (a window not on screen), the form still lands in view after a second. Checked in the browser on a test copy: the window moved 1, 17, 61, 130, 195, 253, 299 and 315 pixels at 0.1-second steps, then stayed. Builds with no errors or warnings.

- **Components/Shared/LibraryPanel.razor.js**: Listen now chooses its voice instead of using the computer's default (Matthew, 23 October 2026: a light female voice with the slightest British accent, not the default man's voice). It uses only voices that run on the device (online voices send the text to Google or Microsoft), and picks the first match: a female British voice (Hazel, Susan, Kate, Serena, Stephanie and others), any British voice not known to be a man's, a female American voice (Zira, Samantha and others), any English voice, then the default. Pitch slightly raised (1.06) for a lighter sound. Matthew's laptop has only David, Mark and Zira (all American), so it picks Zira (checked in the browser); after the English (United Kingdom) voices are added in Windows, it picks Hazel.

- **Isa's greeting and question buttons fit on a laptop without scrolling** (Matthew, 23 October 2026: on his laptop the third question button was cut off below the chat):
  - **Components/Layout/ChatWidget.razor.css**: the greeting card uses the chat's full width (not the 84% of a reply), so the question buttons sit two to a row instead of one per line; the buttons are a touch more compact; a little less space at the top and bottom of the chat and between messages.
  - **Components/Shared/TodayNote.razor and .razor.css**: the full Today note lists the next two "Coming up" steps, then "and N more this week, in Today" (this also settles the open question of how long that list should be); its spacing is a little tighter.
  - Checked in the browser on a test copy at a short laptop window (1330 × 614): with the one-line note the buttons fit in two rows with room to spare; with the full note (first visit of the day) the last button now just fits (it was 25 pixels below the bottom). Builds with no errors or warnings.

- **Components/Shared/IsaRail.razor.css**: on shorter screens the rail tightens up, so Help and Backstage at the bottom are never pushed off it (Matthew, 23 October 2026: Backstage was being squeezed off the bottom on his laptop, after Library and Help were added). Below 820px tall: smaller tiles (34px), less space between buttons; below 640px: smaller again (30px). Every button stays at least 44px tall and keeps its name; taller screens are unchanged. Checked in the browser on a test copy with the Developer role at 900, 690, 600 and 540px tall: Backstage stays on the rail every time, and its menu (Config, Developer Dashboard) opens fully on screen. Builds with no errors or warnings.

- **Claude outputs/Escalation and answer quality - for Willis, Monday.md** (new, a proposal; nothing built): for Matthew to talk through with Willis on Monday 26 October before anyone changes the pipeline. Why now (ISACA's two top priorities at the September 17 Q&A: escalation and accuracy), the five questions tested on 23 October and what happened, Camille's six playbook categories mapped onto the October 20 hand-off proposal (gaps: the "twisting governing documents" line, severe performance issues, multi-chapter or regional issues), a proposed plan (test list, topic file, the question check reading it, answer quality, renaming the "compliance" stages on the slides), and questions for Willis.

- **Claude outputs/Presentation script - Matthew.md** (new, first draft, 26 October 2026): a word-for-word script for Matthew's parts of the presentation (intro and slides 1 to 5, slide 12 "Additional features", slide 13), written from presentation/slides.json, with timings, pointing cues, and what to cut if running long. For the dry run. Focused on sprint 2 (the pipeline and how it works): slide 12 is kept to about 35 seconds and framed as "after this sprint". Rewritten for i.c.stars staff, the audience: it proves the pipeline is built and working, and slide 12 says what is built and being refined (onboarding, the succession notebook) and what is still planned. Reworded so nothing Matthew says repeats the slide's own words; each slide now shows an "On screen" line for comparison.

## Suggested commit message

```
Fit the chat's opening on a laptop; gentler scroll; Listen voice

- Ignore dated backup copies (*.bak-*)
- Remove data/notebook.db.bak-2026-10-23 from Git (kept locally)
- Add a note for Willis explaining the backup file
- Today: glide gently to the introduction form after Write message
- Library: prefer a light female British voice on the device for Listen
- Fit Isa's greeting and question buttons on a laptop without scrolling
- Today note: show the next two Coming up steps, then how many more
- Rail: tighten up on shorter screens so Help and Backstage always fit
- Add an escalation and answer-quality proposal for Willis (nothing built)
- Add a first-draft presentation script for Matthew's slides
```
