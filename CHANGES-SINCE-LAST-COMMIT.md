# Changes since the last commit

A running list of every change made since Matthew's last commit (October 28, 2026: fa31f7b, "updated two small database reference points", which shares data/onboarding.db through Git). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **data/notebook.db.bak-2026-10-23** still to be taken out of Git: it is still tracked after fa31f7b. Run `git rm --cached "data/notebook.db.bak-2026-10-23"` before the next commit. The file stays on Matthew's computer; .gitignore already ignores dated backups, so it won't come back. Harmless if it waits.

- **ONBOARDING-FEATURE.md**: new section "Sample pages and the RFP": the sample Engage and Chapter 360 pages are outside the RFP (Willis's point, Matthew agreed), so they stay as they are with no more building; the onboarding feature stays; remaining time goes to what ISACA scores.

- **ISA-PROJECT-CONTEXT.md**: two new sections for the Sprint 3 presentation and the supplement document: "The Sprint 1 presentation: its structure" (its five sections, intended outcomes, risk register, metrics and ask; a Sprint 3 plan that closes the loop on each) and "Framing the work in the RFP's terms" (three framing moves and each piece of work in the RFP's words).

- **presentation/slides.json, slide 12 "Additional features"** (Matthew's slide): brought up to date with everything built. "Built" (was "In the build, not finished", two items): First 90 days, Today note, Meet your people, Succession notebook, Worth saving?, Handoff checklist, Listen Library, How to use Isa. "Planned": Terms explained, Day 30/60/90 check-ins, Ask the last leader, Support team view, More roles, Phone and browser (Android app, Chrome extension), and "Also" (offline packages, a peer content panel, side-by-side model tests, all kept from the old list). The client isn't named, per the deck's rule; the sample Engage and Chapter 360 pages aren't listed, per the RFP decision. Checked on /present at 1280x720: every line fits on one row, clear of the slide controls.
  - Updated to match (October 28 to 29), all in Claude outputs:
    - **Presentation script - Matthew.md** and **Note cards for review - Matthew.md**: new slide 12 wording (about 45 seconds): everything on the left works in the app today; the right side is planned. The "running long" line and the reminder about the slide's labels changed to match.
    - **Presentation script cards - Matthew.pdf**: rebuilt (the earlier source wasn't kept), same layout, eight cards on two landscape pages; card 6 has the new wording, card 8 the new "running long" line.
    - **Run of show - Sprint 2 presentation.md and .pdf**: slide 12 now lists the 8 built and 7 planned items. Also fixed in passing: slide 11's translation line said LibreTranslate / Argos; the deck changed it to Meta's NLLB-200 on 26 October. The PDF was rebuilt in the same style and is now six pages.
    - **Presentation talking points - Eliyas.md**: a note that slide 12 is now Matthew's and has changed; the old points are kept as background.
    - **Matthew Sprint 2 note cards (large print).pdf** (new, October 29): Matthew's own latest cards (his Matthew_Sprint2_Note_Cards_2.pdf), word for word, in large print, except card 6 "Additional features", which has the new slide 12 wording with his stage directions ("Willis brings up slide 12", NEXT SLIDE). Card 6 rewritten with Matthew (October 29): his own opening and "fixtures" lines kept word for word, plus the daily note, booking a welcome call or writing a quick hello to the support contact, meeting the board, the Listen Library, and the two sample pages (the client's chapter tool first, then the member community with a joke about Isa's launch announcement), with no product names said out loud. About 1¼ minutes. Card 8's running-long line (skip the sample pages) and timing (slides 12 and 13: about 1½ minutes) changed to match. 14 cards, four to a landscape page (four pages). A one-card-per-page copy, "(one per page, for viewing)", is for reading on screen. This is the set Matthew uses.
    - **Presentation script cards (large print) - Matthew.pdf** (new, October 29, superseded by the file above): the same script, word for word, in large print for easy reading: 17 cards, four to a landscape letter page (five pages, dashed cut lines). Longer cards are split at natural pauses ("continued on the next card"), and each card's print is as large as fits.

## Suggested commit message

```
Update the Additional features slide; record presentation notes
```
