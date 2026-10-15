$outLog = Join-Path $PSScriptRoot 'isa-run.log'
$errLog = Join-Path $PSScriptRoot 'isa-run.err.log'
$pidFile = Join-Path $PSScriptRoot 'isa.pid'
$csproj = Join-Path $PSScriptRoot 'IsaChatApp.csproj'

if (-not (Test-Path $csproj)) {
    Write-Error "Cannot find $csproj."
    exit 1
}

# Launch the same way "dotnet run" from this folder does manually (which is
# known to work), just hidden and in the background instead of in a visible
# console. This matches the dev workflow exactly: same working directory,
# same environment (Development, from launchSettings.json), so wwwroot and
# the static asset dev-time patching resolve the same way they do when run
# by hand. No separate build/publish step needed -- "dotnet run" builds
# itself.
try {
    $p = Start-Process -FilePath 'dotnet' `
        -ArgumentList @('run', '--project', "`"$csproj`"", '-c', 'Release', '--urls', 'http://localhost:5067') `
        -WorkingDirectory $PSScriptRoot `
        -WindowStyle Hidden `
        -PassThru `
        -RedirectStandardOutput $outLog `
        -RedirectStandardError $errLog

    $p.Id | Out-File -Encoding ascii -NoNewline $pidFile
    Write-Host "Started IsaChatApp, PID $($p.Id)"
} catch {
    Write-Error "Failed to start IsaChatApp: $_"
    exit 1
}
