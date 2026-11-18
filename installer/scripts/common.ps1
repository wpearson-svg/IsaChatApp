# common.ps1 -- shared by first-run.ps1, start-isa.ps1 and stop-isa.ps1 (Isa, installed copy, for demonstrations).
# Windows PowerShell 5.1. The install folder looks like:
#   <root>\app\        IsaChatApp.exe and the files it reads (data, content, rules, presentation, ...)
#   <root>\services\   docker-compose.yml, docker-compose.gpu.yml, keycloak-import\, ai-services\
#   <root>\scripts\    these scripts
#   <root>\logs\       first-run.log, isa.log, isa-errors.log

$ErrorActionPreference = 'Stop'
$Root     = Split-Path -Parent $PSScriptRoot
$AppDir   = Join-Path $Root 'app'
$Services = Join-Path $Root 'services'
$Marker   = Join-Path $Root 'first-run.done'
$PidFile  = Join-Path $Root 'isa-app.pid'
$LogDir   = Join-Path $Root 'logs'
$AppUrl   = 'http://localhost:5067'
$Model    = 'phi4-mini:3.8b'

function Say([string]$text) { Write-Host ('[Isa] ' + $text) }

function Test-Gpu {
    try { $null = & nvidia-smi -L 2>$null; return ($LASTEXITCODE -eq 0) } catch { return $false }
}

function Get-ComposeArgs {
    $a = @('compose', '--project-directory', $Services, '-f', (Join-Path $Services 'docker-compose.yml'))
    if (Test-Gpu) { $a += @('-f', (Join-Path $Services 'docker-compose.gpu.yml')) }
    return $a
}

function Invoke-Compose([string[]]$more) {
    $a = (Get-ComposeArgs) + $more
    & docker @a
    if ($LASTEXITCODE -ne 0) { throw ('docker ' + ($more -join ' ') + ' failed (exit ' + $LASTEXITCODE + ')') }
}

function Assert-Docker {
    $ok = $false
    try { $null = & docker info --format '{{.ServerVersion}}' 2>$null; $ok = ($LASTEXITCODE -eq 0) } catch { $ok = $false }
    if (-not $ok) { throw 'Docker Desktop is not running. Start Docker Desktop, wait until it says it is running, then try again.' }
}

# Waits until a web address answers (any status below 500), or gives up after $seconds.
function Wait-Url([string]$name, [string]$url, [int]$seconds) {
    $deadline = (Get-Date).AddSeconds($seconds)
    Say ('Waiting for ' + $name + ' ...')
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -lt 500) { Say ($name + ' is ready.'); return $true }
        } catch {
            $resp = $_.Exception.Response
            if ($resp -ne $null -and [int]$resp.StatusCode -lt 500) { Say ($name + ' is ready.'); return $true }
        }
        Start-Sleep -Seconds 3
    }
    Say ($name + ' did not answer in ' + $seconds + ' seconds.')
    return $false
}

# The services Isa needs. The voice is optional: without it, Listen uses the computer's own voice.
function Wait-Services([int]$seconds) {
    $ok = $true
    if (-not (Wait-Url 'sign-in (Keycloak)' 'http://localhost:8080/realms/isa' $seconds)) { $ok = $false }
    if (-not (Wait-Url 'the AI model (Ollama)' 'http://localhost:11434/api/tags' $seconds)) { $ok = $false }
    if (-not (Wait-Url 'search by meaning' 'http://localhost:8092/health' $seconds)) { $ok = $false }
    if (-not (Wait-Url 'translation' 'http://localhost:8091/health' $seconds)) { $ok = $false }
    if (-not (Wait-Url 'the voice' 'http://localhost:8880/docs' 60)) { Say 'The voice is optional: Listen falls back to the computer''s own voice.' }
    return $ok
}
