# Rule tester: a request for Monday

From Matthew · for Willis · Monday, November 2, 2026

Willis, I know you have a lot on. This is one small screen, and I think it gives us the strongest governance moment in the presentation for about 45 seconds of demo time. Full design: docs/Isa_Rules_Design.md, section 14 (and the whole page for the rules format).

## The idea in one line

ISACA owns its governance; Isa is the adapter it plugs into. The rule tester proves it: type a question, pick some chapters, and see side by side what Isa would do for each and which ISACA rule decided it.

## Why it's worth building

- **It solves the demo problem.** We stay signed in as Watanabe the whole time, so we can't switch chapters. The tester shows Japan, Europe and the United States in one screen.
- **It's how engineers test rules.** James will recognize it straight away.
- **It's not extra machinery.** It calls the same rule engine you're building for Input Compliance. It just runs it for several chapters and shows the result.
- **It grows into the impact preview and the test runner later**, in the same panel. Not needed now.

## What it needs (the short version)

1. A panel on /dev-dashboard, Developer role only.
2. Inputs: a question, a list of chapters (default C-001, C-005, C-009), a role (default President).
3. For each chapter: run the Input Compliance rules **through the same code the live stage uses** (not a copy), and show the outcome, the deciding rule ID, its file and version, and the approval record.
4. Opening a row: every rule that matched and why the winner won (the conflict order).
5. Read-only: no search, no AI, no ticket. Logged as a rule test, kept out of the real figures.

Matthew and Claude write the three sample rules it needs (E-02 global, EU-01 Europe, JP-01 Japan) in the rules format.

## Fallback if time runs out

Watanabe asks the demo question in the chat; JP-01 hands it off; developer mode shows the rule. That needs only the rule engine itself.

## Questions for you

- Does this fit how you're building the rule engine?
- Roughly how long would it take you?
- Anything you'd rather do differently?
