# Paths to a person: design

November 2, 2026 · Matthew Brady, written with Claude · from the promise inventory, gap 3 (rows S1-18 and S1-7)

**BUILT November 2, 2026** (Matthew: "Just build everything"): the "Talk to a person" and "Not quite" doors. The "answer, then point" door waits for the rule engine. Wording approved by Matthew on November 2 (Services/ShowTheWork/Wording.cs).

What was built:

- **Services/PersonRequestService.cs:** a hand-off the leader asks for. It runs the pipeline's own sign-in check and Input Compliance on the note (Orchestrator.CheckTextAsync, purpose=handoff_note), then the pipeline's own TicketingStage, unchanged (reference number; Salesforce Case when switched on, with escalated_by=leader_request and the door as the reason). It logs the door and counts, never the text. It stores only what the leader ticked in data/person_requests.db (local, ignored by Git), because the Salesforce Case doesn't carry the text yet. One hand-off per answer.
- **Components/Shared/PersonRequestCard.razor (+ .css):** the card.
- **Components/Shared/AnswerFeedback.razor:** after "Not quite", the offer "Sorry that didn't help. Want me to pass it to a person?" with "Yes, pass it on" and "No thanks".
- **Components/Layout/ChatWidget.razor:** "Talk to a person" opens the card; the card sits above the question box; the confirmation joins the conversation. Also fixed in passing: each message now has a fixed key, so an older answer's buttons ("Thanks", "Saved to the notebook") no longer reset when a new message arrives (an existing bug, seen while testing).
- **Services/ShowTheWork/Wording.cs:** three additions (card title, the "Not quite" offer, the confirmation); nothing existing changed.

Tested in the browser on November 2 (a test copy on port 5068 with Salesforce switched off, signed in as Watanabe Kenji): "Talk to a person" with no question (note required, Send disabled until typed); demo question 1 answered exactly as before; "Not quite" → offer → "Yes, pass it on" → card with name, role and chapter and the question → sent, reference number in the chat; "No thanks"; the event log (sign-in check, Input Compliance, ticket with door and counts, `salesforce=off`) and the stored record (only what was ticked); a harassment note → the safety line added; a second request for the same answer → "already passed on" with its reference; an out-of-scope note ("pizza") → refused, nothing sent. Not re-checked after the last polish (the card's intro line, the refusal wording, labels for screen readers); it builds with no errors or warnings.

**For the rehearsal:** with Salesforce switched on (appsettings.json), one click of "Send to the team" creates a real Salesforce Case, which also appears in the presentation's live Cases panel (Alt+C).

## 1. The promise and where it stands

Sprint 1 risk register, line 5, "A leader who can't get to a person":

> Three paths: Isa routes low-confidence and out-of-scope questions with context · 'this did not answer my question' on every answer · contact the team, always on screen.

Sprint 1 solution slide: "Contact the team: always on screen · one click from any answer · question + history go with it."

Checked in the code on November 2:

| Path | Today |
|---|---|
| 1. Isa routes the question herself | **Mostly built.** Not-found and sensitive questions get a reference number and a Salesforce Case (TicketingStage). Out-of-scope questions are stopped on purpose; "low confidence" can't route yet (placeholder score) |
| 2. "This did not answer my question" | **Half built.** "Yes, this answered my question" and "Not quite" sit under every answer (Components/Shared/AnswerFeedback.razor). "Not quite" says "Thanks for letting us know" and records a count for the Content Health Dashboard. It doesn't lead to a person |
| 3. Contact the team, always on screen | **Placeholder.** "Talk to a person" is in the chat footer but not wired up (Components/Layout/ChatWidget.razor). Willis's Milestone 1 hole |

## 2. The idea: one "connect me" card, three doors

The same small card serves three places:

1. **"Answer, then point" replies:** the connect button already agreed for the policy adapter (`offer_handoff`, docs/Isa_Rules_Design.md).
2. **"Not quite":** after the thanks, Isa offers to pass it on.
3. **"Talk to a person":** in the chat footer, always on screen.

One card means one thing to build, one thing to test, and one experience for the leader.

## 3. The card

**Title:** "Pass this to ISACA's Chapter Experience team?" (DRAFT WORDING)

**What will be sent, shown plainly before anything is sent:**

- Your name, chapter and role (from your sign-in)
- This question and Isa's answer, with its sources: **included, and the leader can untick it**
- Earlier questions in this conversation: **not included unless the leader ticks it**
- An optional box: "Anything you'd like to add?" (one or two lines)

**Buttons:** "Send to the team" · "Cancel"

**After sending:** the hand-off message with a reference number, in the agreed style (DRAFT WORDING): "I've passed this to ISACA's Chapter Experience team. Your reference number is TKT-0001. Keep it handy if you follow up."

Why the defaults are set this way: the promise says "question + history go with it", and the onboarding promise says "your questions to Isa stay private unless you choose to share them". So the question being asked about goes with it (that's the point of asking), but the card shows it and the leader can remove it; the rest of the conversation goes only if the leader chooses.

## 4. Each door

**From "answer, then point":** the card opens with that question and answer filled in. Nothing new.

**From "Not quite":**

1. The leader taps "Not quite". The count is recorded as today.
2. Instead of only "Thanks for letting us know", Isa adds: "Sorry that didn't help. Want me to pass it to a person?" (DRAFT WORDING) with "Yes, pass it on" and "No thanks".
3. "Yes" opens the card with that question and answer filled in. "No thanks" ends it.

**From "Talk to a person":**

- If the leader has asked something in this conversation, the card offers the latest question and answer (ticked, removable).
- If not, the card asks "What would you like help with?" and the box is required.

## 5. Rules (no side door)

- **Typed text goes through Input Compliance** (Orchestrator.CheckTextAsync, purpose `handoff_note`), the same check as notebook notes, and is logged like every other check.
- **If the typed text is a safety concern** (rule A-01), the ticket gets the `urgent` flag and the message gets the safety line, as for any safety hand-off.
- **One hand-off per answer.** If an answer has already been passed on, the card shows its reference number instead of making a second ticket.
- **Every hand-off goes to the Chapter Experience team** (Matthew, October 21), through the existing ticketing path and Salesforce Case.
- **Counted by door, never by person:** answer-then-point, not quite, talk to a person. This feeds two Sprint 1 metrics: "escalation quality, not rate" and "the right kind of use".
- **The answer receipt** (Output Compliance proposal, idea 9) records that a hand-off was made and from which door.

## 6. Accessibility

Keyboard reachable; focus moves into the card and back when it closes; buttons at least 44px tall; the card is announced to screen readers; works at phone width (one panel at a time).

## 7. Who builds what

| Piece | Who |
|---|---|
| The card, the "Not quite" follow-up, the footer link | Matthew (screens, with Claude) |
| A hand-off started by the leader (ticket, reference number, Salesforce Case, the `handoff_note` check, one per answer) | Willis (it is the "Talk to a person" hole on his Milestone 1 list) |
| The agreed wording | Matthew, then into Services/ShowTheWork/Wording.cs |

## 8. Timing: DECIDED (Matthew, November 2, 2026): before the presentation, and rehearsed

- **Before the presentation:** the "Talk to a person" door and the "Not quite" door, so no link on screen is dead if ISACA clicks it live. One click of "Talk to a person" is added to the rehearsal.
- **After the presentation:** the "answer, then point" door, because it comes with the rule engine, which isn't switched on before the presentation (rules/tests/rehearsed_demo.json).
- The rehearsed questions must still give exactly today's answers: the doors only add a card when a leader clicks, they change no answer.
- Order: mockup and wording approved by Matthew → Willis builds the leader-started hand-off → the screens → tested in the browser → rehearsed.

## 9. Open for Matthew

1. **The draft wording** (card title, "Not quite" follow-up, confirmation message).
2. **The defaults:** current question included and removable; earlier conversation only if ticked. Agree?
3. **A mockup** of the card before anything is built.
