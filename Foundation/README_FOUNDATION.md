# Working Foundation — Backend (Eliyas)

<p align="center">
 <img width="1168" height="784" alt="image" src="https://github.com/user-attachments/assets/a808532a-7288-4ff7-ad72-e58ff25f5089" />

</p>

<p align="center">
<img width="1168" height="784" alt="image" src="https://github.com/user-attachments/assets/5cbd1df3-13a6-4285-8e4e-d58bc4585b7a" />

</p>

> **Drop these files into IsaChatApp and wire them as described below.**  
> **Due: Fri 10/16/2026**

---

## What this package contains

| Path | Task | Purpose |
|------|------|---------|
| `docker-compose.yml` | **T2** | Keycloak (pinned 26.0) + volume |
| `Services/EventLogService.cs` | **T9** | SQLite event log, WAL, write + read-by-trace |
| `Services/AccessControlAdapter.cs` | **T5** | Keycloak token validation + access_control stage |
| `Services/Orchestrator/StageModels.cs` | **T10** | QueryContext, StageResult, IStage |
| `Services/Orchestrator/Orchestrator.cs` | **T10** | Pipeline spine |
| `Services/Orchestrator/Stages/StubStages.cs` | **T11b** + placeholders | Database Search, KG, RAG + other stubs |
| `docs/Program_cs_additions.cs` | **T10** | How to register services + `POST /api/ask` |

---

## Setup steps

### 1. NuGet packages

```bash
dotnet add package Microsoft.Data.Sqlite
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
```

### 2. Copy files into IsaChatApp

```
IsaChatApp/
├── docker-compose.yml          ← from this package
├── data/                       ← create empty folder (gitkeep)
├── images/
│   ├── isaca-ai-gear.png       ← README banner (AI gear + ISACA)
│   └── isa-chatbot.png         ← README banner (chat bot UI)
├── Services/
│   ├── EventLogService.cs
│   ├── AccessControlAdapter.cs
│   └── Orchestrator/
│       ├── StageModels.cs
│       ├── Orchestrator.cs
│       └── Stages/
│           └── StubStages.cs
```

### 3. Merge `docs/Program_cs_additions.cs` into `Program.cs`

Follow the comments in that file.

### 4. Update `appsettings.json`

```json
{
  "EventLog": { "Path": "data/events.db" },
  "ContentDb": { "Path": "data/content.db" },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/isa"
  },
  "AccessControl": {
    "DevBypass": true
  }
}
```

Set `DevBypass` to **false** for the Friday demo.

### 5. Start Keycloak (T2)

```bash
docker compose up -d
```

Keycloak admin: http://localhost:8080  (admin / admin)

Tell **Willis** Keycloak is up so he can do T4 (realm, client, roles).

### 6. Run the app

```bash
dotnet run
```

### 7. Test `/api/ask`

With DevBypass on:

```bash
curl -X POST http://localhost:5067/api/ask ^
  -H "Content-Type: application/json" ^
  -d "{\"question\":\"What is the deadline to file a reimbursement request?\",\"traceId\":\"test-001\"}"
```

Check events:

```bash
curl http://localhost:5067/api/events/test-001
```

---

## Stage order (do not change)

```
access_control → translation → input_compliance → database_search
→ knowledge_graph → rag → complex_ai → output_compliance → ticketing
```

(`front_end` event is written by the orchestrator before stages run.)

---

## Four test paths (stubs support these)

| Path | Example | Expected |
|------|---------|----------|
| **Direct hit** | reimbursement deadline | Answer + sources, AI skipped |
| **AI fallback** | bought supplies, get money back | Meaning map / RAG stub answer |
| **Stopped** | pizza place / ignore rules | status=stopped |
| **Escalated** | harassment / cryptocurrency | status=escalated + ticket ref |

---

## Handoffs

| You finish | Tell | So they can |
|------------|------|-------------|
| Keycloak up | **Willis** | T4 |
| README + start | **A'keyrah** | T2b |
| `/api/ask` working | **Willis** | T12 |
| Event log + GET /api/events/{id} | **A'keyrah** | T13 |

---

## Not in Foundation (do not build yet)

Real Ollama calls, vector search, real KG, Salesforce ticketing, dashboards.

---

**Eliyas Eshtea** · IS Solution Analyst · Second Star Collective · I.C.stars Kansas Cycle 7
