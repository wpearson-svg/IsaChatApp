# Isa wording table: what leaders see at each stage

**Agreed by Matthew and Willis on October 21, 2026** (sections 1 to 6, with Willis's three additions from his review, task T11W). Willis moves every string into one file, Services/ShowTheWork/Wording.cs. Section 7, the warmer wording, was approved by Matthew on October 21 and sent to Willis; it is waiting for Willis.

October 20, 2026 · Matthew Brady · reviewed with Willis October 21, 2026

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
| Answer check | Passed to a person (no approved source) | Blue, person | Checked the answer against its sources | The answer didn't have an approved source, so it's going to a person. |
| Answer check | Passed to a person (any other answer check failed) | Blue, person | Checked the answer against its sources | The answer didn't pass a check, so it's going to a person. |
| Any stage | Something went wrong (leader) | Blue, "try again" | The stage's running words, e.g. "Searching ISACA's approved content" | Something went wrong here. |
| Any stage | Something went wrong (developer mode) | Red, warning | Same as above | The raw error, e.g. complex_ai · stop · 4.0s · error=TimeoutException |
| Any stage | Skipped (developer mode only) | Gray, dash | Skipped + the stage, e.g. "Skipped Diving Deeper" | Why, e.g. "Not needed: the exact text answers it" or "Not needed, because the question is going to a person" |

The Answer check has two parts. On the AI side, a run can pass to a person because there was no approved source, the cited source was not one of the passages found, the NLI check failed, the rule recheck failed, or the confidence score was too low. Leaders see one of the two lines above; developer mode keeps showing the exact reason.

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
| Passed to a person: the answer couldn't be checked | Isa · Passed to a person | See section 5. |

## 5. Hand-off messages (with the ticket reference)

These are the only replies that show a reference: the ticket number in the message, and the "Reference #" line under it (stage contract, section 5: the trace ID is shown only on the escalation screen).

**Sensitive topic** (passed to a person by the Rules check):

> This sounds like something a person should help with, so I've passed it to the right team. Your reference number is **TKT-0001**. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support.

**Not covered by ISACA's guidance** (passed to a person by the Answer check):

> ISACA's approved guidance doesn't cover this yet, so I've passed it to a person to follow up. Your reference number is **TKT-0001**. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support.

**The answer couldn't be checked** (an AI-side answer check failed, or the model call failed, for example because the model is turned off; these are not gaps in ISACA's guidance):

> I couldn't check this answer well enough to share it, so I've passed it to a person. Your reference number is **TKT-0001**. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support.

**Safety topics** (group A of the hand-off proposal) add one fixed line after the message:

> If anyone is in immediate danger, contact your local emergency services.

Until the hand-off topic file exists, the line shows on the one group A topic Isa has today: harassment (test path 5). When the topic file lands, the line follows the file.

Under the message: **Reference # 3f2b8c1e-...** (the full trace ID, which the event log is searched by).

## 6. What would change from today's code

Most of sections 1 to 3 is already built (Services/ShowTheWork/WorkStepText.cs). Willis is making these changes by moving every string into Services/ShowTheWork/Wording.cs and wiring the panel, the stages and the chat to read from it:

| # | Change | Where | Owner (stage contract) |
| --- | --- | --- | --- |
| 1 | The AI step reads "Diving Deeper" (today: "Writing the answer..." while running, "Wrote the answer using only those passages" when done) | Services/ShowTheWork/WorkStepText.cs | Matthew |
| 2 | The outside-scope message (today: "This question is outside the scope of chapter support.") | Services/Orchestrator/Stages/StubStages.cs, Input Compliance | Willis and Matthew |
| 3 | The three hand-off messages, including the new "couldn't be checked" one (today: "This looks like a sensitive matter, so I have passed it to a person who can help..." and "I could not find an approved answer to this, so I have sent it to a person to follow up...") | Services/Orchestrator/Stages/ResponseStages.cs, ticketing | Willis |
| 4 | The "something went wrong" message (today: "Something went wrong. Please try again.") | Services/Orchestrator/Orchestrator.cs and Components/Layout/ChatWidget.razor | Eliyas (orchestrator), Matthew (chat) |
| 5 | The no-access message (today: "Your account does not have access to this assistant.") | Services/AccessControlAdapter.cs | Eliyas |
| 6 | The outside-scope bubble label "Isa" (today: "Unable to answer") | Components/Layout/ChatWidget.razor | Matthew |
| 7 | "Reference #" shown only on hand-offs (today: under every reply) | Components/Layout/ChatWidget.razor | Matthew |
| 8 | The safety line on group A hand-offs (harassment only until the topic file exists) | Input Compliance and ticketing, with the hand-off topic file | Willis and Matthew |
| 9 | The two Answer check lines for AI-side failures (section 2) | Output Compliance | Willis |

## 7. Warmer wording (approved by Matthew on October 21, 2026; waiting for Willis)

Sections 1 to 6 are the wording Matthew and Willis agreed. This section is a second, warmer pass that Matthew asked for, to make the words more friendly, polite, kind, warm and easy to read. **Matthew approved all of it on October 21, 2026**, and sent Willis the full list of changes against the agreed wording: Claude outputs/Wording changes for Willis - October 21.md. That list is the final version. Where it differs from the tables below, the list wins:

- "Outside what Isa covers": the step line is "This one's outside what Isa can help with" and the closed line is "Not something Isa can help with" (the tables below have "I", which breaks principle 1).
- Sensitive topic: "...so I've passed it to someone who can help" (the safer form, without "someone will follow up with you").
- New: warmer versions of Willis's two Answer check lines and his "couldn't be checked" message.
- New, from Camille's answers: every hand-off message says "ISACA's Chapter Experience team" instead of "your Chapter Experience contact", since most chapters have no assigned contact.
- "Diving deeper" in sentence case everywhere, including "Skipped Diving deeper".

Until Willis agrees, sections 1 to 6 remain what is being built. Note: since this was written, Willis's commit "Changed contract and added stage feed" (fb84fcd) added a "not found" state (blue "?") and its own list of running words (WorkStepText.RunningStage); check those against this list too.

Principles:

1. One voice: Isa says "I" in reply bubbles; the step list describes what happened, with no subject (no "Isa" or "she").
2. Everyday words: "ISACA's guidance" (not "approved content"), "sections" (not "passages"), "reference number" (not "ticket"), no "scope" or "assistant".
3. Always a next step: every "no" says what to do next.
4. Warmth where it matters: thank and reassure on sensitive hand-offs.
5. Short sentences and contractions.

### While Isa is working

| Stage | Today | Suggested |
| --- | --- | --- |
| Your question | Reading your question... | keep |
| Sign-in check | Checking you can use Isa... | Checking your sign-in... |
| Language check | Checking the language... | keep |
| Rules check | Checking your question... | keep |
| Search | Searching ISACA's approved content... | Searching ISACA's guidance... |
| Related topics | Looking for related topics... | keep |
| Gathering | Gathering passages... | Gathering the right sections... |
| AI step | Writing the answer... | Diving deeper... |
| Answer check | Checking the answer against its sources... | Double-checking the answer against ISACA's guidance... |
| Hand-off | Passing your question to a person... | Finding the right person to help... |
| After the last stage | Finishing up... | Almost there... |

### The steps, once opened

| Stage | Today | Suggested |
| --- | --- | --- |
| Sign-in check | Checked that you can use Isa | Confirmed your sign-in |
| Rules check | Checked your question against Isa's rules | Made sure Isa can help with this |
| Search | Searched ISACA's approved content | Searched ISACA's guidance |
| Search, exact match | Found the answer in ISACA's approved text | Found the answer in ISACA's own words |
| Search, related | Found N passages on the same topic | Found N sections on this topic |
| Search, nothing | Found nothing that matches | Couldn't find this in ISACA's guidance |
| Related topics | Looked for related topics | keep |
| Gathering | Gathered the passages to answer from · N passages | Gathered the sections to answer from · N sections |
| AI step | Wrote the answer using only those passages | Diving deeper · Put the answer together using only ISACA's guidance |
| Answer check | Checked the answer against its sources | Double-checked the answer |
| Answer check, exact | Answer uses ISACA's exact wording · backed by N sources | Uses ISACA's exact wording · N sources |
| Hand-off | Created your ticket · Reference TKT-0001 | Passed your question on · Reference TKT-0001 |

### When a stage does not pass

| Situation | Today | Suggested |
| --- | --- | --- |
| Not signed in | Couldn't confirm you can use Isa · "Not signed in or invalid token" (raw system text shown to leaders) | Couldn't confirm your sign-in · Please sign in to continue. |
| Outside what Isa covers | It's outside chapter support, so Isa stopped here and didn't search | This one's outside what I can help with |
| Needs a person | This needs a person, so Isa won't answer it herself | This is best answered by a person |
| Not in ISACA's guidance | No approved content found, so it's going to a person | ISACA's guidance doesn't cover this yet, so a person will help |
| Something went wrong | Something went wrong here | Something didn't work on our side |

### The closed line above each reply

| Outcome | Today | Suggested |
| --- | --- | --- |
| Answered | Answered from approved content · N checks | Answered from ISACA's guidance · N checks |
| Out of scope | Outside what Isa covers | Not something I can help with |
| Sign-in | Couldn't confirm you can use Isa | Please sign in |
| Hand-off | Passed to a person · TKT-0001 | keep |
| Error | Something went wrong on our side | keep |

### Reply bubbles

| Situation | Today | Suggested |
| --- | --- | --- |
| Answer opening | "Based on the approved content: ..." | "Here's what ISACA's guidance says: ..." |
| Out of scope, label | Unable to answer | Isa |
| Out of scope, message | This question is outside the scope of chapter support. | I can only help with running your chapter, using ISACA's guidance. Try asking about board meetings, elections or your yearly paperwork. |
| Not signed in | Please sign in to ask a question. Use the Sign in button at the top of the Isa home page. | Please sign in so I can help. The Sign in button is at the top of the home page. |
| No access | Your account does not have access to this assistant. | Your account doesn't have access to Isa yet. If you think it should, ISACA Support can help. |
| Error, label | Error | Isa |
| Error, message | Something went wrong. Please try again. | Sorry, something went wrong on my side. Please try again in a moment. If it keeps happening, you can talk to a person. |

### Hand-off messages

Sensitive topic. Today: "This looks like a sensitive matter, so I have passed it to a person who can help. Your reference number is TKT-0001."

Suggested: "Thank you for telling me. This is something a person should help with, so I've passed it on, and someone will follow up with you. Your reference number is TKT-0001. Keep it handy if you contact ISACA Support or your Chapter Experience contact."

Not covered by ISACA's guidance. Today: "I could not find an approved answer to this, so I have sent it to a person to follow up. Your reference number is TKT-0001."

Suggested: "ISACA's guidance doesn't cover this yet, so I've passed your question to a person who can help. Your reference number is TKT-0001. Keep it handy if you follow up."

Safety topics add: "If you or anyone else is in immediate danger, please contact your local emergency services."

### Open choices for Matthew

1. "Diving Deeper" (Willis's capitals) or "Diving deeper" (sentence case, like every other label). Suggested: "Diving deeper".
2. How much to promise: "someone will follow up with you" is warm but is a promise ISACA must keep; the safer form is "I've passed it to a person who can help."
