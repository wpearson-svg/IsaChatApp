# Workflow — Login (US-Login)

```
[Chapter Leader]
      |
      v
[Login Page — Blazor]
  enter username + password
      |
      v
[POST /api/auth/login]
      |
      +-- invalid --> [Show generic error] --> back to Login Page
      |
      +-- valid ----> [Create session / JWT]
                          |
                          v
                    [Home Dashboard]
```

## Design checklist

| Step | Detail |
|------|--------|
| **Inputs** | username/email, password |
| **Process** | validate → authenticate → issue token |
| **Outputs** | session/token, user display name, roles, chapter |
| **Interfaces** | `POST /api/auth/login` |
| **Contracts** | see `05_JSON_Schema_Login.md` |
