# Runs the four test paths from the stage contract (section 7) and checks the result and the event trail.
# Usage:  .\test-paths.ps1            (signs in as d.whitfield if Keycloak + the password file are available)
#         .\test-paths.ps1 -NoToken   (send no token; only works while AccessControl:DevBypass is ON)
# The app must be running. Needs Windows PowerShell 5.1 or newer.
param(
    [string]$User = "d.whitfield",
    [switch]$NoToken,
    [string]$App = "http://localhost:5067",
    [string]$Keycloak = "http://localhost:8080"
)

$headers = @{}
if (-not $NoToken) {
    $pwFile = Join-Path $PSScriptRoot "keycloak\dev-passwords.local.csv"
    $row = $null
    if (Test-Path $pwFile) { $row = Import-Csv -Path $pwFile -Encoding UTF8 | Where-Object { $_.username -eq $User } }
    if ($row) {
        try {
            $tok = Invoke-RestMethod -Method Post -Uri "$Keycloak/realms/isa/protocol/openid-connect/token" -Body @{
                grant_type = "password"; client_id = "isa-chat"; client_secret = "dev-only-placeholder-secret"
                username = $User; password = $row.password } -ContentType "application/x-www-form-urlencoded"
            $headers = @{ Authorization = "Bearer $($tok.access_token)" }
            Write-Host "Signed in as $User." -ForegroundColor Green
        } catch { Write-Host "Could not sign in ($($_.Exception.Message)). Sending no token." -ForegroundColor Yellow }
    } else { Write-Host "No password on file for $User. Sending no token." -ForegroundColor Yellow }
}

$allStages = "front_end","access_control","translation","input_compliance","database_search","knowledge_graph","rag","complex_ai","output_compliance"
$cases = @(
    @{ Name = "1. Direct hit";  Q = "How many board meetings must a chapter hold each year?";      Status = "answered";  Ticket = $false; AiSkipped = $true  },
    @{ Name = "2. AI fallback"; Q = "Who writes down what the board decided?";                       Status = "answered";  Ticket = $false; AiSkipped = $false },
    @{ Name = "3. Stopped";     Q = "Where's a good pizza place near the conference?";               Status = "stopped";   Ticket = $false; AiSkipped = $null  },
    @{ Name = "4a. Escalated (no content)";  Q = "What is the cryptocurrency policy?";               Status = "escalated"; Ticket = $true;  AiSkipped = $null  },
    @{ Name = "4b. Escalated (sensitive)";   Q = "A board member keeps harassing me, what should I do?"; Status = "escalated"; Ticket = $true; AiSkipped = $null }
)

$fails = 0
foreach ($c in $cases) {
    $problems = @()
    $trace = [guid]::NewGuid().ToString()
    $res = $null
    try {
        $body = @{ question = $c.Q; traceId = $trace } | ConvertTo-Json
        $res = Invoke-RestMethod -Method Post -Uri "$App/api/ask" -ContentType "application/json; charset=utf-8" `
            -Headers $headers -Body ([System.Text.Encoding]::UTF8.GetBytes($body))
    } catch {
        $problems += "request failed: HTTP $([int]$_.Exception.Response.StatusCode)"
    }
    if ($res) {
        if ($res.status -ne $c.Status) { $problems += "status is '$($res.status)', expected '$($c.Status)'" }
        if ($res.traceId -ne $trace)   { $problems += "traceId was not kept" }
        if ($c.Ticket) {
            if ($res.ticketReference -notmatch '^TKT-\d{4}$') { $problems += "no TKT- reference" }
            if (@($res.sources).Count -ne 0) { $problems += "escalated run still lists sources" }
        } else {
            if ($res.ticketReference) { $problems += "unexpected ticket reference" }
        }
        if ($c.Status -eq "answered" -and @($res.sources).Count -lt 1) { $problems += "answered with no sources" }
        if ($c.Status -eq "stopped"  -and @($res.sources).Count -ne 0) { $problems += "stopped run lists sources" }

        try {
            $ev = @(Invoke-RestMethod -Uri "$App/api/events/$trace")
            $stages = @($ev | ForEach-Object { $_.stage })
            if ($ev.Count -eq 0) { $problems += "no events found for this trace" }
            else {
                if ($stages[0] -ne "front_end") { $problems += "first event is '$($stages[0])', expected front_end" }
                if ($c.Status -eq "answered" -and ($stages -join ",") -ne ($allStages -join ",")) { $problems += "stages were: $($stages -join ', ')" }
                if ($c.Ticket -and $stages[-1] -ne "ticketing") { $problems += "last event is '$($stages[-1])', expected ticketing" }
                if (-not $c.Ticket -and ($stages -contains "ticketing")) { $problems += "ticketing ran but nothing escalated" }
                if ($c.Status -eq "stopped" -and $stages[-1] -ne "input_compliance") { $problems += "stopped run did not end at input_compliance" }
                $bad = @($ev | Where-Object { $_.data -notmatch '^result=(pass|stop|escalate); reason=' })
                if ($bad.Count -gt 0) { $problems += "$($bad.Count) event(s) not in 'result=...; reason=...' format (first: $($bad[0].stage))" }
                if ($null -ne $c.AiSkipped) {
                    $aiRows = @($ev | Where-Object { $_.stage -eq "complex_ai" })
                    $aiText = [string]($aiRows | ForEach-Object { $_.data }) 
                    $skipped = [bool]($aiText -like '*note=skipped*')
                    if ($skipped -ne $c.AiSkipped) { $problems += "complex_ai skipped=$skipped, expected $($c.AiSkipped)" }
                }
            }
        } catch { $problems += "could not read /api/events: $($_.Exception.Message)" }
    }

    if ($problems.Count -eq 0) {
        Write-Host ("PASS  {0}  ->  {1}{2}" -f $c.Name, $res.status, $(if ($res.ticketReference) { " ($($res.ticketReference))" } else { "" })) -ForegroundColor Green
    } else {
        $fails++
        Write-Host ("FAIL  {0}" -f $c.Name) -ForegroundColor Red
        $problems | ForEach-Object { Write-Host "        - $_" -ForegroundColor Red }
    }
    if ($res) { Write-Host ("        message: " + (($res.message -replace '\s+', ' ').Substring(0, [Math]::Min(110, $res.message.Length)))) -ForegroundColor DarkGray }
}
Write-Host ""
if ($fails -eq 0) { Write-Host "All 5 checks passed." -ForegroundColor Green } else { Write-Host "$fails check(s) failed." -ForegroundColor Red; exit 1 }
