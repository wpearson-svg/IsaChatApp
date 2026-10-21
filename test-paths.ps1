# Runs the test paths from the stage contract (docs/stage-contract.md, version 2, section 11)
# and checks the result AND the event trail for each.
#
# Usage:  .\test-paths.ps1                    sign-in checks + the main paths
#         .\test-paths.ps1 -NoToken          send no token for the paths (DevBypass ON); skips the sign-in checks
#         .\test-paths.ps1 -SkipSignIn        skip the sign-in checks (use while AccessControl:DevBypass is ON)
#         .\test-paths.ps1 -ModelOff          also run the "model turned off" path (stop Ollama first)
#         .\test-paths.ps1 -ForcedFailure     also run the "source not retrieved" path (start the app with
#                                             set OutputCompliance__ForceFail=source first)
# The app must be running. Needs Windows PowerShell 5.1 or newer.
#
# What each path proves (contract section 11):
#   1  Direct hit          answered with a source; knowledge_graph, rag and complex_ai skipped;
#                          output_compliance still ran (database side); no model was called
#   2  Model path          database_search not_found; rag and complex_ai ran; the model was called;
#                          the cited source was one of the retrieved passages; output_compliance ran
#                          (AI side). Checks source and status only, never the exact wording.
#   3  Stopped             input_compliance stop; nothing after it; no ticket
#   4  Escalated (no content)   not_found, then escalate; ticketing runs; the stages in between are skipped
#   5  Escalated (sensitive)    input_compliance escalate; ticketing runs
#   6  Non-English         language=es on the front end; translation runs (not skipped)
#   7  English             language=en on the front end; translation skipped, english
#   Extra (switches)       model off -> error + ticket; unretrieved source -> escalates
# Every path also checks: the trace ID is kept, events are in 'result=...; reason=...' format,
# ticketing runs only when something escalated, and the question text is never in the event log.
#
# NOTE: written ahead of the back end (task T7W). Paths that depend on Eliyas's stages (not_found,
# model_called, section=ai ...) will fail until he hands over. That is expected.
param(
    [string]$User = "d.whitfield",
    [switch]$SkipSignIn,
    [switch]$NoToken,
    [switch]$ModelOff,
    [switch]$ForcedFailure,
    [string]$App = "http://localhost:5067",
    [string]$Keycloak = "http://localhost:8080"
)

# ---------- helpers ----------
function Get-AuthHeader([string]$name) {
    $pwFile = Join-Path $PSScriptRoot "keycloak\dev-passwords.local.csv"
    if (-not (Test-Path $pwFile)) { return $null }
    $row = Import-Csv -Path $pwFile -Encoding UTF8 | Where-Object { $_.username -eq $name }
    if (-not $row) { return $null }
    try {
        $tok = Invoke-RestMethod -Method Post -Uri "$Keycloak/realms/isa/protocol/openid-connect/token" -Body @{
            grant_type = "password"; client_id = "isa-chat"; client_secret = "dev-only-placeholder-secret"
            username = $name; password = $row.password } -ContentType "application/x-www-form-urlencoded"
        return @{ Authorization = "Bearer $($tok.access_token)" }
    } catch { return $null }
}

# Sends one question. Returns @{ Code = http status; Res = response body or $null; Trace = trace id }.
function Send-Ask([string]$question, $headers) {
    $trace = [guid]::NewGuid().ToString()
    $body = @{ question = $question; traceId = $trace } | ConvertTo-Json
    try {
        $res = Invoke-RestMethod -Method Post -Uri "$App/api/ask" -ContentType "application/json; charset=utf-8" `
            -Headers $headers -Body ([System.Text.Encoding]::UTF8.GetBytes($body))
        return @{ Code = 200; Res = $res; Trace = $trace }
    } catch {
        $code = 0
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        return @{ Code = $code; Res = $null; Trace = $trace }
    }
}

# The data text of the first event for a stage, or $null.
function Get-StageData($events, [string]$stage) {
    $row = @($events | Where-Object { $_.stage -eq $stage }) | Select-Object -First 1
    if ($row) { return [string]$row.data }
    return $null
}

function Test-Skipped([string]$data) { return ($null -ne $data) -and ($data -like '*note=skipped*') }

$fullOrder = "front_end","access_control","translation","input_compliance","database_search","knowledge_graph","rag","complex_ai","output_compliance"
$spanish = "$([char]0xBF)Cu$([char]0xE1)ntas reuniones de la junta debe celebrar un cap$([char]0xED)tulo cada a$([char]0xF1)o?"

$fails = 0; $ran = 0
function Report([string]$name, $problems, $res, [string]$note) {
    $script:ran++
    if ($problems.Count -eq 0) {
        $extra = ""
        if ($res -and $res.ticketReference) { $extra = " ($($res.ticketReference))" }
        $st = ""; if ($res) { $st = $res.status }
        Write-Host ("PASS  {0}  ->  {1}{2}" -f $name, $st, $extra) -ForegroundColor Green
    } else {
        $script:fails++
        Write-Host ("FAIL  {0}" -f $name) -ForegroundColor Red
        $problems | ForEach-Object { Write-Host "        - $_" -ForegroundColor Red }
    }
    if ($note) { Write-Host "        $note" -ForegroundColor DarkGray }
}

# ---------- sign-in checks (stage contract section 7: 401 and 403) ----------
$headers = @{}
if ($NoToken) { $SkipSignIn = $true }
if (-not $SkipSignIn) {
    $p = @()
    $r = Send-Ask "How many board meetings must a chapter hold each year?" @{}
    if ($r.Code -ne 401) { $p += "no token: got HTTP $($r.Code), expected 401 (is AccessControl:DevBypass still ON?)" }
    Report "Sign-in: no token is refused" $p $null ""

    $p = @()
    $h = Get-AuthHeader "norole.test"
    if (-not $h) { $p += "could not sign in as norole.test (password file or Keycloak not available)" }
    else {
        $r = Send-Ask "How many board meetings must a chapter hold each year?" $h
        if ($r.Code -ne 403) { $p += "norole.test: got HTTP $($r.Code), expected 403" }
    }
    Report "Sign-in: account with no role gets 403" $p $null ""
}

$headers = $null
if (-not $NoToken) { $headers = Get-AuthHeader $User }
if ($NoToken) { $headers = @{}; Write-Host "Sending no token (only works while DevBypass is ON)." -ForegroundColor Yellow }
elseif ($headers) { Write-Host "Signed in as $User." -ForegroundColor Green }
else { $headers = @{}; Write-Host "Could not sign in as $User. Sending no token (only works while DevBypass is ON)." -ForegroundColor Yellow }
Write-Host ""

# ---------- the paths ----------
# Status: expected status. Lang: expected language. Ticket: ticketing must run. Final: the last event stage.
$cases = @(
    @{ Id = "1";  Name = "1. Direct hit";                Q = "How many board meetings must a chapter hold each year?";            Status = "answered";  Lang = "en"; Ticket = $false },
    @{ Id = "2";  Name = "2. Model path";                Q = "Who writes down what the board decided?";                            Status = "answered";  Lang = "en"; Ticket = $false },
    @{ Id = "3";  Name = "3. Stopped";                   Q = "Where's a good pizza place near the conference?";                    Status = "stopped";   Lang = "en"; Ticket = $false },
    @{ Id = "4";  Name = "4. Escalated (no content)";    Q = "What is the cryptocurrency policy?";                                 Status = "escalated"; Lang = "en"; Ticket = $true  },
    @{ Id = "5";  Name = "5. Escalated (sensitive)";     Q = "A board member keeps harassing me, what should I do?";               Status = "escalated"; Lang = "en"; Ticket = $true  },
    @{ Id = "6";  Name = "6. Non-English question";      Q = $spanish;                                                             Status = $null;       Lang = "es"; Ticket = $null  },
    @{ Id = "7";  Name = "7. English question";          Q = "How many board meetings must a chapter hold each year?";            Status = $null;       Lang = "en"; Ticket = $null  }
)
if ($ModelOff) {
    $cases += @{ Id = "M"; Name = "Extra: model turned off";   Q = "Who writes down what the board decided?"; Status = "escalated"; Lang = "en"; Ticket = $true }
}
if ($ForcedFailure) {
    $cases += @{ Id = "F"; Name = "Extra: source not retrieved"; Q = "Who writes down what the board decided?"; Status = "escalated"; Lang = "en"; Ticket = $true }
}

foreach ($c in $cases) {
    $problems = @()
    $r = Send-Ask $c.Q $headers
    $res = $r.Res; $trace = $r.Trace
    if (-not $res) { $problems += "request failed: HTTP $($r.Code)" }
    $ev = @()
    if ($res) {
        # --- the response ---
        if ($c.Status -and $res.status -ne $c.Status) { $problems += "status is '$($res.status)', expected '$($c.Status)'" }
        if (-not $c.Status -and $res.status -notin @("answered","stopped","escalated")) { $problems += "unknown status '$($res.status)'" }
        if ($res.traceId -ne $trace) { $problems += "traceId was not kept" }
        $ticketExpected = $c.Ticket
        if ($null -eq $ticketExpected) { $ticketExpected = ($res.status -eq "escalated") }
        if ($ticketExpected) {
            if ($res.ticketReference -notmatch '^TKT-\d{4}$') { $problems += "no TKT- reference" }
            if (@($res.sources).Count -ne 0) { $problems += "escalated run still lists sources" }
        } elseif ($res.ticketReference) { $problems += "unexpected ticket reference" }
        if ($res.status -eq "answered" -and @($res.sources).Count -lt 1) { $problems += "answered with no sources" }
        if ($res.status -eq "stopped"  -and @($res.sources).Count -ne 0) { $problems += "stopped run lists sources" }

        # --- the event trail ---
        try {
            # Windows PowerShell 5.1 returns the JSON list as ONE nested array, so unroll it into one row per event.
            $ev = @(Invoke-RestMethod -Uri "$App/api/events/$trace" | ForEach-Object { $_ })
        } catch { $problems += "could not read /api/events: $($_.Exception.Message)" }
    }
    if ($res -and $ev.Count -eq 0 -and $problems.Count -eq 0) { $problems += "no events found for this trace" }
    if ($ev.Count -gt 0) {
        $stages = @($ev | ForEach-Object { $_.stage })
        $joined = $stages -join ","
        if ($stages[0] -ne "front_end") { $problems += "first event is '$($stages[0])', expected front_end" }

        # shape: result=(pass|not_found|stop|escalate); reason=...
        $bad = @($ev | Where-Object { $_.data -notmatch '^result=(pass|not_found|stop|escalate); reason=' })
        if ($bad.Count -gt 0) { $problems += "$($bad.Count) event(s) not in 'result=...; reason=...' format (first: $($bad[0].stage))" }

        # the question text is never logged
        if (@($ev | Where-Object { $_.data -like "*$($c.Q)*" }).Count -gt 0) { $problems += "the question text appears in the event log" }

        # language (front end) and translation
        $fe = Get-StageData $ev "front_end"
        if ($fe -notmatch "language=$($c.Lang)\b") { $problems += "front_end should log language=$($c.Lang) (got: $fe)" }
        $tr = Get-StageData $ev "translation"
        if ($stages -contains "translation") {
            if ($c.Lang -eq "en" -and -not (Test-Skipped $tr)) { $problems += "English should skip translation (got: $tr)" }
            if ($c.Lang -ne "en" -and (Test-Skipped $tr))     { $problems += "non-English must not skip translation (got: $tr)" }
        } else { $problems += "no translation event" }
        if ($stages -contains "translation" -and $stages -contains "input_compliance" -and
            [array]::IndexOf($stages,"translation") -gt [array]::IndexOf($stages,"input_compliance")) { $problems += "translation ran after input_compliance" }

        # ticketing only when something escalated
        $escalated = ($res.status -eq "escalated")
        if ($escalated  -and $stages[-1] -ne "ticketing") { $problems += "last event is '$($stages[-1])', expected ticketing" }
        if (-not $escalated -and ($stages -contains "ticketing")) { $problems += "ticketing ran but nothing escalated" }

        $dbs = Get-StageData $ev "database_search"
        $ai  = Get-StageData $ev "complex_ai"
        $rag = Get-StageData $ev "rag"
        $kg  = Get-StageData $ev "knowledge_graph"
        $oc  = Get-StageData $ev "output_compliance"

        switch ($c.Id) {
            "1" {
                if ($joined -ne ($fullOrder -join ",")) { $problems += "stages were: $joined" }
                if ($dbs -notmatch '^result=pass')     { $problems += "database_search should pass on a direct hit (got: $dbs)" }
                foreach ($s in @(@("knowledge_graph",$kg), @("rag",$rag), @("complex_ai",$ai))) {
                    if (-not (Test-Skipped $s[1])) { $problems += "$($s[0]) should be skipped on a direct hit (got: $($s[1]))" }
                }
                if ($ai -like '*model_called=yes*') { $problems += "the model was called on a direct hit" }
                if ($oc -notmatch '^result=pass' -or (Test-Skipped $oc)) { $problems += "output_compliance must run and pass on a direct hit (got: $oc)" }
                if ($oc -notlike '*section=database*') { $problems += "output_compliance should be the database section (got: $oc)" }
            }
            "2" {
                if ($joined -ne ($fullOrder -join ",")) { $problems += "stages were: $joined" }
                if ($dbs -notmatch '^result=not_found') { $problems += "database_search should be not_found (got: $dbs)" }
                foreach ($s in @(@("knowledge_graph",$kg), @("rag",$rag), @("complex_ai",$ai))) {
                    if (Test-Skipped $s[1]) { $problems += "$($s[0]) should run on a miss (got: $($s[1]))" }
                }
                if ($ai -notlike '*model_called=yes*') { $problems += "complex_ai should log model_called=yes (got: $ai)" }
                if ($ai -notmatch 'model=\S+')          { $problems += "complex_ai should log the model name (got: $ai)" }
                if ($ai -notmatch 'model_ms=\d+')       { $problems += "complex_ai should log model_ms (got: $ai)" }
                $src = ""
                if ($ai -match 'source=([^;]+)') { $src = $Matches[1].Trim() }
                if (-not $src) { $problems += "complex_ai should log the source it cited (got: $ai)" }
                elseif ($rag -notlike "*$src*" -and $rag -notlike "*retrieved=*") { $problems += "cited source '$src' is not in rag's retrieved list (got: $rag)" }
                if ($oc -notlike '*section=ai*')            { $problems += "output_compliance should be the AI section (got: $oc)" }
                if ($oc -notlike '*source_retrieved=yes*')  { $problems += "output_compliance should log source_retrieved=yes (got: $oc)" }
            }
            "3" {
                if ($stages[-1] -ne "input_compliance") { $problems += "stopped run did not end at input_compliance (ended at $($stages[-1]))" }
                $ic = Get-StageData $ev "input_compliance"
                if ($ic -notmatch '^result=stop') { $problems += "input_compliance should be result=stop (got: $ic)" }
                if ($stages -contains "database_search") { $problems += "a stage ran after the stop" }
            }
            "4" {
                if ($dbs -notmatch '^result=not_found') { $problems += "database_search should be not_found (got: $dbs)" }
                $esc = @($ev | Where-Object { $_.data -match '^result=escalate' })
                if ($esc.Count -ne 1) { $problems += "expected exactly one escalate event, found $($esc.Count)" }
                $afterEsc = $false
                foreach ($e in $ev) {
                    if ($afterEsc -and $e.stage -ne "ticketing" -and -not (Test-Skipped $e.data)) { $problems += "$($e.stage) ran after the escalation" }
                    if ($e.data -match '^result=escalate') { $afterEsc = $true }
                }
            }
            "5" {
                $ic = Get-StageData $ev "input_compliance"
                if ($ic -notmatch '^result=escalate') { $problems += "input_compliance should be result=escalate (got: $ic)" }
                foreach ($s in @("database_search","knowledge_graph","rag","complex_ai","output_compliance")) {
                    if ($stages -contains $s) {
                        if (-not (Test-Skipped (Get-StageData $ev $s))) { $problems += "$s should be skipped after the escalation" }
                    }
                }
            }
            "M" {
                if ($ai -notmatch 'error=(unavailable|timeout|empty)') { $problems += "complex_ai should log error=unavailable|timeout|empty (got: $ai)" }
                if ($ai -notmatch '^result=escalate; reason=model call failed') { $problems += "complex_ai should escalate with 'model call failed' (got: $ai)" }
            }
            "F" {
                if ($oc -notmatch '^result=escalate; reason=ai check failed: source not retrieved') { $problems += "output_compliance should escalate with 'ai check failed: source not retrieved' (got: $oc)" }
            }
        }
    }
    Report $c.Name $problems $res ""
    if ($res) { Write-Host ("        message: " + (($res.message -replace '\s+', ' ').Substring(0, [Math]::Min(110, $res.message.Length)))) -ForegroundColor DarkGray }
}

if (-not $ModelOff)      { Write-Host "SKIP  Extra: model turned off     (run with -ModelOff after stopping Ollama)" -ForegroundColor Yellow }
if (-not $ForcedFailure) { Write-Host "SKIP  Extra: source not retrieved (run with -ForcedFailure after starting the app with OutputCompliance__ForceFail=source)" -ForegroundColor Yellow }
Write-Host ""
if ($fails -eq 0) { Write-Host "All $ran checks passed." -ForegroundColor Green } else { Write-Host "$fails of $ran check(s) failed." -ForegroundColor Red; exit 1 }
