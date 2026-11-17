# Sprint 3 presentation outline

November 9, 2026 · Matthew Brady, written with Claude · replaces the November 6 version (it is in Git history)

**What this is:** the outline for the final presentation to ISACA: 15 minutes for the whole team, over Microsoft Teams, in internship week 12. It is a story with a clear progression, not a scorecard: the problem, what we promised, what Isa does, how she runs, what she does for a new chapter president, what she gives ISACA behind the scenes, and the promises kept.

**Team only until the presentation:** it names the onboarding feature, which is a surprise for ISACA.

**The team:** Willis Pearson, Eliyas Eshta and Matthew Brady. Willis and Matthew share the quality, risk and success-metrics work.

**The slides:** presentation/slides.json (open /present), 19 slides, rebuilt from this outline on November 9. No slide repeats what a live demo shows: Show the work is opened live in the Isa demo, the journey features are shown live in the onboarding demo (the Library with Listen has its own step, and Terms explained is shown as a hover; the planned items are on "After the pilot"), and behind the scenes is one live slide.

## Decided (Matthew, November 9, 2026)

1. **Onboarding gets about 4 minutes.** Proposed to the team, not yet agreed.
2. **No tracker on every slide.** September's promises open the talk and come back at the end with a status beside each one.
3. **ISACA's survey figures on onboarding are shown on the slide, but never committed:** the repository is public and the survey is confidential. Slide 12 has [figure] placeholders; Matthew types the figures in locally on presentation day (the survey is in private/, page 19).
4. **Speed is measured on the demo machine**, with the Dev Dashboard's Front end tab.
5. **Dashboards are shown live**, real parts only.
6. **One sign-in for the whole demo:** Watanabe Kenji, day 3 of his term (the demo date, July 3, 2026). No switching users. His account also opens the Dev Dashboard.
7. **A one-step setup script** prepares the demo: a fresh progress file, Sato Yumiko's decision saved in the notebook, Kenji's board introduction saved, and Sato's profile reply.
8. **The role filter**, in these words: "Rules can already target a role. The content filter is designed, and isn't needed until ISACA adds restricted content, because today every document is meant for all chapter leaders."
9. **The Library and Listen are shown live** (Matthew, November 16, 2026), as their own step in Kenji's demo, between the calendar and the notebook, so the demo follows the four struggles in order. This replaces "mention it in one sentence; don't play audio over Teams". ISACA hears the voice: Teams is shared with "Include computer sound" on (decided November 16, after Matthew and Willis tested it with Antonio). With Meet your board as its own, fuller step (the same day), the demo runs about 4 minutes and the onboarding section about 5 and a half; the extra time is not yet agreed with the team.

## The story in one breath

"Every year, chapter leaders change, and what the last president knew leaves with them. In September we promised an assistant that answers only from ISACA's approved content, shows its work, and hands off to a person when it should. Here she is: how she answers, how she runs, what she does for a new president in their first 90 days, and what she tells ISACA about what its leaders need."

## Time plan

| # | Section | Minutes | Suggested speaker |
|---|---|---|---|
| 1 | The problem | 1:00 | Matthew |
| 2 | What we promised | 0:45 | Willis |
| 3 | Meet Isa (live) | 3:00 | Willis, with Eliyas |
| 4 | How she runs | 1:00 | Eliyas |
| 5 | A new president's first 90 days (live) | 4:00 | Matthew |
| 6 | Behind the scenes: Isa grows with ISACA (live) | 2:30 | Willis, with Matthew on the rules |
| 7 | Promises kept, and the ask | 1:30 | Matthew, then Willis for the ask |
| | Switching screens and slack | 1:15 | |

The team decides the speakers. Willis shares the supplement document through the Teams chat (decided November 16). The plan has been to post it at the start, so ISACA can follow along; the exact moment is still to confirm.

---

## 1. The problem (1 slide, 1 minute)

Short and familiar. Three lines, ending on the handover, because the rest of the talk answers it:
- Questions that already have written answers take up volunteer and staff time.
- What leaders need is spread across many documents and tools.
- Every year the leaders change, and what the last president knew leaves with them.

Say: "You told us this in September. Here's what we did about it."

## 2. What we promised (1 slide, 45 seconds)

**September's diagram** (Sprint 1, slides 27 to 31), redrawn in the deck's style: the chapter leader, Isa and ISACA-approved content across the top, then the eight parts we promised, in September's rows: sign in (your ISACA account, by role), translation (any language), Check 1 (reads the question), Check 2 (reads the answer), your team gets a ticket, 1 · search first, 2 · AI if search isn't enough (role filter, RAG, logs), and contact the team (always on screen, one click from any answer, the question and history go with it). One line under it names Camille's three outcomes.

Say: "You've seen this. Hold us to it: it comes back at the end."

## 3. Meet Isa (live, 3 minutes)

One question, start to finish, then two more that end differently. Kenji is signed in; Isa is in the corner of the page.

| Question | What it shows |
|---|---|
| "How many board meetings must a chapter hold each year?" | A direct answer in ISACA's own words, with the page. Open **Show the work**: every step it went through. |
| "What insurance must we have?" | An answer, then a pointer to a person (a rule ISACA owns), with "Pass this to a person". |
| "What is the cryptocurrency policy?" | Nothing approved covers it, so Isa says so and passes it to a person with a reference number. |

Optional, if time allows: "A board member keeps harassing me, what should I do?" (handed to a person straight away, with the safety line).

These are the team's settled demo questions; rehearse them on the demo machine. Keep Spanish and Amharic out of the live demo unless the translation warm-up is fixed (M6 findings).

Say: "Every answer can be traced to an approved page. When she can't answer, she says so, and she gets you to a person."

## 4. How she runs (1 slide, 1 minute)

Numbers, not adjectives. Recount every number on the demo machine the day before.

| Number | What it means |
|---|---|
| 131 passages | 81 from ISACA's approved documents, searched first and by meaning; 50 from the nine governance guides the team wrote, searched last. |
| 114 of 114 | Rule checks passing: every rule's own tests and the rehearsed questions (corrected November 16; 61 was the November 2 figure) |
| 595 of 603 | Test questions the team wrote that followed their expected route (the November 12 lock run). The old "78, 5 of 5" had no record behind it. |
| [measure] | Typical wait: answer found in ISACA's text, and answer written by the AI model (Front end tab, on the demo machine) |
| About 200 | Languages the translation model can handle; Spanish and Amharic connected today |
| $0 per question | Runs on the team's own computers; nothing is sent to a paid service |

Say: "Search first. The AI model only writes when search alone can't, and only from the passages found, and then it's checked sentence by sentence."

## 5. A new president's first 90 days (live, 4 minutes)

This is where we go beyond the RFP, and it is the section to deliver with the most care. Don't present a list of features. Tell a story about one person.

**Open (on a slide, 20 seconds):** ISACA's own survey finding on onboarding (figures typed in locally on the day; see decision 3). Then: "So we asked: what does a new president actually struggle with?"

**The four struggles** (one slide, shown before the demo and left up in the corner of everyone's mind):

| What new leaders struggle with | What Isa does |
|---|---|
| They don't know who to call, or when | Their Chapter Experience contact with three ways to reach them, and their board, one page per person |
| Deadlines are hidden | A Today note, ISACA's key dates, and a calendar they can add to their own |
| Resources are spread out | ISACA's documents in one Library, read aloud word for word, and ISACA's definitions on hover |
| What the last president knew leaves with them | A succession notebook that passes from president to president |

**The demo, through Kenji's eyes** (about 4 minutes):

"It's July 3. Kenji Watanabe has been president of the Sakura Bay Chapter for three days."

1. **He opens Isa.** The Today note: day 3, today's steps, and the "Last year" row quoting what Sato Yumiko, the president before him, wrote at this time last year. Say: "The handover starts in the first ten seconds."
2. **Who to call.** The contact step: book a call, send an introduction, or join a group welcome.
3. **Meet your board** (its own step; Matthew, November 16). The board list: names, roles, term dates, and each person's time zone with the time there now. Then two pages: Takahashi Mei, Vice President (certifications, member since, and the role in ISACA's words) and Sato Yumiko (the profile answers she chose to share, and how she prefers to be reached). Sato's page has no role description: ISACA's documents define only the President, Vice President, Secretary and Treasurer.
4. **Hidden deadlines.** The calendar: November's compliance window, shaded. Day 60 is said, not shown (Matthew, November 16): "By day 60, Kenji will see new steps: Isa will be walking him through the compliance requirements."
5. **Resources spread out.** The Library: ISACA's documents in one place. Press Listen on the President Guidebook for one sentence: read aloud word for word, nothing summarized.
6. **What the last president knew.** He opens the notebook. The decision Sato saved before her term ended is waiting, with its source. "Ask Sato Yumiko about this."
7. **The loop closes.** Kenji asks a question; the save button says "Deadline · worth saving?"; one tap saves it. Say: "And now Kenji is leaving something for whoever comes after him."

On the way, hover over one term (for example "Engage") to show ISACA's definition.

**Close (one slide, 20 seconds), in the RFP's terms:**
- Fewer repeat questions from new leaders ("Automate").
- It supports the Chapter Experience team; it never replaces them. Isa connects leaders to people.
- Knowledge survives the yearly handover.
- Everything a leader sees still goes through Isa's checks. Nothing here is a side door.

Everything else (the handoff checklist, notebook check-ins, "Ask the outgoing president", the board introduction and profile form, the glossary of 26 terms) is in the supplement document.

## 6. Behind the scenes: Isa grows with ISACA (live, 2.5 minutes)

One slide ("Live: your team's view"), whose three steps are the three points. Same sign-in, open the Dev Dashboard. Say: "This is what your team would see." Three short points, one live moment each. **Check every one on screen first:** Willis's November 9 merge rebuilt these tabs, and they haven't been opened in the browser yet.

1. **ISACA owns the rules.** The rule tester: type a question and see which rule fires and why. Rules live in files ISACA approves, with versions; a lower layer can only tighten; no developer needed. Every settings change is in the change log (who, when, old and new value).
2. **Your team tunes her.** The four sliders (answer confidence, sentence support, strong match, search pass mark) apply to the next question without a restart. Health alerts show every service and warn when the AI model or Salesforce keeps failing.
3. **ISACA learns what its leaders need.** Content Health: questions grouped by the passage they matched, hand-offs by chapter and region, time to resolution from Salesforce, and which documents are due for review. Counted by topic, never by person, and no question text is stored.

Say: "Isa doesn't just answer leaders. She tells you what they're asking, where your content has gaps, and where your team is needed."

## 7. Promises kept, and the ask (2 slides, 1.5 minutes)

**Slide: the same diagram, now with a status on every part** (Willis and Eliyas: please confirm):

| Part | Status |
|---|---|
| Sign in | Built (test accounts, not ISACA's) |
| Translation | Reworded: one model for about 200 languages; two connected today |
| Check 1 | Built: rules files your team owns and approves |
| Check 2 | Built: every AI sentence checked, then ISACA's words, then a person |
| Your team gets a ticket | Built: reference number and Salesforce case |
| 1 · Search | Built |
| 2 · AI | Partly: RAG and logs built; "Rules can already target a role. The content filter is designed, and isn't needed until ISACA adds restricted content, because today every document is meant for all chapter leaders." |
| Contact the team | Built |

The risk controls and the cost promises from September aren't on this slide; they're in the supplement (docs/Promise_Inventory.md).

**Slide: the ask, the same as September.** "A pilot: a small set of chapters, a defined window, your Chapter Experience team as the judges." Recommended group: new chapter presidents, who change every year, need the most help early, and are easy to measure: time to first contact with the Chapter Experience team, repeat questions in the first 90 days, steps where leaders get stuck, hand-offs by topic.

**The interface, said as a plan, not an apology** (Matthew, November 16): "A pilot is also where the interface gets tuned: we'd watch real chapter leaders use Isa and shape her screens around what they reach for." Matthew says it at the end of the promises slide, leading into the ask. The demo itself shows two panels at a time and a filtered notebook list, so the screen is easier to read over Teams.

**Roadmap** (one line on the ask slide, or in the supplement; labelled planned): the role-based content filter; the Chapter Experience staff view with ISACA's on/off switch; day 30, 60 and 90 check-ins; journeys for other board roles; Listen in the notebook; board messaging stays in Engage; an Android app and a Chrome extension.

---

## Before the slides are rebuilt

1. **The team agrees** to this order and the time plan (decision 1), and Willis and Eliyas check every line about their parts.
2. **The survey figures:** type them into slide 12 locally on presentation day, from private/ (page 19), and don't commit them.
3. **The demo machine:** decide which one, set up everything it needs (the full content with 131 passages, search vectors, the HHEM model files, translation), then measure speed there (decision 4).
4. **Check Willis's new dashboard tabs on screen:** Front end, Health alerts, Content Health, the rule list and the change log (decision 5).
5. **Write the setup script** (decision 7) and rehearse from it three times in a row.
6. **Fix "[NUMBER] hours"** wherever Sprint 1 wording is reused, and take A'keyrah off the team slide.
7. **Record the backup video** this week (week 11).
