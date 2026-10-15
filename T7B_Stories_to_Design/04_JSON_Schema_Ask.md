# Interface Contract — POST /api/ask

## Request

```json
{
  "type": "object",
  "required": ["question"],
  "properties": {
    "question": { "type": "string", "minLength": 1, "maxLength": 2000 },
    "traceId":  { "type": "string", "format": "uuid" },
    "language": { "type": "string", "default": "en" }
  }
}
```

**Example**

```json
{
  "question": "What is the deadline to file a reimbursement request?",
  "traceId": "test-001",
  "language": "en"
}
```

**Header:** `Authorization: Bearer <token>` (required when DevBypass is false)

## Response

```json
{
  "type": "object",
  "required": ["status", "message", "traceId"],
  "properties": {
    "status": { "type": "string", "enum": ["answered", "stopped", "escalated"] },
    "message": { "type": "string" },
    "sources": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "document": { "type": "string" },
          "section":  { "type": "string" },
          "page":     { "type": "string" }
        }
      }
    },
    "ticketReference": { "type": ["string", "null"] },
    "traceId": { "type": "string" }
  }
}
```

**Example (answered)**

```json
{
  "status": "answered",
  "message": "Reimbursement requests are due within 30 days of the expense.",
  "sources": [
    { "document": "Chapter Finance Guide", "section": "Reimbursements", "page": "12" }
  ],
  "ticketReference": null,
  "traceId": "test-001"
}
```
