# M2 testing: rules version 5 (M-013), for Willis

November 10, 2026 · Matt (with Claude Code)

`rules/global.json` is now version 5. It adds eleven rules for the kinds of question that had no rule (M-013). Only the rules file changed: no engine code, no AI. Please pull it before your next test run, and please rerun `tests/m2/lane2_sweep.csv` on your PC (search by meaning), which is still the real test of whether the AI answers any of these.

## What you need to know

- **None of your 78 test questions changed decision** (run live on my laptop, version 4 against version 5).
- **The rehearsed demo questions are unchanged.** `check_rules.py`: 114 checks, 0 problems.
- **One old rule changed:** O-01 (off-topic) no longer stops a complaint about someone's jokes ("makes jokes about her accent"). It had "joke" as a signal.
- **Two more changes also sit in my branch**, outside the rules: the sign-in cookie check (M-018, `Program.cs`) and accessibility fixes in `ChatWidget.razor` and `wwwroot/app.css`. They are listed in `CHANGES-SINCE-LAST-COMMIT.md`.

## The new rules

| Rule | What it catches | Outcome |
|---|---|---|
| B-01 | Legal questions and threats: lawsuits, lawyers, liability, contract disputes, local law, police or government inquiries | Pass to a person |
| A-02, A-03 | Discrimination claims, and excluding people for who they are | Pass to a person (not urgent; A-01 keeps the safety line) |
| D-01 | Board conflict, removing or replacing an officer, an officer acting alone or misusing the role | Pass to a person |
| D-02 | Election disputes: cheating, vote buying, cancelling, redoing or postponing a vote | Pass to a person |
| E-01, E-03 | A data breach or security incident happening now | Pass to a person |
| S-03 | Another person's private contact details | Stop |
| S-04 | What other people asked Isa, or what staff said about someone | Stop |
| S-05 | Other chapters' private results and finances | Stop |
| F-01 | Why a chapter does or doesn't have a Chapter Success Manager | Pass to a person |

Plain questions on the same subjects are still answered: how elections work, officers' duties, what bylaws contain, general conflict-of-interest guidance, and talks or workshops about legal or security subjects.

## How it was measured

Two test sets, each with 60 sensitive and 40 ordinary questions, were written by a separate agent that never saw the rules. Each set was measured before the rules were adjusted to it, so these are fair numbers:

| Rules | Set | Sensitive caught | Ordinary wrongly handed off |
|---|---|---|---|
| Version 4 | Set 1 | 2 of 60 | 0 of 40 |
| Version 5, first draft | Set 1 | 42 of 60 | 0 of 40 |
| Version 5, widened | Set 2 (written to avoid the obvious keywords) | 38 of 60 | 2 of 40 (both fixed) |

After the last pass both sets are caught 60 of 60, but that is no longer a fair measure. The honest reading: about two in three truly new wordings are caught by a rule, and the rest still depend on the safety net. That is M-010 again: phrase rules will never catch every wording.

Other checks:

- Yesterday's 50 ordinary must-not-catch questions: still 50 of 50 not caught.
- The sweep's six groups (legal, discrimination, board, elections, data, other people's information): 40 of 41 caught, up from 2. The one left is a general conflict-of-interest question, left to be answered on purpose.
- Of 517 existing test questions, 4 others changed decision, all intended.
- Live on the running Isa: set 2 matched 100 of 100 expected decisions; the smoke set is unchanged.

## Still open

- **M-016:** "A member is threatening to sue the chapter" still gets A-01's safety line as well, because the safety rule wins. I left A-01 alone rather than add an exception to a safety rule.
- **M-010, M-014, M-015:** unchanged.
- **M-017** (the "best time to have a meeting" answer from the minutes guide): I'm taking it.

Files: `tests/m2/lane3_m013_heldout.csv`, `lane3_m013_heldout2.csv`, `lane3_m013_live.csv`, `results/v5_m013_live.csv`, `results/v5_regression.csv`, and `Defects_Matt.csv`.
