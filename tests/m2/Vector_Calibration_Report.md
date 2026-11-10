# Search-by-meaning pass mark: calibration (lane 4, steps 1 to 4)

November 9, 2026. Everything ran on Willis's home PC (digitalstorm-pc), where Isa and the services run: embedding service `http://localhost:8092` (what `services-config.ps1` gives for Local). Steps 1 to 4 need no model and no sign-in. **Step 5 (end to end) has not run: it waits for Willis to set the slider.**

Files: `test-vector-calibration.csv` (the labelled set, made by `make_vector_calibration.py`), `score_vector_calibration.py` (the scorer), `results/vector_calibration.csv` (per question), `results/vector_calibration_report.txt` (raw output). Questions sent to the embedding service: 143 (plus 3 for the cross-check). Questions sent to Isa: 0.

## 1. Preflight: Pass

- Embedding service: `model_ready` true, `intfloat/multilingual-e5-base`, 768 dimensions.
- `data/vectors.db` (`passage_vectors`): 131 rows, same model and dimensions; passage IDs identical to `content.db`'s 131 passages (none added or removed). Built November 9 on this PC.
- Pass mark in use: 0.80 (no `output-compliance-settings.json` on this PC, so the default).

## 2. Scores already in the event log (background, not labelled)

From today's test runs on this PC: 279 vector hits and 94 misses. Hit `top_score`: min 0.80, p10 0.82, p25 0.83, median 0.84, p75 0.87, p90 0.89, max 0.91. Misses: 62 "no passage close enough" and **32 "guides only"**: the top 3 were all Best Practice Guide passages, so the rag stage was left with nothing even when an ISACA passage scored above the mark (see "As built" below).

## 3. The labelled set

80 in_scope (every ISACA document: Compliance Framework 7, Common Tasks 11, Core Roles 8, Systems Overview 12, President Guidebook 34, plus 8 from test-paths and the candidates file whose answer is clearly in a passage), 40 out_of_scope, 23 near. Each in_scope question was checked against its passage text. Questions an input rule stops or hands off were left out: they never reach search. Best Practice Guide documents are not in in_scope: the rag stage never returns them.

**Cross-check against Isa:** three questions Isa had logged during lane 1 match the script's top score to the logged two decimals (0.8927/0.89, 0.8797/0.88, 0.8744/0.87).

## 4. Results

**As built vs. as described.** This brief says "top 3, excluding" guides. Isa today takes the top 3 of *all* passages and then drops guides (VectorSearchService then RagStage; defect W-010). Both are reported; "isaca_only" is what the brief describes, "as built" is what Isa does now.

### 4.1 Top-1 score by kind (isaca_only)

| kind | n | min | p10 | p25 | median | p75 | p90 | max |
|---|---|---|---|---|---|---|---|---|
| in_scope | 80 | 0.784 | 0.814 | 0.826 | 0.846 | 0.867 | 0.883 | 0.911 |
| out_of_scope | 40 | 0.711 | 0.729 | 0.752 | 0.781 | 0.800 | 0.813 | 0.825 |
| near | 23 | 0.771 | 0.804 | 0.821 | 0.843 | 0.857 | 0.864 | 0.867 |

### 4.2 Top-1 correctness (in_scope, 80)

- Top-1 is the expected passage: **48 of 80 (60%)**. Expected passage in the top 3: **59 of 80 (74%)**; as built, 50 of 80 (63%).
- **High score on the wrong passage** is the bigger problem than low scores: 31 in_scope questions have a wrong top-1 at 0.81 to 0.89. Typical: role questions ("What is the treasurer responsible for?") land on the President Guidebook's general pages instead of Core Roles; Systems Overview "Purpose" passages beat their "Typical Uses" passages. The full list is in the raw report.
- Low scores (top-1 under 0.80): 4 (three of them still the right passage).

### 4.3 Pass marks 0.70 to 0.90

| mark | in acc % | in + expected in top 3 % | out acc % | near acc % | as built: in acc % | in + exp % | out % | near % |
|---|---|---|---|---|---|---|---|---|
| 0.70 | 100.0 | 73.8 | 100.0 | 100.0 | 90.0 | 62.5 | 82.5 | 95.7 |
| 0.75 | 100.0 | 73.8 | 77.5 | 100.0 | 90.0 | 62.5 | 65.0 | 95.7 |
| 0.77 | 100.0 | 73.8 | 62.5 | 100.0 | 90.0 | 62.5 | 55.0 | 95.7 |
| 0.78 | 100.0 | 73.8 | 50.0 | 95.7 | 90.0 | 62.5 | 42.5 | 91.3 |
| 0.79 | 97.5 | 71.2 | 35.0 | 95.7 | 90.0 | 62.5 | 27.5 | 91.3 |
| **0.80** (today) | 95.0 | 70.0 | 25.0 | 91.3 | 87.5 | 61.2 | 22.5 | 87.0 |
| 0.81 | 91.2 | 66.2 | 15.0 | 87.0 | 85.0 | 58.8 | 12.5 | 82.6 |
| **0.82** | 83.8 | 62.5 | 7.5 | 73.9 | 77.5 | 56.2 | 5.0 | 69.6 |
| 0.83 | 70.0 | 56.2 | 0.0 | 65.2 | 67.5 | 52.5 | 0.0 | 60.9 |
| 0.84 | 58.8 | 47.5 | 0.0 | 56.5 | 56.2 | 43.8 | 0.0 | 52.2 |
| 0.85 | 46.2 | 36.2 | 0.0 | 30.4 | 45.0 | 35.0 | 0.0 | 30.4 |
| 0.86 | 32.5 | 26.2 | 0.0 | 13.0 | 32.5 | 26.2 | 0.0 | 13.0 |
| 0.87 | 23.8 | 18.8 | 0.0 | 0.0 | 23.8 | 18.8 | 0.0 | 0.0 |
| 0.88 | 12.5 | 11.2 | 0.0 | 0.0 | 12.5 | 11.2 | 0.0 | 0.0 |
| 0.90 | 3.8 | 3.8 | 0.0 | 0.0 | 3.8 | 3.8 | 0.0 | 0.0 |

(Every step from 0.70 to 0.90 is in `results/vector_calibration_report.txt`.)

### 4.4 The overlap

Lowest-scoring correct in_scope question: 0.7838 ("Who takes the minutes?"). Above it: **all 23 near questions but two** and **17 of 40 out_of_scope**. Highest out_of_scope: 0.8245 "I'm stressed about my job, any advice?" (-> Core Roles, Treasurer activities), 0.8233 "Recommend a good book on leadership.", 0.8222 "How do I lose weight before the conference?". Highest near: 0.8666 "Where do I get help with Engage?", 0.8660 "What quorum does ISACA require for our board?", 0.8650 "What happens to a chapter that holds fewer than four board meetings in a year?". The full list with scores is in the raw report.

## 5. Recommendation

**No pass mark separates the kinds cleanly.** Out-of-scope questions can be kept out (none above 0.825), but near questions score like in-scope ones (median 0.843 against 0.846): for those, the pass mark is no help and the second line (the model saying the passages don't answer, then the output checks) has to do the work.

**Recommended range 0.81 to 0.82; recommended value 0.82.**
- At 0.82, out-of-scope questions reaching the model fall from 25% to 7.5% (3 of 40, all wellness or general-help questions landing on Core Roles), for a loss of 11 points of in-scope acceptance (95% to 84%), most of it questions whose top-1 was the wrong passage anyway (in-scope with the right passage in the top 3 falls only from 70% to 62.5%).
- I weighted the **false accept** more heavily: an out-of-scope question that reaches the model gets an answer built on wrong text, and the output checks are a second line, not the first (lane 1 found an answer that passed every check while saying nothing: W-002). A false reject costs a person's time on a question Isa could have answered; that is the safer failure.
- 0.83 would block every out-of-scope question but turn away 30% of in-scope questions; that is too many hand-offs for questions the content answers.

**Before any value, fix W-010** (drop guide passages before taking the top 3): as built, Isa finds the expected passage for 9 fewer in-scope questions at every mark (for example 61% instead of 70% at 0.80). That is worth more than any pass-mark change.

### Weak-match check (path 14)

At 0.82, 37 labelled questions land at or above the mark and below 0.85 (as built: 26 in_scope, 2 out_of_scope, 9 near), so path 14 stays possible. **But its test question, "When does the compliance review start and end?", scores 0.8196, just under 0.82:** at 0.82 it becomes a clean miss and test-paths path 14 needs a new question. At 0.81 it stays in the band. Willis's choice: 0.82 and a new path 14 question, or 0.81 and keep it.

## 6. Step 5: end to end at 0.82 (November 10)

Willis chose 0.82. Set in `output-compliance-settings.json` (`searchPassMark: 0.82`, the file the slider saves; NLI 0.40, confidence 0.70 and strong-match 0.00 unchanged), then Isa restarted. Ran on Willis's laptop (WillisPLaptop) with services on the home PC over Tailscale (`100.89.6.69`), rules version 5, signed in as d.whitfield, nothing forced, model warmed first, Salesforce switched off for the run (laptop's `appsettings.Local.json`, put back afterwards). 30 questions from the labelled set, chosen to sit on both sides of 0.82 (`tests/m2/step5_calibration.csv`), plus 1 warm-up. Results: `tests/m2/results/step5_calibration_082.csv`.

**The script matches Isa.** Every question that reached search by meaning logged the same top score the script predicted, to two decimals.

| Kind | Asked | Full-text hit (never searched by meaning) | Below 0.82: person | Reached the model | What the model and checks did |
|---|---|---|---|---|---|
| in_scope | 12 | 4 (answered) | 2 (0.798, 0.814, as predicted) | 6 | 1 clean answer (0.85); 3 ISACA's words after a failed check (strength, strength, nli+numbers+confidence); 1 "passages don't answer" (0.83); 1 source check failed, to a person (0.83) |
| out_of_scope | 9 | 0 | 7 | 2 (0.82, 0.82) | Both: "passages don't answer", to a person |
| near | 9 | 1 (answered) | 2 | 6 (0.82 to 0.87) | All 6: "passages don't answer", to a person |

- In-scope questions answered: 8 of 12 (4 full-text, 4 by meaning). The 2 turned away by the mark were the two the calibration predicted.
- Out-of-scope reaching the model: 2 of 9, both at the mark, both declined by the model. None answered.
- Near questions: 6 of 9 reached the model and the model declined every one, so here the model, not the mark, did the work, as section 5 expected.
- To look at: "How many members must attend each educational event?" (near: the passages don't say) was **answered as a full-text hit**, from the passage on holding four educational events a year. Full-text search runs before the pass mark, so the mark can't stop it. A wording match that doesn't answer the question, like M-017.

**Path 14 at 0.82.** "When does the compliance review start and end?" (0.8196) now misses the mark, as predicted. "Can we submit last year's audit if this year's isn't done?" landed at 0.84 here and failed the strength check, so it still makes a weak match at a strong-match mark of 0.85; use it as test-paths' path 14 question. Not rerun at 0.85 in this step.

**Count:** 31 questions sent to Isa in step 5 (30 + 1 warm-up).
