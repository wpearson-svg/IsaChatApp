# Isa Project Context (from planning conversations)

Written October 20, 2026, from Matthew's planning conversations in the Claude chat app. This file covers Isa work that is NOT already in ONBOARDING-FEATURE.md: questions for ISACA, the presentation plan, the supplement document, and what new chapter leaders struggle with most. The onboarding feature itself is defined in ONBOARDING-FEATURE.md.

Rules that still apply: never commit or push to Git (Matthew handles Git himself), and keep ISACA survey figures and study details out of code, comments, and sample data.

=== GROUND RULE: WHAT ISA CAN DO (CONFIRMED BY MATTHEW, OCTOBER 20, 2026) ===

Never design anything that assumes Isa can do more than this:
- Isa knows only what is in her approved documents (the content database and RAG). No outside AI service, no internet, no general knowledge.
- The AI model, when it is added, only turns the passages Isa found into a readable answer. It cannot add anything those passages don't say.
- A question the documents don't cover goes to a person. Isa doesn't try to answer it.
- So the questions that work are mostly known in advance: each one maps to a section of an approved document. Design with that in mind: preset question buttons with known, cited answers (for example "Ask Isa about this step"), fixed rules instead of AI judgment (for example the save offer keys off tagged document sections), and features like the succession notebook that need no AI at all.
- This is a strength to present to ISACA: every answer can be traced to an approved page.

=== QUESTIONS FOR ISACA ===

No questions have been sent to ISACA yet. Final list of 11 questions, decided with Matthew on October 21, 2026 (it replaces the earlier 16). Matthew picks which ones go in each weekly Wednesday update; when an email is sent, note here which questions went out. The wording never describes the onboarding feature, which is a surprise for the presentation.

Chapter calendar and deadlines
1. To make sure Isa gives accurate answers about the chapter year and its deadlines: when do most chapters hold their Annual General Meeting, and when do newly elected officers usually take office? Is there a common pattern, or does it vary a lot by chapter or region?
2. Is it common for a newly elected president to take office shortly before the November 1 to December 31 compliance window, or do most have several months in office before it?
3. How often does a president take office partway through a term, for example after a resignation?

Documents
4. When it's convenient, and there's no rush: could you share the guidebooks for the other board roles (Vice President, Secretary, Treasurer, Membership Director, and Immediate Past President)? They would help Isa answer questions from leaders in those roles, too. (Low priority.)
5. Could you share the Chapter Affiliation Agreement itself?

New leaders
6. Does each chapter have an assigned Chapter Experience contact?
7. After a chapter elects a new president, how soon does ISACA's record show the change, and when does the new president get access to the Chapter Leader Portal and their chapter's Engage site? (Why it matters: Isa sets what each person can see from the role ISACA has on record, so a delay means a new president is treated as a member until the record changes.)
8. What do you see as the most important things a new chapter president should get done early in their term?

Board information
9. What information does ISACA keep about each chapter's board, such as names, roles, and term dates?
10. Which information about board members, such as their certifications or years of membership, would a chapter president normally be allowed to see?

Scope
11. Should Isa also draw on the outside governance sources Camille mentioned (ASAE, BoardSource, BoardEffect, and Association Forum), or stay with ISACA's own materials?

Sent: all 11 questions went out in the weekly update of October 21, 2026 (to Camille, Michelle Phillips, Joshua Belz and James Ross). Camille answered inline the same day; her answers are below.

Camille's answers (October 21, 2026):
1. AGMs: no set timeframe; it varies greatly by chapter and region and is set by each chapter's bylaws.
2. Taking office: chapters are independently affiliated, so ISACA's compliance season doesn't set when officers take office; the chapter's governing documents do. Most chapters open voting before their AGM and announce or ratify the results at the AGM.
3. Mid-term presidents: rare. Only when a president resigns or can no longer serve (for example illness, death, or board action to remove them); the process is in the chapter's bylaws and varies.
4. Other role guidebooks: already in Dropbox (uploaded one to two weeks before October 21); for roles with no guidebook, Camille pasted the role descriptions into Word documents there. Check the team's Dropbox.
5. Chapter Affiliation Agreement: attached to her reply. Once saved into the project, the Library entry can change from "Requested from ISACA" to a real document.
6. Chapter Experience contacts: NOT assigned to every chapter. About 65 chapters have an assigned Chapter Success Manager (CSM), as the RFP notes; the other 165 (as of October 21) are supported by the Chapter Experience Operations Team as needed. Never put why a chapter has a CSM (its selection profile) into Isa, her content or her sample data.
7. Roster changes: the chapter submits an updated leader roster through ISACA's support ticketing system, and the ISACA Customer Support Center makes the change, typically within 10 business days. (This supports the J-20 note that access can lag.)
8. What a new president should do early: "Please reference the guidebook."
9. Board information: ISACA keeps names, roles and term dates, and every board member is an ISACA member with a full member account and membership history.
10. What a president may see: everything on the member roster. A member of Camille's team will send a sample header of that report separately.
11. Outside sources: for ISACA process and policy, use ISACA's documentation only. For broader governance questions (for example, what to cover in a new board member's orientation), drawing on established best practice from ASAE, BoardSource, BoardEffect and Association Forum is fine, even ideal.

What this means for the design (for Matthew to decide; nothing changed yet):
- The "Meet your Chapter Experience contact" step assumes every chapter has its own contact. Most don't. The step may need two versions: the chapter's Chapter Success Manager where one is assigned, and the Chapter Experience Operations Team for everyone else. Isa must never say why a chapter has a CSM.
- Outside governance sources would have to become approved documents in Isa's content before she could answer from them (the ground rule above still holds: approved documents only). A decision for the whole team.
- AGM and term dates come from each chapter's bylaws, so the open fixed-date question stays open; the prototype keeps counting from each leader's term start date.

Dropped on October 21, 2026, and why:
- President Guidebook as a Word document; the outside AI services question; made-up restricted content; approved wording for the local-law and emergency lines: Matthew's choice not to ask.
- The Chapter 360 asterisks: the team will try to answer it from the material it has.
- Differing member and chapter numbers: this is a mock scenario, so exact numbers don't matter; Isa uses the numbers in the documents the team has.

Decided instead of asked (Matthew, October 21, 2026; his position for Willis's review of docs/Handoff_and_Refusal_Rules_Proposal.md):
- Every question Isa passes to a person goes to the Chapter Experience team.
- Suspected fraud also goes to the Chapter Experience team, with a special "potential fraud" flag. The flag is used very sparingly; when it is set needs a strict, fixed rule, still to be worked out.

=== WHAT NEW CHAPTER LEADERS STRUGGLE WITH ===

Matthew's top four, in order:
1. They don't know who to call, or when.
2. Resources are spread out. Isa should work with Chapter 360 and introduce the leader to it.
3. They feel alone in the role.
4. Deadlines are hidden.

Full list from the brainstorm, with where each idea comes from and what Isa offers right away:

| Pitfall | Where it comes from | What to offer right away |
|---|---|---|
| They don't know who to call, or when | Highland study | A "who to call" card on day 1: their Chapter Experience contact, plus ISACA Support through the "Chapter" tab |
| Resources are spread across many places, and leaders struggle to find things like Chapter 360 | Highland study; the 2026 survey | Isa as the one place to start, using the systems overview to answer "where do I do this?" |
| What the last president knew leaves with them | Team reasoning (the reason behind the succession notebook) | The predecessor's notes in days 3-5 |
| Deadlines are hidden, like the compliance window and dates counted from the AGM | Compliance framework, President Guidebook | A "what's due soon" view based on the leader's own dates |
| They're volunteers with limited time | Team reasoning | A light day 1, quick wins in week 2, and steps short enough to finish in one sitting |
| The documents can be confusing: TBD entries, and numbers that differ between documents | Content health flags the team found | Isa gives one clear answer with its source and date |
| They feel alone in the role | The 2026 survey (best-practice sharing was a top request, and leaders prefer human contact) | The group welcome option, plus examples of other chapters' work |
| Some problems need a person, like board conflict or local laws | Highland study | Isa says clearly when a question goes to a human, and sends the leader to the right one |

Still a best guess: what new leaders actually struggle with is worth confirming with real chapter leaders. The presentation can name these pitfalls and say the team would confirm them.

=== THE PRESENTATION ===

- The team has 15 minutes to present the finished product to ISACA over Microsoft Teams.
- Not everything designed needs to be shown. Sort every feature into one of three places:
  - Show it live: a short demo of the moments that tell the story (the two-minute demo sequence in ONBOARDING-FEATURE.md).
  - Name it on one slide: a single "What's built into the journey" slide, mentioned in a sentence or two (for example the "Coming up" overview, board introduction and roster, Chapter 360 button, notebook reminders, quick wins in week 2).
  - Put it in writing: the supplement document below, including roadmap items like audio briefings and the calendar.
- The onboarding feature should fit in about three minutes: the two-minute demo plus about one minute for the slide.
- Still open: how much of the 15 minutes the team gives the onboarding feature compared with the rest of Isa.
- A presentation note for the setup phase (no survey figures in this file): "We designed the setup phase around quick wins. In week two, new presidents complete tasks they can finish on their own, so they see progress right away. Tasks that depend on other people, like bank signers and records updates, get weeks three and four. We built it this way because volunteer leader onboarding is the area ISACA's own research rated weakest." Matthew should check the exact survey figure against the original document before presenting it.

=== THE SUPPLEMENT DOCUMENT ===

Matthew's idea: a detailed, attractive "pitch" document that supplements the presentation. He doesn't expect other teams to provide one, and sees it as a way to set the team apart and help sway ISACA's vote.

How it is delivered
- Shared at the START of the Teams presentation, dropped into the meeting chat, so ISACA can follow along.
- Sent as a PDF so it looks the same on every computer. A Word version can be offered if ISACA wants to comment.
- Check with i.c.stars program staff whether final deliverables go directly to ISACA or through i.c.stars first.

What it covers: the ENTIRE Isa solution, not just onboarding. This is a team document, so Willis and Eliyas need to supply the facts about their parts and agree on how it is presented.

Proposed outline (follows the same order as the slides):
1. Cover: Isa, the team's name, the date.
2. At a glance: one page on what Isa is, who she helps, and why she's trustworthy.
3. The problem: what ISACA's own research found.
4. Meet Isa: what a chapter leader experiences, in the same order as the demo.
5. Built for trust: governance, citations, compliance gates, privacy rules, human approval of content, and the AI Use Register.
6. New leader onboarding: the 90-day journey, the succession notebook, the board roster, Chapter 360, and the Chapter Experience staff view.
7. Client health dashboard: what ISACA staff can see about how chapters are doing.
8. DevOps dashboard and tickets: how problems get caught, logged, and fixed.
9. Tech stack and why: Ollama running locally, and the reasons for each choice.
10. How Isa grows with ISACA: how staff add and update content, and how she scales to thousands of leaders.
11. Roadmap: what comes next, like audio briefings and calendar connection.
12. The team and next steps.

What makes it attractive and convincing
- ISACA's own brand colors and fonts.
- A one-page "At a glance" summary right after the cover.
- Sections that match the slides, each opening with a short headline and one visual.
- Visuals instead of paragraphs: a timeline of the 90-day journey, the approved Chapter Experience contact mockup, and a diagram of the succession notebook passing from one president to the next.
- Generous white space and short text, so nothing looks crowded on a laptop screen during a Teams call.
- Every feature labeled as built, designed, or planned. James warned about promising features that don't exist yet, and an audience of auditors will trust honesty about status more than overselling.

Timing: build it close to the presentation, once the journey steps and the demo are settled, so every page matches what the team actually shows.

=== TEAM REVIEW ITEMS ADDED OCTOBER 20, 2026 ===

Matthew keeps a running list of items to bring to the whole team at once. Two were added today:
- The supplement document described above.
- The Chapter Experience staff view of onboarding progress (described in ONBOARDING-FEATURE.md), which may overlap with the client health dashboard.

=== "SHOW THE WORK" DESIGN (DECIDED OCTOBER 20, 2026) ===

Willis's request. A first version is built (see CHANGES-SINCE-LAST-COMMIT.md). The "Android mockup" Willis mentioned is not a drawing: it is a future idea to turn Isa into an Android app and a Chrome extension (roadmap only, labeled as planned). So the design is drawn for narrow screens first (360px, a common Android width), with rows tall enough to tap (at least 44px), so it works in the web chat, a phone app and an extension side panel alike.
Mockup approved by Matthew on October 20, 2026, and built: Claude outputs/Show the work mockups.html (six panels: working, answered closed, answered opened, outside what Isa covers, passed to a person, something went wrong in leader and developer views).

Colors, each with its own icon (color alone never carries the meaning):
- Green: the step passed.
- Blue: the step did not pass but was handled. This covers a stop (for example an out-of-scope question) and a hand-off to a person; the two get different icons.
- Gray: the step was skipped. Developer mode only (decided October 20, 2026).
- Leaders see only the steps that ran. A component Isa didn't use is never mentioned, including the AI model: "Skipped the AI model" could make a leader think a "better" answer was possible. When ISACA's own text answers the question, Isa says so as a strength: "Found the answer in ISACA's approved text" and "Answer uses ISACA's exact wording". Steps that ran with nothing to work with are hidden from leaders too.
- A leader never sees red. A real error (a stage that crashed) shows to the leader in blue, as handled: "Something went wrong on our side. Try again, or talk to a person."
- Red appears only in developer mode, for real errors, together with the raw error (for example error=TimeoutException). The event log records every error either way.
- Use darker shades of green and gray than the brand swatches, so small icons and text stay readable (text at least 4.5:1 on white), including over a Teams screen share.

To design from the mockup: the collapsed line, the live status line for the current stage, and how the sources are shown with the answer.

=== GREETING IN THE LEADER'S LANGUAGE (DECIDED OCTOBER 20, 2026) ===

- Only the word "Welcome" on the landing page is shown in the leader's language of preference (Services/WelcomeWords.cs), to show ISACA that Isa can greet each person in their own language. Names stay exactly as in the directory.
- Everything else stays in English for now (Matthew: "leave the rest of it off the table"). Isa's answers stay in English because they come word for word from ISACA's English documents.
- Have a native speaker check the words before showing them to ISACA.

=== WHAT THE RFP MEANS FOR ISA'S DESIGN (OCTOBER 21, 2026) ===

The updated RFP is saved in docs/ISACA_RFP_AI_Chapter_Support_Agent.md.

- Scope: the RFP speaks of "chapter leaders" throughout, never only presidents. So Isa as a whole serves all chapter leaders; the onboarding feature's focus on presidents is one well-chosen use case inside it (a pilot normally starts with one group). Confirm the team's scope with Willis.
- It confirms the design: approved content only (the ground rule above); staff kept for "judgment, interpretation, mediation, escalation, or exception handling" (almost exactly the hand-off proposal's topic groups); source transparency and "see uncertainty" (source cards, "show the work", the "not found" state); conflicting and current content (content health flags, approval dates); no premature technology lock-in and commercial clarity (local Ollama, SQLite, no paid services); language support "if required later" (the welcome word only); accessibility (reduced motion, contrast, 44px tap targets, screen-reader labels).
- "Direct users to human support ONLY when questions are out of scope": a warning against handing off too much. Supports the hand-off proposal's "answer, then point to a person" outcome and "not blocking too much". Quote it to Willis.
- The AI-use register is required and covers coding assistants: Matthew's work with Claude Code (code, documents, mockups, wording) must be entered: tool, provider and model, purpose, information submitted, the resulting artifact, and whether a person reviewed it. Matthew reviewed and approved each step, which is a strength to record.
- Phases with approval gates (discovery, knowledge and governance design, prototype, testing and pilot, evaluation): the team's work is phase 3, the prototype. Frame the presentation and supplement document in those phases ("here is our prototype; here is what a pilot would measure").
- Measurement for a go/no-go decision: a short "what a pilot would measure" slide (adoption, usefulness, answer quality, hand-offs by topic, knowledge currency) scores directly. Pieces already exist: hand-offs and save offers counted by category, the planned Chapter Experience staff view, the Content Health Dashboard.
- Onboarding is not in the RFP. That is fine as a surprise, but present it in the RFP's terms: it reduces repetitive questions from new leaders ("Automate"), supports the Chapter Experience team rather than replacing it, and keeps knowledge through the yearly handover.
- The onboarding feature is a SURPRISE for the presentation: never describe it to ISACA beforehand, in questions or updates.
