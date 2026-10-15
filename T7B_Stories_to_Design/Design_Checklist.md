# Design Checklist (T7B)

For every ISA feature, fill this before coding:

| Element | Question | Example (Ask ISA) |
|---------|----------|-------------------|
| **Inputs** | What does the user send? | question, traceId, token |
| **Process** | What steps run? | auth → search → answer → log |
| **Outputs** | What comes back? | status, message, sources, ticket |
| **Interfaces** | Which API routes? | POST /api/ask |
| **Contracts** | Exact JSON shapes? | 04_JSON_Schema_Ask.md |

**Rule:** If you can explain it in a diagram, you can explain it in code.
