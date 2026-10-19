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
2. Question type label. The Input Compliance gate attaches one label to each question: "decision," "deadline," "how-to," or "general information." A small Ollama model can return this one word. Offer only for "decision" and "deadline."
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

=== FEATURE 4: THE "TODAY" NOTE (ADDED OCTOBER 19, 2026) ===

- When a leader opens Isa, she greets them with a short "Today" note at the top of the chat. Example: "You're on day 12. Coming up this week: book your welcome call. The compliance window opens in 19 days."
- The note is built from data Isa already has: the leader's day of term, their next journey steps, and dated items from ISACA-approved content (the President Guidebook's annual checklist and the compliance timeline).
- It only appears inside Isa when the leader opens it. Nothing is sent by email or notification while they are away, because ISACA has said new tools should support its staff, not replace email or the help desk.
- The dates and counts in the note come from the database, not from the AI model, so they are always exact. The day of term uses the demo date setting.
- Everything in the note follows the same rules as the rest of the feature: approved content only, no side door around Isa's pipeline, and any question about an item goes through the normal question-and-answer pipeline with citations.
- Not part of this feature: a full calendar where leaders add or remove their own deliverables and keep private notes. That goes on the roadmap slide, labeled as planned. If it is built later, ISACA's required items stay locked so they cannot be removed.

=== MORE ONBOARDING PIECES (ADDED OCTOBER 19, 2026) ===

Five more pieces, all built from data and content the project already has. All of them follow the same rules as the rest of the feature: approved content only, no side door around Isa's pipeline, invented sample people only.

A. Handoff checklist for the outgoing president
- In the last 30 days of a term, Isa prompts the outgoing president to fill in the succession notebook: open items, key contacts, what worked, what's unfinished.
- The prompts appear in the outgoing president's "Today" note and as journey steps.
- This closes the loop in the story: one leader leaves something behind, the next one finds it.

B. Journeys for other board roles (FUTURE BUILD, not built for the demo)
- Treasurers, secretaries and other board roles would get their own short journeys, built from the guidebook sections on their duties.
- They would use the same journey tables and screens as the president journey; only the steps differ.
- Decided October 19, 2026: not built now. It goes in the presentation as a future build, labeled as planned. The demo shows the president journey only.

C. "Meet your board" step
- Shows the new leader who else is on their chapter board, from the team's directory data.
- Names, roles and time zones only. No emails or other contact details on this screen.

D. ISACA terms explained
- A short glossary of terms new leaders meet early (for example AGM, CPE, Engage), loaded into Isa's knowledge base so leaders can ask "What's an AGM?" and get a cited answer.
- Definitions come from ISACA-approved documents. Any term the documents don't define is left out until ISACA approves wording for it.

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
6. ISACA terms glossary
7. "Meet your board" step
8. Document library with "Listen" button
9. Smart save offer (fallback: the plain save icon)
10. Day 30/60/90 check-ins (fallback: roadmap slide)
11. Full help section (fallback: a short static page)

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

=== MATTHEW'S TO-DO ===

- Review the draft journey steps in onboarding/journey_steps.csv: titles, "What to do" wording, and day ranges. Only the 60-day deadline comes from the guidebook; the other day ranges are suggestions. Things to look at:
  - Days 6-7 and days 61-89 have no steps. Fine as quiet days, or add something?
  - The guidebook has no orientation checklist for a new president. Steps J-06 to J-09 use the topics the president must cover when orienting incoming leaders (guidebook p. 13).
  - The prototype counts AGM-based deadlines from the term start date, because the test data has no AGM dates.
  After any change, run: python onboarding\build_onboarding_db.py

=== STILL TO CONFIRM WITH WILLIS BEFORE WEEK 8 ===

- How this feature sends a question through Isa's pipeline and gets the answer back.
- Where this code lives so it merges cleanly into the team code.
- When the basic question-and-answer pipeline will be steady, since this feature depends on it.
