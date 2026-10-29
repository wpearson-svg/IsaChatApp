# Dev test: does Isa refuse tokens that are not really from Keycloak, and does it keep working when Keycloak's keys change?
# Added 28 October 2026 with the signature check in Services/AccessControlAdapter.cs.
#
# Usage (Isa and Keycloak running, AccessControl:DevBypass OFF):
#   .\test-tokens.ps1 -User d.whitfield                    forged and tampered tokens: every one must be refused (401)
#   .\test-tokens.ps1 -User d.whitfield -Mode Restart      restarts the Keycloak container, then checks a fresh token still works
#   .\test-tokens.ps1 -User d.whitfield -Mode Reset        you click "Reset Keycloak" in the launcher (new keys), then it checks again
#
# The demo account's password is read from the git-ignored keycloak\dev-passwords file, as test-ask.ps1 does.
# Nothing here prints a token or a password.
param(
    [Parameter(Mandatory=$true)][string]$User,
    [ValidateSet("Forged","Restart","Reset")][string]$Mode = "Forged",
    [string]$App = "http://localhost:5067",
    [string]$Keycloak = "http://localhost:8080"
)

$script:fail = 0
function Pass($t) { Write-Host "[ PASS ] $t" -ForegroundColor Green }
function Fail($t) { Write-Host "[ FAIL ] $t" -ForegroundColor Red; $script:fail++ }
function Info($t) { Write-Host "[ info ] $t" -ForegroundColor Gray }

function B64Url([byte[]]$bytes) { [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_') }
function B64UrlText([string]$text) { B64Url ([System.Text.Encoding]::UTF8.GetBytes($text)) }
function FromB64Url([string]$s) {
    $s = $s.Replace('-','+').Replace('_','/'); switch ($s.Length % 4) { 2 { $s += '==' } 3 { $s += '=' } }
    [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($s))
}

# Sends a question with the given token; returns the HTTP status (200, 401, 403 ...) or 0 if Isa is not reachable.
function Ask($token) {
    try {
        $body = @{ question = "How many board meetings must a chapter hold each year?" } | ConvertTo-Json
        $headers = @{}
        if ($token) { $headers["Authorization"] = "Bearer $token" }
        $null = Invoke-RestMethod -Method Post -Uri "$App/api/ask" -ContentType "application/json; charset=utf-8" `
            -Headers $headers -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 120
        return 200
    } catch {
        if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }
        return 0
    }
}

# A real token for the demo account (never shown).
function Get-RealToken {
    $pwFile = Join-Path $PSScriptRoot "keycloak\dev-passwords.local.csv"
    $row = $null
    if (Test-Path $pwFile) { $row = Import-Csv -Path $pwFile -Encoding UTF8 | Where-Object { $_.username -eq $User } }
    if (-not $row) { return $null }
    try {
        $tok = Invoke-RestMethod -Method Post -Uri "$Keycloak/realms/isa/protocol/openid-connect/token" -Body @{
            grant_type = "password"; client_id = "isa-chat"; client_secret = "dev-only-placeholder-secret"
            username = $User; password = $row.password }
        return $tok.access_token
    } catch { return $null }
}

function Wait-Keycloak([int]$seconds = 180) {
    $until = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $until) {
        try { $null = Invoke-RestMethod -Uri "$Keycloak/realms/isa/.well-known/openid-configuration" -TimeoutSec 3; return $true } catch { Start-Sleep -Seconds 3 }
    }
    return $false
}

# ---- Start: Isa must be up, and a real token must work ----
$real = Get-RealToken
if (-not $real) { Write-Host "Could not get a token for '$User' (is Keycloak up, and is the account in keycloak\dev-passwords.local.csv?)." -ForegroundColor Red; exit 1 }
$s = Ask $real
if ($s -eq 0) { Write-Host "Isa is not reachable at $App." -ForegroundColor Red; exit 1 }
if ($s -eq 200) { Pass "A real token is accepted (HTTP 200)" } else { Fail "A real token was refused (HTTP $s). Is AccessControl:DevBypass off, and is Keycloak:Authority right?" }

if ($Mode -eq "Forged") {
    $parts = $real.Split('.')
    $payloadJson = FromB64Url $parts[1]
    $claims = $payloadJson | ConvertFrom-Json
    $iss = $claims.iss
    $exp = [int][double]::Parse(([DateTimeOffset]::UtcNow.AddHours(1).ToUnixTimeSeconds()).ToString())
    $fake = @{ iss = $iss; sub = "00000000-forged"; azp = "isa-web"; preferred_username = $User; exp = $exp; iat = ([int]$exp - 3600)
               realm_access = @{ roles = @("chat_user","dev_dashboard") } } | ConvertTo-Json -Compress -Depth 5
    $payload = B64UrlText $fake

    # 1. No signature at all, alg "none".
    $t1 = (B64UrlText '{"alg":"none","typ":"JWT"}') + "." + $payload + "."
    # 2. Signed with a key we invented (HS256), claims copied from what a real token would say.
    $hs = New-Object System.Security.Cryptography.HMACSHA256 (,[System.Text.Encoding]::UTF8.GetBytes("not-keycloaks-key-at-all"))
    $h2 = B64UrlText '{"alg":"HS256","typ":"JWT"}'
    $t2 = "$h2.$payload." + (B64Url $hs.ComputeHash([System.Text.Encoding]::UTF8.GetBytes("$h2.$payload")))
    # 3. Looks like a Keycloak RS256 token (the real header, so the real key id), but the signature is garbage.
    $t3 = $parts[0] + "." + $payload + "." + (B64Url (New-Object byte[] 256))
    # 4. A real token with its contents changed after signing (a different person), signature left as it was.
    $c4 = $claims | ConvertTo-Json -Depth 10 -Compress | ConvertFrom-Json
    $c4.sub = "00000000-tampered"
    $t4 = $parts[0] + "." + (B64UrlText ($c4 | ConvertTo-Json -Depth 10 -Compress)) + "." + $parts[2]
    # 5. A real token with its last character changed.
    $last = $real[$real.Length - 1]; $swap = if ($last -eq 'A') { 'B' } else { 'A' }
    $t5 = $real.Substring(0, $real.Length - 1) + $swap
    # 6. Nothing but junk.
    $t6 = "not.a.token"

    $cases = @(
        @("unsigned token (alg none)", $t1), @("signed with a made-up key (HS256)", $t2),
        @("real header, garbage signature", $t3), @("real token with the contents changed", $t4),
        @("real token with one character changed", $t5), @("junk instead of a token", $t6), @("no token at all", $null))
    foreach ($c in $cases) {
        $status = Ask $c[1]
        if ($status -eq 401) { Pass "Refused: $($c[0]) (401)" } else { Fail "NOT refused: $($c[0]) (HTTP $status, expected 401)" }
    }
}
else {
    if ($Mode -eq "Restart") {
        Info "Restarting the Keycloak container (docker compose restart keycloak)..."
        docker compose restart keycloak | Out-Null
    } else {
        Write-Host ""
        Write-Host "Now click 'Reset Keycloak' in the launcher (this gives Keycloak new signing keys)." -ForegroundColor Yellow
        Read-Host "Press Enter here when the reset has finished"
    }
    if (-not (Wait-Keycloak)) { Write-Host "Keycloak did not come back within 3 minutes." -ForegroundColor Red; exit 1 }
    Pass "Keycloak is back"

    # The old token: after a plain restart the keys are kept, so it may still be valid; after a reset the keys are new, so it must be refused.
    $old = Ask $real
    if ($Mode -eq "Reset") { if ($old -eq 401) { Pass "The old token is refused after the reset (401)" } else { Fail "The old token was still accepted after the reset (HTTP $old)" } }
    else { Info "The old token after a plain restart: HTTP $old (either is fine: the keys are kept)" }

    # The important one: a brand new token, signed with whatever keys Keycloak has now, must work without restarting Isa.
    $fresh = Get-RealToken
    if (-not $fresh) { Fail "Could not sign in again after Keycloak came back" }
    else {
        $status = Ask $fresh
        if ($status -eq 200) { Pass "A new token works without restarting Isa (HTTP 200): the keys were refreshed" }
        else { Fail "A new token was refused (HTTP $status). Isa did not pick up Keycloak's new keys; check the Isa log for 'signing keys'." }
    }
}

Write-Host ""
if ($script:fail -eq 0) { Write-Host "All checks passed." -ForegroundColor Green; exit 0 }
Write-Host "$($script:fail) check(s) failed." -ForegroundColor Red; exit 1
