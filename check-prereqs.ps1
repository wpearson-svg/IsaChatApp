# check-prereqs.ps1 -- run by start-isa.bat before the app starts.
# Exit code 0 = OK to start. Exit code 1 = stop (something required is missing,
# or the user chose not to continue after a warning).
# Nothing is installed automatically; this only reports what is missing.

$ErrorActionPreference = 'SilentlyContinue'
$hardFail = $false
$warnings = New-Object System.Collections.ArrayList

function Show-Ok($msg)   { Write-Host ("  [ OK ]  " + $msg) -ForegroundColor Green }
function Show-Fail($msg) { Write-Host ("  [FAIL]  " + $msg) -ForegroundColor Red }
function Show-Warn($msg) { Write-Host ("  [WARN]  " + $msg) -ForegroundColor Yellow }
function Show-Info($msg) { Write-Host ("  [info]  " + $msg) }

# Returns $null if the host is unreachable, otherwise the list of models it has
# (which may be empty).
function Get-OllamaModels($baseUrl) {
    try {
        $r = Invoke-RestMethod -Uri ($baseUrl + '/api/tags') -TimeoutSec 3 -ErrorAction Stop
        $m = @($r.models | Where-Object { $_ })
        return ,$m
    } catch {
        return $null
    }
}

Write-Host ""
Write-Host "Checking what IsaChatApp needs before it starts..."
Write-Host ""

# --- 1. .NET SDK 10 or newer (REQUIRED) ---------------------------------
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$sdkOk = $false
$sdkList = @()
if ($dotnet) {
    $sdkList = @(& dotnet --list-sdks 2>$null)
    foreach ($line in $sdkList) {
        if ($line -match '^(\d+)\.') {
            if ([int]$Matches[1] -ge 10) { $sdkOk = $true }
        }
    }
}
if ($sdkOk) {
    Show-Ok ".NET SDK 10 or newer is installed."
} elseif ($dotnet) {
    $hardFail = $true
    Show-Fail ".NET is installed, but no SDK version 10 or newer was found."
    if ($sdkList.Count -gt 0) { Show-Info ("SDKs found: " + ($sdkList -join '; ')) }
    Show-Info "Install the .NET 10 SDK (the SDK, not just the runtime):"
    Show-Info "https://dotnet.microsoft.com/download/dotnet/10.0"
} else {
    $hardFail = $true
    Show-Fail ".NET SDK is not installed (the 'dotnet' command was not found)."
    Show-Info "Install the .NET 10 SDK, then close this window and run start-isa.bat again:"
    Show-Info "https://dotnet.microsoft.com/download/dotnet/10.0"
}

# --- 2. Port 5067 free (REQUIRED) ---------------------------------------
$portBusy = $false
$net = @(netstat -ano | Select-String ':5067\s' | Select-String 'LISTENING')
if ($net.Count -gt 0) { $portBusy = $true }
if ($portBusy) {
    $hardFail = $true
    Show-Fail "Port 5067 is already in use (IsaChatApp may already be running)."
    Show-Info "Run stop-isa.bat first, then start-isa.bat again."
} else {
    Show-Ok "Port 5067 is free."
}

# --- 3. Ollama with at least one model (needed for chat to work) --------
# 'remote' is the project's shared Ollama host; it is only reachable for people
# connected to that network and is optional. If its address changes, update it
# here and in Components/Layout/MainLayout.razor.
$localModels  = Get-OllamaModels 'http://localhost:11434'
$remoteModels = Get-OllamaModels 'http://100.89.6.69:11434'
$localCount  = 0; if ($null -ne $localModels)  { $localCount  = $localModels.Count }
$remoteCount = 0; if ($null -ne $remoteModels) { $remoteCount = $remoteModels.Count }

if ($localCount -gt 0) {
    Show-Ok ("Ollama is running on this computer with " + $localCount + " model(s).")
}
if ($remoteCount -gt 0) {
    Show-Ok ("Shared Ollama host is reachable with " + $remoteCount + " model(s).")
}
if (($localCount -eq 0) -and ($remoteCount -eq 0)) {
    if ($null -ne $localModels) {
        [void]$warnings.Add("Ollama is running but has no models yet.")
        Show-Warn "Ollama is running but has no models yet."
        Show-Info "Pull at least one, for example:  ollama pull qwen2.5-coder:1.5b-instruct"
    } elseif (Get-Command ollama -ErrorAction SilentlyContinue) {
        [void]$warnings.Add("Ollama is installed but not running.")
        Show-Warn "Ollama is installed but not running."
        Show-Info "Open the Ollama app (or run:  ollama serve ), then start again."
    } else {
        [void]$warnings.Add("Ollama is not installed and no shared host is reachable.")
        Show-Warn "Ollama is not installed, and no shared Ollama host is reachable."
        Show-Info "The app will start, but the chat cannot answer without Ollama."
        Show-Info "Install it from https://ollama.com/download then pull a model, for example:"
        Show-Info "ollama pull qwen2.5-coder:1.5b-instruct"
    }
}
if (($remoteCount -eq 0) -and ($localCount -gt 0)) {
    Show-Info "Shared Ollama host not reachable. That is fine; the app will use your local Ollama."
}

# --- 4. Internet for the first build (NuGet package download) -----------
if (-not (Test-Path (Join-Path $PSScriptRoot 'obj\project.assets.json'))) {
    $online = $false
    try {
        $null = Invoke-WebRequest -Uri 'https://api.nuget.org/v3/index.json' -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        $online = $true
    } catch { }
    if ($online) {
        Show-Ok "Internet access to NuGet is available for the first build."
    } else {
        [void]$warnings.Add("Cannot reach NuGet for the first build.")
        Show-Warn "Cannot reach api.nuget.org. The first build downloads packages and needs internet."
    }
}

Write-Host ""

if ($hardFail) {
    Write-Host "Something required is missing (see [FAIL] above)." -ForegroundColor Red
    exit 1
}

if ($warnings.Count -gt 0) {
    $answer = Read-Host "There are warnings above. Start anyway? (Y/N)"
    if ($answer -notmatch '^[Yy]') { exit 1 }
}

exit 0
