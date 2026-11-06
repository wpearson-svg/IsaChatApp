I'm sharing design decisions from a planning conversation about my Isa project, so you have the full context. Please read all of it.

When you're done:
1. Save this entire message as a file named ONBOARDING-FEATURE.md in the top level of my IsaChatApp folder.
2. Add the line @ONBOARDING-FEATURE.md to the end of CLAUDE.md so you read it every session. If CLAUDE.md doesn't exist, create it with just that line.
3. Don't change anything else in the project, and do NOT commit or push to Git. I always handle Git myself.
4. Then summarize the plan back to me in a few sentences and wait for me before building anything.

=== WHAT THIS IS ===

I'm designing and building the New Chapter Leader (onboarding) part of Isa myself, working with you, not with my teammates. ISACA wants something that walks new chapter leaders through their first 90 days. Chapter leadership rotates every year, so knowledge is lost at every handoff, and volunteer onboarding is a known weak spot for ISACA.

The feature tells one story: a new leader arrives, is guided through their first months, and leaves something behind for the next leader.

ISACA has said new tools should support its staff, not replace email or the help desk. Isa guides leaders and connects them to people. She never replaces the human relationship.

=== CONSTRAINTS FOR THIS FEATURE ===

- Models run locally through Ollama. No paid services.
- No real personal information. Use invented sample people only, including a sample Chapter Experience contact.
- ISACA's survey results and study findings are confidential design material. Never put survey figures or study details in code, comments, or sample data.
- Everything leaders see must go through Isa's own pipeline: the Input Compliance gate, the search with citations, the Output Compliance gate, permission tiers, and logging. No part of this feature may create a side door around those controls.
- Work with me one step at a time, and explain what each piece of code does before moving on. I need to be able to explain every part of it to ISACA.

=== FEATURE 1: THE FIRST-90-DAYS JOURNEY ===

- Isa reads each leader's term start date and shows the steps for where they are in their term. A president on day 3 and a president on day 60 see different next steps.
- Steps come from ISACA-approved content: the President Guidebook's turnover and orientation checklists, its annual checklist (fixed dates plus dates counted from the AGM), and the compliance timeline (submission window November 1 to December 31).
- Leaders can mark steps complete, and a progress bar moves.
- Any question about a step goes through Isa's normal question-and-answer pipeline, with citations.
- The President Guidebook's orientation checklist sets the final order of steps.

Week-one pacing (not everything on day 1):
- Day 1: Welcome, plus "Get to know Isa," a five-minute guided tour. Every later step uses Isa, so the leader learns the tool first. Day one stays light.
- Days 2-3: "Meet your Chapter Experience contact." Booking a call takes lead time, so the leader picks an option early. The call itself can happen later in the week.
- Days 3-5: "Read what your predecessor left you" (the succession notebook). By then the leader knows Isa and their contact.
- The guidebook's orientation checklist picks up from week two.

=== THE "MEET YOUR CHAPTER EXPERIENCE CONTACT" STEP (MOCKUP I APPROVED) ===

Layout of the step card, top to bottom:
1. Header: "Your first 90 days" with "Week 1 · Day 3" below it, and a role badge on the right ("Chapter president").
2. A thin progress bar showing progress through the 90 days.
3. Step title "Meet your Chapter Experience contact," with one line below: "Your contact is the person to reach when Isa can't help, or when you'd rather talk to someone."
4. A contact card: an initials circle, the contact's name, and their team and region (for example "Chapter Experience · Asia-Pacific chapters"). Use an invented sample contact, never a real staff member.
5. Three option cards side by side. Any one of them completes the step:
   - Book a welcome call: available time slots converted to the leader's own time zone (for example "Times shown in Asia/Tokyo"), with one button per slot.
   - Send an introduction: a short hello by email, written the leader's way, with a "Write message" button.
   - Join a group welcome: meet other new presidents in the same region, with a "See next session" button.
6. A privacy line at the bottom with a lock icon: "Your contact sees your chapter, role, and term dates. Your questions to Isa stay private."

Design rules behind it:
- Three options instead of a calendar only, because leaders prefer different ways of making contact, and the Chapter Experience team is small.
- Time zones come from the time zone field in our synthetic person records.
- The contact sees the leader's chapter, role, term dates, and onboarding progress. The leader's questions to Isa stay private unless the leader chooses to share them.
- For the prototype, the calendar is fake: a small database table of made-up availability slots. Booking a slot marks it taken and completes the step. A real calendar connection is ISACA's decision later.
- Open question for Camille at ISACA: does each chapter already have an assigned contact, and how much welcome-call time can her team give?

=== FEATURE 2: THE SUCCESSION NOTEBOOK ===

- Each chapter has one notebook that passes from president to president.
- After certain answers, Isa offers one line: "Want me to save this to your chapter's succession notebook?" One tap saves the question, the answer, its sources, and the date. If the leader adds a sentence about what they decided, that is saved too.
- Leaders can also add notes directly: contacts, what worked, what's unfinished.
- A quiet save icon appears on every answer, so leaders can always save something themselves.
- When a new president's term starts, the notebook appears in their journey as the step "Read what your predecessor left you." That is where the two features meet.

When Isa offers to save. These are simple rules designed ahead of time, not AI guesswork, so the reason for every offer is visible and checkable:
1. Tagged source sections. ISACA content sections involving decisions or deadlines are tagged (election rules, quorum, compliance timeline, AGM-relative dates). If the answer cites a tagged section, that is a reason to offer.
2. Question type label. Each question gets one label: "decision," "deadline," "how-to," or "general information." Offer only for "decision" and "deadline." Changed October 20, 2026: the label comes from fixed rules, not an AI model, because Isa's model only words answers from the passages found (see the ground rule in ISA-PROJECT-CONTEXT.md). The simplest rule: the label is the tag of the document section the answer cites, set ahead of time. How a stage passes those tags along is a question for Willis.
3. Chapter-specific wording, such as "our chapter," "should we," or "we decided."
4. Frequency cap: at most one offer per conversation, never twice on the same topic.

Examples:
- "When is the compliance submission window?" -> deadline label, tagged section -> offer
- "Can our chapter hold elections online?" -> decision label, "our chapter," tagged section -> offer
- "Where do I log in to Engage?" -> how-to label -> no offer
- "What does CISA stand for?" -> general information -> no offer

Also:
- Log whether each offer is accepted or dismissed, counted by category only, never by person. An admin can switch off a category in the Dev Dashboard if leaders keep dismissing it.
- Show-the-work: the expandable view shows why Isa offered the save, for example "Offered because: deadline question, cites Compliance Framework Q4 timeline."
- Changed October 28, 2026 (Matthew): no pop-up offer. A leader often asks again just to remind themselves, and a question after the answer would get in the way; the "Save to notebook" button is already under every answer. Instead, the button itself carries a quiet label when the answer cites a deadline or decision section, for example "Deadline · worth saving?". Nothing pops up and nothing needs dismissing. The tags live in our own list in onboarding/ (document and section, label, reason), matched against the sources the save button already receives, so Willis's pipeline, the content database and the stages are not changed. Rules 3 and 4 above (chapter wording, frequency cap) are not needed for a label that never interrupts.
- Built October 28, 2026 (Matthew asked Claude to build it directly): onboarding/save_hints.csv tags 13 of the 45 sections in Isa's content (7 deadline, 6 decision; each row has the reason shown to the leader and a "Why tagged" note for reviewers). Tagged: the compliance review window and the Common Tasks compliance page; President Guidebook pp. 3 (elections, committees), 5 (financial controls), 6 (reporting deadlines), 7 (board meetings), 8 (after the AGM), 9 (handover records), 10 (how to list leaders online), 11 (President-Elect role), 12 (turnover timeline), 13 and 14 (annual checklist, AGM-relative dates). Not tagged: role summaries, systems overview, Engage features, CPE upload, purpose statements, and the guidebook's general pages (pp. 2 and 4). Only the source the answer cites (the first one) counts. Built into data/onboarding.db (save_hints table); the build script warns if a tagged section is missing from data/content.db, for example after the content is rebuilt with new section names. JourneyService.GetSaveHint, Components/Shared/SaveToNotebook.razor. The open save box explains it: "Marked as a deadline because this answer cites ...". Not built: counting how often labelled answers are saved (would need the shared event log).

=== FEATURE 3: "HOW TO USE ISA" HELP SECTION ===

Two forms: the short guided tour as the day-1 journey step, and a full section available anytime from the menu. The full section covers:
1. What Isa can help with: finding ISACA guidance with sources, the 90-day journey, the succession notebook, compliance deadlines.
2. What Isa won't do: rule on legal questions, local-law conflicts, board disputes, or anything financial or personal. Those go to a person.
3. Example questions that work well.
4. How to save decisions and notes.
5. How to reach a person, and what happens when Isa hands off a question.
6. What happens to your information: what the contact can see, and that questions stay private.

Rules:
- The guide describes only what Isa actually does at the time of each demo. Never document a feature that isn't built yet.
- Load the guide into Isa's knowledge base, so leaders can ask "What can you do?" and get a cited answer.
- Built October 23, 2026 (Matthew's approved mockup, Claude outputs/How to use Isa mockups.html): a "?" Help button at the bottom of the rail opens the six-part guide in the right-hand place; the "Get to know Isa" step (J-02) has a five-card tour ending in "Read the full guide". All words in onboarding/help.json. Checked against the app: "What Isa doesn't do" is written as advice (those topics aren't blocked yet; input compliance is a keyword stub), and only one example question is listed (the only one of four answered correctly on 23 October). Not yet loaded into Isa's knowledge base (Matthew's decision: later, with Willis).

=== FEATURE 4: THE "TODAY" NOTE (ADDED OCTOBER 19, 2026) ===

- When a leader opens Isa, she greets them with a short "Today" note at the top of the chat. Example: "You're on day 12. Coming up this week: book your welcome call. The compliance window opens in 19 days."
- The note is built from data Isa already has: the leader's day of term, their next journey steps, and dated items from ISACA-approved content (the President Guidebook's annual checklist and the compliance timeline).
- It only appears inside Isa when the leader opens it. Nothing is sent by email or notification while they are away, because ISACA has said new tools should support its staff, not replace email or the help desk.
- The dates and counts in the note come from the database, not from the AI model, so they are always exact. The day of term uses the demo date setting.
- Everything in the note follows the same rules as the rest of the feature: approved content only, no side door around Isa's pipeline, and any question about an item goes through the normal question-and-answer pipeline with citations.
- Built October 20, 2026 (Components/Shared/TodayNote.razor), as Matthew asked: the note appears each day. The first time a leader opens the chat on a day (the demo date while it is on), the full note sits at the top of the chat: day of term, the steps for today that aren't done (or "You're up to date for today"), steps starting in the next 7 days, and any key date that is open or starts within the next 30 days, with "Open Today" and "Got it". "Got it" shrinks it to one line for the rest of that day ("Day 3 · 2 steps for today", with Show and Open Today); that is remembered in data/progress.db (today_note_seen).
- Changed October 27, 2026 (Matthew): the note now always opens as the one line, with Show to open the full note and "Got it" to fold it back. The old "full the first time each day" rule depended on data/progress.db, which is local to each computer, so the note looked different on each team member's copy.
- Key dates live in onboarding/key_dates.csv (built into data/onboarding.db), each with its source. The first is the annual compliance window, 1 November to 31 December. The 30-day rule is a default chosen while the fixed-date question below is still open; Matthew can change it.
- Not part of this feature: a full calendar where leaders add or remove their own deliverables and keep private notes. That goes on the roadmap slide, labeled as planned. If it is built later, ISACA's required items stay locked so they cannot be removed.
- Built November 6, 2026 (Matthew asked Claude to build it directly), a read-only calendar instead: "Calendar · the whole month" in the Today window shows a month at a time: journey steps on the day they start, ISACA's key dates (open days shaded), the leader's booked welcome call or group seat, and the chapter notebook's entries from the same day last year. Clicking a day lists them; steps open from there. No booking, reminders or own items: reminders would break the inside-Isa-only rule, and a personal calendar would compete with the leader's own (the sample-pages lesson). Those stay on the roadmap. Components/Shared/TodayPanel.razor.
- Added the same day (Matthew chose to build it for real rather than show a button that doesn't work): "Add to my calendar" on each step, key date and booked call or seat in the calendar's day details. It hands the leader a calendar file (.ics) for Outlook, Google or Apple Calendar; Isa sends nothing and connects to no calendar, and the leader's own calendar does any reminding. The event holds only the title, dates and a line pointing back to Isa. It's a one-time copy that doesn't update; say so in the supplement document, not beside the button (Matthew). Services/Onboarding/CalendarFile.cs.
- Built November 6, 2026: "Last year around now". The full Today note has a "Last year" row quoting up to two succession-notebook entries from 3 days before to 14 days after the same date a year earlier, word for word, with "Open the notebook". A fixed date rule, no AI; the notebook's reading rules apply. So the demo has a past year, onboarding/notebook_history.csv holds 10 invented notes from Sato Yumiko's 2025-26 year, built into data/onboarding.db and copied into data/notebook.db once when Isa starts. NotebookService.GetLastYear, Components/Shared/TodayNote.razor.

=== MORE ONBOARDING PIECES (ADDED OCTOBER 19, 2026) ===

Five more pieces, all built from data and content the project already has. All of them follow the same rules as the rest of the feature: approved content only, no side door around Isa's pipeline, invented sample people only.

A. Handoff checklist for the outgoing president
- In the last 30 days of a term, Isa prompts the outgoing president to fill in the succession notebook: open items, key contacts, what worked, what's unfinished.
- The prompts appear in the outgoing president's "Today" note and as journey steps.
- This closes the loop in the story: one leader leaves something behind, the next one finds it.
- Decided and built October 20, 2026: the checklist is the President Guidebook's own handover list, word for word, all 13 items (p. 8: notify the Chapter Experience team of the new leaders; pp. 12-13: the Turnover Procedures, including Recognition and Appreciation), each with a short title in Isa's words and its page. Nothing added that the guidebook doesn't say, and no question to ISACA needed: the prototype shows what Isa can do with ISACA's own text. Data: onboarding/handoff_steps.csv (built into data/onboarding.db); every sentence was checked against the guidebook.
- Shown from 30 days before the term ends until 30 days after (the same window in which the outgoing president can still write to the notebook), so Sato Yumiko sees it on the demo date. It sits at the top of the notebook panel (Components/Shared/HandoffChecklist.razor), with "Mark as done" and history like the journey steps; "List the open items" also has "Add to the notebook", which opens "Add a note" with "Unfinished" chosen. The outgoing president's Today note shows their handoff progress, the time left and the next item, with "Open the checklist".

B. Journeys for other board roles (FUTURE BUILD, not built for the demo)
- Treasurers, secretaries and other board roles would get their own short journeys, built from the guidebook sections on their duties.
- They would use the same journey tables and screens as the president journey; only the steps differ.
- Decided October 19, 2026: not built now. It goes in the presentation as a future build, labeled as planned. The demo shows the president journey only.

C. "Meet your board" step
- Shows the new leader who else is on their chapter board, from the team's directory data.
- Names, roles and time zones only. No emails or other contact details on this screen.
- Built October 23, 2026 (Matthew's approved mockup, Claude outputs/Meet your board mockups.html): the J-05 step in the Today window lists the chapter's board: name, role, term dates (added: ISACA keeps them, Camille confirmed), and each member's time zone with the time there now and the difference from the leader's. Four invented board members added to Kenji's chapter (C-001) in directory/ and data/synthetic; only his chapter has a full board for now. LeaderTermService.GetBoard, Components/Shared/TodayPanel.razor.

D. ISACA terms explained
- A short glossary of terms new leaders meet early (for example AGM, CPE, Engage), loaded into Isa's knowledge base so leaders can ask "What's an AGM?" and get a cited answer.
- Definitions come from ISACA-approved documents. Any term the documents don't define is left out until ISACA approves wording for it.
- Built November 3, 2026 (Matthew: "Let's just stick with ISACA's words"): "Terms explained", a card at the top of the Library (Matthew chose the Library over the Help panel or a rail button). 26 terms that ISACA's documents define, A to Z, with search: each shows ISACA's words exactly, in quotes, with the document, page and part, and "Read it" opens the document at that passage (on the named page), ready to read or listen from there; nothing plays until the leader presses play, and Back returns to the term. Sources: the President Guidebook, the Chapter Systems Overview, Core Roles, Chapter 360's Introduction and the Affiliation Agreement's definitions. onboarding/glossary.json (read while Isa runs); onboarding/check_glossary.py checks every quote word for word against the text the Library shows (26 terms, 0 problems; a quote with one word changed is caught). No new content in Isa's search, so no pipeline change: "What is Engage?" in the chat still goes through the normal pipeline. Not a tax glossary: tax terms differ by country and Isa's rules send tax questions to a person (Matthew agreed, November 3).
- The terms ISACA's documents use without defining (AGM's meaning, bylaws, minutes, ex officio, audit, review, compilation, financial verification, D&O insurance, Willingness to Serve Agreement, the Chapter Experience team, ISACA Support and more, plus quorum, which the documents don't use) are listed in docs/Glossary_Terms_To_Request.md, with where each appears in ISACA's documents, to send to ISACA. Worded about Isa's content only, never the onboarding feature.

E. Day 30, 60 and 90 check-ins
- At each milestone, Isa asks one question: "What's still unclear?"
- The leader chooses whether to share the answer with their Chapter Experience contact. Nothing is shared unless the leader says yes.
- Unshared answers stay private to the leader. Counts are logged by milestone only, never by person.
- This supports ISACA staff without replacing them, and keeps the leader in control of what is shared.

=== FEATURE 5: DOCUMENT LIBRARY WITH A "LISTEN" BUTTON (ADDED OCTOBER 19, 2026) ===

- A "Library" page lists the ISACA-approved documents Isa answers from, grouped by topic, each with a short description and a way to open the document itself.
- Each document has a "Listen" button. Isa reads the document aloud word for word, with pause and play, so a leader can listen on the way to work.
- It uses the read-aloud feature built into the leader's web browser and the voices already on their computer or phone. No paid service, nothing to install, no extra load on the demo laptop, and no ISACA material is sent anywhere.
- Word for word means nothing is rewritten or summarized, so nothing can be wrong that isn't already in the approved document.
- Limits to state honestly: the page has to stay open while it reads; voices and languages depend on what the leader's device has installed; some phones stop reading when the screen locks.
- Only approved documents are listed. Any sample chapter documents made for the sandbox must be labeled as samples and kept separate from ISACA guidance.
- Added October 20, 2026 (Matthew confirmed Feature 5 stays in the plan):
  - Listen on journey steps: a step that points to an approved document (for example J-07, J-22, J-27) has its own Listen button for that document. Same browser read-aloud as the Library.
  - Listen in the succession notebook: the new president can listen to the notes the previous president left. These are the chapter's own words, read word for word, so they need no ISACA approval.
  - Documents we don't have yet, such as the Chapter Affiliation Agreement (ISACA question 4), are still listed, marked "Requested from ISACA," with no Listen button until the document arrives. This shows it is part of the solution without pretending we have it.
- Built October 23, 2026 (Matthew's approved mockup, Claude outputs/Library mockups.html): a Library button on the rail (ISACA Purple) opens the Library in the notebook's place on the right. For now it lists only ISACA's five documents that Isa answers from (Matthew's choice), from onboarding/library.json, grouped by topic, with Read and Listen. Listen reads the approved passages from data/content.db word for word, one sentence at a time, highlighting the sentence being read; pause, back and forward by section, speed, and click a sentence to read from there. Shown to anyone signed in. Components/Shared/LibraryPanel.razor, Services/Onboarding/LibraryService.cs. Not yet: Listen on journey steps and in the notebook, opening the original file, the team's governance guides, and the Chapter Affiliation Agreement (Camille has since sent it; it isn't in Isa's content yet).
- Added October 28, 2026 (Matthew): a "Chapter 360" group in the Library with ISACA's seven Chapter 360 documents (Introduction and the six Focus Areas), Read and Listen like the others. A note under the group says Isa doesn't answer questions from them yet (they aren't in her approved content; adding them is a team decision with Willis). The text comes from ISACA's Word files through onboarding/build_chapter360.py into onboarding/chapter360.json; left out are ISACA's internal "[Node]" codes, the leftover text of a diagram in the Introduction, and the asterisk after template names (its meaning isn't known). "TBD" entries stay, so the text is word for word. library.json marks them "source": "chapter360". Willis's voice warm-up (Tts:PreGenerate) now also pre-makes audio for these documents, about 11,000 more words, once, in the background.
- Added November 3, 2026 (Matthew): the Chapter Affiliation Agreement (ISACA's 2024 template, which Camille sent) in a new "Your chapter and ISACA" group, Read and Listen only, with the same note that Isa doesn't answer questions from it (adding it to her approved content is a team decision with Willis). onboarding/build_affiliation_agreement.py turns the Word file into onboarding/library_texts/affiliation-agreement.json, word for word, with the clause numbers and letters Word shows (I., A., i.) put back, because the agreement refers to them; checked against its own cross-references (Clause VIII. Chapter's Duties, Clause XIII. Indemnification, paragraph D. of Clause III., paragraph I. of Clause VIII.). Left out: the notice-address and signature tables at the end (a form to fill in), and the page header and footer; the Library's description says so. The template's blanks (<DATE>, [CHAPTER NAME]) stay. About 8,600 words, 57 minutes; the voice warm-up pre-makes its audio too. library.json "source": "text" means a file in onboarding/library_texts/. A line under the title, in the list and at the top of the document, says: "This is ISACA's template. Your chapter's signed agreement may differ, for example in its country-specific exhibit (Exhibit D). For questions about your chapter's agreement, ask ISACA's Chapter Experience team." (library.json "note", shown but never read aloud.) Matthew will ask ISACA about including it in the November 4 update.
- Built November 3, 2026: Listen on journey steps. Every step that cites an ISACA document in the Library has a "Listen to the source" box with one button per place it cites ("President Guidebook, page 14 · about 1 minute"). It opens the Library beside Today, reads that page (or the whole document, for the short ones and the Affiliation Agreement) word for word, and stops with "That was the part this step points to. Press play to keep listening." Clicking a sentence or skipping a section means the leader chose, so it then reads on. Which step points where: onboarding/step_listen.json (read while Isa runs). Steps from Isa's onboarding plan have none, so Watanabe Kenji's day-3 steps (J-03, J-04) look the same as before. J-07 has two: guidebook p. 13 and the Affiliation Agreement. Listen in the succession notebook is still to do.

=== SAMPLE PAGES AND THE RFP (DECIDED OCTOBER 28, 2026) ===

- Willis pointed out that the sample Engage and Chapter 360 pages are outside the RFP, and Matthew agreed: the RFP asks for an AI agent that answers from ISACA-approved content and hands off to people; it never asks to rebuild or redesign ISACA's own products, and none of its evaluation criteria reward that.
- So: no more building on the sample pages. They stay as they are, as backdrop that makes the demo feel real (the rail buttons pointing leaders to the tools they already use do fit the RFP's "Interface" goal). Show them briefly at most, and never present them as something the team is offering ISACA.
- The onboarding feature stays (Matthew): it is an important part of the team's work. Present it in the RFP's terms: it reduces repetitive questions from new leaders ("Automate"), supports the Chapter Experience team rather than replacing it, and keeps knowledge through the yearly handover.
- Remaining time goes to what ISACA scores: the AI-use register (required), testing and the QA report, the pilot measurement slide, and answer quality once RAG is in.

=== CHAPTER 360 SAMPLE PAGE (BUILT OCTOBER 27-28, 2026) ===

- The Chapter 360 button on the rail opens /sample/chapter-360 in a new tab: a look-alike of ISACA's Chapter 360 for the sample Sakura Bay Chapter (the real one is behind ISACA's login).
- October 28, 2026 (Matthew: "it doesn't have to work ENTIRELY, but it needs to LOOK like it works"): parts of it now work.
  - Overview: the "360 view" chart, self-assessment progress, the six Focus Area cards (each opens its Focus Area), recommendations, the leadership-continuity tip (opens Governance at Chapter Leadership Continuity) and the board.
  - A Focus Area: what it is, why it matters, and its Practice Areas, each opening to its numbered practices with their benefits and tools; "Read & listen" plays the whole document in Isa's voice with the Library's player.
  - Tools & templates: all 271 tools and templates the documents name, searchable and filtered by kind. Opening one shows a note that templates are in ISACA's real Chapter 360, not the prototype.
  - Self-assessment: rate each Practice Area 1 to 5; the chart reshapes as you go; the Focus Area score is the average (ISACA's own scoring rule). Saved only on the page: reloading starts over. Advocacy and Continuous Improvement start unassessed, so the demo can fill one in.
  - Help: ISACA's Chapter 360 introduction, to read or listen to.
- Changed later on October 28, 2026 (Matthew asked for the chart to focus on the leader; decided together):
  - The chapter's 360 view stays ISACA's design. ISACA's introduction: each leader rates the chapter on their own and "the scores will be averaged into one final score". In the sample, 3 of 6 board members have rated four Focus Areas; nobody has rated Advocacy or Continuous Improvement. The self-assessment is now "Your rating of the chapter": the signed-in leader's rating is averaged in (each row shows the board average so far). The board card shows who has rated ("✓ Rated", "Not yet"), never anyone's own scores. Rating individuals was considered and set aside: it would change what ISACA's own tool measures, and scoring volunteers could feel like a performance review.
  - "My Chapter 360": the leader's own learning, private to them. It records which Focus Areas they've opened, which Practice Areas they've opened, and whether they've read or listened, on the page only. A card lists all six with a bar and suggests the chapter's lowest-rated area they haven't opened yet. The chart shows it as a dashed purple shape; Focus Area cards and pages show "✓ Explored".
  - The chart is clickable: hovering over or focusing a point shows a card (the chapter's level, how many ratings, the lowest Practice Area, the leader's own exploring, and "Open" and "Add your rating"); clicking a point opens that Focus Area, or in the self-assessment picks it to rate. Keyboard: Tab to a point, Enter to open. On a phone the card sits under the chart.
  - Later, optional (not built): "Who can help", showing on each Focus Area what board members said they'd enjoy helping with, from the planned board profile form, shared only if each member chooses.
- The ribbon says: sample page, not ISACA's real Chapter 360, some parts work, nothing is saved. The chapter, scores, progress and people are invented; the one-line summary on each Focus Area card is the team's wording; everything else is ISACA's.
- Files: Components/Pages/SampleChapter360.razor (+ .razor.css, .razor.js), Services/Onboarding/Chapter360Service.cs.

=== ENGAGE SAMPLE PAGE (BUILT OCTOBER 27-28, 2026) ===

- The Engage button on the rail opens /sample/engage in a new tab: a look-alike of the Sakura Bay Chapter's community on ISACA's Engage, with the "Say hello to Isa" announcement.
- October 28, 2026 (Matthew): parts now work, like the Chapter 360 page.
  - Home: the announcement's example conversation steps through two real questions Isa answers from ISACA's text, word for word (board meetings, President Guidebook p. 7; the yearly compliance report, Common Tasks and Navigation Guide). "What can Isa do?" opens a short, honest list of what she does and won't do. "Post a message" and "Following" work.
  - Discussion: seven invented threads with filters (Isa, Events, Study group, Volunteering, Photos), search, and "New discussion". A thread opens with its replies; you can like it and reply. Replies about Isa describe only what she really does (answers with the page it came from; passes a local-tax question to the Chapter Experience team with a reference number).
  - Library: folders and eight invented files; opening a file shows a note that files aren't in the prototype.
  - Events: upcoming and past events; each opens with details, Register (the count goes up, "including you") and Add to calendar.
  - Members: 16 invented members "who chose to be listed in the directory", names and roles only, searchable.
  - The search box in the header searches discussions. Connect and Volunteer show a note that they're part of ISACA's real Engage.
- Anything typed stays on the page until it is reloaded: nothing is saved or sent, and none of it goes through Isa. The ribbon says: sample page, not ISACA's real Engage, some parts work, nothing is saved.
- Files: Components/Pages/SampleEngage.razor (+ .razor.css; it uses the Chapter 360 page's small scrolling script).

=== OPEN NOTEBOOK: WHERE IT FITS AND WHERE IT DOESN'T ===

Open Notebook is a free, open-source, self-hosted alternative to Google's NotebookLM. It runs locally in Docker, works with Ollama, and has a REST API. Google's NotebookLM itself is not an option: it runs only in Google's cloud, the developer version costs money, and ISACA material can't leave our sandbox.

- Open Notebook is NOT the core of the 90-day journey. It waits for questions, but the journey has to lead the leader step by step. The journey is built inside Isa with our database and Isa's pipeline.
- If it is used at all, it runs behind Isa, never as a separate app with its own chat. A separate front door would skip Isa's compliance gates and logging.
- Best use: a behind-the-scenes authoring tool for ISACA staff, turning guidebooks into audio briefings, study guides, or a "your first week" summary. Staff review and approve the output before it becomes Isa content.
- What NotebookLM does well, and Isa should match: answers grounded only in approved documents, clickable citations to the exact passage, and saying "that's not in my sources" instead of guessing.
- Hardware caution: Open Notebook running alongside Ollama could slow our old demo laptop. Test it on the actual demo machine before committing to it.
- Scale caution: it appears to be built mainly for one person's research, not thousands of users. Check before describing it as production-ready.

Roadmap ideas (slides only, labeled as planned, not built for the demo): journeys for other board roles (treasurer, secretary and others); downloadable audio files of the library documents, so leaders can listen offline, for example in the car; a compliance evidence folder checked against ISACA's 11 compliance requirements; side-by-side chapter bylaws and ISACA rules (Isa flags possible conflicts and routes them to a person, never rules on them); board meeting prep from past minutes; and audio briefings. Leader-uploaded files would have to pass through the Input Compliance gate, since uploads could contain personal information or hidden instructions.

=== THE DEMO (ISACA PRESENTATION, INTERNSHIP WEEK 12) ===

A working demo of about two minutes, one reliable sequence:
1. The outgoing president asks Isa a question, gets a cited answer, and saves the decision with one tap.
2. Switch to the incoming president on day 3. Their week-one steps appear, including the contact step with three options.
3. They open the succession notebook and find the saved decision waiting.
4. Switch to a president on day 60, who sees a different next step.

Slides carry the why, the save-offer rules, the privacy rules, and the roadmap. Anything shown working must actually work. Anything planned must be labeled as planned. Record a backup video of the demo in week 11.

=== BUILD PLAN (WEEK 7 ENDED OCTOBER 16, 2026) ===

- Week 8, foundation: database tables for journey steps, leader progress, contact time slots, and notebook entries, plus synthetic presidents with different term start dates. Done when any test president can be looked up with their current day of term.
- Week 9, the journey: week-one steps, day 3 vs. day 60 view, marking steps complete, the "Get to know Isa" tour, the "Today" note, the "Meet your board" step, and the ISACA terms glossary. Done when the day 3 and day 60 presidents see different next steps and a different "Today" note.
- Week 10, the notebook: save icon, save offer, the "Read what your predecessor left you" step, the outgoing president's handoff checklist, the day 30/60/90 check-ins, and the document library with its "Listen" button. Done when a decision saved by one president shows up for the next.
- Week 11, freeze: no new features. Fix bugs, QA, rehearse, record the backup video. Done when the full sequence runs cleanly three times in a row.
- Week 12: present.

If we fall behind, cut from the bottom of this list first:
1. Day 3 vs. day 60 journey (must have)
2. Saving a decision and finding it in the notebook (must have)
3. Contact step with fake time slots
4. "Today" note
5. Handoff checklist for the outgoing president
6. ISACA terms glossary (built November 3, 2026, as "Terms explained" in the Library)
7. "Meet your board" step
8. Document library with "Listen" button
9. "Ask the outgoing president" (fallback: roadmap slide) (built November 3, 2026)
10. Smart save offer, now a quiet "worth saving?" label on the save button (fallback: the plain save icon)
11. Day 30/60/90 check-ins (fallback: roadmap slide) (planned, November 3, 2026: roadmap)
12. Full help section (fallback: a short static page)

Sprint 3, M3 decided (Matthew, November 3, 2026). Built before the presentation (M4): "Ask the outgoing president", Listen on journey steps, the Chapter Affiliation Agreement in the Library, "Terms explained", the board introduction and profile form, and notebook check-ins. On the roadmap, labelled planned, not built: the day 30/60/90 check-ins, the "Coming up" overview, progress sharing with the Chapter Experience staff view (with ISACA's on/off switch), Listen in the succession notebook, and journeys for other board roles (already planned, October 19). The sample Engage and Chapter 360 pages stay as they are (decided October 28). Designed-but-not-built pieces are listed in docs/Supplement_Items.md.

=== DECIDED ===

- Database: SQLite, the same as the rest of the team's code (decided October 16, 2026). The onboarding tables (journey steps, leader progress, contact time slots, notebook entries) go in SQLite files in the data folder, like the event log and content database.
- Test data: the team's directory/ folder (people.csv, role_assignments.csv, chapters.csv) is the one master list of test people and chapters, because it is what Isa reads to build the directory database and the sign-in accounts. Onboarding test people go into those shared files, not a separate onboarding-only file. data/synthetic keeps a matching copy; change directory/ first (decided October 16, 2026; master list moved to directory/ on October 19, 2026, after the two lists were found to disagree).
- Demo date: Isa gets a "demo date" setting on the Config page. Day of term is counted from that date instead of the real date, so the demo looks the same whatever day it is given. For the demo it is set to 3 July 2026. Day 1 is the term start date itself (decided October 16, 2026).
- Demo cast, on the demo date of 3 July 2026:
  - Sato Yumiko (P-0012, new), C-001: outgoing President (1 July 2025 to 30 June 2026), now Immediate Past President. Saves a decision to the succession notebook.
  - Watanabe Kenji (P-0001), C-001: incoming President, term started 1 July 2026, so day 3. Asia/Tokyo, matching the approved contact-step mockup. Finds Sato Yumiko's saved decision.
  - Ana Beatriz Souza (P-0013, new), C-010: President since 5 May 2026, so day 60. Sees a different next step.
  - Backup: Fahad Al-Harbi (P-0006) is also on day 3, for showing the same screens in Arabic, right to left.

- "Today" note: added as Feature 4, built by Matthew without needing approval from Willis or Eliyas. It appears inside Isa only, never as an email or notification (decided October 19, 2026).

- More onboarding pieces: handoff checklist, "Meet your board," ISACA terms glossary, and day 30/60/90 check-ins added to the plan. Journeys for other board roles are a future build for the presentation's roadmap, not built now (decided October 19, 2026).

- Document library with a "Listen" button (word-for-word read-aloud in the browser) added as Feature 5; downloadable audio files go on the roadmap slide (decided October 19, 2026).

- Journey steps: stored in onboarding/journey_steps.csv (the master copy) and built into data/onboarding.db by onboarding/build_onboarding_db.py, the same pattern as the directory. That database holds content only. What leaders do (such as marking a step complete) will go in a separate database, so rebuilding the steps never erases anyone's progress. The first 18 steps are a DRAFT, built before Matthew's review (decided October 19, 2026).

- Today window (built October 19, 2026): the chat shows a slim "Your first 90 days" strip with a Today button left of the role badge. Today slides the chat left and opens a same-size window on the right; closing it slides the chat back. Every step can be opened. Opening a step never completes it: a step is complete only when the leader clicks "Mark as done", or, on the contact step, books a call, sends the introduction or saves a group seat. The contact step's three options work with made-up data (invented contacts in onboarding/contacts.csv, a fake calendar worked out from the demo date). Introductions are saved in Isa, never emailed. Progress is kept in data/progress.db.

- Progress record and sharing with ISACA (decided October 19, 2026). Not raised with Camille in advance: Matthew wants to show it as a finished, anticipatory feature. Build in this order, checking each with Matthew before the next:
  1. The leader's own record: each finished step shows "Done on [date]" (the demo date, not the real date); "Mark as not done" adds to the history instead of erasing it; a "My progress" list covering all 90 days. BUILT October 19, 2026: My progress opens inside the Today window from a link under today's steps (Matthew chose this over a second button in the strip); each step shows a History list.
  2. The contact's view: a simple page where the Chapter Experience contact sees their chapters' new leaders (day of term, steps done, whether contact was made). Steps only, never the leader's questions to Isa. A page to open when useful, not an email for every step.
  3. ISACA's on/off switch for progress sharing ("we built it, you decide whether to use it"), plus honesty for the leader:
     - The privacy line under the contact options names exactly what the contact sees, including onboarding progress, whenever sharing is on.
     - A one-time note at the top of the Today window the first time a leader opens it (not a blocking pop-up): two sentences and a "Got it" button, e.g. "As you finish steps, your Chapter Experience contact can see your progress, so they know when to check in. Your questions to Isa stay private." Remembered per leader; shown only when sharing is switched on.
     - The full explanation goes in the "How to use Isa" help section under "What happens to your information."
  - "Done" is the leader's own word, not proof: present it to ISACA as a guide for when to check in, not a compliance record.

=== DECIDED OCTOBER 20, 2026 (PLANNING CONVERSATION) ===

Journey phases for the full 90 days:
- Week 1, Welcome: Day 1 welcome and "Get to know Isa"; Days 2-3 "Meet your Chapter Experience contact"; Days 3-5 "Read what your predecessor left you."
- Week 2, Get set up (quick wins): tasks the leader can finish alone and quickly, so they see progress right away. Includes: send an introduction email to the board (with the optional profile form), log in to ISACA's systems, check that the chapter's information is correct, and "Meet Chapter 360," a short guided tour.
- Weeks 3-4, Get set up (tasks that wait on others): bank signers, ISACA's records update, the handoff meeting with the outgoing president, and board members' profile replies arriving.
- Weeks 5-8, Run the chapter: the board meeting rhythm and how committees work.
- Weeks 9-13, Look ahead: the compliance requirements and the next dates on the annual checklist.
- Why: chapter presidents are volunteers with limited time, and volunteer onboarding is a known weak spot for ISACA, so the journey is designed for quick wins early. (Keep survey figures out of this file, per the constraints above.)
- Journey steps reviewed with Matthew and fitted to these phases (October 20, 2026). onboarding/journey_steps.csv now has 28 steps and every day from 1 to 90 has at least one. Step IDs never change once used, because saved progress refers to them; new steps start at J-19.
  - Week 1: J-01 and J-02 (day 1), J-03 (days 2-3), J-04 (days 3-5), J-05 "Meet your board" moved to days 6-7, leading into the board introduction email. Changed November 3, 2026 (Matthew: a president shouldn't wait a week to introduce themselves): J-19 "Introduce yourself to your board" moved into week 1, days 6-7, beside "Meet your board"; J-05 now points to it as "Next".
  - Week 2, quick wins: J-06 mission and history, J-19 introduce yourself to your board (moved to week 1 on November 3), J-20 log in to ISACA's chapter systems, J-21 check your chapter's leader listing, J-22 Meet Chapter 360.
  - Weeks 3-4, waits on others: J-23 handoff meeting, J-24 bank signers (at least two, three is best), J-11 volunteer list to ISACA (the records update), J-10 signed volunteer agreements, J-25 board profile replies (days 22-28, about two weeks after the email).
  - Weeks 5-8, run the chapter: J-26 board meeting rhythm, J-09 committees and meetings, J-07 bylaws and Chapter Affiliation Agreement (moved out of week 2 because it is not a quick win), J-08 strategic plan and financials, J-12 events on Engage, J-13 strategic planning session, J-14 plan the orientation.
  - Weeks 9-13, look ahead: J-15 the 60-day goals deadline, J-27 compliance requirements, J-28 the year ahead.
  - J-25 shows whatever profile replies have arrived so far and never waits for everyone, because the form is optional.
  - J-20 says: "If you can't see your chapter leader tools yet, ISACA may not have your new role on record. Your Chapter Experience contact can help!" New leaders get access after the outgoing president notifies ISACA (guidebook p. 8), so access can lag. Question for ISACA added next to question 9.
  - Demo check: on the demo date, Watanabe Kenji (day 3) sees J-03 and J-04; Ana Beatriz Souza (day 60) sees J-15, J-27 and J-17.
  - Known gap: the President's Annual Checklist puts the orientation, volunteer list, strategic planning session and signed agreements in the 1 November to 31 December window, not on days of a term. The prototype counts days from the term start date. This ties to the open fixed-date question below and to ISACA questions 1 and 2.

"Coming up" overview:
- Shows the steps scheduled for the next two weeks, in order, with their days.
- Steps the leader can do early (such as the board introduction email) have a "Start now" button.
- Steps that wait on other people (such as bank signers) show their dates but no "Start now" button.

Board introduction email and board roster (builds on piece C, "Meet your board"):
- Isa offers to draft an introduction email from the new president to the board, written the leader's way, like the contact introduction. The leader reads, edits and sends it. Isa never sends it on her own. In the prototype it is saved in Isa, never emailed.
- Scheduled as the first quick win of week 2, but the leader can start it any time from day 1 through the "Coming up" overview.
- The email links to a short, optional profile form. Each board member fills it in only if they choose. Four questions: What are you good at? What would you enjoy helping with this year? About how much time can you give each month? How do you prefer to be contacted?
- Answers fill a board roster, in layers:
  - Basic board information: name, board role, term start and end dates (ISACA already keeps this).
  - Certifications, years as an ISACA member, and years on the board: ISACA decides whether presidents may see these.
  - Specialties and interests: only what each member chooses to share in the profile form.
- Isa does not learn names or roles from conversations. The roster comes from ISACA's official records, so the promise that questions to Isa stay private holds. The prototype uses synthetic board members only.
- Built November 3, 2026 (Services/Onboarding/BoardIntroService.cs, Components/Shared/BoardProfileCard.razor, TodayPanel and ChatWidget):
  - J-19: "Write your introduction" opens a draft Isa fills in from ISACA's records (name, chapter, term start); Isa never writes it herself. The president edits and saves it; saving completes the step. It passes the sign-in check and Input Compliance (purpose=board_introduction). Saved in Isa, never emailed; the step says so.
  - Once it is saved, each current board member sees one line at the top of their chat: "[president], your new president, invited the board to fill in a short, optional profile", with "Fill it in" and "No thanks". The form (above the question box): What are you good at? What would you enjoy helping with this year? About how much time can you give each month? (Less than 2 hours, 2 to 5, 5 to 10, More than 10) How do you prefer to be contacted? (Email, Phone, A message on Engage, A chat app). All optional; the written answers pass Input Compliance (purpose=board_profile). "Only [president] sees your answers, and you can change or withdraw them any time." After answering, a quiet line: "Your board profile is shared with [president] · Change".
  - J-25 lists the board with each member's answers, or "No reply yet". Anyone who said no or withdrew also shows as "No reply yet", so the president never learns who said no, and Isa never chases anyone. "Only you see these answers."
  - In the demo only Sato Yumiko (Immediate Past President) has a sign-in account among Watanabe Kenji's board, so she is the one who answers (Matthew, November 3); the other four show "No reply yet", which fits the optional form.
  - Stored in data/progress.db (board_introductions, board_profiles), local like the rest of the journey's progress.
  - Also fixed (November 3): when the sign-in check refuses a typed note, question, introduction or profile, the message now says "You're not signed in any more, so nothing was saved. Please sign in again and try once more." It used to blame the words. NotebookService and BoardIntroService.

Chapter 360:
- "Meet Chapter 360" is a week 2 step, because it is a large resource and leaders should meet it early.
- A "Chapter 360" button sits next to the Today button at all times.
- In the prototype, the real Chapter 360 is behind ISACA's login, so the button opens a sample page or ISACA's public description of it.

Succession notebook reminders:
- Isa prompts leaders to add notes for their successor in two ways: a weekly check-in ("Anything from this week your successor should know?") and a prompt right after each board meeting.
- Each prompt has a one-line answer box, a "Nothing this week" button, an "Ask me later" button (Isa brings it back after a short delay), and a dismiss button (closes it until the next scheduled reminder).
- If the leader already saved something to the notebook that week, the weekly check-in is skipped. Changed November 3, 2026 (Matthew): instead of skipping, it asks for anything more: "You saved 2 notes this week. Anything else your successor should know?", with "That's everything" in place of "Nothing this week". Check-ins start in week 2 (day 8), so week 1 stays light. The prompt after each board meeting is not built (Isa has no meeting dates); it is in docs/Supplement_Items.md.
- Built November 3, 2026 (Services/Onboarding/NotebookReminderService.cs, Components/Shared/NotebookCheckInCard.razor): the check-in card at the top of the chat, for the chapter's current president only (the outgoing president has the handoff checklist), from day 8. A one-line box and Save (saved to the notebook as a private "Other" note through NotebookService.AddNoteAsync, so it passes the sign-in check and Input Compliance), "Nothing this week" (or "That's everything" after saving something, "Nothing to add" for longer periods), "Ask me later" (hidden until Isa is next opened; not stored) and × (closed until the next check-in). How often: "How often Isa checks in" in the notebook's ⋯ menu: every week (default), every two weeks or every four weeks; weekly in the last three months of a term whatever the setting. Periods count from day 8 of the term; an answer given at any time in the current period counts, so changing how often never asks again straight away. Stored in data/progress.db (notebook_checkins, notebook_reminder_settings). Watanabe Kenji on day 3 sees none; Ana Beatriz Souza on day 60 does.
- The leader can switch the weekly check-in to every two weeks or monthly.
- In the last few months of a term, Isa prompts a little more often (this fits the handoff checklist, piece A).
- Prompts appear inside Isa only, never by email or notification, the same rule as the Today note.

Chapter 360 audio briefings (ROADMAP ONLY, not built or played in the demo):
- ISACA staff use Open Notebook to turn the Chapter 360 documents into short audio briefings (about 5 minutes each), approve each one, and publish them in Isa.
- Leaders play them from the Chapter 360 button or the week 2 tour, with one tap and no need to look at the screen. Each briefing shows the date it was approved.

Chapter Experience staff view (expands step 2 of "Progress record and sharing with ISACA"):
- Each leader: where they are in the 90 days, steps completed, when they last used Isa, and any step they have been stuck on for a while. Used for reaching out to a leader who may be struggling.
- All leaders together: which steps many leaders get stuck on or skip, and how many finish each phase. Used to see whether the Today journey itself is working and which steps need fixing.
- Guardrails: show progress, never questions; tell leaders up front (the privacy line and one-time note); flag gently, as a friendly check-in, not an alert.
- The all-leaders view may overlap with the client health dashboard. Check with whoever builds that so the two fit together.

Presentation (15 minutes for the whole team, over Teams):
- Sort every feature into one of three places: show it live (the demo), name it on one slide, or put it in writing.
- A supplement document covering the entire Isa solution is shared at the START of the presentation so ISACA can follow along. This is a team item to raise with Willis and Eliyas.

Still open:
- Fixed-date deadlines: proposed two layers (journey steps follow the leader's own day count; a fixed-date deadline such as the November 1 compliance window appears as an alert only if it falls inside that leader's 90 days). Not decided yet.
- AGM timing: ask ISACA whether AGMs fall early in the year, before the compliance window.

Succession notebook: who can do what (decided October 20, 2026):
- Write: the chapter's president, and the outgoing president for 30 days after their term ends (the handoff window, matching the guidebook's turnover duties). So on the demo date Sato Yumiko, whose term ended 30 June 2026, can still save a decision, until 30 July 2026.
- Read: only the chapter's president by default (plus the outgoing president during the 30-day handoff window). A saved answer includes the leader's own question, so it is not shown more widely by default.
- "Share with the board": a switch on each entry. The writer chooses entry by entry; other current board members (Leadership roles) see only shared entries. Nothing is shared unless the writer says yes, the same rule as the day 30/60/90 check-ins.
- After the 30 days, a former president who is still on the board (for example Sato Yumiko as Immediate Past President until June 2027) sees only entries shared with the board, like any other board member, and can no longer share or remove their old entries. Anyone who has left the board sees nothing.
- Planned: "Ask the outgoing president". A button at the top of the notebook, and "Ask about this" on each entry. The question waits inside Isa (never emailed) until the former president opens Isa; the answer is saved as a notebook entry, so the next president has it too. Private between the two presidents unless shared. Open while the former president holds any board role; after that it becomes "Ask your Chapter Experience contact". Build after the notebook view and the save icon; cut list: just above the smart save offer, falling back to the roadmap slide.
- Built November 3, 2026 (Sprint 3 M4, Matthew's choice): "Ask the outgoing president". The president has "Ask [name]" beside "Add a note", and "Ask [name] about this" on each entry page (the question quotes the entry). The question passes the sign-in check and Input Compliance (purpose=notebook_question), then waits in the notebook; nothing is emailed. The former president sees one line at the top of their chat ("Watanabe Kenji asked you a question in the succession notebook", with "Open the notebook") and a "Waiting for your answer" card at the top of the notebook; their answer is checked the same way (purpose=notebook_answer) and saved on the same entry, once. Open while the former president is in the 30-day handoff window or holds any board role in the chapter (Sato Yumiko: Immediate Past President until June 2027). Sharing (Matthew, November 3): only the president who asked can share a question and its answer with the board; the former president answers but never shares, and after the handoff window sees only the questions asked of them, never the president's other private entries. When there is no earlier president on record or they can no longer be asked, the button is "Ask the Chapter Experience team" and opens the chat's "Talk to a person" card (Ana Beatriz Souza on the demo date). Questions are a new kind of notebook entry ("Question", with its own filter), stored in data/notebook.db with new columns added to existing files without touching saved entries. Print and email aren't offered for questions. Changes Watanabe Kenji's notebook screen in the demo: "Ask Sato Yumiko" beside "Add a note", and "Ask Sato Yumiko about this" on each entry. NotebookService (GetAskTarget, AskAsync, AnswerAsync, GetWaitingQuestions), LeaderTermService.GetPreviousPresident, NotebookPanel.razor, ChatWidget.razor.
- Typed notes pass Isa's Input Compliance check before they are saved, so a note can't become a side door around the pipeline.
- Decided and built October 22, 2026 (Matthew's approved mockup, Claude outputs/Notebook records mockups.html): the notebook opens as an index (search, filters, one line per entry by month) and each entry has its own page with its follow-ups. Every entry has a topic (fixed word rule, changeable by the writer), the date it was shared, and a record number. Decision records can be printed or saved as a PDF (one record, or all decisions), as a Board copy (shared entries only, without the leader's own question) or My copy (presidents only), and emailed (in the prototype the request is saved in Isa, nothing is sent). Other board members see shared entries without the president's question.
- Stored in its own database, data/notebook.db, so resetting journey progress (data/progress.db) never wipes the notebook, and the other way round.
- Entries are never erased: "Remove" hides an entry and records the date; the row stays, like the step history.
- Built October 20, 2026 (step 1 of the notebook: storage only): Services/Onboarding/NotebookService.cs.
- Built October 20, 2026 (step 2: the notebook panel): the notebook opens as a third panel (Components/Shared/NotebookPanel.razor) with "Add a note" (five kinds: decision, contact, what worked, unfinished, other), "Share with the board", and the entries. Typed notes go through Orchestrator.CheckTextAsync (the sign-in check and the Input Compliance gate, logged like every other check) before they are saved. - Built October 20, 2026 (step 3: the save button): "Save to notebook" under every answered question (Components/Shared/SaveToNotebook.razor), shown only to someone who can write to their chapter's notebook, never on a stopped or passed-to-a-person reply. It saves the question, Isa's answer, its sources and the date, with an optional "What did you decide?" line (checked by the sign-in check and Input Compliance, purpose=notebook_decision) and "Share with the board" (off by default). This covers demo step 1. Next: "Ask the outgoing president", then the smart save offer.

Panels (decided by Matthew, October 20, 2026):
- Chat, Today and Notebook sit side by side, left to right, a third of the screen each, so all three fill the screen. Opening a panel slides the ones to its left over.
- With two panels open, each is still a third.
- "Enlarge" on Today or the notebook widens that panel to half the screen; only one is enlarged at a time. With all three open, the other two shrink to a quarter each so everything fits; with two open, the chat keeps its third.
- Ways in: a Notebook button next to Today in the "Your first 90 days" strip, "Open the notebook" on the "Read what your predecessor left you" step (J-04), and, for someone with no strip (like the outgoing president), a notebook icon in the chat header.
- On narrow screens (phones, a future Android app) each panel fills the screen, one at a time; Close returns to the chat.
- The left rail (decided by Matthew, October 22, 2026): the panels always leave room for the rail on the left, even while it is hidden, so hiding or showing it never changes any panel's size; it just slides out of the chat's left edge. (Resizing the panels during the glide made the header shiver.) With all three panels open and the rail hidden, a 76px strip at the far left stays empty. On phones the room is left only while the rail shows.

=== MATTHEW'S TO-DO ===

- Journey steps: phases and day ranges reviewed October 20, 2026 (see "Decided October 20, 2026"). Still worth a read: the "What to do" wording of each step. Notes:
  - The guidebook has no orientation checklist for a new president. Steps J-06 to J-09 use the topics the president must cover when orienting incoming leaders (guidebook p. 13).
  - The prototype counts AGM-based deadlines from the term start date, because the test data has no AGM dates.
  After any change, run: python onboarding\build_onboarding_db.py, and commit data/onboarding.db with the CSV.
- Decided October 28, 2026 (Matthew: teammates' copies should work like his with no extra steps): data/onboarding.db is committed to Git (an exception in .gitignore). It holds content only, never anyone's progress or notes (those are data/progress.db and data/notebook.db, which stay local), and only Matthew changes the onboarding content, so there is nothing to clash with. A merge now gives everyone the same journey steps, contacts, key dates, handoff checklist and "worth saving?" tags.

=== STILL TO CONFIRM WITH WILLIS BEFORE WEEK 8 ===

- How this feature sends a question through Isa's pipeline and gets the answer back.
- Where this code lives so it merges cleanly into the team code.
- When the basic question-and-answer pipeline will be steady, since this feature depends on it.
