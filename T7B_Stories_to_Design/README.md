# T7B — From Stories to Design (ISA App)

**Session focus:** user story → workflow → components → interface contract  
**Applied to:** ISA Login + Ask ISA chat

## Artifact (what you submit)

1. **Workflow diagram** (steps in order)  
2. **Component diagram** (frontend, API, services, DB)  
3. **JSON schema / OpenAPI snippet** for one interface  

## Traceability chain

```
User Story → Workflow Diagram → Components → Interfaces → Contracts → Code
```

## ISA user stories used here

**US-Login:** As a chapter leader, I want to sign in with username and password, so that I can access my dashboard securely.

**US-019:** As a user, I want to ask questions in plain language, so that I can quickly find answers.

**US-020:** As a user, I want every answer to show its source, so that I can verify information.

## Files in this folder

| File | Content |
|------|---------|
| `01_Workflow_Login.md` | Login workflow |
| `02_Workflow_AskISA.md` | Chat /ask pipeline workflow |
| `03_Component_Diagram.md` | System parts |
| `04_JSON_Schema_Ask.md` | POST /api/ask contract |
| `05_JSON_Schema_Login.md` | Login request/response |
| `Design_Checklist.md` | Inputs → Process → Outputs → Interfaces → Contracts |
