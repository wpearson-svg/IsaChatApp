# Hand-off to Matt: review of the wording table

From Willis · 21 October 2026 · Replies to `docs/Show_the_Work_Wording.md` (your draft of 20 October) · Task T11W

## Where I stand

I agree with the wording table **with three additions** (below). Once you confirm them, I'll move every string into one file, `Services/ShowTheWork/Wording.cs`, and wire the panel, the stages and the chat to read from it. That covers all eight changes in your section 6.

## Agreed as written

- The stage labels, the "while running" text and the "passed" lines (section 1).
- "Diving Deeper" for the AI step, including "Skipped Diving Deeper" in developer mode.
- The closed lines and the right-hand text (section 3).
- The two hand-off messages for a sensitive topic and for a question ISACA's guidance doesn't cover (section 5).
- "Reference #" shown on hand-offs only, and the outside-scope bubble label changed to "Isa".

## Three additions I need from you

### 1. Wording for AI-side failures (missing from section 2)

Output compliance now has two sections. On the AI side, a run can pass to a person for any of these reasons: no approved source, the cited source was not one of the retrieved passages, the NLI check failed, the rule recheck failed, or the confidence score was too low. Your section 2 only covers "no approved content found".

**My suggestion.** Leaders get one line for all of them, and developer mode keeps showing the exact reason.

| Stage | State | Colour and icon | Step line | Second line |
| --- | --- | --- | --- | --- |
| Answer check | Passed to a person (any AI-side check failed) | Blue, person | Checked the answer against its sources | The answer didn't pass a check, so it's going to a person. |
| Answer check | Passed to a person (no approved source) | Blue, person | Checked the answer against its sources | The answer didn't have an approved source, so it's going to a person. |

### 2. A third hand-off message (section 5)

Today the ticket message depends on where the run was escalated. Anything not from the Rules check gets "ISACA's approved guidance doesn't cover this yet". That is **wrong** for an AI-side check failure, and for a failed model call (for example, the model is turned off). Those are not gaps in ISACA's guidance.

**My suggestion.** Add this message for those cases:

> I couldn't check this answer well enough to share it, so I've passed it to a person. Your reference number is **TKT-0001**. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support.

It shows the same way as the other two: the ticket number in the message, and the "Reference #" line under it.

### 3. The safety line

Your draft adds "If anyone is in immediate danger, contact your local emergency services." to group A (safety and wellbeing) hand-offs. Group A needs your topic file, which doesn't exist yet.

**My suggestion.** Until it does, show the line on the one topic that is group A today: harassment (test path 5). When the topic file lands, the line follows the file.

## What I need back from you

- A yes or no on each of the three additions, or your edit to the wording.
- Your **panel styles**. I didn't find them in the repo. If they're in a file I haven't seen, tell me where, or push them to your branch.
- Remove the "DRAFT" header from the wording file once you agree, so the agreed version is the one in the repo.

## What happens next

When I have your answers, I'll build `Wording.cs` and wire it in. After that, you can check every state in the browser against the table (your Block B check).
