# stop-isa.ps1 -- "Stop Isa": stops Isa and her services (installed copy).
#   -Remove   also removes the services and their downloaded models (used by the uninstaller when asked).

param([switch]$Remove)
. (Join-Path $PSScriptRoot 'common.ps1')
$ErrorActionPreference = 'Continue'

if (Test-Path $PidFile) {
    $p = Get-Process -Id ([int](Get-Content $PidFile)) -ErrorAction SilentlyContinue
    if ($p -ne $null -and $p.ProcessName -eq 'IsaChatApp') { Say 'Stopping Isa ...'; Stop-Process -Id $p.Id -Force }
    Remove-Item $PidFile -ErrorAction SilentlyContinue
}
Get-Process -Name 'IsaChatApp' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($AppDir, [StringComparison]::OrdinalIgnoreCase) } |
    ForEach-Object { Stop-Process -Id $_.Id -Force }

$dockerUp = $false
try { $null = & docker info --format '{{.ServerVersion}}' 2>$null; $dockerUp = ($LASTEXITCODE -eq 0) } catch { }
if ($dockerUp) {
    if ($Remove) {
        Say 'Removing the services and their downloaded models ...'
        $a = (Get-ComposeArgs) + @('down', '-v', '--rmi', 'local')
    } else {
        Say 'Stopping the services ...'
        $a = (Get-ComposeArgs) + @('stop')
    }
    & docker @a
} else {
    Say 'Docker Desktop is not running, so the services are already stopped.'
}
if ($Remove) { Remove-Item $Marker -ErrorAction SilentlyContinue }
Say 'Done.'
Start-Sleep -Seconds 2
