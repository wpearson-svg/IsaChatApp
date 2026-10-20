# Changes since the last commit

A running list of every change made since Matthew's last commit (merge of main into Matthew's-Branch, 20 October 2026). Use it to write the Git commit message and to tell Willis what changed. When Matthew commits, this list is cleared and starts again.

Nothing has changed since that commit yet.

The last two commits, for reference:
- "Add show the work, succession notebook and three-panel layout; fit journey to five phases" (afc89d7): everything from 20 October.
- The merge of main into Matthew's-Branch: brought in Willis's launcher scripts, Salesforce and developer-keys settings on the Dev Dashboard, and Keycloak reset scripts. main now keeps the Keycloak sign-in setup in Git (keycloak/import/isa-realm.json and keycloak/dev-passwords.local.csv); main's copy had only the 5 original accounts, so the realm was rebuilt from directory/keycloak_accounts.csv with python directory\build_keycloak_realm.py (Willis said this was fine). It now has all 16 accounts, including the 11 chapter leaders; every other setting is identical to main's, and the 5 original passwords are unchanged. .gitignore follows main's version.

## Suggested commit message

```
(nothing to commit yet)
```
