# Lets teammates' computers and phones on your Tailscale network reach Isa and the services it uses on this computer,
# with nothing opened to the wider network. Tailscale forwards each port into this computer's localhost, inside its
# encrypted tunnel.
#
#   5067  Isa (browsers)                 8091  translation (NLLB-200)
#   8080  Keycloak (sign-in)             8092  embeddings (multilingual-e5-base)
#   8880  voice (Kokoro)
# A computer running its own Isa against these services: Dev Dashboard > Services (this computer) > Remote, then restart Isa.
#   Ollama (11434) is not forwarded here: it is reached through its own network setting (OLLAMA_HOST).
#
#   .\tailscale-phone.ps1 -Mode On      start forwarding
#   .\tailscale-phone.ps1 -Mode Status  show what is forwarded and this computer's tailnet name
#   .\tailscale-phone.ps1 -Mode Off     stop forwarding
#
# After "On", this computer's tailnet address must be listed in appsettings.Development.json:
#   "Keycloak": { "AdditionalIssuers": [ "http://<address>:8080/realms/isa" ] }
# then restart Isa. The Android app's server field takes the same address (the 100.x number or the name).
param([ValidateSet('On','Off','Status')][string]$Mode = 'Status')

$ts = (Get-Command tailscale -ErrorAction SilentlyContinue)
if (-not $ts) { $p = "$env:ProgramFiles\Tailscale\tailscale.exe"; if (Test-Path $p) { $ts = $p } else { Write-Host "Tailscale is not installed on this computer."; exit 1 } }
$tsExe = if ($ts -is [string]) { $ts } else { $ts.Source }

# Port, what it is, and how to start it.
$services = @(
  @{ Port = 5067; Name = 'Isa';        Start = 'start-isa.bat' },
  @{ Port = 8080; Name = 'Keycloak';   Start = 'start-keycloak.bat' },
  @{ Port = 8880; Name = 'voice';      Start = 'start-voice.bat' },
  @{ Port = 8091; Name = 'translation'; Start = 'start-translation.bat' },
  @{ Port = 8092; Name = 'embeddings'; Start = 'start-embeddings.bat' }
)

switch ($Mode) {
  'On'  { foreach ($s in $services) { & $tsExe serve --bg --tcp $s.Port ("tcp://localhost:" + $s.Port) } }
  'Off' { foreach ($s in $services) { & $tsExe serve ("--tcp=" + $s.Port) off } }
}
Write-Host ""
& $tsExe serve status
$st = & $tsExe status --json | ConvertFrom-Json
$name = $st.Self.DNSName.TrimEnd('.')
$ip = ($st.Self.TailscaleIPs | Where-Object { $_ -match '^\d+\.' } | Select-Object -First 1)
Write-Host ""
Write-Host ("This computer's tailnet name: " + $name + "  (" + $ip + ")")
Write-Host ("Isa in a browser:   http://" + $ip + ":5067")
Write-Host ("Keycloak:           http://" + $ip + ":8080/realms/isa")
Write-Host ("Voice:              http://" + $ip + ":8880/v1/models")
Write-Host ("Translation:        http://" + $ip + ":8091/health")
Write-Host ("Embeddings:         http://" + $ip + ":8092/health")
Write-Host ("Ollama:             http://" + $ip + ":11434  (only if OLLAMA_HOST lets it listen beyond localhost)")

# Forwarding only helps if the service is running here.
foreach ($s in $services) {
  # Tailscale's own forwarder listens on the tailnet address, so count only listeners that localhost reaches.
  $local = Get-NetTCPConnection -State Listen -LocalPort $s.Port -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalAddress -in '127.0.0.1', '::1', '0.0.0.0', '::' }
  if (-not $local) {
    Write-Host ("Note: " + $s.Name + " (port " + $s.Port + ") is not running yet. Start it with " + $s.Start + ".")
  }
}
