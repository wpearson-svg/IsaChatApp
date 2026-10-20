# reset-keycloak.ps1 -- restarts Keycloak from a clean slate so it re-imports the isa realm.
#
# Normally started from one of two .bat files:
#   reset-keycloak.bat           resets Keycloak using the realm file already in keycloak\import
#   reset-keycloak-rebuild.bat   rebuilds that file first (python directory\build_keycloak_realm.py)
#
# "docker compose down -v" wipes the Keycloak data volume, then "docker compose up -d" starts it
# and Keycloak imports the realm.
param([switch]$Rebuild)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Fail($msg) { Write-Host "  [FAIL]  $msg" -ForegroundColor Red; exit 1 }
function Ok($msg)   { Write-Host "  [ OK ]  $msg" -ForegroundColor Green }

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Fail "docker was not found. Install and start Docker Desktop first."
}
docker info *> $null
if ($LASTEXITCODE -ne 0) { Fail "Docker is installed but not running. Start Docker Desktop and try again." }

if ($Rebuild) {
    $py = Get-Command python -ErrorAction SilentlyContinue
    if (-not $py) { $py = Get-Command py -ErrorAction SilentlyContinue }
    if (-not $py) { Fail "Python was not found, and the rebuild needs it. Use reset-keycloak.bat instead." }
    Write-Host "Rebuilding the realm file..."
    & $py.Source (Join-Path $PSScriptRoot 'directory\build_keycloak_realm.py')
    if ($LASTEXITCODE -ne 0) { Fail "build_keycloak_realm.py failed (see the messages above)." }
    Ok "Realm file rebuilt."
}

if (-not (Test-Path (Join-Path $PSScriptRoot 'keycloak\import\isa-realm.json'))) {
    Fail "keycloak\import\isa-realm.json is missing. Pull the latest changes, or run reset-keycloak-rebuild.bat."
}

Write-Host "Stopping Keycloak and wiping its data volume..."
docker compose down -v
if ($LASTEXITCODE -ne 0) { Fail "docker compose down -v failed." }

Write-Host "Starting Keycloak..."
docker compose up -d
if ($LASTEXITCODE -ne 0) { Fail "docker compose up -d failed." }

Write-Host "Waiting for the isa realm (first start can take up to a minute or two)..."
$url = 'http://localhost:8080/realms/isa/.well-known/openid-configuration'
$deadline = (Get-Date).AddSeconds(150)
while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3
        if ($r.StatusCode -eq 200) {
            Ok "Keycloak is up and the isa realm is loaded."
            Write-Host "Sign in at /sign-in with d.whitfield; norole.test should be refused."
            exit 0
        }
    } catch { }
    Start-Sleep -Seconds 3
}
Fail "The isa realm did not appear in time. Run: docker compose logs keycloak"
