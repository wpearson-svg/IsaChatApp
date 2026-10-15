# Waits up to 3 minutes for http://localhost:5067 to answer.
# Exits 0 when it does. Exits 1 (after printing the log tails) if the app
# process dies first or the time runs out.
$root = $PSScriptRoot
$pidFile = Join-Path $root 'isa.pid'
$outLog = Join-Path $root 'isa-run.log'
$errLog = Join-Path $root 'isa-run.err.log'
$deadline = (Get-Date).AddSeconds(180)

function Show-Tails {
    Write-Host ""
    Write-Host "---- isa-run.err.log (last 25 lines) ----" -ForegroundColor Yellow
    if (Test-Path $errLog) { Get-Content $errLog -Tail 25 }
    Write-Host "---- isa-run.log (last 25 lines) ----" -ForegroundColor Yellow
    if (Test-Path $outLog) { Get-Content $outLog -Tail 25 }
    Write-Host "-------------------------------------------"
}

while ((Get-Date) -lt $deadline) {
    try {
        $r = Invoke-WebRequest -Uri 'http://localhost:5067' -UseBasicParsing -TimeoutSec 3
        if ($r.StatusCode -ge 200) { exit 0 }
    } catch {
        if ($_.Exception.Response) { exit 0 }   # got an HTTP answer (even an error page) -> it is up
    }
    if (Test-Path $pidFile) {
        $p = [int](Get-Content $pidFile -Raw)
        if (-not (Get-Process -Id $p -ErrorAction SilentlyContinue)) {
            Write-Host "The app process (PID $p) stopped before the site came up." -ForegroundColor Red
            Show-Tails
            exit 1
        }
    }
    Start-Sleep -Seconds 3
}
Write-Host "Timed out after 3 minutes waiting for http://localhost:5067." -ForegroundColor Red
Show-Tails
exit 1
