# services-config.ps1 -- where Isa's services run, read the way Isa reads it (Program.cs):
# appsettings.json first, then appsettings.Local.json (this computer only) wins.
# Services:Location = Remote with a valid RemoteHost means Keycloak, Ollama, the voice, embeddings and translation
# keep their port but live on RemoteHost (the home PC over Tailscale). Dot-source this file, then call
# Get-ServiceUrl 'keycloak' (or 'ollama'). The Isa app itself still runs on this computer.
function Get-ServicesLocation {
    $loc = 'Local'; $svcHost = ''
    foreach ($f in 'appsettings.json', 'appsettings.Local.json') {
        $p = Join-Path $PSScriptRoot $f
        if (-not (Test-Path $p)) { continue }
        try {
            $s = (Get-Content $p -Raw | ConvertFrom-Json).Services
            if ($s.Location) { $loc = [string]$s.Location }
            if ($s.RemoteHost) { $svcHost = [string]$s.RemoteHost }
        } catch { }
    }
    $remote = ($loc -eq 'Remote') -and ($svcHost -match '^[A-Za-z0-9](?:[A-Za-z0-9.\-]{0,251}[A-Za-z0-9])?$')
    return @{ Remote = $remote; Host = $(if ($remote) { $svcHost } else { 'localhost' }) }
}

function Get-ServiceUrl([string]$Name) {
    $ports = @{ keycloak = 8080; ollama = 11434; voice = 8880; translation = 8091; embeddings = 8092 }
    $where = Get-ServicesLocation
    return "http://$($where.Host):$($ports[$Name.ToLower()])"
}
