using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace IsaChatApp.Services;

/// <summary>What Isa was started with: where the services are, and their addresses. Recorded once in Program.cs.</summary>
public sealed record ServicesAtStart(string Location, string RemoteHost, IReadOnlyDictionary<string, string> Endpoints);

/// <summary>
/// The Dev Dashboard's "Services (this computer)" switches, saved to appsettings.Local.json in the project folder
/// (git-ignored, this computer only). Program.cs reads that file last, so it wins over appsettings.json.
///   Services:Location / Services:RemoteHost  where Keycloak, Ollama, the voice, embeddings and translation run (needs a restart)
///   Tts:Enabled                              the voice; off means Isa never calls it (applies at once)
///   Translation:Enabled                      the translation step; off means it is skipped (applies at once)
/// Anything else someone put in appsettings.Local.json is kept.
/// </summary>
public sealed partial class LocalServiceSettings
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly IConfiguration _config;

    public LocalServiceSettings(IConfiguration config, IHostEnvironment env)
    {
        _config = config;
        _filePath = Path.Combine(env.ContentRootPath, "appsettings.Local.json");
    }

    public string FileName => "appsettings.Local.json";
    public bool FileExists => File.Exists(_filePath);

    /// <summary>The saved choice (what the next start uses). Values not in the file come from appsettings.json.</summary>
    public string Location => _config["Services:Location"] ?? "Local";
    public string RemoteHost => _config["Services:RemoteHost"] ?? "";
    public bool VoiceOn => _config.GetValue("Tts:Enabled", false);
    public bool TranslationOn => _config.GetValue("Translation:Enabled", true);

    /// <summary>An IP address or a computer name only, so nothing else can be slipped into the service addresses.</summary>
    public static bool IsValidHost(string host) => HostPattern().IsMatch(host);

    [GeneratedRegex(@"^[A-Za-z0-9](?:[A-Za-z0-9.\-]{0,251}[A-Za-z0-9])?$")]
    private static partial Regex HostPattern();

    public void Save(string location, string remoteHost, bool voiceOn, bool translationOn)
    {
        location = location.Equals("Remote", StringComparison.OrdinalIgnoreCase) ? "Remote" : "Local";
        remoteHost = remoteHost.Trim();
        if (location == "Remote" && !IsValidHost(remoteHost))
            throw new ArgumentException("The remote host must be an IP address or a computer name, for example 100.89.6.69.");

        lock (_lock)
        {
            JsonObject root;
            try { root = File.Exists(_filePath) ? JsonNode.Parse(File.ReadAllText(_filePath)) as JsonObject ?? new() : new(); }
            catch { root = new(); }   // a damaged file is rewritten

            Section(root, "Services")["Location"] = location;
            if (remoteHost.Length > 0) Section(root, "Services")["RemoteHost"] = remoteHost;
            Section(root, "Tts")["Enabled"] = voiceOn;
            Section(root, "Translation")["Enabled"] = translationOn;

            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, root.ToJsonString(Pretty));
            File.Move(temp, _filePath, overwrite: true);
        }
        // Isa re-reads appsettings.Local.json when it changes; give it a moment so the page shows the new values.
        Thread.Sleep(300);
    }

    /// <summary>Back to appsettings.json (deletes appsettings.Local.json).</summary>
    public void Reset()
    {
        lock (_lock)
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }
        Thread.Sleep(300);
    }

    private static JsonObject Section(JsonObject root, string name)
    {
        if (root[name] is JsonObject s) return s;
        var created = new JsonObject();
        root[name] = created;
        return created;
    }
}
