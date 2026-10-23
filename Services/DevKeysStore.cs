using System.Text.Json;
using System.Text.Json.Nodes;

namespace IsaChatApp.Services;

/// <summary>
/// Reads and writes dev-keys.json in the project root. The Dev Dashboard
/// (Models, RAG, access &amp; ticketing) saves to it, and test-salesforce.ps1
/// reads the same file. Other sections of the file (future API keys) are left
/// untouched when the Salesforce section is saved.
/// </summary>
public sealed class DevKeysStore
{
    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    private readonly object _lock = new();

    public DevKeysStore(IHostEnvironment env)
    {
        FilePath = Path.Combine(env.ContentRootPath, "dev-keys.json");
    }

    public string FilePath { get; }

    public sealed record SalesforceKeys(string OrgUrl, string ApiVersion, string ClientId, bool SecretIsSet);

    /// <summary>What the ticketing code needs to sign in. Used only by SalesforceTicketService; never shown or logged.</summary>
    public sealed record SalesforceUse(string OrgUrl, string ApiVersion, string ClientId, string ClientSecret);

    /// <summary>The full settings for signing in, or null when any value is missing or still a placeholder.</summary>
    public SalesforceUse? GetSalesforceForUse()
    {
        lock (_lock)
        {
            var sf = ReadRoot()["salesforce"] as JsonObject;
            string Get(string name) => sf?[name]?.GetValue<string>() ?? "";
            var org = Get("orgUrl"); var ver = Get("apiVersion"); var id = Get("clientId"); var secret = Get("clientSecret");
            if (!IsReal(org) || !IsReal(ver) || !IsReal(id) || !IsReal(secret)) return null;
            return new SalesforceUse(org.TrimEnd('/'), ver, id, secret);
        }
    }

    /// <summary>Placeholder values left in the file count as "not set".</summary>
    private static bool IsReal(string? v) =>
        !string.IsNullOrWhiteSpace(v) && !v.StartsWith("PASTE_", StringComparison.OrdinalIgnoreCase);

    /// <summary>The secret itself is never returned, only whether one is saved.</summary>
    public SalesforceKeys GetSalesforce()
    {
        lock (_lock)
        {
            var sf = ReadRoot()["salesforce"] as JsonObject;
            string Get(string name) => sf?[name]?.GetValue<string>() ?? "";
            var clientId = Get("clientId");
            return new SalesforceKeys(
                Get("orgUrl"),
                Get("apiVersion"),
                IsReal(clientId) ? clientId : "",
                IsReal(Get("clientSecret")));
        }
    }

    /// <summary>A blank secret keeps the one already saved.</summary>
    public void SaveSalesforce(string orgUrl, string apiVersion, string clientId, string? clientSecret)
    {
        lock (_lock)
        {
            var root = ReadRoot();
            var sf = root["salesforce"] as JsonObject ?? new JsonObject();
            sf["orgUrl"] = orgUrl.Trim().TrimEnd('/');
            sf["apiVersion"] = apiVersion.Trim();
            sf["clientId"] = clientId.Trim();
            if (!string.IsNullOrWhiteSpace(clientSecret))
            {
                sf["clientSecret"] = clientSecret.Trim();
            }
            root["salesforce"] = sf;

            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, root.ToJsonString(Pretty));
            File.Move(temp, FilePath, overwrite: true);
        }
    }

    private JsonObject ReadRoot()
    {
        if (!File.Exists(FilePath)) return new JsonObject();
        var text = File.ReadAllText(FilePath);
        return string.IsNullOrWhiteSpace(text)
            ? new JsonObject()
            : JsonNode.Parse(text) as JsonObject ?? new JsonObject();
    }
}
