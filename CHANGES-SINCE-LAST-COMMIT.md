# Changes since the last commit

A running list of every change made since Matthew's last commit (November 17, 2026: db6c2f9, "Subtle changes to Isa"), which Willis merged into main the same day; Matthew's-Branch is now at a358258, level with main. Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

After the merge, checked on Matthew's laptop (November 17, 2026): the project builds (0 warnings, 0 errors); `python rules\check_rules.py` reports 114 checks, 0 problems; `data/onboarding.db` still has Matthew's journey (Kenji's day 3 steps are the contact and the board) and the rewritten notebook notes; the "How she runs, in numbers" slide still has the corrected numbers; `python onboarding\prepare_demo.py` was run, so the demo data starts fresh.

Next piece of work: Matthew's speaking cards for the Sprint 3 presentation. To know first: Willis reordered the deck in this merge (promise, built, proof, with the demos together near the end). "Live demo: Kenji, day 3" is now slide 14, after "Live demo: ask Isa" (12) and "Live: your team's view" (13). `Claude outputs/Presentation script - Matthew - Sprint 3.md` and `Claude outputs/Click list for Willis - Matthew's parts.md` still use the old slide numbers and the old seven-step demo order; both are to be rewritten around the path Matthew settles on when he walks the demo again.

Not in Git, by design: the companion document for ISACA (`private/companion-document/`), which holds survey figures. Its picture of Kenji's steps and its Kenji's-week text still show the old pair of steps; so does the matching screenshot in `Claude outputs/Supplement screenshots/`. Both are to be redone once the demo path is settled.

1. **Kenji's invented Chapter Experience contact renamed to Grace Lim (November 17, 2026, Matthew).** She was "Mei Ling Tan", and Kenji's Vice President is Takahashi Mei: two people named Mei in one short demo.
   - `onboarding/contacts.csv`: CX-01 is now Grace Lim, grace.lim@example.org (still invented, still Asia-Pacific chapters, Asia/Singapore). `data/onboarding.db` rebuilt. The other four contacts are unchanged.
   - `Services/Onboarding/ContactService.cs`: a comment's example name only. `Claude outputs/Click list for Willis - Matthew's parts.md`: her name.
   - Checked on screen as Kenji on a test copy: the contact step shows "GL · Grace Lim · Chapter Experience · Asia-Pacific chapters", "To: Grace Lim", and the prewritten message begins "Hello Grace,". The old name appears nowhere on the page.
   - Not updated: pictures taken before today that show the old name (`Claude outputs/Supplement screenshots/` and the companion document in `private/`).

2. **The rail button "Notebook" is now "Succession Notebook" (November 17, 2026, Matthew).** The panel's title and the presentation both call it the succession notebook; the button now does too.
   - `Components/Shared/IsaRail.razor`: the label and the screen-reader name. Matthew's script and Willis's click list say "Succession Notebook" on the rail.
   - Checked on screen at 1280 by 720: the label sits on two lines, like "Listen Library" and "Chapter 360", inside the button, with nothing spilling.
   - Build: 0 warnings, 0 errors. `appsettings.Local.json` was changed for the check and restored exactly; the test copy is stopped.

3. **"Removed entries": presidents now read what a removed entry said (November 17, 2026, Matthew).** The list showed who removed an entry and when, but only its writer could read the text. Matthew asked what the point of a notice nobody can read was, and how it would satisfy an auditor. It didn't: the content was kept, but nobody with authority over the record could review it.
   - `Services/Onboarding/NotebookService.cs` (`GetRemoved`): a president (and the outgoing president in the handoff window) is given the whole record, text included. "Removed" now means taken off the working list, not unsaid. Other board members still get nothing, and the option still isn't in their menu.
   - `Components/Shared/NotebookPanel.razor`: the line at the top of the list reads "Nothing in the notebook is erased. When a writer removes an entry it comes off the working list and stays on the record here: what it said, who removed it, and when."
   - `ONBOARDING-FEATURE.md` and a comment in `onboarding/prepare_demo.py` say the same.
   - Build: 0 warnings, 0 errors. Checked on a test copy: Kenji and Sato both see "Removed on June 26, 2026 by its writer, Sato Yumiko, President. Decision · Meetings · written on June 20, 2026 · Record NB-C001-0036" with the text "Draft: four board meetings next year, one a quarter."; Sutrisno (a board member in another chapter, not a president) isn't offered the option; the removed draft is still absent from the notebook's working list and from the printed copy.
   - Considered and not built: keeping the text hidden but recording a reason for the removal; moving the list out of the leader's menu into a review screen for ISACA staff.
   - `appsettings.Local.json` was changed for the check and restored exactly; the test copy is stopped.

4. **Matthew's speaking cards for Kenji's demo, working draft (November 17, 2026).** No app code changed.
   - `Claude outputs/Speaking cards - Matthew - Sprint 3.md` (new): one card per stop, in the order Matthew now clicks through Isa: Kenji, Today, the contact step, the board, Takahashi Mei's page, My progress, the calendar (July 2), the succession notebook, its first entry, "Ask Sato Yumiko about this", the "..." menu with Removed entries, then the Listen Library. Cards 8 and 11 are settled with Matthew; the rest are Claude's first wording. The Library card and anything after it are still to be written.
   - It replaces the demo part of `Claude outputs/Presentation script - Matthew - Sprint 3.md` and `Claude outputs/Click list for Willis - Matthew's parts.md`, which still follow the old seven-step order and the old slide numbers. Neither has been rewritten yet.
   - Team only until the presentation: it describes the onboarding demo.
   - Later the same day the set grew to 14 cards: the Listen Library (12, from Matthew's own reason: busy leaders learning on the go), Engage and Chapter 360 as placeholders (13), and a closing card ending "Leaders doing strategy" (14). Cards 8, 11 and 14 are settled. Considered and left out by Matthew: a card where Kenji asks a question and saves the answer, a privacy sentence, and "Terms explained".
   - `Claude outputs/Speaking cards - Matthew - Sprint 3 (print).pdf` (new) and its `.html` source: the cards as printable sheets, US Letter landscape, four cards to a sheet (5.5 by 4.25 inches each), one side only, with dashed lines to cut along. 14 cards on 4 sheets. Each card has its number, title, what to click (small, shaded) and what to say (large). The words are sized to fit each card. All four sheets were looked at as pictures: nothing is cut off. Not test-printed on paper. The small script that builds the sheets from the Markdown file is in Claude's temporary folder, not in the project; when a card's wording changes, the sheets need making again.

5. **The demo setup script now rebuilds the directory too (November 17, 2026, Matthew).** On Willis's computer "Meet your board" showed no board members, and then Takahashi Mei's page had no shared answers. Both come from files that Git doesn't carry: `data/directory.db` (people and board roles, built by a script) and `data/progress.db` (profile replies, filled by the setup script). His copies were out of date.
   - `onboarding/prepare_demo.py`: before anything else, it runs `directory/build_directory_db.py` and checks that Kenji's chapter has 6 board members on July 3, 2026. If the rebuild fails or the count is short, it stops and changes nothing else. Its closing lines now start with "directory.db: rebuilt from directory/; Kenji's chapter has 6 board members on July 3, 2026."
   - So one step, `python onboarding\prepare_demo.py` with Isa stopped, now prepares everything the onboarding demo shows, on any computer.
   - Checked on Matthew's laptop: with the four board members taken out of the local directory file (2 left, as on an out-of-date copy), the script put all 6 back and added both profile replies. Matthew's own progress and notebook files were backed up first and put back exactly afterwards. Not checked on Willis's computer.
   - To tell Willis: after pulling this, stop Isa, run the script, start Isa.

Tried and taken out again (November 17, 2026), so nobody repeats it without asking Matthew: when the succession notebook is enlarged or made smaller, its words re-wrap while the panel glides. Two ways to calm that were built and measured, and Matthew didn't like either: hiding the contents during the glide and fading them back in (the disappearing contents were as distracting as the scrambling), and holding the contents at one fixed width during the glide so they re-wrap once. Both were removed completely; the panels behave exactly as they did in the commit. Not tried: dropping the glide so a panel changes size at once.

## Suggested commit message

```
Kenji's invented Chapter Experience contact renamed to Grace Lim (two people were named Mei); rail button "Notebook" is now "Succession Notebook"; Removed entries: presidents read what a removed entry said; prepare_demo.py also rebuilds data/directory.db, so one step sets up the whole onboarding demo on any computer
```
