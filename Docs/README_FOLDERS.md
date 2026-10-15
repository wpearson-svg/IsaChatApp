# ISA Full Foundation Code

Complete backend code for Working Foundation (Eliyas).  
Due: **Fri 10/16/2026**

## Files

| File | Task |
|------|------|
| `docker-compose.yml` | T2 Keycloak |
| `Services/EventLogService.cs` | T9 Event log |
| `Services/AccessControlAdapter.cs` | T5 Token check |
| `Services/Orchestrator/StageModels.cs` | T10 Models |
| `Services/Orchestrator/Orchestrator.cs` | T10 Pipeline |
| `Services/Orchestrator/Stages/StubStages.cs` | T11b + placeholders |
| `Program.cs.EXAMPLE` | How to wire everything |
| `appsettings.Foundation.json` | Settings to merge |

## Install

```bash
dotnet add package Microsoft.Data.Sqlite
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.IdentityModel.Tokens
```

## Copy into IsaChatApp

```
IsaChatApp/
├── docker-compose.yml
├── data/
├── Services/
│   ├── EventLogService.cs
│   ├── AccessControlAdapter.cs
│   └── Orchestrator/
│       ├── StageModels.cs
│       ├── Orchestrator.cs
│       └── Stages/StubStages.cs
```

## Wire Program.cs

Copy the ADD sections from `Program.cs.EXAMPLE` into your existing `Program.cs`.

## Settings

Merge keys from `appsettings.Foundation.json` into `appsettings.json`.  
Set `AccessControl:DevBypass` to **false** for Friday demo.

## Run

```bash
docker compose up -d          # Keycloak :8080
dotnet run                    # App
```

## Test

```bash
curl -X POST http://localhost:5067/api/ask -H "Content-Type: application/json" -d "{\"question\":\"What is the deadline to file a reimbursement request?\",\"traceId\":\"test-001\"}"

curl http://localhost:5067/api/events/test-001
```

## Handoffs

- Keycloak up → tell **Willis** (T4)
- README ready → tell **A'keyrah** (T2b)
- /api/ask works → tell **Willis** (T12)
- Event log works → tell **A'keyrah** (T13)

— Eliyas Eshtea · IS Solution Analyst · Second Star Collective · I.C.stars Kansas Cycle 7
