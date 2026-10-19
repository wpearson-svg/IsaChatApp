# Changes since the last commit

A running list of every change made since Matthew's last commit ("Redesign chat and landing page in ISACA brand; add onboarding plan and test data," 19 October 2026). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

## Restored files (Isa would not build without these)

Eliyas's commit "Add project" (18 October 2026) deleted these files, and that commit was merged into main. After syncing, Isa would not build: the chat widget and the Content Health Dashboard still use UsageLogService, but its file was gone. All of these were copied back exactly as they were in Matthew's commit of 19 October, with nothing changed:

- **Services/UsageLogService.cs**: records each AI reply for the Content Health Dashboard. This is the missing file that stopped the build.
- **check-prereqs.ps1**: the startup check that start-isa.bat runs first.
- **Properties/launchSettings.json**: tells `dotnet run` to use the Development settings.
- **wwwroot/app.css** and **wwwroot/lib/bootstrap/** (44 files): the base styles that Components/App.razor loads on every page.
- **BRAND_GUIDELINES.md**: the ISACA brand guide the colors are based on.
- **stop-isa-debug.bat**: stops the debug version of Isa.

Files that commit also deleted but that Isa doesn't need to run were left out: ISACA Local Chapters.pdf, README_FOUNDATION.md (a copy is in docs/), images/, the large rw_ image files, .vscode/settings.json and data/.gitkeep. Willis and Eliyas can decide about those.

## Onboarding feature, week 8 foundation: day of term

- **Services/Onboarding/LeaderTermService.cs** (new): looks up what role a person holds on a given date and what day of their term it is (day 1 is the term start date). Reads data/directory.db read-only, so it never changes the directory.
- **Services/WidgetSettings.cs**: new demo date setting (3 July 2026 by default, switched on), so the day of term is the same whatever day the demo is shown. Resets on restart like the other settings.
- **Components/Pages/IsaConfig.razor**: new "Onboarding: demo date" section with an on/off switch, a date picker, and a check table of every test leader with a role on that date and their day of term. If data/directory.db hasn't been built, it says how to build it.
- **Program.cs**: registers LeaderTermService.
- **appsettings.json**: new "DirectoryDb" path setting (data/directory.db).
- Needs `python directory\build_directory_db.py` run once so data/directory.db exists (it is not in Git).

## Onboarding feature: journey steps (draft)

- **onboarding/journey_steps.csv** (new): the master list of the 90-day journey steps for presidents. 18 DRAFT steps: week one from the onboarding plan, the rest from the President Guidebook (pages noted in the Source column). Waiting for Matthew's review.
- **onboarding/build_onboarding_db.py** (new): checks the steps list and builds data/onboarding.db from it, the same way the directory is built. Run `python onboarding\build_onboarding_db.py` after any change to the list.
- **Services/Onboarding/JourneyService.cs** (new): finds the steps a role should see on a given day of term. Reads data/onboarding.db read-only.
- **Components/Pages/IsaConfig.razor**: the check table has a new "Journey steps today" column. On the demo date, Watanabe Kenji (day 3) sees J-03 to J-05 and Ana Beatriz Souza (day 60) sees J-15 and J-17.
- **Program.cs**: registers JourneyService. **appsettings.json**: new "OnboardingDb" path setting.
- **ONBOARDING-FEATURE.md**: records where the steps live, and adds a "Matthew's to-do" section with the step review.

## Test data: one master list

The project had two lists of test people that disagreed. In Matthew's list (data/synthetic), P-0010 was Sato Yumiko and P-0011 was Ana Beatriz Souza. In the team's list (directory/), which Isa actually reads, P-0010 is Dana Whitfield (used by the d.whitfield sign-in test account) and P-0011 did not exist. Marcus Johnson was a Treasurer in one list and a President in the other.

- **directory/people.csv** and **directory/role_assignments.csv**: added Sato Yumiko as P-0012 (roles R-0012 and R-0013) and Ana Beatriz Souza as P-0013 (role R-0014). Nothing already there was changed, so Dana Whitfield and the sign-in tests are unaffected.
- **directory/chapters.csv**: now holds Matthew's ten chapters (C-001 to C-010) with their fictional names, country, region, time zone and work week. Added C-010, Ana Beatriz Souza's chapter. The build script still reads only the ID and name, so nothing else changes.
- **data/synthetic/** (people.csv, role_assignments.csv, README.md): now a matching copy of directory/. The README says directory/ is the master list, and lists the new IDs.
- **docs/Directory_and_Keycloak_README.md**: updated the counts (12 people, 14 role assignments, 10 chapters).
- **ONBOARDING-FEATURE.md**: demo cast updated to the new IDs; the test data decision now names directory/ as the master list.
- Checked with `python directory\build_directory_db.py`: the data passes the team's checks. On the demo date (3 July 2026), Watanabe Kenji is on day 3 and Ana Beatriz Souza on day 60.

## Sign-in accounts for the chapter leaders

Only Dana Whitfield and the four staff/test accounts could sign in. None of the 11 chapter leaders, including the onboarding demo cast (Watanabe Kenji and Ana Beatriz Souza), had a Keycloak account.

- **directory/keycloak_accounts.csv**: added 11 accounts, one per chapter leader, all with the `chat_user` role: k.watanabe, sutrisno, m.fernandez, g.jonsdottir, a.mueller, f.alharbi, p.raghunathan, s.favre, m.johnson, y.sato, a.souza. Each person signs in with their example.org email from directory/people.csv. Nothing already there was changed.
- **keycloak/import/isa-realm.json** and **keycloak/dev-passwords.local.csv** (both git-ignored, so they don't show in Git): rebuilt from the CSVs with `build_keycloak_realm.py` (terms as of 19 October 2026). The five existing accounts and their passwords are unchanged; the 11 new passwords are in dev-passwords.local.csv.
- **docs/Directory_and_Keycloak_README.md**: the accounts table and count (16 accounts) updated.
- Keycloak only reads the realm file when the realm doesn't exist yet, so the new accounts appear after `docker compose down -v` then `docker compose up -d`.
- Teammates who pull this need to run `python directory\build_keycloak_realm.py` once to get the new accounts on their own computers.

## Onboarding feature, week 9: journey strip, Today window, contact step

- **Components/Shared/JourneyCard.razor** and **JourneyCard.razor.css** (new): a slim "Your first 90 days" strip at the top of the chat: title, week and day of term, a **Today** button, the role badge ("Chapter president"), and a progress bar through the 90 days. It shows only for a signed-in leader in the first 90 days of a role that has a journey.
- **Components/Shared/TodayPanel.razor** and **TodayPanel.razor.css** (new): the Today window. Clicking Today slides the chat to the left and opens this window, the same size, on the right; closing it slides the chat back. It lists today's steps (completed ones show a green check). Every step can be clicked:
  - "Meet your Chapter Experience contact" (J-03) opens Matthew's approved mockup: the sample contact's card, three options (book a welcome call with times in the leader's own time zone, send an introduction, join a group welcome), and the privacy line. Any one completes the step. "Change my choice" undoes it.
  - Every other step shows its "What to do" text, its source if it comes from an ISACA document, and "Mark as done" / "Mark as not done".
- **Components/Layout/ChatWidget.razor** and **ChatWidget.razor.css**: show the strip, open and close the Today window, and slide the chat left while it is open. Closing the chat also closes Today.
- **Services/Onboarding/JourneyContextService.cs** (new): puts together a leader's role, day of term and today's steps, so the strip and the Today window always agree.
- **Services/Onboarding/ContactService.cs** (new): finds the chapter's sample contact and works out the fake calendar from the demo date: one call time on each of the next three weekdays (10:00, 14:00, 9:30 in the contact's time zone) and a group welcome on the next Wednesday at 10:00.
- **Services/Onboarding/ProgressService.cs** (new): keeps completed steps, call bookings, group seats and introductions in **data/progress.db**, a separate database created automatically on first run and not in Git. Rebuilding the steps never erases progress. Deleting the file resets everyone's progress (useful before a demo rehearsal).
- **onboarding/contacts.csv** (new): five INVENTED sample Chapter Experience contacts, one per region, each with an example.org email and the chapters they look after. **onboarding/build_onboarding_db.py**: now also checks contacts.csv and builds it into data/onboarding.db. Run `python onboarding\build_onboarding_db.py` again.
- **Program.cs**: registers the three new services. **appsettings.json**: new "ProgressDb" path setting.
- **Services/KeycloakSignInService.cs**: the sign-in cookie now also keeps the person's directory ID (person_id from the Keycloak token). Anyone already signed in needs to sign out and back in once.
- Prototype limits, stated on screen: the calendar is made up, and introductions are saved in Isa but never emailed.
- The leader's own record (progress plan, piece 1):
  - Every finished step shows its date ("Done on Fri 3 July"). The date is the leader's "today", so the demo date while it is on.
  - **Services/Onboarding/ProgressService.cs**: new step_history table in data/progress.db. Every "done" and "not done" is added as its own entry and never erased, so the record is never rewritten. "Mark as not done" and "Change my choice" add an entry instead of wiping the earlier one.
  - **Components/Shared/StepHistoryList.razor** and **.razor.css** (new): the "History" list under each step, e.g. "Booked a welcome call, Fri 3 July" then "Changed my choice, Fri 3 July".
  - **Components/Shared/TodayPanel.razor**: a "My progress" link under today's steps opens all 18 steps of the 90 days, each marked done with its date, not done yet, or coming up on its day; today's steps have a blue edge. Every step there can be opened, and Back returns to My progress. Each step also shows which days it is meant for.
  - **Services/Onboarding/JourneyContextService.cs**: also passes every step of the journey, for My progress.
- Smoother opening: the Today window now glides in a short distance with a fade, starting and settling gently (0.5s), and the chat's slide to the left uses the same timing, so the two move together.
- On the demo date (3 July 2026): Watanabe Kenji sees Week 1, Day 3 with steps J-03 to J-05 and contact Mei Ling Tan; Ana Beatriz Souza sees Week 9, Day 60 with J-15 and J-17; Sato Yumiko (Immediate Past President) and the staff accounts see no strip.

## Changed files

- **ONBOARDING-FEATURE.md**: added Feature 4, the "Today" note. When a leader opens Isa, a short note shows their day of term and what's coming up. It appears inside Isa only, never as an email. Added to the week 9 build plan and the cut list. Also added four more onboarding pieces to build: a handoff checklist for the outgoing president, a "Meet your board" step, an ISACA terms glossary, and day 30/60/90 check-ins. The build plan and cut list now include them. Journeys for other board roles are listed as a future build for the presentation's roadmap slide, not built now. Added Feature 5: a document library where Isa reads each approved document aloud word for word with a "Listen" button (browser read-aloud, free, nothing sent anywhere); downloadable audio files added to the roadmap.
- **Components/Pages/SignIn.razor**: bug fix. The "Back to Isa home" link at the bottom of the sign-in screen did nothing when clicked, so there was no way back to the landing page. The link now does a normal, full page load (`data-enhance-nav="false"`), the same way signing in already does, and it takes you back to the landing page.

## Every C now uses the ISACA-approved C

Every C shape in Isa was redrawn as the C from ISACA's Visual Style page. The old C was a ring with a square notch cut out of it (or, on the landing page, an arc with slanted ends). The approved C has the same thickness and opening but a straight slot with rounded corners. It was measured from ISACA's own artwork and checked by laying it over that artwork.

- **wwwroot/favicon.svg** and **wwwroot/favicon.png**: the browser tab icon is the approved C in ISACA Green.
- **Components/Shared/Throbber.razor** and **Throbber.razor.css**: the spinning Isa icon (chat bubble and input row) draws the approved C as a filled shape. The spin and color cycle work the same as before. The old mask, and the unique mask ID it needed, were removed.
- **Components/Pages/ThrobberTest.razor** and **ThrobberTest.razor.css**: the throbber test page uses the same C.
- **Components/Layout/ChatWidget.razor**: the large faded C in the chat header.
- **Components/Pages/Home.razor**: the large C behind the hero and the cropped white C in the "Questions to try" band.
- **Components/Pages/SignIn.razor**: the large C on the sign-in screen.
- **Components/Pages/Home.razor.css**: the soft C pattern behind the "What Isa has read" band and the footer now uses the approved C, turned a quarter turn each time.
- **BRAND_GUIDELINES.md**: new note with the approved C's measurements and the ready-made SVG path, so any new C matches.
- After rebuilding, press Ctrl+F5 in the browser to see the new tab icon (browsers keep the old one for a while).

## Landing page: the intersecting-C "window" in the hero

The top section of the landing page (the hero) now uses ISACA's intersecting-C "window" from the Visual Style page, in the graphic version with no photo. Matthew chose this design from a mockup with and without a photo.

- **Components/Pages/Home.razor**: two approved Cs of the same size cross on the right side of the hero, with their openings pointing off the edge so only the arcs show. Each C is a brand color at 50% opacity in normal blend (#298dcb and #07649d). Where they cross, the window, a Blue-to-Teal tint is added. Isa's spinning C (the Throbber, still spinning on hover) now sits inside the window. The single faded C that used to be behind the hero was removed.
- **Components/Pages/Home.razor.css**: the hero background is now a Dark Blue to Medium Blue gradient. The window and the Isa badge share one fixed-size group pinned to the right edge, so the badge always stays in the window. On medium screens the group gets smaller so the arcs never reach the text. On tablets and phones it moves below the text and fades in at its top edge, and the Isa badge now shows on phones too (it used to be hidden there). The window group sits above the hero text box, because that box reached under the badge and blocked the mouse, so the badge would not spin on hover. To balance the page, the window group was moved up and to the left so the Isa badge lines up with the headline, and the badge is smaller (160px instead of 200px), so it no longer crowds the chat button in the bottom-right corner. The hero is now 620px tall including its padding (it was about 770px, so it ran off shorter screens), and it shrinks to fit a short screen, down to 480px.
- **Claude outputs/Isa hero window mockups.html**: the mockup Matthew reviewed (with photo and graphic only). Not used by the app.

## Suggested commit message

```
Restore files deleted in "Add project"; fix "Back to Isa home" link; onboarding day-of-term lookup

- Onboarding: add LeaderTermService (role and day of term from directory.db) and a
  demo date setting with a day-of-term check table on the Config page
- Onboarding: add draft journey steps (onboarding/journey_steps.csv), the
  build script for data/onboarding.db, and JourneyService

- Restore Services/UsageLogService.cs (build failed without it), check-prereqs.ps1,
  Properties/launchSettings.json, wwwroot/app.css, wwwroot/lib/bootstrap,
  BRAND_GUIDELINES.md and stop-isa-debug.bat
- Sign-in screen: "Back to Isa home" now does a full page load so it returns to the landing page
- Keycloak: add sign-in accounts for all 11 chapter leaders (chat_user) to
  directory/keycloak_accounts.csv; README accounts table updated
- Onboarding: add the "Your first 90 days" strip and the Today window beside the
  chat (JourneyCard.razor, TodayPanel.razor); clickable steps with "Mark as done";
  the "Meet your Chapter Experience contact" step with book a call, send an
  introduction and group welcome; sample contacts (onboarding/contacts.csv);
  progress kept in data/progress.db; sign-in cookie keeps the person's directory ID
- Test data: make directory/ the one master list; add Sato Yumiko (P-0012) and
  Ana Beatriz Souza (P-0013) and chapter C-010; data/synthetic now matches
- Onboarding plan: add the "Today" note, handoff checklist, "Meet your board",
  ISACA terms glossary, day 30/60/90 check-ins and a document library with a
  "Listen" button; other role journeys and downloadable audio as future builds
- Brand: redraw every C (favicon, throbber, landing page, sign-in, chat header,
  background pattern) as the ISACA-approved C; record its measurements in
  BRAND_GUIDELINES.md
- Landing page: add the intersecting-C "window" to the hero (graphic version),
  with the Isa throbber inside the window; remove the old single hero C
```
