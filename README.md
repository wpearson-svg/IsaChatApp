# ISA App Complete Package

**Eliyas Eshtea** · IS Solution Analyst · Second Star Collective · I.C.stars Kansas Cycle 7

Merge this into **IsaChatApp**. Do not delete existing Components or Program.cs — only add.

## Folder map

```
ISA_App_Complete/
├── Services/
│   ├── AccessControlAdapter.cs      T5
│   ├── EventLogService.cs           T9
│   ├── Orchestrator/                T10 + T11b
│   ├── Auth/AuthService.cs          filled (T7A)
│   ├── Chat/ChatService.cs          filled (T7A)
│   ├── Metrics/MetricsService.cs    filled (T7A)
│   └── Tickets/TicketService.cs     filled (T7A)
├── Models/AskModels.cs
├── data/
├── docs/
├── docker-compose.yml
├── appsettings.Foundation.json
│
├── T7A_Functions_Debugging/         Functions · errors · debug ritual
├── T7B_Stories_to_Design/           Workflows · components · JSON schemas
└── T7C_Portfolio/                   Portfolio homepage + README
```

## Session order (instructor)

1. **T7A** — Functions, Errors & Debugging → use `T7A_Functions_Debugging/` + Services code  
2. **T7B** — Stories to Design → use `T7B_Stories_to_Design/` (ISA login + ask flows)  
3. **T7C** — Portfolio → use `T7C_Portfolio/` for GitHub Pages / presentation  

## Foundation still needs

- Docker Desktop installed → `docker compose up -d`  
- Merge `docs/Program.cs.EXAMPLE` into Program.cs  
- NuGet: Microsoft.Data.Sqlite, System.IdentityModel.Tokens.Jwt  

## Message Willis when

Keycloak is running on http://localhost:8080
