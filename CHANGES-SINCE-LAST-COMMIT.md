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

## Changed files

- **ONBOARDING-FEATURE.md**: added Feature 4, the "Today" note. When a leader opens Isa, a short note shows their day of term and what's coming up. It appears inside Isa only, never as an email. Added to the week 9 build plan and the cut list.
- **Components/Pages/SignIn.razor**: bug fix. The "Back to Isa home" link at the bottom of the sign-in screen did nothing when clicked, so there was no way back to the landing page. The link now does a normal, full page load (`data-enhance-nav="false"`), the same way signing in already does, and it takes you back to the landing page.

## Suggested commit message

```
Restore files deleted in "Add project"; fix "Back to Isa home" link

- Restore Services/UsageLogService.cs (build failed without it), check-prereqs.ps1,
  Properties/launchSettings.json, wwwroot/app.css, wwwroot/lib/bootstrap,
  BRAND_GUIDELINES.md and stop-isa-debug.bat
- Sign-in screen: "Back to Isa home" now does a full page load so it returns to the landing page
- Onboarding plan: add the "Today" note (Feature 4)
```
