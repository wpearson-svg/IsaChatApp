# Component Diagram — ISA Foundation

```
┌─────────────────┐
│  Blazor UI      │  ChatWidget, Login, Dashboard
│  (Frontend)     │
└────────┬────────┘
         │ HTTPS / JSON
         v
┌─────────────────┐
│  ASP.NET API    │  /api/ask  /api/auth  /api/events
│  (Backend)      │
└────────┬────────┘
         │
    ┌────┴────────────────────────────┐
    v                                 v
┌──────────────┐              ┌──────────────┐
│ Orchestrator │              │ Keycloak     │
│ + Stages     │              │ (Auth)       │
└──────┬───────┘              └──────────────┘
       │
       v
┌──────────────┐     ┌──────────────┐
│ Event Log    │     │ Content DB   │
│ SQLite       │     │ SQLite       │
│ (events.db)  │     │ (content.db) │
└──────────────┘     └──────────────┘

Future (not Foundation): Ollama, Qdrant, PostgreSQL, Jira
```

## Parts (minimum 4)

1. **Frontend** — Blazor ChatWidget / pages  
2. **API** — ASP.NET Minimal API `/api/ask`  
3. **Auth** — Keycloak + AccessControlAdapter  
4. **Orchestrator** — stage pipeline  
5. **Data** — event log + content SQLite  
