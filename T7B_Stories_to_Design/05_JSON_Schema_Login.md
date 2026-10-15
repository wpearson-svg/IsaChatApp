# Interface Contract — POST /api/auth/login

## Request

```json
{
  "type": "object",
  "required": ["username", "password"],
  "properties": {
    "username": { "type": "string", "minLength": 1 },
    "password": { "type": "string", "minLength": 1, "maxLength": 128 }
  }
}
```

**Example**

```json
{
  "username": "leader@chapter.example",
  "password": "********"
}
```

## Success response

```json
{
  "userId": "uuid",
  "displayName": "Alex Leader",
  "roles": ["chapter_leader"],
  "chapterId": "uuid"
}
```

## Error

- **401** — generic message: `"Invalid username or password."`  
  (never reveal whether the account exists)
