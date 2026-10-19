# Creates one test Case in the Salesforce developer org, using the values in dev-keys.json.
# Run from the project folder:  .\test-salesforce.ps1
# Prints the new Case Id on success. Never prints the key, secret or token.
$ErrorActionPreference = 'Stop'

$keysPath = Join-Path $PSScriptRoot 'dev-keys.json'
if (-not (Test-Path $keysPath)) { throw "Missing $keysPath. Copy dev-keys.example.json to dev-keys.json and fill it in, or save the settings on the Dev Dashboard." }
$sf = (Get-Content $keysPath -Raw | ConvertFrom-Json).salesforce

foreach ($name in 'orgUrl', 'apiVersion', 'clientId', 'clientSecret') {
    $v = $sf.$name
    if ([string]::IsNullOrWhiteSpace($v) -or $v -like 'PASTE_*') {
        throw "Fill in salesforce.$name in dev-keys.json first."
    }
}

function Show-SfError($err) {
    Write-Host "FAILED: $($err.Exception.Message)" -ForegroundColor Red
    if ($err.ErrorDetails -and $err.ErrorDetails.Message) { Write-Host $err.ErrorDetails.Message }
}

# 1. Get a token (client credentials)
try {
    $tok = Invoke-RestMethod -Method Post -Uri "$($sf.orgUrl)/services/oauth2/token" -Body @{
        grant_type    = 'client_credentials'
        client_id     = $sf.clientId
        client_secret = $sf.clientSecret
    }
} catch { Show-SfError $_; exit 1 }
Write-Host "Token OK"

# 2. Create a Case with the standard fields and the six custom fields
$case = @{
    Subject          = 'Isa escalation: test from test-salesforce.ps1'
    Description      = "Question: test question`nLanguage: en`nEscalated by: output_compliance`nIsa reference: TKT-TEST"
    Status           = 'New'
    Origin           = 'Web'
    Priority         = 'Medium'
    SuppliedName     = 'Dana Whitfield'
    SuppliedEmail    = 'd.whitfield@example.org'
    Trace_ID__c      = 'test-trace-0001'
    Isa_Ticket_Ref__c = 'TKT-TEST'
    Language__c      = 'en'
    Escalated_By__c  = 'output_compliance'
    Chapter_ID__c    = 'test-chapter'
    Chapter_Role__c  = 'member'
} | ConvertTo-Json

$base = if ($tok.instance_url) { $tok.instance_url } else { $sf.orgUrl }
try {
    $res = Invoke-RestMethod -Method Post -Uri "$base/services/data/$($sf.apiVersion)/sobjects/Case" `
        -Headers @{ Authorization = "Bearer $($tok.access_token)" } `
        -ContentType 'application/json' -Body $case
} catch { Show-SfError $_; exit 1 }

Write-Host "Case created. Id: $($res.id)" -ForegroundColor Green
Write-Host "Open it in Salesforce and check the six custom fields are filled."
