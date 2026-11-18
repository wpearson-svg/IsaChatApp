# first-run.ps1 -- sets up Isa's services the first time (installed copy, for demonstrations). Safe to run again.
# Needs Docker Desktop running and an internet connection this once: it builds the translation and
# search-by-meaning services and downloads the models (about 8 GB in all). Later starts work offline.

. (Join-Path $PSScriptRoot 'common.ps1')
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
Start-Transcript -Path (Join-Path $LogDir 'first-run.log') -Append | Out-Null
$failed = $false
try {
    Say 'Setting up Isa. The first time takes a while: several GB to download.'
    Assert-Docker
    if (Test-Gpu) { Say 'NVIDIA card found: translation and the AI model will use it.' }
    else { Say 'No NVIDIA card found: everything runs on the processor (translation and AI answers are slower).' }

    Say 'Building and starting the services ...'
    Invoke-Compose @('up', '-d', '--build')
    if (-not (Wait-Services 900)) { throw 'Some services did not start. See logs\first-run.log, then run "Set up Isa" again.' }

    Say ('Downloading the AI model (' + $Model + ', about 2.5 GB) ...')
    Invoke-Compose @('exec', '-T', 'ollama', 'ollama', 'pull', $Model)

    Set-Content -Path $Marker -Value (Get-Date -Format s) -Encoding ASCII
    Say 'Isa is set up. Use "Start Isa" from the Start menu.'
}
catch {
    Say ('Set-up stopped: ' + $_.Exception.Message)
    $failed = $true
}
Stop-Transcript | Out-Null
if ($failed) { exit 1 }
