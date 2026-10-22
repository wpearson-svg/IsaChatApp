# T1E: is phi4-mini installed and running through Ollama, how long does an answer take, and does it
# run while Keycloak and Isa are up on this machine. ASCII only (Windows PowerShell 5.1).
#
# Usage:  .\test-model.ps1                 1 cold run, then 5 timed warm runs
#         .\test-model.ps1 -Runs 8 -Model phi4-mini -OllamaUrl http://localhost:11434
#
# Run it with Isa and Keycloak (Docker) ALREADY STARTED, so the memory check means something.
# Results are appended to docs\MODEL_TIMING_RESULTS.csv.
param(
    [string]$Model = "phi4-mini",
    [string]$OllamaUrl = "http://localhost:11434",
    [int]$Runs = 5,
    [int]$TimeoutSeconds = 180
)
$ErrorActionPreference = "Stop"
function Test-Port([int]$port) {
    try {
        $c = New-Object System.Net.Sockets.TcpClient
        $r = $c.BeginConnect("localhost", $port, $null, $null)
        $ok = $r.AsyncWaitHandle.WaitOne(400)
        if ($ok) { try { $c.EndConnect($r) } catch { $ok = $false } }
        $c.Close(); return $ok
    } catch { return $false }
}
function Get-FreeMemGb { $os = Get-CimInstance Win32_OperatingSystem; return [math]::Round($os.FreePhysicalMemory / 1MB, 1) }
function Get-TotalMemGb { $os = Get-CimInstance Win32_OperatingSystem; return [math]::Round($os.TotalVisibleMemorySize / 1MB, 1) }

Write-Host "1. Services on this machine" -ForegroundColor Cyan
$svc = @(@("Isa app", 5067), @("Keycloak", 8080), @("Ollama", 11434))
foreach ($s in $svc) {
    if (Test-Port $s[1]) { Write-Host ("   up       {0} (port {1})" -f $s[0], $s[1]) -ForegroundColor Green }
    else { Write-Host ("   not up   {0} (port {1})" -f $s[0], $s[1]) -ForegroundColor Yellow }
}
Write-Host ("   Memory before: {0} GB free of {1} GB" -f (Get-FreeMemGb), (Get-TotalMemGb))
$upApp = Test-Port 5067; $upKc = Test-Port 8080

Write-Host "`n2. Ollama and the model" -ForegroundColor Cyan
try { $tags = Invoke-RestMethod -Uri "$OllamaUrl/api/tags" -TimeoutSec 10 }
catch { Write-Host "   FAIL  Cannot reach Ollama at $OllamaUrl. Start Ollama and run again." -ForegroundColor Red; exit 1 }
$found = @($tags.models | Where-Object { $_.name -eq $Model -or $_.name -like "$Model`:*" })
if ($found.Count -eq 0) {
    Write-Host "   FAIL  '$Model' is not installed. Run:  ollama pull $Model" -ForegroundColor Red
    Write-Host "   Installed: $((@($tags.models | ForEach-Object { $_.name })) -join ', ')"
    exit 2
}
$modelName = $found[0].name
Write-Host ("   PASS  {0} is installed ({1} GB on disk)" -f $modelName, [math]::Round($found[0].size / 1GB, 1)) -ForegroundColor Green

$passage = "Stand-in passage - Chapter Leader Guidebook, Guidebook page 7: The chapter secretary is responsible for documenting what the board decided. Decisions are recorded in the meeting minutes."
$question = "Who writes down what the board decided?"
$prompt = @"
Answer using only the passage below. If the passage does not answer the question, say exactly: The passage does not answer this.
Reply in this format and nothing else:
Answer: <one or two sentences>
Source: <document name, section and page of the passage you used>

Passage:
$passage

Question: $question
"@

function Invoke-Model {
    $body = @{ model = $modelName; prompt = $prompt; stream = $false; options = @{ temperature = 0 } } | ConvertTo-Json -Depth 5
    $sw = [Diagnostics.Stopwatch]::StartNew()
    try {
        $r = Invoke-RestMethod -Method Post -Uri "$OllamaUrl/api/generate" -ContentType "application/json; charset=utf-8" `
            -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec $TimeoutSeconds
        $sw.Stop()
        return @{ Ok = $true; Sec = $sw.Elapsed.TotalSeconds; Load = [double]$r.load_duration / 1e9; Tokens = [int]$r.eval_count; Text = [string]$r.response; Err = ""; Grounded = $false; Source = $false; Kind = ""; N = 0 }
    } catch {
        $sw.Stop()
        return @{ Ok = $false; Sec = $sw.Elapsed.TotalSeconds; Load = 0; Tokens = 0; Text = ""; Err = $_.Exception.Message; Grounded = $false; Source = $false; Kind = ""; N = 0 }
    }
}

Write-Host "`n3. Timing the miss-path question ('$question')" -ForegroundColor Cyan
# Cold run: unload the model first so this one includes loading it into memory.
try { Invoke-RestMethod -Method Post -Uri "$OllamaUrl/api/generate" -ContentType "application/json" -Body (@{ model = $modelName; keep_alive = 0 } | ConvertTo-Json) -TimeoutSec 30 | Out-Null; Start-Sleep -Seconds 2 } catch { }
$results = @()
for ($i = 0; $i -le $Runs; $i++) {
    $kind = "warm"; if ($i -eq 0) { $kind = "cold" }
    $r = Invoke-Model
    $r.Kind = $kind; $r.N = $i
    if ($r.Ok) {
        $r.Grounded = ($r.Text -match "(?i)secretary")
        $r.Source = ($r.Text -match "(?im)^\s*Source:\s*\S+")
        Write-Host ("   {0,-4} run {1}: {2,6:N1} s  (load {3:N1} s, {4} tokens)  secretary: {5}  Source line: {6}" -f $kind, $i, $r.Sec, $r.Load, $r.Tokens, $r.Grounded, $r.Source) -ForegroundColor Green
    } else {
        Write-Host ("   {0,-4} run {1}: FAILED after {2:N1} s: {3}" -f $kind, $i, $r.Sec, $r.Err) -ForegroundColor Red
    }
    $results += $r
    if ($i -eq 0 -and $r.Ok) { Write-Host ("        memory with the model loaded: {0} GB free" -f (Get-FreeMemGb)) }
}
$last = ($results | Where-Object { $_.Ok } | Select-Object -Last 1)
if ($last) { Write-Host "`nLast reply, as the model wrote it:" -ForegroundColor Cyan; Write-Host $last.Text }

Write-Host "`n4. Memory with everything up" -ForegroundColor Cyan
$free = Get-FreeMemGb
Write-Host ("   {0} GB free of {1} GB while the model is loaded" -f $free, (Get-TotalMemGb))
try { $ps = Invoke-RestMethod -Uri "$OllamaUrl/api/ps" -TimeoutSec 10; foreach ($m in $ps.models) { Write-Host ("   Ollama holds {0}: {1} GB in memory" -f $m.name, [math]::Round($m.size / 1GB, 1)) } } catch { }

$warm = @($results | Where-Object { $_.Ok -and $_.Kind -eq "warm" })
$cold = @($results | Where-Object { $_.Ok -and $_.Kind -eq "cold" })
$ok = @($results | Where-Object { $_.Ok }).Count
Write-Host "`n5. Summary" -ForegroundColor Cyan
if ($warm.Count -gt 0) {
    $times = @($warm | ForEach-Object { $_.Sec } | Sort-Object)
    $avg = ($times | Measure-Object -Average).Average; $max = $times[-1]
    $limit = [math]::Max(30, [math]::Ceiling($max * 2 / 5) * 5)
    Write-Host ("   Warm answers: average {0:N1} s, slowest {1:N1} s" -f $avg, $max)
    if ($cold.Count -gt 0) { Write-Host ("   Cold start (model not loaded): {0:N1} s" -f $cold[0].Sec) }
    Write-Host ("   Suggested time limit for the model call: {0} seconds (twice the slowest warm run, at least 30). Settle it with Matt for demo pacing." -f $limit) -ForegroundColor Yellow
    if ($cold.Count -gt 0 -and $cold[0].Sec -gt $limit) { Write-Host "   The cold start is longer than that limit. Warm the model before the demo or the first question will time out." -ForegroundColor Yellow }
}
$checks = @(
    @{ N = "Model installed and answering"; P = ($ok -gt 0) },
    @{ N = "All $($Runs + 1) calls succeeded"; P = ($ok -eq ($Runs + 1)) },
    @{ N = "Reply names the secretary (grounded) every time"; P = ($ok -gt 0 -and @($results | Where-Object { $_.Ok -and -not $_.Grounded }).Count -eq 0) },
    @{ N = "Reply has a readable 'Source:' line every time"; P = ($ok -gt 0 -and @($results | Where-Object { $_.Ok -and -not $_.Source }).Count -eq 0) },
    @{ N = "Isa and Keycloak were up during the test"; P = ($upApp -and $upKc) },
    @{ N = "At least 1 GB of memory left free"; P = ($free -ge 1) }
)
foreach ($c in $checks) { if ($c.P) { Write-Host ("   PASS  {0}" -f $c.N) -ForegroundColor Green } else { Write-Host ("   FAIL  {0}" -f $c.N) -ForegroundColor Red } }

$csv = Join-Path $PSScriptRoot "docs\MODEL_TIMING_RESULTS.csv"
if (-not (Test-Path $csv)) { "date,run,kind,model,elapsed_seconds,load_seconds,tokens,success,grounded,source_line,free_memory_gb,isa_up,keycloak_up" | Out-File -FilePath $csv -Encoding ascii }
$stamp = Get-Date -Format "yyyy-MM-dd HH:mm"
foreach ($r in $results) {
    $line = "{0},{1},{2},{3},{4:N2},{5:N2},{6},{7},{8},{9},{10},{11},{12}" -f $stamp, $r.N, $r.Kind, $modelName, $r.Sec, $r.Load, $r.Tokens, $r.Ok, $r.Grounded, $r.Source, $free, $upApp, $upKc
    Add-Content -Path $csv -Value $line -Encoding ascii
}
Write-Host "`nSaved to docs\MODEL_TIMING_RESULTS.csv. Send Willis the summary above and Matt the time limit." -ForegroundColor Cyan
if (@($checks | Where-Object { -not $_.P }).Count -gt 0) { exit 1 } else { exit 0 }
