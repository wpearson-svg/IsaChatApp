# isa-launcher.ps1 -- a small window with a button for each Isa script, and a check of
# everything needed to run the project.
# Open it with isa-launcher.bat (double-click). It only starts the existing .bat / .ps1 files
# in this folder, each in its own console window, so you can see the output.

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:root = $PSScriptRoot
$script:appUrl = 'http://localhost:5067'

# ---- helpers -------------------------------------------------------------

function Show-Problem($text) {
    [void][System.Windows.Forms.MessageBox]::Show($text, 'Isa launcher', 'OK', 'Warning')
}

# Runs a .bat file from the project folder in its own console window (stays open).
function Run-Bat($name) {
    $path = Join-Path $script:root $name
    if (-not (Test-Path $path)) { Show-Problem "Cannot find $name in $script:root"; return }
    Start-Process -FilePath 'cmd.exe' -ArgumentList @('/k', $name) -WorkingDirectory $script:root
}

# Runs a command line in a new PowerShell window that stays open.
function Run-Ps($commandText) {
    $arg = '"' + $commandText + '"'
    Start-Process -FilePath 'powershell.exe' -WorkingDirectory $script:root `
        -ArgumentList @('-NoExit', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', $arg)
}

# Runs a .ps1 from the project folder, with optional extra arguments (a single string).
function Run-Script($name, $extra) {
    $path = Join-Path $script:root $name
    if (-not (Test-Path $path)) { Show-Problem "Cannot find $name in $script:root"; return }
    Run-Ps ("& '.\" + $name + "' " + $extra)
}

# Runs any command (docker compose ...) in a console window that stays open.
function Run-Cmd($commandText) {
    Start-Process -FilePath 'cmd.exe' -ArgumentList @('/k', $commandText) -WorkingDirectory $script:root
}

function Clean-Text($text) {
    # Quotes cannot be passed through the command line, so remove them.
    return (($text -replace '"', '') -replace "'", '').Trim()
}

function Test-Port($port) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $result = $client.BeginConnect('localhost', $port, $null, $null)
        $ok = $result.AsyncWaitHandle.WaitOne(400)
        if ($ok) { try { $client.EndConnect($result) } catch { $ok = $false } }
        $client.Close()
        return $ok
    } catch { return $false }
}

# ---- window layout: two columns -----------------------------------------

$form = New-Object System.Windows.Forms.Form
$form.Text = 'Isa launcher'
$form.StartPosition = 'CenterScreen'
$form.ClientSize = New-Object System.Drawing.Size(1290, 640)
$form.MinimumSize = New-Object System.Drawing.Size(700, 400)
$form.Font = New-Object System.Drawing.Font('Segoe UI', 9)
$iconPath = Join-Path $script:root 'isa-icon.ico'
if (Test-Path $iconPath) { try { $form.Icon = New-Object System.Drawing.Icon($iconPath) } catch { } }

$split = New-Object System.Windows.Forms.TableLayoutPanel
$split.Dock = 'Fill'
$split.ColumnCount = 2
$split.RowCount = 1
[void]$split.ColumnStyles.Add((New-Object System.Windows.Forms.ColumnStyle('Percent', 50)))
[void]$split.ColumnStyles.Add((New-Object System.Windows.Forms.ColumnStyle('Percent', 50)))
[void]$split.RowStyles.Add((New-Object System.Windows.Forms.RowStyle('Percent', 100)))
$form.Controls.Add($split)

function New-Column() {
    $c = New-Object System.Windows.Forms.FlowLayoutPanel
    $c.Dock = 'Fill'
    $c.FlowDirection = 'TopDown'
    $c.WrapContents = $false
    $c.AutoScroll = $true
    $c.Padding = New-Object System.Windows.Forms.Padding(8)
    return $c
}
$left = New-Column
$right = New-Column
$split.Controls.Add($left, 0, 0)
$split.Controls.Add($right, 1, 0)

# A group with a fixed size: $rows rows of buttons, 38 px each.
function New-Group($column, $title, $rows) {
    $g = New-Object System.Windows.Forms.GroupBox
    $g.Text = $title
    $g.Width = 606
    $g.Height = 28 + ($rows * 38) + 8
    $g.Margin = New-Object System.Windows.Forms.Padding(3, 3, 3, 8)
    $p = New-Object System.Windows.Forms.FlowLayoutPanel
    $p.Dock = 'Fill'
    $p.WrapContents = $true
    $p.FlowDirection = 'LeftToRight'
    $g.Controls.Add($p)
    $column.Controls.Add($g)
    return $p
}

$toolTip = New-Object System.Windows.Forms.ToolTip
$toolTip.AutoPopDelay = 8000

function Add-Btn($panel, $text, $tip, [scriptblock]$onClick) {
    $b = New-Object System.Windows.Forms.Button
    $b.Text = $text
    $b.Width = 188
    $b.Height = 32
    $b.Margin = New-Object System.Windows.Forms.Padding(3)
    $b.Add_Click($onClick)
    $toolTip.SetToolTip($b, $tip)
    $panel.Controls.Add($b)
}

# ---- LEFT: app, keycloak, tests, open -----------------------------------

$pApp = New-Group $left 'App' 3
Add-Btn $pApp 'Start Isa' 'start-isa.bat: checks prerequisites, starts the app in the background.' { Run-Bat 'start-isa.bat' }
Add-Btn $pApp 'Start Isa (debug window)' 'start-isa-debug.bat: runs the app in a visible window, errors show there.' { Run-Bat 'start-isa-debug.bat' }
Add-Btn $pApp 'Start Isa (force AI-check failure)' 'Stop Isa first. Starts Isa with OutputCompliance__ForceFail=source, so a model answer fails the source check. Use it for the Forced failure test, then Stop Isa and Start Isa normally.' { Run-Cmd 'set "OutputCompliance__ForceFail=source" && start-isa.bat' }
Add-Btn $pApp 'Stop Isa' 'stop-isa.bat: stops the background app and frees port 5067.' { Run-Bat 'stop-isa.bat' }
Add-Btn $pApp 'Stop Isa (debug)' 'stop-isa-debug.bat' { Run-Bat 'stop-isa-debug.bat' }
Add-Btn $pApp 'Watch problems' 'watch-isa.bat: shows warnings and errors from the running app.' { Run-Bat 'watch-isa.bat' }
Add-Btn $pApp 'Watch ALL log lines' 'watch-isa.bat ALL: shows every log line.' { Run-Cmd 'watch-isa.bat ALL' }

$pKc = New-Group $left 'Keycloak (needs Docker Desktop running)' 2
Add-Btn $pKc 'Start Keycloak' 'docker compose up -d' { Run-Cmd 'docker compose up -d' }
Add-Btn $pKc 'Stop Keycloak' 'docker compose down (keeps its data)' { Run-Cmd 'docker compose down' }
Add-Btn $pKc 'Reset Keycloak' 'reset-keycloak.bat: wipes Keycloak data and re-imports the realm file as it is.' { Run-Bat 'reset-keycloak.bat' }
Add-Btn $pKc 'Reset + rebuild realm' 'reset-keycloak-rebuild.bat: rebuilds the realm file with Python first, then resets.' { Run-Bat 'reset-keycloak-rebuild.bat' }
Add-Btn $pKc 'Keycloak logs' 'docker compose logs keycloak (last 100 lines, then follows)' { Run-Cmd 'docker compose logs --tail 100 -f keycloak' }

$pTest = New-Group $left 'Tests (the app must be running)' 3
Add-Btn $pTest 'Run test paths' 'test-paths.ps1: sign-in checks and the test paths (five questions, Spanish, English), signed in as d.whitfield.' { Run-Script 'test-paths.ps1' '' }
Add-Btn $pTest 'Run test paths (no token)' 'test-paths.ps1 -NoToken: sends no token, only works while DevBypass is on.' { Run-Script 'test-paths.ps1' '-NoToken' }
Add-Btn $pTest 'Run test paths (model off)' 'test-paths.ps1 -ModelOff: sends the model call to a dead address (no need to quit Ollama). Checks a failed model call gives an error and a ticket, not a hang.' { Run-Script 'test-paths.ps1' '-ModelOff' }
Add-Btn $pTest 'Run test paths (forced failure)' 'test-paths.ps1 -ForcedFailure: start Isa with the button Start Isa (force AI-check failure) first. Checks a source that was not retrieved is escalated.' { Run-Script 'test-paths.ps1' '-ForcedFailure' }
Add-Btn $pTest 'Run failure tests' 'test-failures.ps1: system errors and fail states (bad requests, odd questions, bad tokens, load, a stage crashing), NOT step pass/fail. Signed in as d.whitfield.' { Run-Script 'test-failures.ps1' '' }
Add-Btn $pTest 'Test the model (timing)' 'test-model.ps1: checks Ollama and phi4-mini, times the miss-path question (1 cold + 5 warm), checks memory with Isa and Keycloak up, suggests a time limit. Start Isa and Keycloak first.' { Run-Script 'test-model.ps1' '' }
Add-Btn $pTest 'Test Salesforce' 'test-salesforce.ps1: creates one test Case using dev-keys.json.' { Run-Script 'test-salesforce.ps1' '' }

$pContent = New-Group $left 'Content database (stop Isa first)' 1
Add-Btn $pContent 'Import files...' 'Pick .docx, .pptx or .pdf files to add to the content database (import_content.py).' {
    if (Test-Port 5067) { Show-Problem 'Isa is running. Click Stop Isa first, then import (the import replaces data\content.db).'; return }
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Multiselect = $true
    $dlg.Filter = 'Documents (*.docx;*.pptx;*.pdf)|*.docx;*.pptx;*.pdf'
    $dlg.InitialDirectory = Join-Path $script:root 'content'
    if ($dlg.ShowDialog() -eq 'OK') {
        $q = [char]39
        $list = ($dlg.FileNames | ForEach-Object { $q + ($_ -replace $q, ($q + $q)) + $q }) -join ' '
        Run-Ps ("python content\import_content.py " + $list)
    }
}
Add-Btn $pContent 'Import folder...' 'Pick a folder; every .docx, .pptx and .pdf inside it (subfolders too) is added.' {
    if (Test-Port 5067) { Show-Problem 'Isa is running. Click Stop Isa first, then import (the import replaces data\content.db).'; return }
    $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
    $dlg.SelectedPath = Join-Path $script:root 'content'
    if ($dlg.ShowDialog() -eq 'OK') { $q = [char]39; Run-Ps ("python content\import_content.py " + $q + ($dlg.SelectedPath -replace $q, ($q + $q)) + $q) }
}
Add-Btn $pContent 'Rebuild content database' 'Rebuilds data\content.db from content\content.json (no new files).' { if (Test-Port 5067) { Show-Problem 'Isa is running. Click Stop Isa first.' } else { Run-Ps 'python content\build_content_db.py' } }

$pOpen = New-Group $left 'Open' 2
Add-Btn $pOpen 'Open Isa in browser' 'http://localhost:5067' { Start-Process $script:appUrl }
Add-Btn $pOpen 'Open Dev Dashboard' '/dev-dashboard (Models, RAG, access & ticketing has the Salesforce settings)' { Start-Process ($script:appUrl + '/dev-dashboard') }
Add-Btn $pOpen 'Open Keycloak admin' 'http://localhost:8080 (admin / admin on the dev setup)' { Start-Process 'http://localhost:8080' }
Add-Btn $pOpen 'Open project folder' 'Opens this folder in File Explorer.' { Start-Process 'explorer.exe' -ArgumentList $script:root }

# ---- RIGHT: status, ask, requirements -----------------------------------

$pStatus = New-Group $right 'Status' 2
$lblApp = New-Object System.Windows.Forms.Label
$lblApp.AutoSize = $true
$lblApp.Margin = New-Object System.Windows.Forms.Padding(6, 8, 20, 4)
$lblKc = New-Object System.Windows.Forms.Label
$lblKc.AutoSize = $true
$lblKc.Margin = New-Object System.Windows.Forms.Padding(6, 8, 20, 4)
$pStatus.Controls.Add($lblApp)
$pStatus.Controls.Add($lblKc)

function Update-Status {
    $appUp = Test-Port 5067
    $kcUp = Test-Port 8080
    $lblApp.Text = 'Isa app (port 5067): ' + $(if ($appUp) { 'UP' } else { 'down' })
    $lblApp.ForeColor = $(if ($appUp) { [System.Drawing.Color]::DarkGreen } else { [System.Drawing.Color]::Firebrick })
    $lblKc.Text = 'Keycloak (port 8080): ' + $(if ($kcUp) { 'UP' } else { 'down' })
    $lblKc.ForeColor = $(if ($kcUp) { [System.Drawing.Color]::DarkGreen } else { [System.Drawing.Color]::Firebrick })
}
Add-Btn $pStatus 'Refresh status' 'Check the app and Keycloak now. It also refreshes every 10 seconds.' { Update-Status }

$pAsk = New-Group $right 'Ask Isa as a test user (test-ask.ps1)' 2
$lblUser = New-Object System.Windows.Forms.Label
$lblUser.Text = 'User:'
$lblUser.AutoSize = $true
$lblUser.Margin = New-Object System.Windows.Forms.Padding(3, 8, 3, 3)
$txtUser = New-Object System.Windows.Forms.TextBox
$txtUser.Text = 'd.whitfield'
$txtUser.Width = 110
$lblQ = New-Object System.Windows.Forms.Label
$lblQ.Text = 'Question:'
$lblQ.AutoSize = $true
$lblQ.Margin = New-Object System.Windows.Forms.Padding(10, 8, 3, 3)
$txtQ = New-Object System.Windows.Forms.TextBox
$txtQ.Text = 'How many board meetings must a chapter hold each year?'
$txtQ.Width = 250
$pAsk.Controls.AddRange(@($lblUser, $txtUser, $lblQ, $txtQ))
Add-Btn $pAsk 'Ask Isa' 'Signs in as the user above and asks the question. Needs Keycloak up and DevBypass off.' {
    $u = Clean-Text $txtUser.Text
    $q = Clean-Text $txtQ.Text
    if (-not $u) { Show-Problem 'Type a user name first.'; return }
    Run-Script 'test-ask.ps1' ("-User '" + $u + "' -Question '" + $q + "'")
}

# Requirements group: a Check button on top, the results below.
$gReq = New-Object System.Windows.Forms.GroupBox
$gReq.Text = 'Requirements: is everything installed to run Isa?'
$gReq.Width = 606
$gReq.Height = 372
$gReq.Margin = New-Object System.Windows.Forms.Padding(3, 3, 3, 8)
$right.Controls.Add($gReq)

$reqTop = New-Object System.Windows.Forms.FlowLayoutPanel
$reqTop.Dock = 'Top'
$reqTop.Height = 40
$rtb = New-Object System.Windows.Forms.RichTextBox
$rtb.Dock = 'Fill'
$rtb.ReadOnly = $true
$rtb.BackColor = [System.Drawing.Color]::White
$rtb.Font = New-Object System.Drawing.Font('Consolas', 9)
$rtb.BorderStyle = 'None'
$rtb.Text = 'Click "Check requirements".'
$gReq.Controls.Add($rtb)
$gReq.Controls.Add($reqTop)

function Add-Line($tag, $color, $text) {
    $rtb.SelectionStart = $rtb.TextLength
    $rtb.SelectionColor = $color
    $rtb.AppendText($tag + '  ')
    $rtb.SelectionColor = [System.Drawing.Color]::Black
    $rtb.AppendText($text + "`n")
}
function Req-Ok($text)   { Add-Line '[ OK ]   ' ([System.Drawing.Color]::DarkGreen) $text }
function Req-Miss($text) { Add-Line '[MISSING]' ([System.Drawing.Color]::Firebrick) $text }
function Req-Warn($text) { Add-Line '[ warn ] ' ([System.Drawing.Color]::DarkOrange) $text }
function Req-Hint($text) { Add-Line '          ' ([System.Drawing.Color]::Gray) ('   ' + $text) }

function Run-Quiet($exe, $argList) {
    # Runs a command and returns its output text and exit code, never throwing.
    try {
        $out = & $exe @argList 2>&1 | Out-String
        return @{ Out = $out.Trim(); Code = $LASTEXITCODE }
    } catch {
        return @{ Out = $_.Exception.Message; Code = -1 }
    }
}

function Get-Json($url) {
    try { return Invoke-RestMethod -Uri $url -TimeoutSec 3 -ErrorAction Stop } catch { return $null }
}

function Check-Requirements {
    $form.Cursor = [System.Windows.Forms.Cursors]::WaitCursor
    $rtb.Clear()
    $rtb.Text = 'Checking... (Docker can take a few seconds)' + "`n"
    [System.Windows.Forms.Application]::DoEvents()
    $rtb.Clear()

    # --- software ---
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $sdks = @(& dotnet --list-sdks 2>$null)
        $good = @($sdks | Where-Object { $_ -match '^(\d+)\.' -and [int]$Matches[1] -ge 10 })
        if ($good.Count -gt 0) { Req-Ok ('.NET SDK 10 or newer: ' + (($good[0] -split ' ')[0])) }
        else { Req-Miss '.NET is installed, but no SDK 10 or newer'; Req-Hint 'Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0' }
    } else { Req-Miss '.NET SDK not found'; Req-Hint 'Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0' }
    [System.Windows.Forms.Application]::DoEvents()

    if (Test-Path (Join-Path $script:root 'IsaChatApp.csproj')) { Req-Ok 'Project file IsaChatApp.csproj found' }
    else { Req-Miss 'IsaChatApp.csproj not found'; Req-Hint 'The launcher must sit in the project folder.' }

    $ps = $PSVersionTable.PSVersion
    if ($ps.Major -gt 5 -or ($ps.Major -eq 5 -and $ps.Minor -ge 1)) { Req-Ok ('PowerShell ' + $ps.ToString()) }
    else { Req-Warn ('PowerShell ' + $ps.ToString() + ' (5.1 or newer needed)') }

    if (Get-Command git -ErrorAction SilentlyContinue) { Req-Ok ((Run-Quiet 'git' @('--version')).Out) }
    else { Req-Warn 'Git not found'; Req-Hint 'Needed to pull and push: https://git-scm.com/download/win' }

    $py = Get-Command python -ErrorAction SilentlyContinue
    if (-not $py) { $py = Get-Command py -ErrorAction SilentlyContinue }
    if ($py) { Req-Ok ('Python: ' + (Run-Quiet $py.Source @('--version')).Out) }
    else { Req-Warn 'Python not found (only needed for "Reset + rebuild realm")'; Req-Hint 'https://www.python.org/downloads/' }
    [System.Windows.Forms.Application]::DoEvents()

    # --- docker, wsl ---
    $dockerRunning = $false
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        $info = Run-Quiet 'docker' @('info')
        if ($info.Code -eq 0) {
            $dockerRunning = $true
            Req-Ok 'Docker Desktop is installed and running'
            $comp = Run-Quiet 'docker' @('compose', 'version')
            if ($comp.Code -eq 0) { Req-Ok ($comp.Out) } else { Req-Miss 'docker compose not available'; Req-Hint 'Update Docker Desktop.' }
        } else {
            Req-Miss 'Docker is installed but not running'
            Req-Hint 'Start Docker Desktop and wait until it says "running", then check again.'
        }
    } else {
        Req-Miss 'Docker Desktop not found (needed for Keycloak)'
        Req-Hint 'https://www.docker.com/products/docker-desktop/'
    }
    [System.Windows.Forms.Application]::DoEvents()

    if ($dockerRunning) {
        Req-Ok 'WSL / virtualization: Docker is running, so its backend works'
    } elseif (Get-Command wsl -ErrorAction SilentlyContinue) {
        $w = Run-Quiet 'wsl' @('--status')
        if ($w.Code -eq 0) { Req-Ok 'WSL is installed' }
        else { Req-Warn 'WSL is present but reports a problem'; Req-Hint 'In an admin PowerShell: wsl --install, then restart. Check virtualization is on in Task Manager.' }
    } else {
        Req-Warn 'WSL not installed (Docker Desktop needs it, or Hyper-V on Windows Pro)'
        Req-Hint 'In an admin PowerShell: wsl --install, then restart.'
    }
    [System.Windows.Forms.Application]::DoEvents()

    # --- ollama and the model ---
    $tags = Get-Json 'http://localhost:11434/api/tags'
    if ($null -ne $tags) {
        $names = @($tags.models | ForEach-Object { $_.name })
        if ($names.Count -eq 0) { Req-Warn 'Ollama is running but has no models'; Req-Hint 'ollama pull phi4-mini' }
        elseif (@($names | Where-Object { $_ -like 'phi4-mini*' }).Count -gt 0) { Req-Ok ('Ollama is running with phi4-mini (' + $names.Count + ' model(s) in all)') }
        else { Req-Warn ('Ollama is running with ' + $names.Count + ' model(s), but not phi4-mini'); Req-Hint 'ollama pull phi4-mini' }
    } elseif (Get-Command ollama -ErrorAction SilentlyContinue) {
        Req-Miss 'Ollama is installed but not running'; Req-Hint 'Open the Ollama app, then check again.'
    } else {
        Req-Miss 'Ollama not found (the AI step needs it)'; Req-Hint 'https://ollama.com/download then: ollama pull phi4-mini'
    }
    [System.Windows.Forms.Application]::DoEvents()

    # --- keycloak and project files ---
    if (Test-Path (Join-Path $script:root 'keycloak\import\isa-realm.json')) { Req-Ok 'Realm file keycloak\import\isa-realm.json found' }
    else { Req-Warn 'Realm file keycloak\import\isa-realm.json missing'; Req-Hint 'Pull the latest changes, or use "Reset + rebuild realm".' }

    if (Test-Port 8080) {
        $realm = Get-Json 'http://localhost:8080/realms/isa/.well-known/openid-configuration'
        if ($null -ne $realm) { Req-Ok 'Keycloak is up and the isa realm is loaded' }
        else { Req-Warn 'Keycloak is up, but the isa realm is not loaded'; Req-Hint 'Click "Reset Keycloak".' }
    } else {
        Req-Warn 'Keycloak is not running'; Req-Hint 'Click "Start Keycloak" (or "Reset Keycloak" if it has no realm).'
    }

    $keysPath = Join-Path $script:root 'dev-keys.json'
    if (Test-Path $keysPath) {
        $sf = $null
        try { $sf = (Get-Content $keysPath -Raw | ConvertFrom-Json).salesforce } catch { }
        if ($sf -and $sf.clientId -and $sf.clientSecret -and ($sf.clientId -notlike 'PASTE_*') -and ($sf.clientSecret -notlike 'PASTE_*')) {
            Req-Ok 'dev-keys.json: Salesforce key and secret are filled in'
        } else {
            Req-Warn 'dev-keys.json has no real Salesforce key and secret yet'
            Req-Hint 'Enter them on the Dev Dashboard (Models, RAG, access & ticketing), or edit the file.'
        }
    } else {
        Req-Warn 'dev-keys.json not found (needed only for Salesforce tickets)'
        Req-Hint 'Copy dev-keys.example.json to dev-keys.json, or save the settings on the Dev Dashboard.'
    }

    if (Test-Port 5067) { Req-Ok 'Isa app is running on port 5067' }
    else { Req-Warn 'Isa app is not running (port 5067 is free)'; Req-Hint 'Click "Start Isa".' }

    $rtb.SelectionStart = 0
    $rtb.ScrollToCaret()
    $form.Cursor = [System.Windows.Forms.Cursors]::Default
}

Add-Btn $reqTop 'Check requirements' 'Looks for .NET, Docker, Ollama and phi4-mini, Keycloak, Python, Git and the settings files.' {
    try { Check-Requirements } catch { $form.Cursor = [System.Windows.Forms.Cursors]::Default; Show-Problem ('The check hit an error: ' + $_.Exception.Message) }
}

# ---- run -----------------------------------------------------------------

$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 10000
$timer.Add_Tick({ Update-Status })
$form.Add_Shown({ Update-Status; $timer.Start() })
$form.Add_FormClosed({ $timer.Stop() })

try {
    [void]$form.ShowDialog()
} catch {
    Show-Problem ("The launcher hit an error: " + $_.Exception.Message)
}
