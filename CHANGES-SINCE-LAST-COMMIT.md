# Changes since the last commit

A running list of every change made since Matthew's last commit (October 28, 2026: e543f67, "Interactive Chapter 360 and Engage sample pages, Chapter 360 in the Library, worth-saving label"). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **data/notebook.db.bak-2026-10-23** still to be taken out of Git: it is still tracked after e543f67. Run `git rm --cached "data/notebook.db.bak-2026-10-23"` before the next commit. The file stays on Matthew's computer; .gitignore already ignores dated backups, so it won't come back.

- **data/onboarding.db is now shared through Git** (Matthew, October 28: teammates' copies should work like his with no extra steps).
  - **.gitignore**: one exception, `!data/onboarding.db`, so Git carries the onboarding content database. All other databases stay local, including data/progress.db and data/notebook.db (each person's own progress and notes) and data/content.db.
  - **data/onboarding.db** (new to Git): rebuilt fresh before adding (28 journey steps, 5 sample contacts, 1 key date, 13 handoff steps, 13 "worth saving?" tags; all 13 tags match Isa's content).
  - **onboarding/build_onboarding_db.py**: a note that the database is committed, and to commit it after changing any CSV.
  - **ONBOARDING-FEATURE.md**: the decision.
  - What it means for Willis and Eliyas: after merging, the "worth saving?" label just works, with nothing to run. One thing they'll still notice: the first time Isa starts, the voice warm-up makes audio for the seven Chapter 360 documents (about 11,000 words), once, in the background.

## Suggested commit message

```
Share the onboarding database through Git
```
