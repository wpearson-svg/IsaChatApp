# Runs the "what happens when something breaks" tests: system errors and failure states,
# NOT step pass/fail (that is test-paths.ps1). Added 22 October 2026.
#
# Usage:  .\test-failures.ps1                 signed in as d.whitfield (needs Keycloak)
#         .\test-failures.ps1 -NoToken        sends no token (only works while DevBypass is ON)
#
# The app must be running. Every check asks the same thing: did Isa handle it, and did the
# leader see a plain message with no technical details? A server error (HTTP 500), a hang, or
# an exception text in a message is a FAIL.
#
#   1  Bad requests          empty question, bad JSON, wrong method -> a 4xx, never a 500
#   2  Odd questions         very long, punctuation only, search operators, SQL-looking text,
#                            emoji, non-Latin text, a null character -> handled (answered,
#                            stopped or escalated), and Isa still answers normally afterwards
#   3  Bad sign-in tokens    garbage and tampered tokens -> 401 (skipped while DevBypass is ON)
#   4  Same trace ID twice, unknown trace lookup, 8 questions at once (SQLite under load)
#   5  A stage crashes       each stage is made to throw (needs Testing:AllowForcedFailures,
#                            which is ON in appsettings.Development.json). Expected today: the
#                            run STOPS with a plain message, the event shows error=..., nothing
#                            runs after it, no technical text reaches the leader.
#
# Things this script cannot do for you are printed as a manual checklist at the end.
param(
    [string]$User = "d.whitfield",
    [switch]$NoToken,
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

function Merge-Headers($a, $b) {
    $m = @{}
    if ($a) { foreach ($k in $a.Keys) { $m[$k] = $a[$k] } }
    if ($b) { foreach ($k in $b.Keys) { $m[$k] = $b[$k] } }
    return $m
}

# One HTTP call. Code 0 = no response at all (app down or timed out).
function Send-Raw([string]$Method, [string]$Path, $Headers, [byte[]]$Body, [string]$ContentType = "application/json; charset=utf-8") {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $code = 0; $text = ""
    try {
        $req = @{ Method = $Method; Uri = "$App$Path"; UseBasicParsing = $true; TimeoutSec = 90 }
        if ($Headers) { $req.Headers = $Headers }
        if ($Body) { $req.Body = $Body; $req.ContentType = $ContentType }
        $r = Invoke-WebRequest @req
        $code = [int]$r.StatusCode
        $text = [Text.Encoding]::UTF8.GetString($r.RawContentStream.ToArray())
    } catch {
        if ($_.Exception.Response) {
            $code = [int]$_.Exception.Response.StatusCode
            try { $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream()); $text = $sr.ReadToEnd() } catch { }
        } else { $text = $_.Exception.Message }
    }
    $sw.Stop()
    return @{ Code = $code; Text = $text; Seconds = $sw.Elapsed.TotalSeconds; Res = $null; Trace = $null }
}

function Ask([string]$q, $headers, [string]$trace) {
    if (-not $trace) { $trace = [guid]::NewGuid().ToString() }
    $json = @{ question = $q; traceId = $trace } | ConvertTo-Json
    $r = Send-Raw "POST" "/api/ask" $headers ([Text.Encoding]::UTF8.GetBytes($json))
    if ($r.Code -eq 200) { try { $r.Res = $r.Text | ConvertFrom-Json } catch { } }
    $r.Trace = $trace
    return $r
}

function Get-Events([string]$trace) {
    try { return @(Invoke-RestMethod -Uri "$App/api/events/$trace") | ForEach-Object { $_ } } catch { return @() }
}

# Technical text a leader must never see.
function Test-Leak([string]$text) {
    if (-not $text) { return $false }
    if ($text -match '(?i)exception|stack ?trace|forced test failure|\bat [A-Za-z_.]+\(|System\.|Microsoft\.|\.cs:line|[A-Z]:\\') { $script:leakSnippet = $Matches[0] }
    return ($text -match '(?i)exception|stack ?trace|forced test failure|\bat [A-Za-z_.]+\(|System\.|Microsoft\.|\.cs:line|[A-Z]:\\')
}

$script:ran = 0; $script:fails = 0; $script:skipped = 0
function Report([string]$name, $problems, $res, [string]$note) {
    $script:ran++
    if ($problems.Count -eq 0) {
        $st = ""; if ($res -and $res.status) { $st = $res.status }
        Write-Host ("PASS  {0}  ->  {1}" -f $name, $st) -ForegroundColor Green
    } else {
        $script:fails++
        Write-Host ("FAIL  {0}" -f $name) -ForegroundColor Red
        $problems | ForEach-Object { Write-Host "        - $_" -ForegroundColor Red }
    }
    if ($note) { Write-Host "        $note" -ForegroundColor DarkGray }
}
function Skip([string]$name, [string]$why) {
    $script:skipped++
    Write-Host ("SKIP  {0}" -f $name) -ForegroundColor Yellow
    Write-Host "        $why" -ForegroundColor DarkGray
}
function Heading([string]$t) { Write-Host ""; Write-Host $t -ForegroundColor Cyan }

# A question must be handled: HTTP 200, a real status, a plain message, not slow.
function Test-Handled([string]$Name, [string]$Question, $Headers, [string]$Note = "") {
    $r = Ask $Question $Headers $null
    $p = @()
    if ($r.Code -ne 200) { $p += "HTTP $($r.Code), expected 200 (handled, not a server error)" }
    elseif (-not $r.Res) { $p += "no readable answer came back" }
    else {
        if (@("answered", "stopped", "escalated") -notcontains $r.Res.status) { $p += "status '$($r.Res.status)' is not answered, stopped or escalated" }
        if ([string]::IsNullOrWhiteSpace($r.Res.message)) { $p += "the message is empty" }
        if (Test-Leak $r.Res.message) { $p += "the message shows technical details: $($r.Res.message)" }
    }
    if ($r.Seconds -gt 30) { $p += "took $([int]$r.Seconds) seconds (over 30)" }
    Report $Name $p $r.Res $Note
    return $r
}

# Non-Latin text is built from code points so this file stays plain ASCII (Windows PowerShell 5.1).
$emoji = [char]::ConvertFromUtf32(0x1F642)
$japanese = -join ([char[]](0x7406, 0x4E8B, 0x4F1A, 0x306F, 0x4F55, 0x56DE))
$direct = "How many board meetings must a chapter hold each year?"
$miss = "Who writes down what the board decided?"
$esc = "A board member keeps harassing me, what should I do?"

# ---------- is the app up, and who are we? ----------
$probe = Send-Raw "GET" "/" $null $null
if ($probe.Code -eq 0) {
    Write-Host "Cannot reach $App. Start Isa first (launcher: Start Isa)." -ForegroundColor Red
    exit 2
}
$headers = @{}
if (-not $NoToken) {
    $h = Get-AuthHeader $User
    if ($h) { $headers = $h; Write-Host "Signed in as $User." -ForegroundColor Green }
    else { Write-Host "Could not sign in as $User. Sending no token (only works while DevBypass is ON)." -ForegroundColor Yellow }
} else { Write-Host "Sending no token (only works while DevBypass is ON)." -ForegroundColor Yellow }
$haveToken = $headers.ContainsKey("Authorization")

# Does a request with no token get through? (then DevBypass is ON and the token checks mean nothing)
$bypassOn = $false
$np = Ask $direct @{} $null
if ($np.Code -eq 200) { $bypassOn = $true }

if (-not $bypassOn -and -not $haveToken) {
    Write-Host ""
    Write-Host "Isa refuses requests with no sign-in (DevBypass is off) and the script could not get a token." -ForegroundColor Red
    Write-Host "Use 'Run failure tests' (not the no-token one), with Keycloak running and keycloak\dev-passwords.local.csv present." -ForegroundColor Red
    exit 2
}

# ---------- 1. bad requests ----------
Heading "1. Bad requests (a 4xx, never a 500)"
$bad = @(
    @{ Name = "Empty question";            Body = '{"question":""}';           Method = "POST" },
    @{ Name = "Whitespace-only question";  Body = '{"question":"   "}';        Method = "POST" },
    @{ Name = "Missing question field";    Body = '{"traceId":"abc"}';         Method = "POST" },
    @{ Name = "Body is not JSON";          Body = '{not json';                 Method = "POST" },
    @{ Name = "Question is a number";      Body = '{"question":12345}';        Method = "POST" }
)
foreach ($b in $bad) {
    $r = Send-Raw $b.Method "/api/ask" $headers ([Text.Encoding]::UTF8.GetBytes($b.Body))
    $p = @()
    if ($r.Code -lt 400 -or $r.Code -ge 500) { $p += "HTTP $($r.Code), expected a 4xx" }
    if (Test-Leak $r.Text) {
        $t = $r.Text; if ($t.Length -gt 160) { $t = $t.Substring(0, 160) }
        $p += "the response shows technical details (matched '$($script:leakSnippet)'; HTTP $($r.Code)): $t"
    }
    Report ("Bad request: " + $b.Name) $p $null ""
}
$r = Send-Raw "GET" "/api/ask" $headers $null
$p = @(); if ($r.Code -lt 400 -or $r.Code -ge 500) { $p += "GET /api/ask gave HTTP $($r.Code), expected a 4xx (405)" }
Report "Bad request: wrong method (GET)" $p $null ""

# ---------- 2. odd questions ----------
Heading "2. Odd questions (must be handled, with a plain message)"
$long = ("board meetings chapter " * 1000)
$null = Test-Handled "Very long question (about 23,000 characters)" $long $headers ""
$null = Test-Handled "Punctuation only" "?!?! ... ,,," $headers "Nothing searchable, so expected: nothing found -> a person."
$null = Test-Handled "Search operators and an open quote" 'chapter AND OR NOT * "unclosed NEAR( ' $headers ""
$null = Test-Handled "SQL-looking text" "'; DROP TABLE passages; --" $headers ""
$null = Test-Handled "Emoji only" "$emoji$emoji$emoji" $headers ""
$null = Test-Handled "Non-Latin text (Japanese)" $japanese $headers ""
$null = Test-Handled "Null character inside the text" ("hello" + [char]0 + "world") $headers ""
$null = Test-Handled "Many line breaks" ("board`r`n`r`n`r`nmeetings`n`n") $headers ""
$after = Ask $direct $headers $null
$p = @()
if (-not $after.Res -or $after.Res.status -ne "answered") { $p += "after all of the above, the normal test question did not answer (status '$($after.Res.status)')" }
Report "Isa still answers normally afterwards (nothing was damaged)" $p $after.Res ""

# ---------- 3. bad sign-in tokens ----------
Heading "3. Bad sign-in tokens"
if ($bypassOn) {
    Skip "Garbage token is refused" "DevBypass is ON (a request with no token got through). Set AccessControl__DevBypass=false to run these."
    Skip "Tampered token is refused" "DevBypass is ON."
} else {
    $r = Ask $direct @{ Authorization = "Bearer abc.def.ghi" } $null
    $p = @(); if ($r.Code -ne 401) { $p += "garbage token: HTTP $($r.Code), expected 401" }
    Report "Garbage token is refused (401)" $p $null ""
    if ($haveToken) {
        $good = $headers.Authorization
        $last = $good.Substring($good.Length - 1)
        $swap = "A"; if ($last -eq "A") { $swap = "B" }
        $tampered = $good.Substring(0, $good.Length - 1) + $swap
        $r = Ask $direct @{ Authorization = $tampered } $null
        $p = @(); if ($r.Code -ne 401) { $p += "tampered token: HTTP $($r.Code), expected 401" }
        Report "Tampered token is refused (401)" $p $null ""
    } else { Skip "Tampered token is refused" "No real token to tamper with (could not sign in)." }
}

# ---------- 4. trace IDs and load ----------
Heading "4. Trace IDs and load"
$trace = [guid]::NewGuid().ToString()
$a = Ask $direct $headers $trace
$b = Ask $direct $headers $trace
$p = @()
if ($a.Code -ne 200 -or $b.Code -ne 200) { $p += "same trace ID twice: HTTP $($a.Code) then $($b.Code), expected 200 both" }
Report "Same trace ID sent twice" $p $b.Res "Both runs share one trace ID, so its events appear twice. Handled, not a crash."
$r = Send-Raw "GET" "/api/events/$([guid]::NewGuid())" $null $null
$p = @(); if ($r.Code -ne 200) { $p += "unknown trace lookup: HTTP $($r.Code), expected 200 with nothing in it" }
Report "Looking up a trace ID that does not exist" $p $null ""

$auth = ""; if ($haveToken) { $auth = $headers.Authorization }
$jobs = 1..8 | ForEach-Object {
    Start-Job -ScriptBlock {
        param($app, $auth, $q)
        try {
            $h = @{}; if ($auth) { $h.Authorization = $auth }
            $body = [Text.Encoding]::UTF8.GetBytes((@{ question = $q; traceId = [guid]::NewGuid().ToString() } | ConvertTo-Json))
            $r = Invoke-WebRequest -Method Post -Uri "$app/api/ask" -Headers $h -Body $body -ContentType "application/json; charset=utf-8" -UseBasicParsing -TimeoutSec 90
            return [int]$r.StatusCode
        } catch {
            if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }
            return 0
        }
    } -ArgumentList $App, $auth, $direct
}
$null = Wait-Job -Job $jobs -Timeout 120
$codes = @($jobs | ForEach-Object { Receive-Job -Job $_ -ErrorAction SilentlyContinue })
$jobs | Stop-Job -ErrorAction SilentlyContinue; $jobs | Remove-Job -Force -ErrorAction SilentlyContinue
$p = @()
$notOk = @($codes | Where-Object { $_ -ne 200 })
if ($codes.Count -ne 8) { $p += "only $($codes.Count) of 8 requests finished within 120 seconds" }
if ($notOk.Count -gt 0) { $p += "$($notOk.Count) of 8 requests were not HTTP 200 (codes: $($notOk -join ', '))" }
Report "Eight questions at the same time (database locks)" $p $null ""

# ---------- 5. a stage crashes ----------
Heading "5. A stage crashes (made to throw on purpose)"
$probeCrash = Ask $direct (Merge-Headers $headers @{ "X-Isa-Test-Fail" = "database_search" }) $null
if ($probeCrash.Res -and $probeCrash.Res.status -eq "answered") {
    Skip "All stage crashes" "Forced failures are off. They need Testing:AllowForcedFailures=true (set in appsettings.Development.json), so Isa must run in Development."
} else {
    $stageCases = @(
        @{ S = "access_control";     Q = $direct },
        @{ S = "translation";        Q = $direct },
        @{ S = "input_compliance";   Q = $direct },
        @{ S = "database_search";    Q = $direct },
        @{ S = "knowledge_graph";    Q = $miss },
        @{ S = "rag";                Q = $miss },
        @{ S = "complex_ai";         Q = $miss },
        @{ S = "output_compliance";  Q = $direct },
        @{ S = "ticketing";          Q = $esc }
    )
    foreach ($c in $stageCases) {
        $r = Ask $c.Q (Merge-Headers $headers @{ "X-Isa-Test-Fail" = $c.S }) $null
        $p = @()
        if ($r.Code -ne 200 -or -not $r.Res) { $p += "HTTP $($r.Code), expected 200 with a plain message" }
        else {
            if ($r.Res.status -ne "stopped") { $p += "status is '$($r.Res.status)', expected 'stopped' (a crash stops the run)" }
            if ([string]::IsNullOrWhiteSpace($r.Res.message)) { $p += "the message is empty" }
            if (Test-Leak $r.Res.message) { $p += "the message shows technical details: $($r.Res.message)" }
            if ($r.Res.ticketReference) { $p += "a ticket was created ($($r.Res.ticketReference)); a crash should not make one" }
        }
        $ev = @(Get-Events $r.Trace)
        $stages = @($ev | ForEach-Object { $_.stage })
        $idx = [array]::IndexOf($stages, $c.S)
        if ($idx -lt 0) { $p += "the event log has no event for $($c.S)" }
        else {
            $d = [string]$ev[$idx].data
            if ($d -notlike "result=stop*") { $p += "$($c.S) event should say result=stop, it says: $d" }
            if ($d -notlike "*error=InvalidOperationException*") { $p += "$($c.S) event should record error=InvalidOperationException, it says: $d" }
            if ($idx -lt ($stages.Count - 1)) { $p += "stages ran after the crash: $($stages[($idx + 1)..($stages.Count - 1)] -join ', ')" }
        }
        if ($c.S -ne "ticketing" -and ($stages -contains "ticketing")) { $p += "ticketing ran after a crash" }
        if (@($ev | Where-Object { $_.data -like "*$($c.Q)*" }).Count -gt 0) { $p += "the question text appears in the event log" }
        Report ("Crash in " + $c.S) $p $r.Res ""
    }
}

# ---------- summary and manual checks ----------
Write-Host ""
Write-Host ("{0} checks run: {1} passed, {2} failed, {3} skipped." -f $script:ran, ($script:ran - $script:fails), $script:fails, $script:skipped) -ForegroundColor Cyan
Write-Host ""
Write-Host "Manual checks (a script cannot do these):" -ForegroundColor Cyan
Write-Host "  1. Model off: stop Ollama, then run  .\test-paths.ps1 -ModelOff  (expects an error and a ticket, never a hang)."
Write-Host "  2. Keycloak down: signed in, stop Keycloak (launcher), ask a question in the chat. Expect a plain message, no technical text."
Write-Host "  3. Content missing: while Isa is stopped, rename data\content.db, start Isa, ask a question. Expect 'nothing found' and a person. Rename it back."
Write-Host "  4. Connection lost: in the chat, stop Isa mid-question. Expect the reconnect banner, then a clean page after Start Isa and a refresh."
Write-Host "  5. Hand-off message after a crash: in the chat, check the bubble says the 'something went wrong' text and offers a person."
if ($script:fails -gt 0) { exit 1 } else { exit 0 }
