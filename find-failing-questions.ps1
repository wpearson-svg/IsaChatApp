# find-failing-questions.ps1 -- which of these questions REALLY make an AI answer fail an output check?
# Sends each question in test-questions-candidates.csv to the running Isa several times (default 3) as a real question,
# reads the event log for each run, and shows which checks failed and what Isa did about it (answered in ISACA's own
# words, or passed to a person). A question is a good test of a failure when the same check fails on every run.
# Nothing is forced: the failures are the model's own. They depend on the model and its warm-up, so run it with the model warm.
#
#   .\find-failing-questions.ps1                       every question in the file, 3 runs each
#   .\find-failing-questions.ps1 -Runs 5 -Only nli     only the questions meant to fail the NLI check, 5 runs each
#   .\find-failing-questions.ps1 -File my-questions.csv
#
# The file has two columns: intended (nli, source, rule, confidence, numbers, strength) and question.
# Real runs: they write event-log rows and, on an escalation, tickets. Question text is never written to the event log.
param(
    [string]$User = "d.whitfield",
    [int]$Runs = 3,
    [string]$Only = "",
    [string]$File = "test-questions-candidates.csv",
    [string]$App = "http://localhost:5067",
    [string]$Keycloak = ""
)
. "$PSScriptRoot\services-config.ps1"
if (-not $Keycloak) { $Keycloak = Get-ServiceUrl 'keycloak' }
if ((Get-ServicesLocation).Remote) { Write-Host ("Services are Remote: using " + $Keycloak) -ForegroundColor Yellow }

function Get-Auth {
    $pwFile = Join-Path $PSScriptRoot "keycloak\dev-passwords.local.csv"
    if (-not (Test-Path $pwFile)) { return @{} }
    $row = Import-Csv -Path $pwFile -Encoding UTF8 | Where-Object { $_.username -eq $User }
    if (-not $row) { return @{} }
    try {
        $tok = Invoke-RestMethod -Method Post -Uri "$Keycloak/realms/isa/protocol/openid-connect/token" -Body @{
            grant_type = "password"; client_id = "isa-chat"; client_secret = "dev-only-placeholder-secret"
            username = $User; password = $row.password } -ContentType "application/x-www-form-urlencoded"
        return @{ Authorization = "Bearer $($tok.access_token)" }
    } catch { return @{} }
}

$path = Join-Path $PSScriptRoot $File
if (-not (Test-Path $path)) { Write-Host "Cannot find $File" -ForegroundColor Red; exit 1 }
$rows = @(Import-Csv -Path $path -Encoding UTF8)
if ($Only) { $rows = @($rows | Where-Object { $_.intended -eq $Only }) }

# What failed in one run: read from the output_compliance and complex_ai events.
function Get-Result($trace, $status) {
    $ev = @()
    try { $ev = @(Invoke-RestMethod -Uri "$App/api/events/$trace" | ForEach-Object { $_ }) } catch { }
    $oc = ($ev | Where-Object { $_.stage -eq "output_compliance" } | Select-Object -First 1).data
    $ai = ($ev | Where-Object { $_.stage -eq "complex_ai" } | Select-Object -First 1).data
    $ic = ($ev | Where-Object { $_.stage -eq "input_compliance" } | Select-Object -First 1).data
    $found = @()
    if ($ic -and $ic -notmatch '^result=pass') { $found += "input_rule" }
    if ($ai -match 'error=') { $found += "model_error" }
    elseif ($ai -match 'result=escalate') { $found += "ai_stage" }
    if ($oc) {
        if ($oc -match 'source_retrieved=no') { $found += "source" }
        if ($oc -match 'rule_recheck=fail') { $found += "rule" }
        if ($oc -match 'nli=fail') { $found += "nli" }
        if ($oc -match 'numbers=fail') { $found += "numbers" }
        if ($oc -match 'strength=fail') { $found += "strength" }
        if ($oc -match 'contact=fail') { $found += "contact" }
        if ($oc -match 'check=answer_shape') { $found += "answer_shape" }
        if ($oc -match 'check=tbd') { $found += "tbd" }
        if ($oc -match 'confidence_parts' -and $oc -match 'failed=[^;]*confidence|step_reason=[^;]*confidence|reason=ai check failed: confidence') { $found += "confidence" }
    }
    $how = if ($status -eq "answered") { if ($oc -match 'step_down=isaca_words') { "answered in ISACA's words" } elseif ($oc -match 'step_down=trimmed') { "answered, trimmed" } else { "answered" } } else { $status }
    return @{ Checks = (($found | Select-Object -Unique) -join "+"); How = $how }
}

$headers = Get-Auth
if ($headers.Count -eq 0) { Write-Host "Could not sign in as $User. Sending no token (only works while DevBypass is ON)." -ForegroundColor Yellow }

foreach ($row in $rows) {
    $results = @()
    for ($i = 1; $i -le $Runs; $i++) {
        $trace = [guid]::NewGuid().ToString()
        $body = @{ question = $row.question; traceId = $trace } | ConvertTo-Json
        $res = $null
        for ($try = 1; $try -le 2 -and -not $res; $try++) {
            try {
                $res = Invoke-RestMethod -Method Post -Uri "$App/api/ask" -ContentType "application/json; charset=utf-8" `
                    -Headers $headers -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 120
            } catch {
                if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 401) { $headers = Get-Auth } else { break }
            }
        }
        if ($res) { $results += (Get-Result $trace $res.status) } else { $results += @{ Checks = "no_response"; How = "no response" } }
    }
    $sets = @($results | ForEach-Object { if ($_.Checks) { $_.Checks } else { "none" } })
    $same = (@($sets | Select-Object -Unique)).Count -eq 1
    $verdict = if ($same -and $sets[0] -ne "none") { "REPEATABLE: $($sets[0])" } elseif ($same) { "no failure on any run" } else { "varies between runs" }
    $color = if ($same -and $sets[0] -ne "none") { "Green" } elseif ($same) { "DarkGray" } else { "Yellow" }
    Write-Host ""
    Write-Host ("[intended: {0}] {1}" -f $row.intended, $row.question)
    Write-Host ("    " + $verdict) -ForegroundColor $color
    for ($i = 0; $i -lt $results.Count; $i++) {
        $c = if ($results[$i].Checks) { $results[$i].Checks } else { "none" }
        Write-Host ("      run {0}: failed checks: {1}; result: {2}" -f ($i + 1), $c, $results[$i].How) -ForegroundColor DarkGray
    }
}
Write-Host ""
Write-Host "A question that is REPEATABLE for the check you want is a good test question. Look at the Isa console for what the AI wrote (it prints each failed answer and its sentence scores)." -ForegroundColor Cyan
