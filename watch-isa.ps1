param([string]$Mode = "")
$root = $PSScriptRoot
$outLog = Join-Path $root 'isa-run.log'
$errLog = Join-Path $root 'isa-run.err.log'
$all = ($Mode -eq "ALL")

if (-not (Test-Path $outLog) -and -not (Test-Path $errLog)) {
    Write-Host "No log files yet. Start the app with start-isa.bat first." -ForegroundColor Yellow
    exit 1
}

# Lines worth showing: .NET log levels warn/fail/crit, exceptions, stack traces, our own errors.
$pattern = '^(warn|fail|crit):|Exception|Unhandled|error CS|Pipeline error|Stage .* threw|Token validation failed|Failed to'
$seen = @{}
Write-Host ("Watching " + $(if ($all) { "everything" } else { "warnings and errors only" }) + "...") -ForegroundColor Cyan

# Read each file from its current end, and print new lines as they arrive.
$files = @($outLog, $errLog) | Where-Object { Test-Path $_ }
$pos = @{}
foreach ($f in $files) { $pos[$f] = (Get-Item $f).Length }
$carry = $false
while ($true) {
    foreach ($f in $files) {
        $len = (Get-Item $f).Length
        if ($len -lt $pos[$f]) { $pos[$f] = 0 }        # log was recreated
        if ($len -gt $pos[$f]) {
            $fs = [System.IO.File]::Open($f, 'Open', 'Read', 'ReadWrite')
            $fs.Seek($pos[$f], 'Begin') | Out-Null
            $sr = New-Object System.IO.StreamReader($fs)
            $text = $sr.ReadToEnd()
            $pos[$f] = $fs.Position
            $sr.Close(); $fs.Close()
            foreach ($line in ($text -split "`r?`n")) {
                if ($line -eq "") { continue }
                $isProblem = $line -match $pattern
                # keep indented continuation lines (stack traces / messages) that follow a problem line
                if ($isProblem) { $carry = $true }
                elseif ($line -match '^\s' -and $carry) { $isProblem = $true }
                else { $carry = $false }
                if ($all -or $isProblem) {
                    $color = if ($line -match '^(fail|crit):|Exception|error CS') { 'Red' } elseif ($line -match '^warn:') { 'Yellow' } else { 'Gray' }
                    Write-Host $line -ForegroundColor $color
                }
            }
        }
    }
    Start-Sleep -Milliseconds 700
}
