# Isa demo package

For demonstrations only, not a release. It runs the whole of Isa on one Windows computer: the app plus
every service it needs, in Docker. Made November 17, 2026 (Willis). The team's own setup (the compose
files in the project root and `ai-services/`, the launcher, `appsettings.Local.json`) is not used or changed.

## What the computer needs

- Windows 10 or 11, 64-bit, with about 15 GB free.
- Docker Desktop, installed and running (it needs WSL2).
- An internet connection for the first set-up only (it downloads about 8 GB). After that Isa runs offline.
- Optional: an NVIDIA graphics card with current drivers. With one, translation and the AI model use it.
  Without one, everything still works, but translation and AI-written answers are much slower.

## Installing

1. Run `Isa-Setup.exe`. It installs for your user only, into `%LOCALAPPDATA%\Isa-Demo`, with no
   administrator rights. No installer? Unzip `Isa-demo.zip` anywhere you can write to instead.
2. Leave "Set up Isa's services now" ticked (or later, run **Set up Isa (first run)**). It builds the
   translation and search-by-meaning services, starts all six, and downloads the AI model. This takes
   a while the first time; progress is in `logs\first-run.log`.
3. Start Menu > **Start Isa**. It starts the services, starts Isa and opens `http://localhost:5067`.
   **Stop Isa** stops everything.

From the zip, the same three steps are: `scripts\first-run.ps1`, `scripts\start-isa.ps1`,
`scripts\stop-isa.ps1` (right-click > Run with PowerShell, or
`powershell -ExecutionPolicy Bypass -File scripts\start-isa.ps1`).

## Signing in

The test accounts are the team's: the same usernames and passwords as on the team's computers.
Keycloak's own admin console is at `http://localhost:8080` (admin / admin). Demo settings only.

## What's inside

| Folder | What |
|---|---|
| `app\` | `IsaChatApp.exe` (no .NET needed) and what Isa reads: ISACA's content and vectors, the onboarding content, the rules, the slides. Her own databases (progress, notebook, events, usage) start empty. |
| `services\` | `docker-compose.yml` (Keycloak, Ollama, the voice, translation, search by meaning), `docker-compose.gpu.yml` (added when there's an NVIDIA card), the Keycloak realm, and the source for translation and search by meaning. |
| `scripts\` | First run, start, stop. |
| `logs\` | What each script and Isa printed. |

Settings for this copy are in `app\appsettings.Local.json`: services on this computer, Salesforce off
(no Salesforce org on a demo machine, so a hand-off gives a reference number without a Case).

## Uninstalling

Settings > Apps > Isa (demo) > Uninstall. It stops Isa and removes her services, their downloaded
models and Isa's databases. Docker Desktop itself stays.

## Known limits

- Ports 5067, 8080, 8091, 8092, 8880 and 11434 must be free. Don't install it on a computer that is
  already running the team's development setup.
- Kokoro and Ollama use their `latest` images; pin `KOKORO_TAG` / `OLLAMA_TAG` in `services\.env` if one breaks.
- Not for real use: Keycloak runs in development mode, the passwords are the team's test ones, and the
  licences (NLLB-200 is non-commercial; Docker Desktop needs a paid plan in large organisations) would
  need checking before anyone outside the team used it.

## Building the package

From the project folder: `powershell -ExecutionPolicy Bypass -File installer\build-package.ps1`.
It makes `installer\out\Isa\`, `installer\out\Isa-demo.zip` and, if Inno Setup 6 is installed,
`installer\out\Isa-Setup.exe`. `installer\out\` is never committed.
