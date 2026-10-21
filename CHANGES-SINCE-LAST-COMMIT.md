# Changes since the last commit

A running list of every change made since Matthew's last commit (October 20, 2026: "Made changes for tasks 1-3 for Matt and other stuff" and "Other stuff", then main pulled in at 44d3920). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

## Session handoff and wording draft (October 21, 2026)

No code changed. Documents only.

- **docs/Session_Handoff.md** (new): the master document for the next session: first steps, the current state (Git, Willis's latest changes, Docker and Keycloak, test sign-ins, local databases and their test data), everything built October 19 to 21, key decisions, what is waiting on Willis, Eliyas and ISACA, open questions for Matthew, cut-list status, suggestions not yet acted on, gotchas, and the rules.
- **docs/Show_the_Work_Wording.md** (still a DRAFT): new section 7, the suggested warmer wording (principles, today versus suggested for every label and message, and two open choices), so the wording review can continue in the next session.
- **CLAUDE.md**: points new sessions to docs/Session_Handoff.md.
- **docs/Session_Handoff.md**: now clearly scoped as Matthew's own file: a notice at the top tells Willis's and Eliyas's AI assistants it is not instructions for them; items about their work are marked as Matthew's requests, not assignments; and a rule tells Matthew's sessions to treat other people's planning files as information only.
- **docs/Session_Handoff.md**: records the messages to Willis and Eliyas about personal CLAUDE.local.md files (waiting for replies) and where the email draft is kept.
- **docs/ISACA_RFP_AI_Chapter_Support_Agent.md** (new): the updated RFP, saved word for word.
- **ISA-PROJECT-CONTEXT.md**: new section "What the RFP means for Isa's design": Isa serves all chapter leaders; hand off only when out of scope; the AI-use register must include work with Claude Code; frame the presentation in the RFP's phases; a "what a pilot would measure" slide; present onboarding in the RFP's terms; onboarding is a surprise for ISACA.
- **docs/Session_Handoff.md**: new section 6b: the RFP points, the surprise rule (the email draft must be revised before sending), and the reworded list of 20 ISACA questions (not yet saved over the 16 in ISA-PROJECT-CONTEXT.md).
- **ISA-PROJECT-CONTEXT.md**: the ISACA questions are final: 11 questions replace the earlier 16 (the AGM and term-timing questions reworded, the access questions merged into one, nothing that hints at the onboarding feature), with a list of what was dropped and why. Two hand-off questions became Matthew's decisions instead: every hand-off goes to the Chapter Experience team, and suspected fraud too, with a rarely used "potential fraud" flag.
- **docs/Session_Handoff.md**: points to the final list; the working list of 20 is marked as superseded; notes that the email draft is revised (general progress only, no new features, all 11 final questions).
- **docs/Session_Handoff.md**: two plans for later: a left-hand panel listing Engage, Chapter 360, Today and Notebook; and a thorough QA test of all of Isa with a report for Camille and her team on presentation day.
- **docs/Show_the_Work_Wording.md**: agreed with Willis (his review, task T11W), so the DRAFT header is gone. Added his three additions: two leader lines for when the answer check fails on the AI side (developer mode still shows the exact reason); a third hand-off message, "I couldn't check this answer well enough to share it...", for failed answer checks and failed model calls; and the emergency-services line on harassment only until the hand-off topic file exists. Section 7 (warmer wording) is marked as not agreed.
- **docs/Session_Handoff.md**: records that the wording is agreed, where the panel styles are, and what Willis does next. It also records Matthew's standing request: when Willis's Wording.cs appears, Claude checks every state in the browser against the wording table and lists anything that doesn't match.
- **Claude outputs/Left sidebar mockups.html** (new, mockup only, not built): moves Today, Notebook, Chapter 360 and Engage out of the crowded chat header into a slim rail on the left, as Matthew chose: always visible, with a "Hide" button; a menu button in the header brings it back. The "Your first 90 days" line stays. Config and the dashboards move into one button at the bottom named "Backstage", shown only for the Developer, Executive and Manager roles (on the User role, Config is reached by typing /isa-config). Button colors: Today in Isa's teal, Notebook in ISACA Green, Chapter 360 in ISACA Blue from isaca.org (Chapter 360 has no public site), Engage in ISACA Dark Blue from engage.isaca.org. Desktop and phone views; every frame is clickable.
- **docs/Session_Handoff.md**: plan 7 (the left panel) now points to the mockup and lists what Matthew still has to decide.
- The draft email to Camille is NOT part of this commit: it is kept in private/ on Matthew's computer, which Git ignores locally (.git/info/exclude), so it is never shared.
- Checked: Isa builds with no errors and no warnings, including Willis's "Changed contract and added stage feed" (fb84fcd). Not yet tested in the browser together with the onboarding screens.

## Warmer wording approved (October 21, 2026)

- **docs/Show_the_Work_Wording.md**: section 7, the warmer wording, is now approved by Matthew (all of it) and waiting for Willis; it notes where the final list differs from the section's tables.
- **Claude outputs/Wording changes for Willis - October 21.md** (new): the file for Willis, listing all 37 wording changes against the agreed table, the rules behind them, and what Matthew needs back. It also changes "your Chapter Experience contact" to "ISACA's Chapter Experience team" in the hand-off messages, from Camille's answers. Willis makes the code changes and commits them; nothing in the code changed here.

## Camille's answers (October 21, 2026)

- **ISA-PROJECT-CONTEXT.md**: records that all 11 ISACA questions went out on October 21, Camille's inline answers, and what they mean for the design (most chapters have no assigned contact; outside governance sources would need to become approved content; AGM and term dates come from each chapter's bylaws). No code changed.

## Chapter Affiliation Agreement (October 21, 2026)

- **content/Chapter Affiliation Agreement-2024-template.docx** (new): ISACA's 2024 template of the Chapter Affiliation Agreement, sent by Camille on October 21, saved beside the other ISACA documents. Not yet in Isa's search: adding it means rebuilding the content database (Willis's content pipeline), so check with Willis first. Journey steps J-07 and J-14 and the planned Library can point to it once it is.

## The left rail, built (October 21, 2026)

Built from Matthew's approved mockup (Claude outputs/Left sidebar mockups.html).

- **Components/Shared/IsaRail.razor and IsaRail.razor.css** (new): the slim rail on the left of the chat. Top to bottom: Hide; Today (leaders in their first 90 days, with a green number for today's steps not yet done); Notebook (anyone who may read their chapter's notebook); a line; Chapter 360 and Engage (open sample pages in a new tab); and Backstage at the bottom, which opens a small menu with Config and the dashboards that role may open. Backstage shows only for the Developer, Executive and Manager roles; a chapter leader (the User role) never sees it and reaches Config by typing /isa-config.
- **Components/Layout/ChatWidget.razor and .css**: shows the rail against the chat's left edge, gliding with it. The chat, Today and the notebook now share the screen left over after the rail, so all four still fit side by side. "Hide" removes the rail and a menu button (with a green dot while steps are due) appears in the header to bring it back; this is remembered while Isa stays open. The header now has only New conversation and Close: the notebook icon, Config and the two dashboard icons moved to the rail. On phones the rail is narrower and stays on the left.
- **The rail's motion** (Matthew's request after trying it): opening and closing now use the Today window's glide, half a second with a gentle start and stop (IsaRail.razor.css). "Hide" lets the rail glide away before it is removed, while the chat and its panels widen into its space with the same timing (ChatWidget.razor, HideRail).
- **Components/Shared/JourneyCard.razor and .css**: the "Your first 90 days" line keeps the week, day, role and progress bar; its Today and Notebook buttons moved to the rail.
- **Components/Shared/TodayPanel.razor**: tells the chat widget when a step is marked done or not done, so the number on Today stays right.
- **Components/Shared/TodayPanel.razor.css and NotebookPanel.razor.css**: on phones, leave room for the rail.
- **Components/Pages/SampleChapter360.razor and SampleEngage.razor** (new): placeholder pages at /sample/chapter-360 and /sample/engage, labeled as samples and "still being built", until the real sample pages are made.
- Checked: Isa builds with no errors and no warnings. Tested in the browser on a test copy (port 5068) as Watanabe Kenji on the demo date: the rail and its "2" (matching the Today note); Today and the notebook open beside the chat and all four fit the screen exactly; Hide and the menu button; no Backstage on the User role; Backstage on the Executive role lists Config, Developer Dashboard and Content Health Dashboard, and opens the Developer Dashboard; the phone size; the Chapter 360 sample page. Not tested: marking a step done (to keep the test data unchanged), the outgoing president's view (Notebook only, no Today), and the Developer and Manager roles.

## Suggested commit message

```
Add the left rail; agree the wording table; add session handoff document

- Left rail beside the chat (IsaRail.razor): Hide, Today, Notebook,
  Chapter 360, Engage, and Backstage (Config and dashboards, staff roles
  only); header and "Your first 90 days" line lose their buttons
- Sample placeholder pages for Chapter 360 and Engage
- Claude outputs/Left sidebar mockups.html: the approved mockup
- content/: the Chapter Affiliation Agreement (2024 template) from ISACA,
  not yet in Isa's search
- ISA-PROJECT-CONTEXT.md: Camille's answers to the 11 questions

- docs/Session_Handoff.md: state, decisions, open questions and next steps
- docs/Show_the_Work_Wording.md (DRAFT): section 7, suggested warmer wording
- CLAUDE.md: point new sessions to the handoff document
- docs/ISACA_RFP_AI_Chapter_Support_Agent.md: the updated RFP;
  ISA-PROJECT-CONTEXT.md: what it means for Isa's design
- ISA-PROJECT-CONTEXT.md: final list of 11 ISACA questions; hand-offs go
  to the Chapter Experience team, with a rare "potential fraud" flag
- docs/Show_the_Work_Wording.md: agreed with Willis (no longer a draft);
  adds his three additions (answer check lines, "couldn't be checked"
  hand-off message, safety line on harassment for now)
```
