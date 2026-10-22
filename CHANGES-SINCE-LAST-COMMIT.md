# Changes since the last commit

A running list of every change made since Matthew's last commit (October 22, 2026: e879e48 "fix"). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

Purpose: let the pull request from Matthew's-Branch merge into main without conflicts, so everyone's work comes together. No merge was run; the two files that clashed with main now agree with it.

- **Services/Orchestrator/Stages/StubStages.cs**: now exactly main's version (Willis's new search stage). This drops the "ISACA's documents first, best-practice guides last" search order from Matthew's branch, because Willis rewrote the same lines and the two can't be combined automatically. To be put back into Willis's new search after the merge.
- **Services/ShowTheWork/Wording.cs**: now main's version (Willis's crashed-stage and skipped-step wording and screen-reader labels) plus the best-practice lines from Matthew's branch (BestPracticePrefix, IsBestPractice, BestPracticeOpening, ClosedAnsweredBestPractice, FoundBestPractice), which sit in parts of the file main didn't change.
- **content/content.json**: the 50 best-practice guide passages are taken out (it now matches main). Without the search order above, the guides would compete with ISACA's documents and could push the President Guidebook out of answers. The guides themselves (content/Governance Best Practice/) and content/add_best_practice.py are unchanged, ready to switch back on.
- **CHANGES-SINCE-LAST-COMMIT.md**: this list.
- Checked without merging anything: every file both branches changed now merges with no conflicts (tested file by file with git merge-file), and main plus this branch builds with no errors or warnings. This branch on its own does NOT build until it is merged (StubStages.cs uses StageOutcome.NotFound, which only main has), so don't run Isa from it before the merge. Not checked in the browser.

After the merge, starting from main: put the "ISACA first, guides last" order into Willis's new search stage, run `python content\add_best_practice.py` and `python content\build_content_db.py`, and retest the demo questions.

## Suggested commit message

```
Line up with main so the pull request merges cleanly

- StubStages.cs: take main's version (Willis's new search stage)
- Wording.cs: main's version plus the best-practice wording
- content.json: take the best-practice guides out for now; they go back
  in, with ISACA-first search order, after the merge
```
