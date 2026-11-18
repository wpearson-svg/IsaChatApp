# start-isa.ps1 -- "Start Isa": starts the services, then Isa, then opens her in the browser (installed copy).
# Runs first-run.ps1 first if this computer hasn't been set up yet.

. (Join-Path $PSScriptRoot 'common.ps1')
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
try {
    Assert-Docker
    if (-not (Test-Path $Marker)) {
        Say 'This computer has not been set up yet: running the set-up first.'
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'first-run.ps1')
        if ($LASTEXITCODE -ne 0) { throw 'The set-up did not finish. See logs\first-run.log.' }
    }

    Say 'Starting the services ...'
    Invoke-Compose @('up', '-d')
    if (-not (Wait-Services 300)) { Say 'Some services are not ready; Isa starts anyway and her Health page says which ones.' }

    # Isa herself, unless she is already running
    $running = $false
    if (Test-Path $PidFile) {
        $p = Get-Process -Id ([int](Get-Content $PidFile)) -ErrorAction SilentlyContinue
        if ($p -ne $null -and $p.ProcessName -eq 'IsaChatApp') { $running = $true }
    }
    if (-not $running) {
        Say 'Starting Isa ...'
        $env:ASPNETCORE_ENVIRONMENT = 'Production'
        $p = Start-Process -FilePath (Join-Path $AppDir 'IsaChatApp.exe') -ArgumentList @('--urls', $AppUrl) `
            -WorkingDirectory $AppDir -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput (Join-Path $LogDir 'isa.log') -RedirectStandardError (Join-Path $LogDir 'isa-errors.log')
        Set-Content -Path $PidFile -Value $p.Id -Encoding ASCII
    }
    if (Wait-Url 'Isa' $AppUrl 120) { Start-Process $AppUrl; Say 'Isa is open in your browser. You can close this window.' }
    else { Say 'Isa did not answer. See logs\isa-errors.log.' }
}
catch {
    Say $_.Exception.Message
    Read-Host 'Press Enter to close'
    exit 1
}
Start-Sleep -Seconds 3
