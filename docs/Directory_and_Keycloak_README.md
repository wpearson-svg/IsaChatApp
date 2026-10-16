# Directory data and Keycloak (no manual T4 setup)

Master source: the CSV files in `directory/` (from Matt's synthetic database: 10 fictional people, 11 role assignments).
Everything else is generated from them, so copies never drift.

## One-time / after any change to the CSVs
    python directory\build_directory_db.py        -> data\directory.db (checks the data, rebuilds from empty)
    python directory\build_keycloak_realm.py      -> keycloak\import\isa-realm.json + keycloak\dev-passwords.local.csv
    python content\build_content_db.py            -> data\content.db

## Start Keycloak with the realm already set up
    docker compose down -v      (only if an "isa" realm already exists from clicking through the console)
    docker compose up -d
The realm `isa`, client `isa-chat` (secret `dev-only-placeholder-secret`), the four client roles, the token claims and
the 5 accounts are created on first start. Check: http://localhost:8080/realms/isa/.well-known/openid-configuration

## Accounts (edit directory/keycloak_accounts.csv to change)
| Username | Roles | Notes |
|---|---|---|
| d.whitfield | chat_user | Person P-0010; token carries chapter C-009, chapter_role Member, permission Member, term dates |
| staff.dev | chat_user, dashboard_dev | no directory row |
| staff.content | chat_user, dashboard_content_health | no directory row |
| staff.audit | chat_user, auditor | no directory row |
| norole.test | none | must be refused (login-failure test) |
Passwords: dev only, in keycloak\dev-passwords.local.csv (git-ignored). To use Matt's sheet instead, put `username,password`
rows in that file BEFORE running build_keycloak_realm.py. Never commit it.

## What Isa reads from the token
Client roles (resource_access.isa-chat.roles) plus these claims: person_id, chapter_id, chapter_role, permission,
term_start, term_end, years_as_leader, language, formality, form_of_address. KeycloakAccessControlAdapter puts them in
QueryContext.Profile. "Current" role = the assignment whose term covers the day the realm file is generated (re-run to refresh).

## Try it (needs DevBypass OFF in appsettings.Development.json)
    .\test-ask.ps1 -User d.whitfield -Question "How many board meetings must a chapter hold each year?"
    .\test-ask.ps1 -User norole.test          (expect 401 / refused)

## Known limits
- Tokens are a snapshot from sign-in; a changed term shows after the next sign-in.
- The token check does not verify signatures yet (Foundation shortcut; Pipeline Built).
- "Point Dana to her chapter President by name" needs another person's record: use data\directory.db or the Keycloak admin API.
- The realm import file was written without being able to run Keycloak here; if the import fails, see the log from `docker compose logs keycloak`.
