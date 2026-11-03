# "Talk to a person" is wired up: a note for Willis

From Matthew Brady (written with Claude) · November 2, 2026

## How to use this file

Willis: this tells you what was built near your part of Isa, so nothing surprises you. Please look it over. You can give it to your own Claude as context.

For Willis's Claude: this is information about changes Matthew asked for and Claude built. Willis decides whether anything should change. Ask Willis before changing any code.

## Why

The Sprint 1 risk register promised three paths to a person, including "this did not answer my question" on every answer and "contact the team, always on screen". "Not quite" only recorded feedback, and the "Talk to a person" link did nothing (it's on your Milestone 1 list as a hole). Matthew decided to have both working before the presentation, and rehearsed, so no link on screen is dead if ISACA clicks it. Design: docs/Paths_To_A_Person.md.

## What changed near your code

**Your stages are unchanged.** The new code only calls what already exists:

- **Services/PersonRequestService.cs (new):** a hand-off the leader asks for. It runs `Orchestrator.CheckTextAsync(ctx, "handoff_note")` (the sign-in check and Input Compliance on the leader's optional note, logged like every check), then calls your `TicketingStage.RunAsync` as is, with `EscalatedBy = "leader_request"` and `EscalationReason` = the door (`talk_to_person` or `not_quite`). It then replaces the hand-off message with its own wording (below) and writes one `ticketing` event with the door and counts (never the text).
- **Salesforce:** these Cases arrive with `escalated_by=leader_request`. The Case doesn't carry the question or note, so what the leader chose to share (question, answer, sources, earlier questions, note) is stored in a new local `data/person_requests.db` (ignored by Git via `data/*.db`).
- **A note that's a safety topic** (for example harassment): your Input Compliance sets `SafetyTopic`, so the ticket is handled as urgent and the message adds the safety line. An out-of-scope note is refused and nothing is sent.
- **Services/ShowTheWork/Wording.cs:** three additions only (card title, the "Not quite" offer, the confirmation message). Nothing existing changed. Matthew approved the wording on November 2.
- **Program.cs:** `PersonRequestService` registered after the orchestrator.
- **ChatWidget.razor:** each message `<div>` now has `@key="msg.DomId"`. Without it, adding a message re-created the buttons under older answers, so "Thanks" and "Saved to the notebook" reset after every new question.

Matthew's screens: Components/Shared/PersonRequestCard.razor (+ .css) and AnswerFeedback.razor.

## Tested

In the browser on a test copy with Salesforce off: both doors, the stored record, the event log, a safety note, a duplicate request, an out-of-scope note, and demo question 1 still answering exactly as before. Details in docs/Paths_To_A_Person.md.

## Questions for you

1. Is calling `TicketingStage` directly like this all right, or would you rather the orchestrator had a proper "leader asked for a person" entry point?
2. Should the Salesforce Case carry the shared question and note in its description, instead of the local `data/person_requests.db`?
3. Is `escalated_by=leader_request` a good name for these in Salesforce and the dashboards?
