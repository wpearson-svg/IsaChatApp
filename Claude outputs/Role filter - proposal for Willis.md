# Role filter at retrieval: a proposal for Willis

From Matthew Brady (written with Claude) · November 2, 2026 · Sprint 3, from the promise inventory (docs/Promise_Inventory.md, row S1-16b)

## How to use this file

Willis: this is a proposal, not a decision. It touches your search stage, so nothing happens unless you agree. It's written so you can give it to your own Claude as context.

For Willis's Claude: treat this as background and a request from Matthew. Willis decides whether and how to build it. Ask Willis before changing any code. **The rehearsed demo must not change** (section 6).

## 1. Why

Our Sprint 1 risk register promised ISACA a control for "a leader seeing content outside their role":

> Role filter at retrieval. A document outside a leader's tier cannot be part of their answer.

What exists today (checked in the code on November 2):

- **Built:** roles decide which *screens* a person can open (developer mode, the dashboards, Backstage).
- **Not built:** roles deciding which *documents* can go into an answer. The content database has no audience on any document or passage (`documents`: id, title, source, folder; `passages`: id, document, section, page, text), and `DatabaseSearchStage` and `RagStage` search every passage for everyone. The tier grid on the Developer dashboard is simulated.

Nobody sees anything they shouldn't today, because every document in Isa is meant for all chapter leaders. But it's a named control in our own risk register, so James may ask. "Built and tested" is a much stronger answer than "designed".

## 2. The idea in one line

Every document gets an **audience**; every search only looks in documents whose audience includes the signed-in person; a test proves it.

## 3. What it needs

**Content (Matthew and Claude can do this part):**

1. An optional `audience` on each document in content/content.json. **No audience means everyone signed in**, so every real ISACA document stays exactly as it is today.
2. Audience values come from the sign-in profile Isa already has: the `permission` level (`Leadership`, `Member`, as in directory/role_assignments.csv) and, if wanted, a `chapter_role` (for example `President`). Example: `"audience": ["Leadership"]`.
3. content/build_content_db.py carries the audience into data/content.db, either as a column on `documents` (joined by title) or copied onto each passage (no join). Your choice.

**Search (your part):**

4. `DatabaseSearchStage` and `RagStage` add one condition: only passages whose document has no audience, or whose audience includes the person's `permission` or `chapter_role` from `ctx.Profile`. **The role always comes from the sign-in, never from what the person types.**
5. The meaning map and, later, the knowledge graph follow the same rule if they return passages.
6. **Optional, defense in depth:** Output Compliance's "source approved and found" check also confirms each cited source's audience includes the person.

**What people see:**

7. A leader is **never told** a restricted document exists. If the only match is outside their audience, it's the same as no match: the normal "not found" path and the safety net.
8. Developer mode may show how many passages were filtered out (for example `filtered=2`) so the team can see the control working. Leaders don't see that.

## 4. The sample document (for testing only)

ISACA hasn't sent any restricted documents, and Matthew decided on October 21 not to ask for made-up ones. So the team writes one clearly labelled sample:

- **Title:** "SAMPLE: Board-only planning note (not an ISACA document)"
- **Audience:** `["Leadership"]`
- **Text:** invented, on a topic no rehearsed question touches (for example a board planning retreat), so it can't match a demo question.

**Proposed: the sample lives only in a test copy of the content database,** not in data/content.db. Then the live app's content doesn't change at all, and with every real document having no audience, the new filter changes nothing for anyone. Adding the sample to the live content later is optional.

## 5. Tests

| Test | Signed in as | Expected |
|---|---|---|
| A question only the sample answers | A Leadership account (for example Watanabe Kenji, P-0001) | Answered, citing the sample |
| The same question | The Member test account | Not found → safety net, and no mention of the sample |
| A question about a document with no audience | Both | Same answer for both, as today |
| The seven rehearsed questions (rules/tests/rehearsed_demo.json) | Watanabe Kenji | **Exactly today's answer, sources and steps** |

## 6. The demo

- **Nothing changes live.** The demo stays signed in as Watanabe Kenji in one role the whole time, so the role filter isn't shown on screen.
- Every real document has no audience, so Watanabe's answers can't change. The rehearsed-question test proves it before anything is merged.
- **Where it shows instead:** the risk register slide says "built and tested"; the supplement document shows the test result; if James asks, the answer is ready.

## 7. Size

Small: one optional field in the content file, one condition in two search stages, a test database with one sample document, and four tests. The Developer dashboard's simulated tier grid can stay as it is for now; wiring it to the real audiences is a later option.

## 8. Questions for you

1. Does this fit how you're building search and RAG (including the move to search on meaning)?
2. Audience on `documents` (joined) or copied onto each passage?
3. Is a test-only sample document right, or would you rather have it in the live content?
4. Roughly how long would it take?
