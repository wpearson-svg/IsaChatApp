# Sign-in setup (Keycloak)

Author: Matthew Brady
Screen: `/sign-in` (Components/Pages/SignIn.razor)
Code that talks to Keycloak: `Services/KeycloakSignInService.cs`

The Isa sign-in screen is the app's own page, in ISACA colors. It sends the email address and password to the team's Keycloak server, so Keycloak still owns the users, passwords and roles. This note lists what the Keycloak setup needs for the screen to work.

---

## 1. How sign-in works

1. The person types their email address and password on `/sign-in`.
2. The app sends them to Keycloak's token endpoint (`/realms/isa/protocol/openid-connect/token`, grant type `password`). The app never stores the password.
3. Keycloak sends back an access token and a refresh token. The app keeps them in server memory under a random session ID.
4. The browser gets an encrypted cookie (`Isa.SignIn`) with the person's ID, name, email, roles and that session ID. It lasts 8 hours and renews while the person is active.
5. When the person asks Isa a question, the chat sends the access token to the pipeline as `Bearer <token>`. The `access_control` stage checks it as before. The app refreshes the token with Keycloak when it is about to expire.
6. "Sign out" ends the Keycloak session and clears the cookie.

People sign in with their **email address**, never their name, so it works the same in every country. Passwords can use any characters.

## 2. App settings (appsettings.json)

```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/isa",
  "ClientId": "isa-web",
  "ClientSecret": ""
}
```

- `Authority` is the realm address. It is the same value the access-control stage already uses to check the token issuer.
- `ClientId` must match the client name in Keycloak. `isa-web` is a suggested name. Change it if the client is named something else.
- `ClientSecret` stays empty for a public client (recommended for the prototype, see below). If the client has "Client authentication" turned on, put the secret in `appsettings.Development.json` on your own computer, not in `appsettings.json`, so it never goes to GitHub.

## 3. Keycloak client settings

In the Keycloak admin console (http://localhost:8080), realm `isa`, go to **Clients** and either create a client or edit the existing one:

| Setting | Value |
|---|---|
| Client ID | `isa-web` (or whatever is in appsettings.json) |
| Client authentication | Off (public client), for the prototype |
| Standard flow | On (leave on; needed if we move to Keycloak's own login page later) |
| **Direct access grants** | **On** (required: this is what lets the Isa screen send the email and password to Keycloak) |

If "Direct access grants" is off, the sign-in screen shows "Sign-in isn't set up on this server yet", and the app log shows `unauthorized_client`.

## 4. Realm settings

**Realm settings, Login tab:**

- **Login with email**: On (it is on by default). This lets people use their email address as their sign-in name.
- **Duplicate emails**: Off, so every email address belongs to only one person.

## 5. Test users

For each test user (Users, Add user):

- **Email**: the person's example.org email address from our synthetic test group (sign-in uses this).
- **Email verified**: On.
- **Credentials** tab: set a password, with "Temporary" **off** (a temporary password makes Keycloak ask for a new one, which this screen can't show yet).
- **Role mapping**: give `chat_user` to everyone, plus `dashboard_dev`, `dashboard_content_health` or `auditor` as needed (role names from docs/stage-contract.md).

## 6. Greeting people by name (optional but recommended)

After sign-in, the landing page says "Welcome, ..." using the person's **preferred form of address** in their own script, for example "Welcome, 田中様" or "Welcome, Frau Müller". The app reads these token claims, in this order:

| Claim | What it holds | Example |
|---|---|---|
| `form_of_address` | How Isa should greet them | `田中様` |
| `display_name` | Full name as written, in their own script | `田中 美咲` |
| `name` | Keycloak's built-in first + last name | `Misaki Tanaka` |
| `email` | Last fallback | `misaki.tanaka@example.org` |

`form_of_address` and `display_name` are custom. To add them:

1. **Realm settings, User profile**: add attributes `display_name` and `form_of_address`. (On older Keycloak versions, use the user's Attributes tab instead.)
2. For each test user, fill them in from our synthetic person records.
3. **Client scopes, profile, Mappers, Add mapper, By configuration, User Attribute**: one mapper per attribute. User Attribute = `display_name`, Token Claim Name = `display_name`, Claim JSON Type = String, Add to access token = On. Repeat for `form_of_address`.

Without these, the greeting falls back to the built-in name, then the email.

The app also reads `locale` (Keycloak's built-in language attribute, for example `ja-JP`) and keeps it with the sign-in so Isa can use it later for language.

## 7. Developer mode note

`appsettings.Development.json` has `AccessControl:DevBypass = true`. With it on, the chat answers whether or not anyone is signed in. The sign-in screen still really checks with Keycloak. Set it to `false` to test that the chat refuses questions until someone signs in.

## 8. Quick test

1. Keycloak running, client and one test user set up as above.
2. Start Isa and open http://localhost:5067.
3. Click **Sign in**, enter the test user's email and password. You should land back on the home page with "Welcome, ..." in the top bar.
4. A wrong password should show: "That email address and password don't match an Isa account."
5. With Keycloak stopped, signing in should show: "The sign-in service isn't reachable right now."
6. With DevBypass off: ask Isa a question while signed in (answered) and after signing out (asked to sign in).

## 9. Later

- For production, ISACA may prefer Keycloak's own hosted login page (the standard "redirect" flow), styled with a Keycloak theme that matches this screen. That also unlocks "forgot password", two-step verification and sign-in with ISACA's existing accounts. The Isa app can switch to that without changing the rest of the pipeline.
- The screen's text is all in one place (the `Text` section of SignIn.razor) so it can be translated.
