#!/usr/bin/env python3
"""Generate keycloak/import/isa-realm.json from the CSV files in this folder.

Docker (docker-compose.yml) imports it on first start, so the realm, client, roles,
token mappers and accounts exist without clicking through the admin console.

Passwords: dev-only. If keycloak/dev-passwords.local.csv (username,password) exists it is
used (put Matt's sheet values there). Any account missing from it gets a random password
that is appended to that file. The file and the generated realm file are git-ignored.
Usage: python directory/build_keycloak_realm.py [--today YYYY-MM-DD]"""
import csv, json, os, secrets, sys
from datetime import date

here = os.path.dirname(os.path.abspath(__file__))
root = os.path.dirname(here)
kc_dir = os.path.join(root, "keycloak")
out_path = os.path.join(kc_dir, "import", "isa-realm.json")
pw_path = os.path.join(kc_dir, "dev-passwords.local.csv")
today = date.fromisoformat(sys.argv[sys.argv.index("--today") + 1]) if "--today" in sys.argv else date.today()

def rows(path):
    with open(path, encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))

people = {p["Person ID"]: p for p in rows(os.path.join(here, "people.csv"))}
assignments = rows(os.path.join(here, "role_assignments.csv"))
accounts = rows(os.path.join(here, "keycloak_accounts.csv"))

ROLES = ["chat_user", "dashboard_dev", "dashboard_content_health", "auditor"]
# Attributes that go into the access token as claims (read by KeycloakAccessControlAdapter).
TOKEN_ATTRS = ["person_id", "chapter_id", "chapter_role", "permission", "term_start", "term_end",
               "years_as_leader", "language", "formality", "form_of_address"]

def current_assignment(pid):
    live = [a for a in assignments if a["Person ID"] == pid and a["Term start"] <= today.isoformat() <= a["Term end"]]
    return max(live, key=lambda a: a["Term start"]) if live else None

def split_name(short):
    parts = short.split()
    return (parts[0], " ".join(parts[1:]) or parts[0]) if parts else ("", "")

# ---- passwords (dev only) ----
os.makedirs(os.path.join(kc_dir, "import"), exist_ok=True)
passwords = {}
if os.path.exists(pw_path):
    for r in rows(pw_path): passwords[r["username"]] = r["password"]
new_pw = {}
for a in accounts:
    if a["username"] not in passwords:
        passwords[a["username"]] = new_pw[a["username"]] = secrets.token_urlsafe(12)
if new_pw:
    exists = os.path.exists(pw_path)
    with open(pw_path, "a", encoding="utf-8", newline="") as f:
        w = csv.writer(f)
        if not exists: w.writerow(["username", "password"])
        for u, p in new_pw.items(): w.writerow([u, p])

# ---- users ----
users = []
for a in accounts:
    attrs, first, last, email = {}, a["first_name"], a["last_name"], f"{a['username']}@example.org"
    if a["person_id"]:
        p = people[a["person_id"]]
        first, last = split_name(p["Short name"])
        email = p["Email"]
        cur = current_assignment(a["person_id"])
        attrs = {"person_id": p["Person ID"], "language": p["Language"], "formality": p["Formality"],
                 "form_of_address": p["Form of address"], "years_as_leader": p["Years as leader"]}
        if cur:
            attrs.update(chapter_id=cur["Chapter ID"], chapter_role=cur["Role"], permission=cur["Permission"],
                         term_start=cur["Term start"], term_end=cur["Term end"])
        history = [{"role": x["Role"], "chapter": x["Chapter ID"], "start": x["Term start"], "end": x["Term end"]}
                   for x in assignments if x["Person ID"] == a["person_id"]]
        attrs["role_history"] = json.dumps(history, ensure_ascii=False)   # stored, not put in the token
    roles = [r for r in a["client_roles"].split(";") if r]
    bad = [r for r in roles if r not in ROLES]
    if bad: sys.exit(f"{a['username']}: unknown role {bad}")
    u = {"username": a["username"], "enabled": True, "emailVerified": True, "email": email,
         "firstName": first, "lastName": last,
         "attributes": {k: [v] for k, v in attrs.items()},
         "credentials": [{"type": "password", "value": passwords[a["username"]], "temporary": False}],
         "requiredActions": []}
    if roles: u["clientRoles"] = {"isa-chat": roles}
    users.append(u)

# ---- user profile (declares the standard fields and allows our extra attributes) ----
std = lambda name, display, validations, required: {
    "name": name, "displayName": display, "validations": validations,
    **({"required": {"roles": ["user"]}} if required else {}),
    "permissions": {"view": ["admin", "user"], "edit": ["admin", "user"]}, "multivalued": False}
profile = {
    "attributes": [
        std("username", "${username}", {"length": {"min": 3, "max": 255}, "username-prohibited-characters": {}, "up-username-not-idn-homograph": {}}, False),
        std("email", "${email}", {"email": {}, "length": {"max": 255}}, True),
        std("firstName", "${firstName}", {"length": {"max": 255}, "person-name-prohibited-characters": {}}, True),
        std("lastName", "${lastName}", {"length": {"max": 255}, "person-name-prohibited-characters": {}}, True),
    ],
    "groups": [{"name": "user-metadata", "displayHeader": "User metadata", "displayDescription": "Attributes, which refer to user metadata"}],
    "unmanagedAttributePolicy": "ENABLED",
}

mapper = lambda attr: {
    "name": attr, "protocol": "openid-connect", "protocolMapper": "oidc-usermodel-attribute-mapper",
    "consentRequired": False,
    "config": {"user.attribute": attr, "claim.name": attr, "jsonType.label": "String",
               "id.token.claim": "true", "access.token.claim": "true",
               "userinfo.token.claim": "true", "introspection.token.claim": "true"}}

realm = {
    "realm": "isa", "enabled": True, "sslRequired": "external",
    "registrationAllowed": False, "loginWithEmailAllowed": True, "duplicateEmailsAllowed": False,
    "accessTokenLifespan": 3600,
    "roles": {"client": {"isa-chat": [{"name": r, "description": d} for r, d in zip(ROLES, [
        "Base role: ordinary chat access (everyone gets it)", "Sees the Developer Dashboard link",
        "Sees the Content Health Dashboard link", "Review role"])]}},
    "clients": [{
        "clientId": "isa-chat", "name": "Isa chat", "enabled": True, "protocol": "openid-connect",
        "publicClient": False, "secret": "dev-only-placeholder-secret",
        "standardFlowEnabled": True,
        "directAccessGrantsEnabled": True,     # DEV ONLY: lets test scripts get a token with a username + password
        "rootUrl": "http://localhost:5067",
        "redirectUris": ["http://localhost:5067/signin-oidc", "https://localhost:7234/signin-oidc"],
        "webOrigins": ["http://localhost:5067"],
        "attributes": {"post.logout.redirect.uris": "http://localhost:5067/signout-callback-oidc##https://localhost:7234/signout-callback-oidc"},
        "protocolMappers": [mapper(a) for a in TOKEN_ATTRS],
    }],
    "components": {"org.keycloak.userprofile.UserProfileProvider": [{
        "providerId": "declarative-user-profile", "subComponents": {},
        "config": {"kc.user.profile.config": [json.dumps(profile)]}}]},
    "users": users,
}
with open(out_path, "w", encoding="utf-8") as f: json.dump(realm, f, indent=2, ensure_ascii=False)
print(f"wrote {out_path}: {len(users)} users, {len(TOKEN_ATTRS)} token claims (current date used for terms: {today})")
if new_pw: print(f"generated {len(new_pw)} dev passwords -> {pw_path} (not shown here; file is git-ignored)")
