# Isa wording table: what leaders see at each stage

October 20, 2026 · Matthew Brady · for review with Willis

The user-facing label and message for each pipeline stage and each state, in plain words. It covers "show the work" (the line above each reply and the steps it opens into), the reply bubble itself, and the hand-off messages that carry the ticket reference. The AI step is shown as **"Diving Deeper"**.

Rules behind the wording (agreed October 20, 2026; see ISA-PROJECT-CONTEXT.md):

- **Green** = passed. **Blue** = handled (stopped, passed to a person, or something went wrong on our side). Each state has its own icon.
- **Gray** (skipped) and **red** (a real error) appear in developer mode only. A leader never sees red.
- Leaders see only the steps that ran. A step Isa didn't use is never mentioned, so no one wonders whether they missed a "better" answer.
- Every word comes from a fixed list, never from the AI model.

Placeholders: **N** is a number (passages or sources); **TKT-0001** is the ticket reference.

## 1. Stage labels, while running, and when passed

"While running" is the status line next to the spinner. "Passed" is the step in the opened list, with an optional second line in grey.

| # | Stage (code name) | Plain label | While running | Passed (step line) | Passed (second line) |
| --- | --- | --- | --- | --- | --- |
| 1 | front_end | Your question | Reading your question... | Received your question *(developer mode only)* | — |
| 2 | access_control | Sign-in check | Checking you can use Isa... | Checked that you can use Isa | — |
| 3 | translation | Language check | Checking the language... | Checked the language of your question *(developer mode only)* | — |
| 4 | input_compliance | Rules check | Checking your question... | Checked your question against Isa's rules | — |
| 5 | database_search | Search | Searching ISACA's approved content... | Searched ISACA's approved content | Found the answer in ISACA's approved text · *or* Found N passages on the same topic · *or* Found nothing that matches |
| 6 | knowledge_graph | Related topics | Looking for related topics... | Looked for related topics | — |
| 7 | rag | Gathering passages | Gathering passages... | Gathered the passages to answer from | N passages |
| 8 | complex_ai | **Diving Deeper** | Diving Deeper... | Diving Deeper | Wrote the answer using only the passages found |
| 9 | output_compliance | Answer check | Checking the answer against its sources... | Checked the answer against its sources | Answer uses ISACA's exact wording · backed by N sources · *or* Backed by N sources |
| 10 | ticketing | Hand-off | Passing your question to a person... | Created your ticket | Reference TKT-0001 |

## 2. When a stage does not pass

| Stage | State | Colour and icon | Step line | Second line |
| --- | --- | --- | --- | --- |
| Sign-in check | Stopped | Blue, "no entry" | Couldn't confirm you can use Isa | Please sign in to ask a question. |
| Rules check | Stopped (outside what Isa covers) | Blue, "no entry" | Checked your question against Isa's rules | It's outside what Isa can help with, so she stopped here. |
| Rules check | Passed to a person | Blue, person | Checked your question against Isa's rules | This needs a person, so Isa won't answer it herself. |
| Answer check | Passed to a person | Blue, person | Checked the answer against its sources | No approved content found, so it's going to a person. |
| Any stage | Something went wrong (leader) | Blue, "try again" | The stage's running words, e.g. "Searching ISACA's approved content" | Something went wrong here. |
| Any stage | Something went wrong (developer mode) | Red, warning | Same as above | The raw error, e.g. complex_ai · stop · 4.0s · error=TimeoutException |
| Any stage | Skipped (developer mode only) | Gray, dash | Skipped + the stage, e.g. "Skipped Diving Deeper" | Why, e.g. "Not needed: the exact text answers it" or "Not needed, because the question is going to a person" |

## 3. The closed line above each reply

| Outcome | Colour and icon | Closed line | Small text on the right |
| --- | --- | --- | --- |
| Answered | Green, check | Answered from approved content | N checks |
| Outside what Isa covers | Blue, "no entry" | Outside what Isa covers | — |
| Not signed in or no access | Blue, "no entry" | Couldn't confirm you can use Isa | — |
| Passed to a person | Blue, person | Passed to a person | TKT-0001 |
| Something went wrong | Blue, "try again" (red in developer mode) | Something went wrong on our side | — |

In developer mode the small text on the right shows the time taken instead.

## 4. The reply bubble

| Outcome | Bubble label | Message |
| --- | --- | --- |
| Answered | Isa | The answer from ISACA's approved content, then the source cards and "Save to notebook". |
| Outside what Isa covers | Isa | That's outside what Isa can help with. Isa answers questions about running your chapter, using ISACA's approved guidance. |
| Not signed in | Not signed in | Please sign in to ask a question. Use the Sign in button at the top of the Isa home page. |
| No access | Isa | Your account doesn't have access to Isa. If you think it should, contact ISACA Support. |
| Something went wrong | Isa | Something went wrong on our side. Try again, or talk to a person. |
| Passed to a person: sensitive topic | Isa · Passed to a person | See section 5. |
| Passed to a person: not covered by ISACA's guidance | Isa · Passed to a person | See section 5. |

## 5. Hand-off messages (with the ticket reference)

These are the only replies that show a reference: the ticket number in the message, and the "Reference #" line under it (stage contract, section 5: the trace ID is shown only on the escalation screen).

**Sensitive topic** (passed to a person by the Rules check):

> This sounds like something a person should help with, so I've passed it to the right team. Your reference number is **TKT-0001**. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support.

**Not covered by ISACA's guidance** (passed to a person by the Answer check):

> ISACA's approved guidance doesn't cover this yet, so I've passed it to a person to follow up. Your reference number is **TKT-0001**. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support.

**Safety topics** (group A of the hand-off proposal) add one fixed line after either message:

> If anyone is in immediate danger, contact your local emergency services.

Under the message: **Reference # 3f2b8c1e-...** (the full trace ID, which the event log is searched by).

## 6. What would change from today's code

Most of sections 1 to 3 is already built (Services/ShowTheWork/WorkStepText.cs). These would change once agreed:

| # | Change | Where | Owner (stage contract) |
| --- | --- | --- | --- |
| 1 | The AI step reads "Diving Deeper" (today: "Writing the answer..." while running, "Wrote the answer using only those passages" when done) | Services/ShowTheWork/WorkStepText.cs | Matthew |
| 2 | The outside-scope message (today: "This question is outside the scope of chapter support.") | Services/Orchestrator/Stages/StubStages.cs, Input Compliance | Willis and Matthew |
| 3 | The two hand-off messages (today: "This looks like a sensitive matter, so I have passed it to a person who can help..." and "I could not find an approved answer to this, so I have sent it to a person to follow up...") | Services/Orchestrator/Stages/ResponseStages.cs, ticketing | Willis |
| 4 | The "something went wrong" message (today: "Something went wrong. Please try again.") | Services/Orchestrator/Orchestrator.cs and Components/Layout/ChatWidget.razor | Eliyas (orchestrator), Matthew (chat) |
| 5 | The no-access message (today: "Your account does not have access to this assistant.") | Services/AccessControlAdapter.cs | Eliyas |
| 6 | The outside-scope bubble label "Isa" (today: "Unable to answer") | Components/Layout/ChatWidget.razor | Matthew |
| 7 | "Reference #" shown only on hand-offs (today: under every reply) | Components/Layout/ChatWidget.razor | Matthew |
| 8 | The safety line on group A hand-offs | Input Compliance and ticketing, with the hand-off topic file | Willis and Matthew |
