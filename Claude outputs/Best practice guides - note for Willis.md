# Governance best-practice guides: note for Willis

From Matthew · 22 October 2026

Camille said (question 11) that for broader governance questions, such as what to cover in a new board member's orientation, Isa can draw on best practice from ASAE, BoardSource, BoardEffect and Association Forum. All four sites forbid copying their pages, so we didn't load them. Instead, I had nine short guides written in our own words from 20 of their free pages, organized by topic. Each guide lists the original pages under "Further reading". They are labelled everywhere as **general best practice, not ISACA policy**.

## What's new

- `content/Governance Best Practice/*.md`: the nine guides (orientation, officer roles, meetings, minutes, conflicts of interest, committees, volunteers, succession, governance reviews). Every title starts with `Best Practice Guide:`.
- `content/add_best_practice.py`: adds the guides to `content/content.json` as passages (one per `##` section; the "Further reading" links are not loaded). It is safe to run again and leaves your ISACA passages untouched. **If you regenerate `content.json` from the ISACA documents (for example when you add the role guidebooks), run it again afterwards**, then `build_content_db.py`.
- `content/content.json`: 50 new passages at the end (ids 82 to 131). Nothing else in the file changed.

## Code changes in your area (please look these over)

1. **Search order** (`StubStages.cs`, `DatabaseSearchStage`): ISACA's documents first, then the meaning map, then the guides last. Guides answer only when ISACA's documents have nothing. Results from a guide are tagged `best_practice; count=N`, and handled like a direct hit (the guide's own sentence, no AI). The FTS query moved into a small `SearchAsync` helper with a `document LIKE / NOT LIKE 'Best Practice Guide:%'` filter.
   - Why: without it, the guides outranked the President Guidebook, and "How many board meetings must a chapter hold each year?" answered from a guide.
2. **Answer opening** (`ResponseStages.cs`, `OutputComplianceStage`): an answer from a guide opens "Here's what general governance best practice suggests (not ISACA policy):" instead of "Here's what ISACA's guidance says:".
3. **Wording** (`Wording.cs`): `BestPracticePrefix`, `IsBestPractice()`, `BestPracticeOpening`, `ClosedAnsweredBestPractice` ("Answered from governance best practice") and `FoundBestPractice` ("Not in ISACA's guidance; found general best practice").
4. **Show the work** (`WorkStepText.cs`): the closed line and the search step's second line use the two new strings when the search returned `best_practice`.

The stage contract doesn't know the `best_practice` search result yet. Please add it, or tell me if you'd rather handle it differently.

## Checked

In the browser, as Watanabe Kenji:

| Question | Result |
| --- | --- |
| How many board meetings must a chapter hold each year? | Unchanged: President Guidebook p. 7, "minimum of four board meetings annually" |
| What should we cover when we orient new board members? | Answered from the orientation guide, labelled best practice |
| What should we include in board meeting minutes? | Answered from the minutes guide, labelled best practice |
| What do we do if a board member has a conflict of interest? | Answered from the conflicts guide, labelled best practice |

By script, against the rebuilt database: the README's other test questions (educational events, who writes down decisions, showing members how money was spent, yearly paperwork) give the same answers as before. Cryptocurrency still finds nothing and goes to a person.

Not checked: `test-paths.ps1` (please run it on your side).
