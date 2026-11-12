# Changes since the last commit

A running list of every change made since Matthew's last commit (November 10, 2026: 6d88674, "M2 testing (Matt): rules v5 (11 new rules, M-013), accessibility check and fixes (M-019 to M-024), sign-in fix (M-018), rules docs and note for Willis"). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

Waiting on the merge with main (not a change, a reminder): Matthew's-Branch is pushed and is 5 commits behind main. Main has Willis's own fix for M-018, so the merge will stop on `Program.cs` and `Services/KeycloakSignInService.cs`: keep main's version of both (his `HasSession` does the same job as `IsKnownSession`). After the merge, rebuild and run `python rules\check_rules.py` (expect 114 checks, 0 problems).

1. **Demo runbook for the Sprint 3 presentation (November 10, 2026).** New deliverable in the "Runbook Pattern" shape (Setup, Happy Path, Edge Case, Recovery Script), written for the live Isa demo from docs/Sprint3_Presentation_Outline.md and onboarding/prepare_demo.py. No app code changed.
   - `Claude outputs/Demo runbook - Sprint 3 presentation.html`: the graphic, then the details: setup step by step, the happy path in order, six edge cases with what to say and do, recovery rules, and what is still to do before the day (backup video, screenshots, choosing the demo laptop, who does what). Opening it with `?slide` at the end of the address shows only the graphic.
   - `Claude outputs/Demo runbook - Sprint 3 (slide graphic).png`: the graphic alone, as a picture for the slides.
   - `Claude outputs/Demo runbook - Sprint 3 presentation.pdf`: the same page as a PDF, for sending.
   - Team only until the presentation: it names the onboarding demo. Not yet reviewed by Willis or Eliyas (it covers their parts of the demo too).

2. **Sprint 3 deck: the knowledge graph taken off the stage slides (November 12, 2026, Matthew).** It was never built, so the deck no longer lists it as a stage.
   - `presentation/slides.json`: slide 6 is now "One question, nine stages" (the Knowledge graph stage removed, the three path bars renumbered). Slide 9, "Every stage, honestly", lists nine stages, all built; its lead now reads "In Sprint 2, some stages were placeholders. Now every one is built." Willis and Eliyas still need to confirm that slide.
   - `Components/Pages/Present.razor` and `Present.razor.css`: when every stage on the status slide has the same status, the list spans the slide in three columns (before, a single column would have been squeezed into the left third). The Sprint 2 deck is unchanged.
   - Checked in the browser: slides 6 and 9 both fit and read correctly.
   - NOT changed yet, waiting for Matthew's decision: the two September diagram slides (slide 5 "What we promised in September" and slide 17 "What we promised, what we built") and the matching lines in `docs/Sprint3_Presentation_Outline.md` still say "knowledge graph". September's own deck did show it (slides 30 and 31: "Role filter · RAG · Knowledge graph · Logs"; slide 32: "graph-cached answers"), so removing it there changes what the slide says we promised.
   - Not changed: the app itself. The pipeline still has a pass-through `knowledge_graph` stage, and the Sprint 2 deck and older talking points still name it.

## Suggested commit message

```
Demo runbook for the Sprint 3 presentation (page and slide graphic in Claude outputs); Sprint 3 deck: knowledge graph removed from the stage slides (nine stages, all built), status slide lays out a single column across the slide
```
