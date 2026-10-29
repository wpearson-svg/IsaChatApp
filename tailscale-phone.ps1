# Lets a phone on your Tailscale network reach Isa and its sign-in, with nothing opened to the wider network.
# Tailscale forwards the two ports into this laptop's localhost, inside its encrypted tunnel.
#
#   .\tailscale-phone.ps1 -Mode On      start forwarding
#   .\tailscale-phone.ps1 -Mode Status  show what is forwarded and this laptop's tailnet name
#   .\tailscale-phone.ps1 -Mode Off     stop forwarding
#
# After "On", the laptop's tailnet address must be listed in appsettings.Development.json (already set for 100.124.41.97):
#   "Keycloak": { "AdditionalIssuers": [ "http://<address>:8080/realms/isa" ] }
# then restart Isa. The Android app's server field takes the same address (the 100.x number or the name).
param([ValidateSet('On','Off','Status')][string]$Mode = 'Status')

$ts = (Get-Command tailscale -ErrorAction SilentlyContinue)
if (-not $ts) { $p = "$env:ProgramFiles\Tailscale\tailscale.exe"; if (Test-Path $p) { $ts = $p } else { Write-Host "Tailscale is not installed on this computer."; exit 1 } }
$tsExe = if ($ts -is [string]) { $ts } else { $ts.Source }

switch ($Mode) {
  'On' {
    & $tsExe serve --bg --tcp 5067 tcp://localhost:5067
    & $tsExe serve --bg --tcp 8080 tcp://localhost:8080
  }
  'Off' {
    & $tsExe serve --tcp=5067 off
    & $tsExe serve --tcp=8080 off
  }
}
Write-Host ""
& $tsExe serve status
$st = & $tsExe status --json | ConvertFrom-Json
Write-Host ""
Write-Host ("This laptop's tailnet name: " + $st.Self.DNSName.TrimEnd('.'))
