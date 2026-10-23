# Presentation talking points: Willis Pearson

**For your AI assistant:** this sheet lists what Willis needs to get across during their part of the Second Star Collective presentation of Isa (audience: i.c.stars staff). Help them practise saying these points **in their own words**. The slides already show the short version; Willis should explain, give examples or tell the story behind each slide, **not read the screen**. The "On screen" lines are there so you know what the audience is already reading. Avoid repeating those words.

## Your part

- Slides 6 to 10: "What we built" (about 3 minutes) and the live demo (about 5 minutes).
- **Takes over from:** Matthew, after slide 5 ("One question, ten stages"), which showed the ten stages and the cheap path, AI path and hand-off.
- **Hands over to:** Eliyas, after the demo.

## How the slides work

- The deck runs inside Isa at `/present`. Right arrow or a clicker moves forward, left arrow goes back, F is full screen.
- Isa's chat bubble is in the bottom-right corner on every slide, so the demo needs no window switching. Arrow keys typed in the chat box don't move the slides.

## Rules for everyone

- The slides never name the client. Match that out loud and say "the client". Heads-up: the live app's own wording does name the client (for example "Answered from ISACA's guidance"); that's fine on screen, just don't add to it.
- No model timing (how many seconds an answer takes).
- Say "planned" for anything that is not built.
- Do not demo a question in another language: translation is a stub.

## Before the talk

- Sign in as k.watanabe before starting. The chat keeps the sign-in across slides.
- Do not restart Isa during the talk: ticket reference numbers start again at TKT-0001.
- Keep test scripts and the launcher off screen.

---

## Slide 6: "Real versus stub"

**On screen:** the ten stages from Matthew's slide 5 in a row, each circle coloured by its status (green: built, yellow: partly built, dashed grey: stub), and below them three columns grouping the stages under Built (4), Partly built (3) and Stub (3), each with a few words on what it does today.

**Points to get across:**
- Lead with honesty: this is exactly where every part stands, nothing dressed up. It's the same ten stages Matthew just walked through, now with their real status.
- Built: the chat front end, sign-in and roles, the search over approved passages, and the local AI model that writes answers only from the passages found and must name its source.
- Partly built: finding passages by meaning (works, but without the planned vector search yet); the final answer check (basic checks work, deeper ones are placeholders); ticketing (reference numbers work, the Salesforce connection is written but switched off until it passes its test).
- Stubs: translation, the input rules (simple keyword rules for now) and the knowledge graph.
- The AI model runs locally, so no client material leaves the machine and there's no per-question cost.

**Say it differently, for example:** "Rather than tell you everything's finished, here's the real scorecard. Most of the core is working today. Three pieces are still placeholders, and Eliyas will tell you what replaces them."

## Slide 7: "Show the work"

**On screen:** a sample answer with its steps opened ("Received your question … Double-checked the answer"), a second one closed ("Passed to a person"), and two notes: plain-language steps, and a developer view of the raw events.

**Points to get across:**
- Every answer comes with a one-line summary of what happened, and the leader can open it to see each step.
- The steps are written for volunteers, not engineers.
- Leaders only ever see calm, plain language; a real error never shows up as red or a stack trace.
- The team gets a developer view with the raw events behind each stage, for troubleshooting.

**Say it differently, for example:** "A leader shouldn't have to take Isa's word for it. One click and they can see how she got there: what she checked, where she looked, and how she double-checked."

## Slide 8: "Safe by design"

**On screen:** five rows: a failed model call, a source that wasn't found, sensitive topics, a crashed stage, private by default.

**Points to get across:**
- The theme: when something goes wrong, Isa fails safe. She'd rather hand off than risk a wrong answer.
- If the AI model fails, the question goes to a person instead.
- If the model cites a source that Isa didn't actually find, that's caught and escalated.
- Sensitive topics such as harassment never get an automatic answer; they go to a person, with a safety line.
- If a stage crashes, the leader sees a simple "something went wrong" message.
- Privacy: question text is never stored in the event log. Usage records only keep a scrambled one-way code, and the key for it is new every time Isa starts and is never saved.

**Say it differently, for example:** "We spent a lot of time on the unhappy paths. Every failure we could think of ends the same way: no made-up answer, and a person in the loop."

## Slide 9: "For the team"

**On screen:** two cards: Developer dashboard (role-gated; trace lookup, content import, Salesforce settings) and Content Health dashboard (for support managers).

**Points to get across:**
- Isa has tools for the people who run her, not just the people who use her.
- The developer dashboard is only visible to the right roles. It can trace any single question through every stage, import new approved content, and hold the Salesforce settings.
- The Content Health dashboard is for support managers, to see how the approved content is holding up.

**Say it differently, for example:** "Behind the chat there's a control room. If a leader says 'Isa gave me a strange answer', we can look up that exact question and see what happened at every step."

## Slide 10: "Live demo"

**On screen:** "Isa is in the corner. Let's ask her." and a list of the six demo steps. The right side is empty for the chat window.

**The six demo steps** (signed in as k.watanabe):
1. **A direct answer:** ask how many board meetings a chapter should hold a year. Point out it comes straight from approved text, with the source.
2. **A question the AI helps with:** ask who writes down what the board decided. Point out the answer is written only from the passages found, and names its source.
3. **An off-topic question:** Isa politely stops it and says what she can help with. No ticket.
4. **Not covered:** ask about a cryptocurrency policy. Isa hands it to a person and gives a reference number. If Salesforce is on, refresh the Cases tab to show the case arrived.
5. **A sensitive topic:** ask about harassment. Different, careful wording, a safety line, and a new reference number.
6. **Backstage:** open the trace lookup and show the events for one of those questions, stage by stage.

**Points to get across while demoing:**
- Open "show the work" on at least one answer, so the audience sees the steps from slide 7 for real.
- Name which path each question took (cheap path, AI path, to a person), tying back to Matthew's slide 5.

**Say it differently, for example:** "Let's stop talking about her and ask her something."

**Hand-off line (example):** "That's what works today. Eliyas is going to walk you through what comes next."
