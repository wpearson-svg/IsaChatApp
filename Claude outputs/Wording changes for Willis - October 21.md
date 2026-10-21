# Wording changes for Willis: warmer wording, approved by Matthew

From Matthew · 21 October 2026 · Follows your review of `docs/Show_the_Work_Wording.md` (task T11W)

Thanks for agreeing the wording table with your three additions. Since then I went through the warmer wording in section 7 of that file, line by line, and approved all of it. This file lists every change against the wording we agreed, so you can put it straight into `Services/ShowTheWork/Wording.cs`.

I haven't committed or pushed anything. Please make these changes in your branch along with the rest of `Wording.cs`.

**N** is a number; **TKT-0001** is the ticket reference.

## The rules behind the new wording

1. One voice: Isa says "I" only in reply bubbles and hand-off messages. The step list and the closed line never have a speaker (no "I", no "she").
2. Everyday words: "ISACA's guidance" instead of "approved content", "sections" instead of "passages".
3. Every "no" says what to do next.
4. Warmth where it matters, without promises ISACA would have to keep.
5. Short sentences and contractions.

## 1. While Isa is working (the status line)

| # | Stage | Agreed | New |
| --- | --- | --- | --- |
| 1 | Sign-in check | Checking you can use Isa... | Checking your sign-in... |
| 2 | Search | Searching ISACA's approved content... | Searching ISACA's guidance... |
| 3 | Gathering passages | Gathering passages... | Gathering the right sections... |
| 4 | AI step | Diving Deeper... | Diving deeper... |
| 5 | Answer check | Checking the answer against its sources... | Double-checking the answer against ISACA's guidance... |
| 6 | Hand-off | Passing your question to a person... | Finding the right person to help... |
| 7 | After the last stage | Finishing up... | Almost there... |

Unchanged: "Reading your question...", "Checking the language...", "Checking your question...", "Looking for related topics...".

Note on 4: sentence case, like every other label. It applies everywhere the AI step is named, including developer mode: "Skipped Diving deeper".

## 2. The steps, once opened

| # | Step | Agreed | New |
| --- | --- | --- | --- |
| 8 | Sign-in check | Checked that you can use Isa | Confirmed your sign-in |
| 9 | Rules check | Checked your question against Isa's rules | Made sure Isa can help with this |
| 10 | Search | Searched ISACA's approved content | Searched ISACA's guidance |
| 11 | Search, exact match | Found the answer in ISACA's approved text | Found the answer in ISACA's own words |
| 12 | Search, related | Found N passages on the same topic | Found N sections on this topic |
| 13 | Search, nothing | Found nothing that matches | Couldn't find this in ISACA's guidance |
| 14 | Gathering passages | Gathered the passages to answer from · N passages | Gathered the sections to answer from · N sections |
| 15 | AI step | Diving Deeper · Wrote the answer using only the passages found | Diving deeper · Put the answer together using only ISACA's guidance |
| 16 | Answer check | Checked the answer against its sources | Double-checked the answer |
| 17 | Answer check, exact wording | Answer uses ISACA's exact wording · backed by N sources | Uses ISACA's exact wording · N sources |
| 18 | Hand-off | Created your ticket · Reference TKT-0001 | Passed your question on · Reference TKT-0001 |

Unchanged: "Looked for related topics"; "Received your question" and "Checked the language of your question" (developer mode only).

Note on 12 and 14: "1 section" in the singular, as "1 passage" is today.

## 3. When a step does not pass

The step line stays as in the agreed table; only the second line changes, except where shown.

| # | Situation | Agreed | New |
| --- | --- | --- | --- |
| 19 | Not signed in | Couldn't confirm you can use Isa · Please sign in to ask a question. | Couldn't confirm your sign-in · Please sign in to continue. |
| 20 | Outside what Isa covers | It's outside what Isa can help with, so she stopped here. | This one's outside what Isa can help with |
| 21 | Needs a person | This needs a person, so Isa won't answer it herself. | This is best answered by a person |
| 22 | Answer check: no approved source (your line) | The answer didn't have an approved source, so it's going to a person. | ISACA's guidance doesn't cover this yet, so a person will help |
| 23 | Answer check: any other check failed (your line) | The answer didn't pass a check, so it's going to a person. | The answer couldn't be fully checked, so a person will help |
| 24 | Something went wrong (leader) | Something went wrong here. | Something didn't work on our side |

Developer mode keeps showing the exact reason for 22 and 23, as you suggested, and the raw error for 24.

## 4. The closed line above each reply

| # | Outcome | Agreed | New |
| --- | --- | --- | --- |
| 25 | Answered | Answered from approved content · N checks | Answered from ISACA's guidance · N checks |
| 26 | Outside what Isa covers | Outside what Isa covers | Not something Isa can help with |
| 27 | Not signed in or no access | Couldn't confirm you can use Isa | Please sign in |

Unchanged: "Passed to a person · TKT-0001" and "Something went wrong on our side".

## 5. Reply bubbles (Isa speaks as "I")

| # | Situation | Agreed | New |
| --- | --- | --- | --- |
| 28 | Answer opening | Based on the approved content: ... | Here's what ISACA's guidance says: ... |
| 29 | Outside what Isa covers | That's outside what Isa can help with. Isa answers questions about running your chapter, using ISACA's approved guidance. | I can only help with running your chapter, using ISACA's guidance. Try asking about board meetings, elections or your yearly paperwork. |
| 30 | Not signed in | Please sign in to ask a question. Use the Sign in button at the top of the Isa home page. | Please sign in so I can help. The Sign in button is at the top of the home page. |
| 31 | No access | Your account doesn't have access to Isa. If you think it should, contact ISACA Support. | Your account doesn't have access to Isa yet. If you think it should, ISACA Support can help. |
| 32 | Something went wrong | Something went wrong on our side. Try again, or talk to a person. | Sorry, something went wrong on my side. Please try again in a moment. If it keeps happening, you can talk to a person. |

Bubble labels stay as agreed ("Isa" everywhere, "Not signed in" for the sign-in message, "Isa · Passed to a person" on hand-offs).

## 6. Hand-off messages (with the reference number)

**33. Who to follow up with: in all three messages.** Camille told us on 21 October that most chapters don't have an assigned Chapter Experience contact (they're supported by the Chapter Experience Operations Team as needed). So "your Chapter Experience contact" isn't true for most leaders.

- Agreed: "...if you follow up with your Chapter Experience contact or ISACA Support."
- New: "...if you follow up with ISACA's Chapter Experience team or ISACA Support."

**34. Sensitive topic** (passed to a person by the Rules check):

- Agreed: "This sounds like something a person should help with, so I've passed it to the right team. Your reference number is TKT-0001. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support."
- New: "Thank you for telling me. This is something a person should help with, so I've passed it to someone who can help. Your reference number is TKT-0001. Keep it handy if you follow up with ISACA's Chapter Experience team or ISACA Support."

**35. Not covered by ISACA's guidance** (passed to a person by the Answer check):

- Agreed: "ISACA's approved guidance doesn't cover this yet, so I've passed it to a person to follow up. Your reference number is TKT-0001. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support."
- New: "ISACA's guidance doesn't cover this yet, so I've passed your question to a person who can help. Your reference number is TKT-0001. Keep it handy if you follow up with ISACA's Chapter Experience team or ISACA Support."

**36. The answer couldn't be checked** (your third message: an AI-side check failed, or the model call failed):

- Agreed: "I couldn't check this answer well enough to share it, so I've passed it to a person. Your reference number is TKT-0001. Keep it handy if you follow up with your Chapter Experience contact or ISACA Support."
- New: "I couldn't double-check this answer well enough to share it, so I've passed your question to a person who can help. Your reference number is TKT-0001. Keep it handy if you follow up with ISACA's Chapter Experience team or ISACA Support."

**37. Safety line** (still on harassment only until the topic file exists, as you suggested):

- Agreed: "If anyone is in immediate danger, contact your local emergency services."
- New: "If you or anyone else is in immediate danger, please contact your local emergency services."

The ticket number stays in the message, with the "Reference #" line under it, on hand-offs only, as agreed.

## What I need back from you

- A yes, or your edits, on anything that doesn't fit how the stages report.
- If any of these strings sit outside your part of the code (the no-access message in `AccessControlAdapter.cs` and the error message in `Orchestrator.cs` are Eliyas's), tell me and I'll check with him.

When `Wording.cs` is in, I'll check every state in the browser against this list.
