# Run of show: Sprint 2 presentation

Second Star Collective · Isa · 26 October 2026 · audience: i.c.stars staff

What the presentation proves: the pipeline is built, and every question runs through it end to end.

Built from the deck on main (719305c, Willis's "Update to presentation", 26 October), Matthew's script, and the talking-point sheets in Git. Willis and Eliyas have written their own scripts, which aren't in Git, so their rows below are summaries of the slides and their old sheets. Their own scripts win wherever they differ. Times are estimates; check them at the dry run.

## At a glance

| Start | Slide | Presenter | Length | Section |
|---|---|---|---|---|
| 0:00 | Opening screen, then the intro animation | Matthew | 0:05 | |
| 0:05 | 1 Meet Isa | Matthew | 0:25 | The plan |
| 0:30 | 2 Pipeline built | Matthew | 0:25 | The plan |
| 0:55 | 3 The problem | Matthew | 0:35 | The plan |
| 1:30 | 4 What Isa promises | Matthew | 0:35 | The plan |
| 2:05 | 5 One question, ten stages | Matthew | 0:50 | The plan |
| 2:55 | 6 Real versus stub | Willis | 0:45 | What we built |
| 3:40 | 7 Show the work | Willis | 0:40 | What we built |
| 4:20 | 8 Safe by design | Willis | 0:50 | What we built |
| 5:10 | 9 For the team | Willis | 0:40 | What we built |
| 5:50 | 10 Live demo | Willis | 5:00 | Demo |
| 10:50 | 11 Next steps | Eliyas | 1:30 | What comes next |
| 12:20 | 12 Additional features | Matthew | 0:45 | What comes next |
| 13:05 | 13 Closing | Matthew | 0:20 | |
| 13:25 | Questions | All | | |

Total: about 13½ minutes before questions. Matthew about 4¼ minutes, Willis about 8, Eliyas about 1½.

Hand-offs: Matthew → Willis after slide 5; Willis → Eliyas after the demo (slide 10); Eliyas → Matthew after slide 11.

---

## Before we start

- [ ] Start Docker Desktop and the sign-in server (Keycloak) on the presenting laptop, then Isa.
- [ ] Sign in as **k.watanabe** (Watanabe Kenji). The chat keeps the sign-in across the slides.
- [ ] Open **/present**. The opening screen checks that everything is running and shows **Ready** (or **Not ready**, with what's wrong). Don't start until it says Ready.
- [ ] Full screen: **F**.
- [ ] Keep test scripts and the launcher off screen.
- [ ] **Never restart Isa during the talk**: ticket reference numbers start again.
- [ ] Teams: share the right window, and check the slide text is readable on the call.

### Keys

| Key | What it does |
|---|---|
| Right arrow, Space, clicker | Next slide (on the opening screen: start the intro) |
| Left arrow | Previous slide |
| F | Full screen |
| I | Replay the intro |
| Alt+1 to Alt+5 | Type demo question 1 to 5 into Isa's chat |
| Alt+C | Show or hide the live Salesforce Cases panel |
| Alt+6 | Developer panel (testing console, trace lookup, content import) |
| Alt+7 | Content Health panel |
| Esc | Close a panel |

Arrow keys typed inside the chat box don't move the slides.

### Rules for everyone

- Say "the client", never the client's name. The slides do the same.
- No model timing (how many seconds an answer takes).
- Say "planned" for anything that isn't built.
- Don't read the slide. Say the story or the reason behind it.

---

## Slide by slide

### Opening screen and intro · Matthew · 0:05
Press next on **Ready**. Let the animation play in silence: two Cs roll in and join, the Isa logo spins, and they open onto slide 1.

### 1 · Meet Isa · Matthew · 0:25
- Welcome; who we are: Matthew, Willis, Eliyas.
- Isa helps the client's volunteer chapter leaders find answers they can trust.
- **Cue:** point to the chat bubble, bottom right: "That's Isa, live."

### 2 · Pipeline built · Matthew · 0:25
- This sprint's one goal was the pipeline.
- The plumbing metaphor: the pipes were on the workbench last sprint; now the water runs.
- Honest from the start: some stages are placeholders, and Willis will show which.

### 3 · The problem · Matthew · 0:35
- The 9 pm volunteer with an election rule question.
- **Cue:** point to the 6: "That's the whole team."
- **Cue:** point to the two panels: Isa answers what's written down, and gets the rest to a person.

### 4 · What Isa promises · Matthew · 0:35
- What each card means, not its title: nothing outside the client's documents; nothing behind a curtain; Isa knows her limits, and the leader gets a number.

### 5 · One question, ten stages · Matthew · 0:50
- Let the stages light up first.
- Follow one question through, in plain words.
- **Cue:** blue bar: the AI never wakes up. Dark blue bar: the AI pieces it together, only from what Isa found. Green bar: a person.
- **Hand-off line:** "Willis is going to show you what's actually working, and then let Isa prove it live."
- **Matthew doesn't click.** Slide 6 is Willis's.

### 6 · Real versus stub · Willis · 0:45
On screen, the ten stages coloured by status:
- **Built (5):** front end; access control; database search; complex AI; ticketing (reference number, and a Salesforce Case for a person to pick up).
- **Partly built (2):** RAG (finds passages by meaning; vector search to come); output compliance (checks the source is approved and was found).
- **Stub (3):** translation (detects the language only); input compliance (simple keyword rules); knowledge graph.

Key point: the honest scorecard. The AI model runs locally, so nothing leaves the machine.

> Correction to Willis's old sheet: it says ticketing is "partly built" and groups the stages 4 / 3 / 3. The deck now says ticketing is built: 5 / 2 / 3.

### 7 · Show the work · Willis · 0:40
- Every answer has a one-line summary, and the leader can open it to see each step.
- Plain language for volunteers; a developer view shows the raw events.

### 8 · Safe by design · Willis · 0:50
New layout on main: four failures (a failed model call, a source that wasn't found, a sensitive topic, a crashed stage), each with an arrow into one bar: **"Hand-off to a person, never a wrong answer."** Beside them, a lock box: private by default.
- Key point: when something goes wrong, Isa fails safe.
- **Don't rush this slide.** The AI model starts warming up here, ready for the demo.

### 9 · For the team · Willis · 0:40
- **Developer dashboard:** role-gated; testing console and trace lookup, and content import.
- **Content Health dashboard:** for support managers, how the approved content is holding up.
- Both open as panels over the slides: Alt+6 and Alt+7, Esc to close.

> Correction to Willis's old sheet: the dashboard no longer lists "Salesforce settings".

### 10 · Live demo · Willis · about 5:00
Signed in as k.watanabe. The demo questions are the team's own, chosen and checked in advance. A couple are meant to go wrong, to show how the system handles it.

| Step on the slide | Shortcut | Question built into the deck | What it shows |
|---|---|---|---|
| 1 A direct answer | Alt+1 | How many board meetings must a chapter hold each year? | Straight from approved text; the AI steps are skipped |
| 2 A question the AI helps with | Alt+2 | Who writes down what the board decided? | The AI path, written only from the passages found |
| 3 An off-topic question | Alt+3 | Where's a good pizza place near the conference? | The question check stops it; no ticket |
| 4 A question Isa's content doesn't cover | Alt+4 | What is the cryptocurrency policy? | Passed to a person with a reference number; Alt+C shows the Salesforce Case |
| 5 A sensitive topic | Alt+5 | A board member keeps harassing me, what should I do? | Careful wording, a safety line, a ticket |
| 6 Backstage: tracing one question | Alt+6 | (none) | Trace lookup: one question, stage by stage |

- Open "show the work" on at least one answer.
- Name each question's path (short road, AI road, to a person) to tie back to slide 5.
- **Hand-off line:** "That's what works today. Eliyas will walk you through what comes next."

### 11 · Next steps · Eliyas · 1:30
In priority order, six items:
1. **Real translation:** Meta's NLLB-200 model, self-hosted, which covers Amharic, Oromo, Somali and Tigrinya (the team's decision of 26 October).
2. **Real input rules:** rule-based checks instead of the keyword stub.
3. **Vector search and RAG:** embedding search instead of the meaning map.
4. **Knowledge graph:** related topics from linked sections.
5. **Output compliance:** the answer-check and confidence layers.
6. **Measuring quality:** accuracy sampling and a feedback loop.

Key point: every stub Willis showed has a plan.
- **Hand-off line:** to Matthew.

> Corrections to Eliyas's old sheet: "Salesforce tickets" is no longer on this slide (ticketing is built), so there are six items, not seven. And slide 12 is now Matthew's, not Eliyas's.

### 12 · Additional features · Matthew · 0:45
- "Thanks, Eliyas."
- **Left, built (8):** First 90 days, Today note, Meet your people, Succession notebook, Worth saving?, Handoff checklist, Listen Library, How to use Isa. All work in the app today, with sample data.
- **Right, planned (7):** terms explained, day 30/60/90 check-ins, ask the last leader, a support team view, more roles, phone and browser (Android app, Chrome extension), and also offline packages, a peer content panel and side-by-side model tests. None are built.

> Updated 28 October: the slide now lists everything built since the sprint began (it used to show only onboarding and the notebook, "In the build, not finished").

### 13 · Closing · Matthew · 0:20
- Let them read the line first.
- "That's sprint 2. What we set out to build is built, and it's working."
- Thanks; invite questions.

### Questions · All
- Pipeline, stages, status: Willis.
- Next steps, translation: Eliyas.
- The problem, the promises, additional features: Matthew.

---

## If we're running long

- Slide 3: skip the 9 pm story.
- Slide 5: skip the walk through the stages; keep the three bars.
- Demo: skip step 6 (Backstage), or show Alt+6 only briefly.
- Slide 12: one sentence: "Everything on the left works today; the right side is planned."

## Still to agree at the dry run

- Who holds the clicker, and how it passes at each hand-off.
- Each person's real timing, against the estimates above.
- Who answers which questions.
