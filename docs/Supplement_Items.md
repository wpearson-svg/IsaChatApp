# For the supplement: designed, not built

Started November 3, 2026 (Matthew). A running list of pieces that are designed but deliberately not built before the presentation, so none gets lost when the ISACA supplement document is written. In the supplement, each is labelled **designed** (James warned against promising features that don't exist; an audience of auditors trusts honesty about status).

Rows marked **Planned** are roadmap items (designed in outline, labelled planned on the roadmap slide). Add to this list whenever something is designed and parked. When something here gets built, move it out and say where it went.

| Item | What it would do | Why it isn't built | Where the design is |
|---|---|---|---|
| Notebook prompt after each board meeting | The day after each board meeting, Isa asks the president, inside Isa, "Anything from the meeting your successor should know?", with the same buttons as the weekly check-in. | Isa has no board meeting dates: step J-26 only asks the president to set them. It would need a place to enter the year's dates. (Matthew, November 3) | ONBOARDING-FEATURE.md, "Succession notebook reminders" |
| The rest of Isa's answer checks | Answer receipts, shadow mode, and the other ideas from the November 2 proposal that Willis hasn't built. | Not built yet. Willis built and switched on the main checks (see "Moved out of this list" below). | docs/Output_Compliance_Proposal.md |
| A role-specific rule | A rule that applies only to some roles (`applies_to_roles`). | Not built, by decision (November 2). | docs/Isa_Rules_Design.md, section 7 |
| A chapter's own rules file | The chapter layer of the policy adapter: rules that only tighten, for one chapter. | Not built, by decision (November 2). | docs/Isa_Rules_Design.md, section 7 |
| Day 30, 60 and 90 check-ins | At each milestone Isa asks "What's still unclear?"; the leader chooses whether to share the answer with their Chapter Experience contact; counts by milestone only. | **Planned** (roadmap), M3 decision, November 3. | ONBOARDING-FEATURE.md, piece E |
| "Coming up" overview | The next two weeks of steps, with "Start now" on steps a leader can do early. | **Planned** (roadmap), M3 decision, November 3. | ONBOARDING-FEATURE.md, "Coming up" overview |
| Progress sharing and the Chapter Experience staff view | The contact sees each new leader's day of term and steps done (never their questions); an all-leaders view of where leaders get stuck; ISACA's on/off switch, the privacy line and a one-time note for the leader. | **Planned** (roadmap), M3 decision, November 3. May overlap the client health dashboard. | ONBOARDING-FEATURE.md, "Progress record and sharing with ISACA" and "Chapter Experience staff view" |
| Listen in the succession notebook | The new president listens to the notes the previous president left, word for word. | **Planned** (roadmap), M3 decision, November 3. | ONBOARDING-FEATURE.md, Feature 5 |
| Journeys for other board roles | Treasurers, secretaries and others get their own short journeys from their role guidebooks. | **Planned** (roadmap), decided October 19. | ONBOARDING-FEATURE.md, piece B |
| Meaning match for rules | Comparing a question with each rule's example questions, as well as matching phrases. | Waits on the real RAG embeddings; to be added with Willis later, in shadow mode first. | docs/Isa_Rules_Design.md |

## Moved out of this list

- **Isa's answer checks** (moved November 6, 2026). Willis built them in pull requests #43 and #44 (November 3 to 5) and switched them on. They check each sentence with a natural-language inference model (HHEM), and check dates and numbers, "must", contact details and the answer's shape. They also cover a source that says "TBD", the fallback ladder (Isa's wording, then trimmed, then ISACA's own words, then a person) and confidence from checkable parts. The rehearsed demo questions are no longer exempt. In the supplement these are **built**, not designed. Two caveats: the HHEM model files have to be downloaded on each computer (docs/HHEM_Setup.md), and the M6 check of November 6 found a few edge cases (Claude outputs/M6 findings - for Willis's Claude.md). The ideas Willis hasn't built stay in the table as "The rest of Isa's answer checks".
