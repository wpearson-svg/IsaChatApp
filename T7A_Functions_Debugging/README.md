# T7A — Functions, Errors & Debugging (ISA App)

**Session focus:** reusable functions + systematic debugging  
**Applied to:** ISA Auth, Chat, Metrics, Tickets services

## Artifact (what you submit)

Code that:
1. Uses **at least two functions**
2. Includes **error handling** and comments
3. Has **one bug fix** with commit message: `fix: root cause …`

## ISA examples in this package

| File | Functions |
|------|-----------|
| `Services/Auth/AuthService.cs` | `NormalizeEmail`, `IsPasswordAcceptable` |
| `Services/Chat/ChatService.cs` | `ValidateQuestion`, `CreateTraceId`, `GetFriendlyError` |
| `Services/Metrics/MetricsService.cs` | `SelfServiceRate`, `AnswerAccuracy`, `TicketReduction` |
| `Services/Tickets/TicketService.cs` | `CreateTicketReference`, `ValidateTicketInput` |

## Debug Ritual (use this every time)

1. **Reproduce** — make the error happen again  
2. **Inspect logs** — read the message  
3. **Add logs** — `[timestamp] [level] [message]`  
4. **Hypothesize** — what do you think is wrong?  
5. **Fix** — change the code  
6. **Verify** — run again, confirm fixed  
7. **Commit** — `fix: root cause short description`

## Sample commit messages

```
fix: root cause empty question crashed ValidateQuestion
fix: root cause division by zero in SelfServiceRate when total=0
```

## Practice file

See `Practice_BuggyTotals.cs` — refactor into functions and fix the bugs using the ritual.
