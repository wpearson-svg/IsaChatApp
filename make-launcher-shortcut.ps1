# make-launcher-shortcut.ps1 -- puts an "Isa launcher" shortcut with the Isa icon on your Desktop.
$root = $PSScriptRoot
$desktop = [Environment]::GetFolderPath('Desktop')
$link = Join-Path $desktop 'Isa launcher.lnk'
$shell = New-Object -ComObject WScript.Shell
$s = $shell.CreateShortcut($link)
$s.TargetPath = Join-Path $root 'isa-launcher.bat'
$s.WorkingDirectory = $root
$s.IconLocation = (Join-Path $root 'isa-icon.ico') + ',0'
$s.WindowStyle = 7
$s.Description = 'Isa launcher'
$s.Save()
Write-Host ('Shortcut created: ' + $link)
