# ISA Application — Folder Structure

Drop these folders into **IsaChatApp** (merge with what already exists).

```
IsaChatApp/
├── Components/                 ← Blazor UI (already exists)
│   ├── Layout/                 ChatWidget, MainLayout, NavMenu
│   ├── Pages/                  Home, Dashboard, Chat, Admin...
│   └── Shared/                 Throbber, shared controls
│
├── Services/                   ← Backend logic (ADD Foundation here)
│   ├── AccessControlAdapter.cs     T5
│   ├── EventLogService.cs          T9
│   ├── Orchestrator/               T10
│   │   ├── Orchestrator.cs
│   │   ├── StageModels.cs
│   │   └── Stages/
│   │       └── StubStages.cs       T11b
│   ├── Auth/                       future login services
│   ├── Chat/                       future chat services
│   ├── Metrics/                    future KPIs
│   └── Tickets/                    future support tickets
│
├── Controllers/                ← optional API controllers (if not Minimal API)
├── Models/                     ← DTOs / entities later
├── data/                       ← SQLite files (events.db, content.db)
├── docs/                       ← Program.cs example, contracts, notes
├── wwwroot/                    ← css, js, images
├── Properties/
├── docker-compose.yml          ← T2 Keycloak
├── Program.cs
├── appsettings.json
└── IsaChatApp.csproj
```

## What each folder is for

| Folder | Purpose |
|--------|---------|
| **Components** | All Blazor UI (pages, layout, chat widget) |
| **Services** | Business logic, pipeline, event log, access control |
| **Services/Orchestrator** | Pipeline spine + stages |
| **data** | SQLite databases (not committed with real data) |
| **docs** | Specs, Program.cs merge guide, stage contract |
| **Controllers** | Optional if you use controller-based API later |
| **Models** | Shared request/response types later |
| **wwwroot** | Static files (CSS, JS, images) |

## What to copy now (Foundation)

From this package into IsaChatApp:

1. `docker-compose.yml` → root
2. `Services/EventLogService.cs` → Services/
3. `Services/AccessControlAdapter.cs` → Services/
4. `Services/Orchestrator/` → Services/Orchestrator/
5. `data/` → data/
6. Merge settings from `appsettings.Foundation.json`
7. Merge code from `docs/Program.cs.EXAMPLE` into Program.cs

## Do not delete existing IsaChatApp folders

Keep: Components, wwwroot, Program.cs, existing Services (UsageLogService, WidgetSettings).
Only **add** the new Services files and folders.

— Eliyas Eshtea · IS Solution Analyst · Second Star Collective · I.C.stars Kansas Cycle 7
