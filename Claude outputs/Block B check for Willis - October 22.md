# Block B check: Wording.cs against the agreed wording

From Matthew · 22 October 2026 · Follows your commit "Apply Matthew's warmer wording to Wording.cs" (task T11W)

Thanks for putting the warmer wording in. I ran Isa from main (d73c41b) and checked every state I could reach in the browser, signed in as k.watanabe (day 3 president), against `Claude outputs/Wording changes for Willis - October 21.md`. Almost everything matches word for word. I found two things still using old words and fixed them myself in `Services/ShowTheWork/Wording.cs` (below; it builds with no errors or warnings), so there's nothing for you to change. Please look over the fixes when you merge my branch. There's also a short list of what I couldn't test.

## What matches

Checked in the browser, leader view unless noted:

| State | Checked | Result |
| --- | --- | --- |
| Not signed in | Bubble label and message | Matches (#30) |
| Answered, direct hit (test 1) | Closed line, green check, "N checks", answer opening, every step line and second line | Matches (#8 to #11, #16, #17, #25, #28) |
| Answered, model path (test 2) | "Found 1 section on this topic", "Gathered the sections to answer from · 1 section", "Diving deeper · Put the answer together using only ISACA's guidance", "1 source" | Matches (#12, #14, #15), singular forms included |
| Outside what Isa covers (test 3) | Closed line, blue "no entry" icon, step second line, bubble, no Reference # | Matches (#20, #26, #29) |
| Not covered by ISACA's guidance (test 4) | Closed line with TKT number, blue person icon, step lines, hand-off message, Reference # under it | Matches (#13, #18, #22, #33, #35) |
| Sensitive, harassment (test 5) | Step lines, hand-off message, safety line | Matches (#21, #34, #37) |
| Answer couldn't be checked (test 2 with `OutputCompliance__ForceFail=source`) | Step second line, hand-off message | Matches (#23, #36) |
| Developer mode (test 1) | Time instead of checks, gray dash icons, "Skipped Diving deeper", raw stage data, user-view switch | Matches |

Checked in the code only: the live status line (`Wording.Running`, with "..." added in ShowTheWork.razor) matches #1 to #7.

## Fixed by Matthew in Wording.cs (please check)

### 1. The step line when a stage crashes still uses the old words

Found by reading the code, not seen in the browser (I couldn't make a stage crash; see "Not tested"). `Wording.Doing(stage)` (used by `WorkStepText.cs` line 127) gives the step line leaders see when something goes wrong. The wording table says that line is the stage's running words, but `Doing()` still has the agreed-before-warmer versions:

| Stage | `Doing()` today | Running words (should match) |
| --- | --- | --- |
| access_control | Checking that you can use Isa | Checking your sign-in |
| database_search | Searching ISACA's approved content | Searching ISACA's guidance |
| rag | Gathering passages | Gathering the right sections |
| output_compliance | Checking the answer | Double-checking the answer against ISACA's guidance |
| ticketing | Creating your ticket | Finding the right person to help |

**Fixed:** `Doing()` now returns `Running(stage)`, so the two can't drift apart again. For an unknown stage it falls back to "Working on your question".

### 2. Old words in developer mode, and one line leaders might see

They broke "sections, not passages" and "ISACA's guidance, not approved content". **Fixed:**

- `Skipped()`: "searching approved content" → "searching ISACA's guidance"; "gathering passages" → "gathering the right sections"; "checking an answer" → "double-checking the answer".
- `ContentNotLoaded`: "The approved content isn't loaded yet" → "ISACA's guidance isn't loaded yet". Leaders could see this one if the content database is missing.

## One change I made: the trace ID on hand-offs

A hand-off showed two different reference numbers: "Your reference number is TKT-0001" in the message, and "Reference # 8fc7fc45-…" (the trace ID) under it. Leaders couldn't tell which to quote. I changed `ChatWidget.razor` so the "Reference #" line shows only for the Developer role; leaders see just the TKT number. The trace ID is still in the event log and still searchable. Since the stage contract (section 5) says the trace ID is shown on the escalation screen, please update that line of the contract, or tell me if it causes a problem.

## Not tested

- **No access:** there's no test account without access to Isa.
- **Something went wrong (blue for leaders, red in developer mode):** I couldn't force a stage to crash, and the AI step is still a stand-in that doesn't call the model, so turning the model off doesn't cause an error yet. So fix 1 above comes from the code alone. If you have a way to force a crash, tell me and I'll check both views and confirm fix 1 in the browser.
- **The live status line in the browser:** the test browser freezes animations, so I checked these words in the code only.
- **The /dev/stage-feed page:** not opened.

Once there's a way to trigger a crash, I'll check that state in the browser.
