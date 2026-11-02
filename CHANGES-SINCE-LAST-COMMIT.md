# Changes since the last commit

A running list of every change made since Matthew's last commit (November 2, 2026: 389180b, "Updates to the Isa policy adapter", pushed to Matthew's-Branch: the policy adapter design, the rule tester, the three sample rules files, the notes for Willis, and the record of the November 2 ISACA update). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

- **rules/global.json**: new sample rules, written so today's behaviour stays exactly the same when Willis switches over. A-01 harassment and safety (hand off, urgent, with the safety line), S-01 asking Isa to break her rules, S-02 asking for restricted content, O-01 out of scope: these four reproduce today's placeholder checks (pizza, "ignore your rules", "show me every document", harassment). Group C (Tax and money) as C-02 to C-09 (C-02 from the draft is split into C-02 insurance, C-08 compilations and C-09 running short of money, because each has its own "point to a person" line; the lines are the ones agreed on October 30). Each rule carries its tests. Nothing in Isa reads these files yet.
- **rules/tests/rehearsed_demo.json** (new): every rehearsed or on-screen question (demo questions 1 to 5, the chat's suggestion buttons, the landing page) with the result it gets at the rules check today, tested as Watanabe Kenji. The switch to the rule engine waits until every one matches.
- **rules/check_rules.py** (new): a reference check (not Isa's engine; Isa never runs it) that reads the rules files and checks headers, approval records, tighten-only, every rule's question and answer tests, and the rehearsed questions. Run `python rules\check_rules.py`. November 2: 61 checks, 0 problems; all seven rehearsed questions behave exactly as today.
- **docs/Isa_Rules_Design.md**: an empty kind signal means the topic alone is enough; signals match at the start of a word ("tax" matches "taxes", not "syntax"); new `extra_line` field (the safety line); new section "Protecting the rehearsed demo".
- **docs/Isa_Rules_Draft.md**: notes that C-02 became three rules in the file, and which sample rules are now written.
- **Claude outputs/Policy adapter - handoff for Willis.md**: new section 7a, "Update, November 2", on protecting the rehearsed demo, the new rules and the reference check.

## Suggested commit message

```
Add sample rules that keep today's behaviour, the rehearsed-demo tests and a rules check
```
