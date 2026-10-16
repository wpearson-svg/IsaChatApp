# Dev test: sign in as a demo account against the local Keycloak, then ask Isa a question with that token.
# Usage:  .\test-ask.ps1 -User d.whitfield -Question "How many board meetings must a chapter hold each year?"
# Passwords come from keycloak\dev-passwords.local.csv (git-ignored). Needs Keycloak up and the app running.
# Turn AccessControl:DevBypass OFF first, or the app ignores the token.
param(
    [Parameter(Mandatory=$true)][string]$User,
    [string]$Question = "How many board meetings must a chapter hold each year?",
    [string]$App = "http://localhost:5067",
    [string]$Keycloak = "http://localhost:8080"
)
$pwFile = Join-Path $PSScriptRoot "keycloak\dev-passwords.local.csv"
if (-not (Test-Path $pwFile)) { Write-Host "No $pwFile - run: python directory\build_keycloak_realm.py" -ForegroundColor Red; exit 1 }
$row = Import-Csv -Path $pwFile -Encoding UTF8 | Where-Object { $_.username -eq $User }
if (-not $row) { Write-Host "No password on file for '$User'." -ForegroundColor Red; exit 1 }

try {
    $tok = Invoke-RestMethod -Method Post -Uri "$Keycloak/realms/isa/protocol/openid-connect/token" -Body @{
        grant_type = "password"; client_id = "isa-chat"; client_secret = "dev-only-placeholder-secret"
        username = $User; password = $row.password } -ContentType "application/x-www-form-urlencoded"
} catch {
    Write-Host "Keycloak sign-in failed for '$User' (wrong password, unknown user, or Keycloak not ready): $($_.Exception.Message)" -ForegroundColor Red; exit 1
}
Write-Host "Signed in as $User." -ForegroundColor Green
try {
    $body = @{ question = $Question } | ConvertTo-Json
    $res = Invoke-RestMethod -Method Post -Uri "$App/api/ask" -ContentType "application/json; charset=utf-8" `
        -Headers @{ Authorization = "Bearer $($tok.access_token)" } -Body ([System.Text.Encoding]::UTF8.GetBytes($body))
    $res | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Ask failed: HTTP $([int]$_.Exception.Response.StatusCode) $($_.Exception.Message)" -ForegroundColor Yellow
}
