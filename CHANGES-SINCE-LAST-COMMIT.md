# Changes since the last commit

A running list of every change made since Matthew's last commit ("This is the updated background screen and added sign-in function," 16 October 2026). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

## New files

- **ONBOARDING-FEATURE.md**: the design plan for the New Chapter Leader (onboarding) feature: the 90-day journey, succession notebook, "How to use Isa" guide, demo plan, build plan, and the decisions made so far.
- **CLAUDE.md**: tells Claude to read ONBOARDING-FEATURE.md and this file at the start of every session.
- **CHANGES-SINCE-LAST-COMMIT.md**: this list.
- **data/synthetic/** (people.csv, role_assignments.csv, chapters.csv, README.md): the team's synthetic test data, now saved in the project for everyone. All people are invented and use example.org emails.
  - Two new test presidents added for the onboarding demo: Sato Yumiko (P-0010, outgoing president of C-001) and Ana Beatriz Souza (P-0011, president of C-010 on day 60). The original nine people are unchanged.
  - New **chapters.csv**: the ten test chapters with clearly fictional names (Sakura Bay, Emerald Isles, Sierra Alta, Northern Lights, Black Forest, Desert Rose, Monsoon Coast, Alpine Lakes, Windy City, Rainforest Coast), each with its real country, region, time zone, and work week. Every name was checked against ISACA's list of real chapters.
  - README updated: new people described, onboarding demo cast added, chapter range now C-001 to C-010, and the decision that chapter names are clearly fictional.

## Changed files

- **Components/Pages/Home.razor.css** and **Components/Pages/SignIn.razor.css**: replaced off-brand greys and pale blues with ISACA's official colors from BRAND_GUIDELINES.md (ISACA Grey 1, 2, 3 and 8, ISACA Light Blue, ISACA Red and its 10% tint). Text contrast checked against the guide's accessibility rule. No layout changes.
- **Components/Layout/ChatWidget.razor.css** (the Isa chat bot): replaced every off-brand grey and color with ISACA's official ones. Small grey labels were too faint to read (2.85:1 contrast) and now meet the guide (6.46:1). "Unable to answer" and other error messages are now a light ISACA Grey box with an ISACA Medium Blue edge and Dark Blue text, with no red or pink (Matthew chose this from two mockups). No layout changes.
  - Bug fix: long text with no spaces (like a table of contents' dot leaders) now wraps inside the bubble instead of running out of it, and the chat never shows a sideways scrollbar.
- **Chat panel redesign** (Components/Layout/ChatWidget.razor and ChatWidget.razor.css, from Matthew's approved mockup):
  - New ISACA Dark Blue header with Isa's ring, the name "Isa," "Chapter Support Assistant," and the cropped "C" brand graphic.
  - Developer tools (Ollama hosts, model list, model picker, response-time labels) now show only when the role on the Config page is Developer. Chapter leaders see just the conversation.
  - Welcome message with three suggested-question buttons whenever the conversation is empty. The buttons send the question through the normal pipeline, exactly like typing it. New "New conversation" button in the header brings the welcome back.
  - Conversation on an ISACA Grey 1 background; Isa's answers are white cards; sources shown as ISACA Light Blue document chips; "Trace ID" relabeled "Reference #".
  - Footer: "Answers come from approved ISACA documents" and "Talk to a person" (still a placeholder for the ticketing bridge).
  - Backdrop behind the panel made stronger (ISACA Grey 4 at 85%). Panel font is now Roboto, ISACA's brand font.
  - Bug fix: the send button's arrow pointed left; it now points right.
  - The small "ISA" label on Isa's answers is ISACA Medium Blue when it reads well on the bubble color.
- **Landing page redesign** (Components/Pages/Home.razor and Home.razor.css, from Matthew's approved mockup):
  - "How Isa answers": three big numbered steps joined by a dashed line, each with a small brand-style illustration (3px Dark Blue line, ISACA Blue 40% and Green 40% fills).
  - Step 3 drawing redrawn (Matthew's corrections): the speech bubble's tail now points at the person, the person's head and shoulders sit close together, and the stray dots are gone.
  - "What Isa has read so far": ISACA Medium Blue band with a big "5" and numbered white document cards. The sentence above the "5" no longer repeats the number ("These are the chapter leader documents Isa can answer from. More are being added as ISACA approves them.").
  - "Questions to try": the questions are shown as "You ask" chat bubbles, with a white cropped C and the Isa ring (spins on hover).
  - Three touches borrowed from isaca.org after comparing the two sites: buttons are now rectangles with bold uppercase labels (Sign in, Sign out), a soft ISACA-style ring pattern sits behind the knowledge band and the footer, and the footer is now a full dark band with link columns (On this page, Your account, About this prototype).
  - All text meets the brand guide's 4.5:1 contrast minimum. Teal accents removed from this page.
- **content/build_content_db.py**: the build now skips table-of-contents passages (the President Guidebook's contents page, 3 passages). They matched many searches ("Finances ........ 3") and showed up in Isa's answers as rows of dots, pushing real answers down. The database now holds 80 passages; all the test questions in docs/Content_and_Database_README.md still give the same answers. Because content.db isn't in Git, everyone needs to run `python content\build_content_db.py` once after pulling.
- **docs/Content_and_Database_README.md**: notes the table-of-contents rule.
- **Services/WidgetSettings.cs**: Isa's answer bubble now defaults to White (a white card on the grey chat background) instead of ISACA Teal; White, from the brand guide's greyscale, was added to the Config page's color choices. Fixed ISACA Purple from #6658b1 to the brand guide's #6558b1. Bubble text color is now chosen with the real accessibility contrast formula (white on Teal had been 2.81:1, below the 4.5:1 minimum), and dark text is ISACA Grey 10. New LabelColor rule for the "ISA" label.
- **Components/Pages/SignIn.razor.css**: the "wrong password" box now matches the chat's "Unable to answer" box (grey with a Medium Blue edge, no pink or red).
- **Components/Shared/Throbber.razor.css** (the spinning Isa ring): one color in the spin animation changed from #0f69a0 to ISACA Medium Blue #07649d.
- **Components/Pages/DeveloperDashboard.razor.css** and **Components/Pages/ContentHealthDashboard.razor.css**: same ISACA Purple fix (#6658b1 to #6558b1).

## Suggested commit message

```
Add onboarding plan and test data; redesign chat and landing page in ISACA brand

- Add ONBOARDING-FEATURE.md (New Chapter Leader feature plan) and CLAUDE.md
- Add data/synthetic test data (people, roles, chapters); add two test presidents for the onboarding demo
- Landing page, sign-in, chat widget and throbber: use ISACA's official colors only
- Redesign the chat panel: branded header, welcome message with suggested questions, New conversation button, source chips, footer
- Hide developer tools (models, picker, timings) unless the role is Developer
- Redesign the landing page: illustrated steps, knowledge band, question bubbles, ISACA-style buttons, ring pattern and full footer
- "Unable to answer" and sign-in errors use a blue edge (no red/pink); send arrow now points right; long text wraps
- Fix text contrast in the chat widget (labels, error messages, bubble text)
- Fix ISACA Purple hex (#6558b1) in settings and both dashboards
- Content database build skips table-of-contents passages (rebuild content.db after pulling)
```
