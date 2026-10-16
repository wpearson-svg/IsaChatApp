# Waits up to 7 minutes for port 5067 to accept connections (the app is up).
# Exits 0 when it does. Exits 1 (after printing the log tails) if the app
# process dies first or the time runs out.
$root = $PSScriptRoot
$pidFile = Join-Path $root 'isa.pid'
$outLog = Join-Path $root 'isa-run.log'
$errLog = Join-Path $root 'isa-run.err.log'
$deadline = (Get-Date).AddSeconds(420)
$started = Get-Date
$lastNote = Get-Date

function Show-Tails {
    Write-Host ""
    Write-Host "---- isa-run.err.log (last 25 lines) ----" -ForegroundColor Yellow
    if (Test-Path $errLog) { Get-Content $errLog -Tail 25 }
    Write-Host "---- isa-run.log (last 25 lines) ----" -ForegroundColor Yellow
    if (Test-Path $outLog) { Get-Content $outLog -Tail 25 }
    Write-Host "-------------------------------------------"
}

while ((Get-Date) -lt $deadline) {
    # Is the port accepting connections? (Instant. Do NOT request the home page: it can take several
    # seconds because the chat widget waits on unreachable Ollama hosts, which made this check time out.)
    try {
        $c = New-Object System.Net.Sockets.TcpClient
        $iar = $c.BeginConnect('127.0.0.1', 5067, $null, $null)
        $ok = $iar.AsyncWaitHandle.WaitOne(1000) -and $c.Connected
        $c.Close()
        if ($ok) { exit 0 }
    } catch { }
    if (Test-Path $pidFile) {
        $p = [int](Get-Content $pidFile -Raw)
        if (-not (Get-Process -Id $p -ErrorAction SilentlyContinue)) {
            Write-Host "The app process (PID $p) stopped before the site came up." -ForegroundColor Red
            Show-Tails
            exit 1
        }
    }
    if (((Get-Date) - $lastNote).TotalSeconds -ge 15) {
        $secs = [int]((Get-Date) - $started).TotalSeconds
        Write-Host ("  still building/starting... {0}s (first run can take several minutes)" -f $secs)
        $lastNote = Get-Date
    }
    Start-Sleep -Seconds 3
}
Write-Host "Still no answer after 7 minutes. The app may still be starting -- try http://localhost:5067 in your browser." -ForegroundColor Yellow
Show-Tails
exit 1
