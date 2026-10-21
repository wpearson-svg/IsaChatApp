# Hand-off to Matt: answers on the hand-off and refusal rules

From Willis · 21 October 2026 · Replies to `docs/Handoff_and_Refusal_Rules_Proposal.md` ("Decisions for Willis") and settles task T8W, the split of the two compliance milestones.

## 1. The split (T8W)

| Milestone | Lead | Other person |
|---|---|---|
| **Questions Are Screened** (was due 10/16) | **Matt** | Willis keeps the Input Compliance code and makes any change you ask for |
| **Answers Are Checked Before They Leave** (due 10/21) | **Willis** | Matt checks the wording of failed-check messages and the states in the browser |

**For this milestone**, Questions Are Screened only needs the stub to keep its fixed rules and the topic list that always goes to a person, and to show the right result for a stop and an escalation. Test paths 3 (stop) and 5 (sensitive, escalated) already pass in `test-paths.ps1`. Your job now: review the topic list and the stop and escalation messages, and tell me what to change.

**Your proposal is the plan for after the milestone.** The real screening is deferred, so none of it is needed for Friday.

## 2. Your four decisions

### Are topic groups A to H complete? Anything to add, move or drop?

Complete enough to build from. I'm not adding or dropping a group.

The point from your "Other things to consider" matters more than the list: rules must look at **what is being asked**, not only which words appear. "When is our financial verification due?" mentions money, but ISACA's documents answer it, so Isa must still answer it. Please build that into the sample wordings, with "should answer" examples next to the "should hand off" ones for every group.

### Agree the four outcomes, especially "answer, then point to a person"?

Yes, I agree with all four.

One thing to know about the third. "Answer, then point to a person" does not exist in the stage contract today. The contract has Pass, NotFound, Stop and Escalate. An answered run would carry an extra line after the answer, for example "Whether a policy meets your country's law is a question for a person." That is a contract change, and I'll make it after the milestone and mark it in the confirmed changes (T15W). Eliyas needs to hear about it then.

### Who builds the topic file, the test list and the Input Compliance changes?

| Piece | Owner |
|---|---|
| Topic data file (each group, its outcome, route, message, sample wordings) | **Matt** |
| Test list of 50 to 80 tricky questions with expected outcomes | **Matt** |
| Input Compliance change that reads the file and replaces the placeholder keywords | **Willis** |
| Running the test list before every demo | **Willis**, from `test-paths.ps1` once the list exists |

Please keep the topic file in plain data (JSON or CSV, like the other data files) so I can read it without changing the code each time you edit it.

### Does translation run before Input Compliance for every language Isa accepts?

Yes. The stage order is already set that way: translation comes before Input Compliance.

The limit to be honest about: **translation is still a placeholder.** A question in Spanish reaches the gate in Spanish, and English keyword rules would miss it. The safety net catches it today. In my test run a Spanish question escalated because nothing approved matched it. Please state that limit in the supplement document, as your point 3 suggests.

## 3. What I need back from you

- A yes or no on the split in section 1.
- Any change to the topic list or the stop and escalation messages for this milestone.
- When the wording table is final: your `docs/Show_the_Work_Wording.md` is in the repo now, and I'll use it for the panel (task T11W).

## 4. What I'm doing next

I'm building the two-section output compliance (database side and AI side). When it is in, I'll send you the failure wording: what a person sees when an AI-side check fails. That wording belongs in your table.
