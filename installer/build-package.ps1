# build-package.ps1 -- builds the Isa demo package (Willis, November 17, 2026). FOR DEMONSTRATIONS, NOT A RELEASE.
# Run from anywhere:   powershell -ExecutionPolicy Bypass -File installer\build-package.ps1
#
# Makes installer\out\Isa\ (the files to install) and installer\out\Isa-demo.zip, and, when Inno Setup 6
# is installed, installer\out\Isa-Setup.exe. Nothing in the team's own setup is changed.
#   app\       IsaChatApp.exe (self-contained, no .NET needed) plus what Isa reads while running:
#              data\ (content, directory, onboarding and vector databases, and the HHEM model),
#              content\, onboarding\, rules\, presentation\, directory\, appsettings.Local.json
#              (services on this computer, Salesforce off) and output-compliance-settings.json when present.
#              Left out: the per-person databases (progress, notebook, events, usage, API keys, requests;
#              Isa makes them empty on her first start), dev-keys.json and every password file.
#   services\  installer\docker-compose*.yml, the Keycloak realm, and the translation and embeddings source.
#   scripts\   first-run.ps1, start-isa.ps1, stop-isa.ps1, common.ps1.

$ErrorActionPreference = 'Stop'
$Repo  = Split-Path -Parent $PSScriptRoot
$Out   = Join-Path $PSScriptRoot 'out'
$Pkg   = Join-Path $Out 'Isa'
$App   = Join-Path $Pkg 'app'
$Svc   = Join-Path $Pkg 'services'

function Say([string]$t) { Write-Host ('[build] ' + $t) }
function Copy-Tree([string]$from, [string]$to) {
    # Copies a folder without Python scripts, caches or test scratch files.
    New-Item -ItemType Directory -Force -Path $to | Out-Null
    & robocopy $from $to /E /NFL /NDL /NJH /NJS /NP /XD __pycache__ /XF *.py *.pyc | Out-Null
    if ($LASTEXITCODE -ge 8) { throw ('robocopy failed for ' + $from) }
}

if (Test-Path $Pkg) { Say 'Removing the previous package ...'; Remove-Item -Recurse -Force $Pkg }
New-Item -ItemType Directory -Force -Path $App, $Svc | Out-Null

Say 'Publishing Isa (self-contained, win-x64) ...'
& dotnet publish (Join-Path $Repo 'IsaChatApp.csproj') -c Release -r win-x64 --self-contained true -o $App -nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Say 'Copying the files Isa reads while running ...'
$data = Join-Path $App 'data'
New-Item -ItemType Directory -Force -Path $data | Out-Null
foreach ($db in 'content.db', 'directory.db', 'onboarding.db', 'vectors.db') {
    Copy-Item (Join-Path $Repo ('data\' + $db)) (Join-Path $data $db)
}
Copy-Tree (Join-Path $Repo 'data\models') (Join-Path $data 'models')
foreach ($dir in 'content', 'onboarding', 'rules', 'presentation', 'directory') {
    Copy-Tree (Join-Path $Repo $dir) (Join-Path $App $dir)
}
$occ = Join-Path $Repo 'output-compliance-settings.json'
if (Test-Path $occ) { Copy-Item $occ (Join-Path $App 'output-compliance-settings.json') }
# The web SDK publishes every .json file in the project folder: take out what Isa doesn't read while running
# (an old saved sign-in experiment, a second copy of the Keycloak realm, example settings files).
foreach ($junk in '_saved_T6_signin', 'keycloak', 'appsettings.Local.example.json', 'dev-keys.example.json', 'appsettings.Development.json') {
    $p = Join-Path $App $junk
    if (Test-Path $p) { Remove-Item -Recurse -Force $p }
}
# Never in the package, even if a copy above picked one up:
Get-ChildItem $App -Recurse -Include 'dev-keys.json', '*.local.csv', 'dev-passwords*' -File -ErrorAction SilentlyContinue | Remove-Item -Force

# Services on this computer; Salesforce off (no org on a demo machine); voice and translation on.
@'
{
  "//": "Isa demo package: every service runs on this computer in Docker (installer/README.md).",
  "Services": { "Location": "Local" },
  "Ticketing": { "Salesforce": { "Enabled": false } },
  "Tts": { "Enabled": true },
  "Translation": { "Enabled": true }
}
'@ | Set-Content -Path (Join-Path $App 'appsettings.Local.json') -Encoding UTF8

Say 'Copying the services ...'
Copy-Item (Join-Path $PSScriptRoot 'docker-compose.yml') $Svc
Copy-Item (Join-Path $PSScriptRoot 'docker-compose.gpu.yml') $Svc
Copy-Tree (Join-Path $Repo 'keycloak\import') (Join-Path $Svc 'keycloak-import')
foreach ($s in 'translation', 'embeddings') {
    Copy-Item -Recurse (Join-Path $Repo ('ai-services\' + $s)) (Join-Path $Svc ('ai-services\' + $s))
}
Get-ChildItem $Svc -Recurse -Directory -Filter __pycache__ | Remove-Item -Recurse -Force

Say 'Copying the scripts ...'
Copy-Item -Recurse (Join-Path $PSScriptRoot 'scripts') (Join-Path $Pkg 'scripts')
Copy-Item (Join-Path $PSScriptRoot 'README.md') (Join-Path $Pkg 'README.md')

Say 'Making the zip ...'
$zip = Join-Path $Out 'Isa-demo.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $Pkg '*') -DestinationPath $zip -CompressionLevel Optimal

$iscc = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if ($iscc) {
    Say 'Making the installer with Inno Setup ...'
    & $iscc (Join-Path $PSScriptRoot 'isa.iss') ('/O' + $Out) '/Q'
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup failed' }
    Say ('Installer: ' + (Join-Path $Out 'Isa-Setup.exe'))
} else {
    Say 'Inno Setup 6 is not installed, so no Isa-Setup.exe. The zip works on its own (see README.md).'
}
$size = (Get-ChildItem $Pkg -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
Say ('Done. Package: ' + $Pkg + ' (' + [Math]::Round($size) + ' MB), zip: ' + $zip)
