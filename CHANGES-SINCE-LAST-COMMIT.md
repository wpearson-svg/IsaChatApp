# Changes since the last commit

A running list of every change made since Matthew's last commit (merge of main into Matthew's-Branch, 20 October 2026). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

## Greeting in the leader's own language (20 October 2026)

- **Services/WelcomeWords.cs** (new): the word "Welcome" in each test leader's language, from a fixed list (never written by an AI model): English, Japanese, Arabic, Indonesian, Spanish, Icelandic, German, French and Portuguese, with English for anything else. Where the usual word changes with gender (Spanish, Portuguese, Icelandic), a form that doesn't is used, because the directory doesn't record gender. Not yet checked by native speakers.
- **Components/Pages/Home.razor**: the greeting at the top of the landing page now uses that word, from the person's language at sign-in (the locale claim, from the Language column in directory/people.csv). The name is unchanged: the form of address exactly as in the directory. Only this word is translated; the rest of Isa stays in English (Matthew's decision, to show ISACA that Isa can greet each person in their own language).
- Tested signed in: Watanabe Kenji sees "ようこそ, Watanabe-san", Fahad Al-Harbi "أهلاً وسهلاً, Ustadh Fahad" (the Arabic sits correctly beside the English name), and Ana Beatriz Souza "Boas-vindas, Ana Beatriz".

## Chat: a reply now opens at its start (20 October 2026)

Matthew noticed that a long reply (the "show the work" line, the answer, the source cards, the save button and the reference number) landed at its END, so the reader had to scroll back up to find where it starts.

- **Components/Layout/ChatWidget.razor.js** (new): a small script that scrolls the chat so the question sits at the top of the message area, with the start of the answer right under it. Short replies that already fit don't move. Respects "reduce motion".
- **Components/Layout/ChatWidget.razor**: calls it once a reply has arrived. Each message now has its own id on the page, so the chat can find the question (and two messages with the same text never count as the same message). **ChatWidget.razor.css**: comment updated.
- Tested with a short window: a 503px answer in a 391px chat area now opens with the question 8px from the top and the answer right under it.

## Onboarding feature, Feature 4: the "Today" note (20 October 2026)

- **Components/Shared/TodayNote.razor** and **.razor.css** (new): the note at the very top of the chat (above Isa's welcome message), shown to a signed-in leader in their first 90 days. Full note the first time each day: "Day 3 of your first 90 days", the date, "For today" (steps not done yet, or "You're up to date for today"), "Coming up" (steps starting in the next 7 days, with their days), and "Key date" when one is open or starts within 30 days. "Open Today" opens the Today panel; "Got it" shrinks the note to one line for the rest of the day, with "Show" and "Open Today". Every fact comes from the database, never from the AI model.
- **onboarding/key_dates.csv** (new): key calendar dates from ISACA documents, with their source. First entry: the annual compliance window, 1 November to 31 December (Compliance Framework; President Guidebook p. 13). **onboarding/build_onboarding_db.py**: checks it and builds it into data/onboarding.db (new key_dates table). Run `python onboarding\build_onboarding_db.py` again.
- **Services/Onboarding/JourneyService.cs**: `GetKeyDates` places each yearly date around today: open now (with days left) or starting in a number of days.
- **Services/Onboarding/ProgressService.cs**: new today_note_seen table in data/progress.db, so "Got it" is remembered for the day.
- **Components/Layout/ChatWidget.razor**: shows the note; "Open Today" opens the Today panel.
- **ONBOARDING-FEATURE.md**: Feature 4 marked as built, with how it behaves.
- Tested as Watanabe Kenji on the demo date (day 3): the note listed his two steps for today and four coming up (Meet your board on days 6 to 7, then three week-2 steps on day 8); "Got it" shrank it and was still remembered after reloading; "Open Today" opened the Today panel. The key-date arithmetic was checked on six dates outside the project: hidden on 3 July (121 days away), shown from 30 days out, "opens tomorrow" on 31 October, open with 46 days left on 15 November, "closes today" on 31 December, hidden again in January.
- data/progress.db (not in Git) now records that Kenji tapped "Got it" on 3 July, so his note opens as one line on that date. Deleting data/progress.db brings the full note back (it also resets journey progress).

## Onboarding feature, piece A: the outgoing president's handoff checklist (20 October 2026)

- **onboarding/handoff_steps.csv** (new): the 13 handover items from the President Guidebook (August 2026), word for word: p. 8 (notify the Chapter Experience team of the new leaders) and the Turnover Procedures on pp. 12-13, including Recognition and Appreciation. Each has a short title in Isa's words (Matthew's choice) and its page. Every sentence was checked against the guidebook text. "List the open items" is marked to offer "Add to the notebook".
- **onboarding/build_onboarding_db.py**: checks the file and builds it into data/onboarding.db (new handoff_steps table). Run `python onboarding\build_onboarding_db.py` again.
- **Services/Onboarding/HandoffService.cs** (new): decides who sees the checklist and when: a president in the last 30 days of their term, or a former president for the 30 days after it ends. **Services/Onboarding/JourneyService.cs**: reads the handoff items. **Program.cs**: registers HandoffService.
- **Components/Shared/HandoffChecklist.razor** and **.razor.css** (new): the checklist at the top of the notebook panel: progress ("2 of 13 done") with a bar, how much time is left, and the 13 titles. Tapping one shows the guidebook's words (the examples under "List the open items" as a list), its page, "Mark as done" or "Mark as not done", and its history. "Add to the notebook" opens "Add a note" with "Unfinished" chosen.
- **Components/Shared/NotebookPanel.razor**: shows the checklist; "Add to the notebook" moves to the note box.
- **Components/Shared/TodayNote.razor**: for the outgoing president (who has no 90-day journey), the note shows their handoff progress, the time left and the next item, with "Open the checklist" (opens the notebook). It now updates as soon as an item is marked in the notebook. **Components/Layout/ChatWidget.razor**: passes those changes along.
- **ONBOARDING-FEATURE.md**: piece A marked as decided and built.
- Tested as Sato Yumiko on the demo date: her Today note showed "0 of 13", 27 days left and the next item; "Open the checklist" opened the notebook with the checklist on top; "List the open items" showed the guidebook's text and examples; "Mark as done" updated the count, the history and the Today note; "Add to the notebook" opened the note box with "Unfinished" chosen.
- data/progress.db (not in Git) now has two of Sato's items marked done (notify ISACA, list the open items). Delete data/progress.db before a rehearsal to start her checklist empty (it also resets journey progress).

## Landing page: the "How Isa answers" icons tidied (20 October 2026)

Matthew noticed three things in the three step pictures.

- **Components/Pages/Home.razor**: the two papers (steps 1 and 2) are now drawn at the same size and the same position, so they read as the same page (before, one sat higher and further right, so they looked like different sizes). The magnifying glass and the check mark moved with them. The chat bubble's tail in step 3 is 40% smaller, still attached at the same corner.
- **Components/Pages/Home.razor.css**: the dotted line is now one short connector after each picture, running across to the next step's number. Before, one long line ran behind all three pictures, and their white boxes cut it into uneven pieces (a stray dash after "2", then a gap). On phones, where the steps stack, the connectors are hidden as before.
- Checked in the browser at laptop size (1440 pixels wide).

## Today window: smooth moves between its screens (20 October 2026)

Matthew noticed that opening the Today window glides in smoothly, but moving inside it (for example from today's steps to "Meet your Chapter Experience contact") just switched instantly.

- **Components/Shared/TodayPanel.razor**: each screen (today's steps, My progress, a step) is now drawn fresh when it changes, so it glides in. Going into a step or My progress, it comes from the right; "Back" brings it in from the left, so the direction is felt. The first screen doesn't glide, because the window itself is already sliding in. Each new screen also starts at the top (before, opening a step from far down My progress kept the old scroll position).
- **Components/Shared/TodayPanel.razor.css**: the glide, a short 16px move with a fade over 0.35s, with the same gentle start and finish as the window's own opening (a little quicker, because the move is smaller). Turned off for anyone who asks their device to reduce motion.
- Checked in the browser as Watanabe Kenji: into the contact step uses the forward glide, Back uses the glide from the left, and the first screen has none.

## Side-by-side windows: headers and edges line up (20 October 2026)

Matthew noticed that, with the chat, Today and Notebook open side by side, the bottoms of the blue headers didn't line up, and the edges between the windows didn't meet cleanly.

- **ChatWidget.razor.css**, **TodayPanel.razor.css**, **NotebookPanel.razor.css**: all three blue headers now have the same fixed height (72px, enough for the chat's round Isa badge), so the line where blue meets white runs straight across. Before, each header grew to fit what was inside it, and the chat's badge made its header taller.
- The seams: only the chat keeps rounded corners, on its outer left side. Where windows meet, the edges are square. Today and Notebook no longer cast a shadow onto the window beside them (it darkened that window's edge); a thin light divider line separates them instead.
- Measured in the browser at 1440 pixels wide with all three open: every header ends at the same height, and the windows meet edge to edge with no gaps.
- The band under the headers now matches too. **JourneyCard.razor** and **.razor.css**: the chat's "Your first 90 days" strip and the Today window's band (**TodayPanel.razor.css**) have the same fixed height (70px), the same spacing, and the progress bar pinned to the bottom, so the bars line up. In the chat strip, the role moved into the second line ("Week 1 · Day 3 · Chapter president") instead of a separate badge, so the strip stays one row (before, the Today and Notebook buttons pushed the badge onto a second row and made the strip taller). The Today window keeps its badge, as in the approved mockup. When the chat is squeezed to a quarter of the screen (another window enlarged), the Today and Notebook buttons show only their icons, so the strip still doesn't wrap; screen readers still hear their names. Measured at a third and at a quarter of the screen: both bands end, and both bars sit, at exactly the same height.

## Sign-in page: build warning BL0008 fixed (20 October 2026)

- **Components/Pages/SignIn.razor**: the form's `Input` (what the person types) no longer has its own starting value (`= new()`). Blazor fills it when the form is sent, and a starting value could be overwritten with nothing on a form post, which is what warning BL0008 was about. It is now created in `OnInitialized` only if Blazor hasn't already filled it (`Input ??= new()`), the pattern Microsoft recommends. The form is wrapped in a check that `Input` is there, so the compiler is satisfied too.
- Nothing about security changed: Keycloak still checks the password, the anti-forgery token, the safe return address, the vague error message and the cleared password field all work as before.
- The build now has no warnings at all. Tested in the browser: a wrong password shows the same error, keeps the email and clears the password; the right password signs in (greeting "ようこそ, Watanabe-san"); an empty form shows "Enter your email address." and "Enter your password."

## Proposal for Willis: hand-off and refusal rules (20 October 2026)

No code changed.

- **Claude outputs/Isa hand-off and refusal rules - proposal for review.pdf** (new, 7 pages): which questions Isa answers, passes to a person, or declines, for the real Input Compliance gate (Willis and Matthew's stage). Covers the four outcomes (answer; answer, then point to a person; pass to a person; stop), the "always to a person" topic groups A to H with suggested routes, the requests Isa stops, ways people may try to get around Isa and what protects against each, other considerations, and next steps with decisions for Willis. Also kept as an editable doc: https://claude.ai/artifact/UcHZZuoo2quuEV7JwDzSfW
- **docs/Handoff_and_Refusal_Rules_Proposal.md** (new): the same proposal as a Markdown file in the docs folder, so it lives with the code and shows up in Git.
- **docs/Show_the_Work_Wording.md** (new): the wording table for review with Willis: the plain label and message for each of the 10 pipeline stages in each state (running, passed, stopped, passed to a person, something went wrong, skipped in developer mode), the closed line above each reply, the reply bubble labels and messages, and the two hand-off messages with the ticket reference (plus the safety line). The AI step is shown as "Diving Deeper". Its last section lists the 8 changes from today's code, with the file and owner for each. No code changed yet.

The last two commits, for reference:
- "Add show the work, succession notebook and three-panel layout; fit journey to five phases" (afc89d7): everything from 20 October.
- The merge of main into Matthew's-Branch: brought in Willis's launcher scripts, Salesforce and developer-keys settings on the Dev Dashboard, and Keycloak reset scripts. main now keeps the Keycloak sign-in setup in Git (keycloak/import/isa-realm.json and keycloak/dev-passwords.local.csv); main's copy had only the 5 original accounts, so the realm was rebuilt from directory/keycloak_accounts.csv with python directory\build_keycloak_realm.py (Willis said this was fine). It now has all 16 accounts, including the 11 chapter leaders; every other setting is identical to main's, and the 5 original passwords are unchanged. .gitignore follows main's version.

## Suggested commit message

```
Greet leaders in their own language; add the Today note; open replies at their start

- Landing page: "Welcome" in each leader's own language (WelcomeWords, a fixed
  list with gender-neutral forms where needed, English otherwise); names unchanged
- Onboarding: the "Today" note at the top of the chat, shown in full once a
  day (day of term, today's steps, coming up, key dates within 30 days);
  "Got it" shrinks it to one line for the day; key dates in
  onboarding/key_dates.csv (the compliance window), built into onboarding.db
- Chat: when a reply arrives, scroll the question to the top so the reply is
  read from its start (ChatWidget.razor.js)
- Onboarding: the outgoing president's handoff checklist, the President
  Guidebook's 13 handover items word for word (onboarding/handoff_steps.csv),
  shown at the top of the notebook from 30 days before the term ends to 30 days
  after; Mark as done with history; "Add to the notebook" for the open items;
  handoff progress in the Today note
- Landing page: "How Isa answers" icons tidied (papers the same size and
  position, smaller chat tail, an even dotted connector between steps)
- Today window: screens glide in when moving between them (forward from the
  right, back from the left); each new screen starts at the top
- Side-by-side windows: the same header height in all three, square seams
  with a thin divider, rounded corners only on the chat's outer edge; the
  "Your first 90 days" bands match in height with the progress bars aligned
- Sign-in page: fix build warning BL0008 (the form input is created in
  OnInitialized instead of with a starting value); the build has no warnings
```
