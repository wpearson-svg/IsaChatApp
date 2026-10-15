# Workflow — Ask ISA (US-019, US-020)

```
[User types question in ChatWidget]
      |
      v
[Create trace_id]
      |
      v
[POST /api/ask  +  Bearer token]
      |
      v
[access_control] -- fail --> status=stopped
      |
      v
[translation] → [input_compliance]
      |                    |
      |                    +-- stop/escalate --> end
      v
[database_search] → [knowledge_graph] → [rag]
      |
      v
[complex_ai] → [output_compliance]
      |
      +-- escalate --> [ticketing] --> ticket reference
      |
      v
[Answer + Citations returned to Chat UI]
```

Each stage writes one row to the **event log** under the same `trace_id`.
