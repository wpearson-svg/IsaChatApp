# Changes since the last commit

A running list of every change made since Matthew's last commit (October 22, 2026: 22d60ed "Notebook expansion"). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **Merge of main (bb51ace) into Matthew's-Branch** (Matthew ran `git pull origin main` on October 22). This brings in Willis's and Eliyas's pipeline work (pull requests #22 and #23: the real AI model call, content import, developer access and more). Git combined most files by itself; two files conflicted because both branches had changed the same lines, and Claude resolved them:
  - **Services/ShowTheWork/Wording.cs**: kept Willis's version of the skipped-step and crashed-stage wording (the same fix both branches had made, in slightly different words) and his new screen-reader labels. The best-practice wording from Matthew's branch is kept.
  - **Services/Orchestrator/Stages/StubStages.cs**: kept Willis's new search stage (plain full-text search; a miss goes on to the meaning map in the rag stage and then the AI model) and fitted the best-practice guides into it: ISACA's documents are searched first; only if they have nothing, and the question is not in the meaning map, are the guides searched. The search helper now keeps Willis's match score and no longer hides database errors, like the rest of his stage.
  - Rebuilt data/content.db (main brought a new meaning_map.json). Builds with no errors or warnings.
  - Checked in the browser after the merge: "How many board meetings must a chapter hold each year?" answers from the President Guidebook; the orientation and conflict-of-interest questions answer from the guides, labelled as best practice; the cryptocurrency question goes to a person. "Who writes down what the board decided?" correctly goes down Willis's miss path to the AI model, but his answer check then hands it off ("ai check failed: source not retrieved": the model named its source "PASSAGE: ..."). That is in the new AI checks, not the merge; worth telling Willis, since it was a demo question.

## Suggested commit message

```
Merge main into Matthew's-Branch

- Wording.cs: keep Willis's crashed-stage and skipped-step wording and
  screen-reader labels; keep the best-practice wording
- StubStages.cs: keep Willis's new search stage; search the
  best-practice guides only when ISACA's documents and the meaning map
  have nothing
```
