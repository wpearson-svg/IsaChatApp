using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IsaChatApp.Services;

/// <summary>
/// Creates a Salesforce Case when Isa passes a question to a person (the ticketing stage).
/// Uses the same settings as test-salesforce.ps1: dev-keys.json (saved on the Dev Dashboard),
/// client-credentials sign-in, one Case with the six custom fields.
///
/// Off unless Ticketing:Salesforce:Enabled is true. If Salesforce is down or refuses, the
/// leader still gets Isa's reference number, and the run records salesforce=failed.
/// The key, secret and token are never logged, returned or put in an error message.
/// </summary>
public class SalesforceTicketService
{
    private readonly IHttpClientFactory _http;
    private readonly DevKeysStore _keys;
    private readonly IConfiguration _config;
    private readonly ILogger<SalesforceTicketService> _log;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _token;
    private string? _instanceUrl;
    private DateTime _tokenExpiresUtc = DateTime.MinValue;

    public SalesforceTicketService(IHttpClientFactory http, DevKeysStore keys, IConfiguration config, ILogger<SalesforceTicketService> log)
    {
        _http = http; _keys = keys; _config = config; _log = log;
    }

    public bool Enabled => _config.GetValue("Ticketing:Salesforce:Enabled", false);

    public sealed record CaseInput(
        string IsaReference, string TraceId, string Language,
        string EscalatedBy, string? Reason, string? ChapterId, string? ChapterRole, bool SafetyTopic,
        string? RequestedBy = null, string? PersonId = null);

    /// <summary>One Isa Case, as the presentation's live Cases panel shows it.</summary>
    public sealed record CaseRow(string CaseNumber, string? IsaReference, string? Status, string? Priority,
        DateTimeOffset? CreatedAt, string RequestedBy, string Chapter, string? EscalatedBy);

    /// <summary>The newest Isa Cases, or a short error word (never a secret).</summary>
    public sealed record CaseList(bool Ok, IReadOnlyList<CaseRow> Rows, string? Error);

    /// <summary>Reads the newest Cases that Isa created (they carry an Isa reference). Read only.</summary>
    public async Task<CaseList> ListRecentCasesAsync(int count, CancellationToken ct = default)
    {
        if (!Enabled) return new CaseList(false, [], "off");
        try
        {
            var sf = _keys.GetSalesforceForUse();
            if (sf is null) return new CaseList(false, [], "not_configured");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var (token, baseUrl, tokenError) = await GetTokenAsync(sf, timeout.Token);
            if (token is null) return new CaseList(false, [], tokenError ?? "token_failed");

            var soql = "SELECT CaseNumber, Status, Priority, CreatedDate, Description, Isa_Ticket_Ref__c, Chapter_ID__c, Chapter_Role__c, Escalated_By__c " +
                       $"FROM Case WHERE Isa_Ticket_Ref__c != null ORDER BY CreatedDate DESC LIMIT {Math.Clamp(count, 1, 20)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/services/data/{sf.ApiVersion}/query?q={Uri.EscapeDataString(soql)}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var resp = await _http.CreateClient("salesforce").SendAsync(req, timeout.Token);
            if (!resp.IsSuccessStatusCode)
            {
                if ((int)resp.StatusCode == 401) _tokenExpiresUtc = DateTime.MinValue;
                return new CaseList(false, [], "http_" + (int)resp.StatusCode);
            }
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(timeout.Token));
            var rows = new List<CaseRow>();
            foreach (var r in doc.RootElement.GetProperty("records").EnumerateArray())
            {
                string? Str(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                var description = Str("Description") ?? "";
                string Line(string prefix) => description.Split('\n').Select(l => l.TrimEnd('\r'))
                    .FirstOrDefault(l => l.StartsWith(prefix, StringComparison.Ordinal))?[prefix.Length..].Trim() ?? "";
                DateTimeOffset? created = DateTimeOffset.TryParse((Str("CreatedDate") ?? "").Replace("+0000", "+00:00"), out var dt) ? dt : null;
                var chapter = Str("Chapter_ID__c");
                var role = Str("Chapter_Role__c");
                rows.Add(new CaseRow(Str("CaseNumber") ?? "", Str("Isa_Ticket_Ref__c"), Str("Status"), Str("Priority"), created,
                    Line("Requested by:"),
                    string.IsNullOrWhiteSpace(chapter) ? "" : string.IsNullOrWhiteSpace(role) ? chapter : $"{chapter} ({role})",
                    Str("Escalated_By__c")));
            }
            return new CaseList(true, rows, null);
        }
        catch (OperationCanceledException) { return new CaseList(false, [], "timeout"); }
        catch (Exception ex)
        {
            _log.LogWarning("Salesforce case list failed: {Type}", ex.GetType().Name);
            return new CaseList(false, [], ex.GetType().Name);
        }
    }

    /// <summary>Readiness check, no Case created: "off", "not_configured", "ok", or a short error word.</summary>
    public async Task<string> CheckAsync(CancellationToken ct = default)
    {
        if (!Enabled) return "off";
        try
        {
            var sf = _keys.GetSalesforceForUse();
            if (sf is null) return "not_configured";
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var (token, _, err) = await GetTokenAsync(sf, timeout.Token);
            return token is null ? (err ?? "token_failed") : "ok";
        }
        catch (OperationCanceledException) { return "timeout"; }
        catch (Exception ex) { return ex.GetType().Name; }
    }

    /// <summary>Case Id on success, or an error word (never a secret) on failure.</summary>
    public sealed record CaseResult(bool Ok, string? CaseId, string? Error);

    public async Task<CaseResult> CreateCaseAsync(CaseInput c, CancellationToken ct = default)
    {
        var seconds = Math.Clamp(_config.GetValue("Ticketing:Salesforce:TimeoutSeconds", 10), 3, 60);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(seconds));
        try
        {
            var sf = _keys.GetSalesforceForUse();
            if (sf is null) return new CaseResult(false, null, "not_configured");

            var (token, baseUrl, tokenError) = await GetTokenAsync(sf, timeout.Token);
            if (token is null) return new CaseResult(false, null, tokenError ?? "token_failed");

            var body = new JsonObject
            {
                ["Subject"] = "Isa escalation: " + c.IsaReference,
                ["Description"] = $"Requested by: {(string.IsNullOrWhiteSpace(c.RequestedBy) ? "unknown" : c.RequestedBy)}"
                                  + (string.IsNullOrWhiteSpace(c.PersonId) ? "" : $" (person {c.PersonId})")
                                  + $"\nChapter: {(string.IsNullOrWhiteSpace(c.ChapterId) ? "unknown" : c.ChapterId)} ({(string.IsNullOrWhiteSpace(c.ChapterRole) ? "role unknown" : c.ChapterRole)})"
                                  + $"\nLanguage: {c.Language}\nEscalated by: {c.EscalatedBy}\nReason: {c.Reason ?? "none"}\nIsa reference: {c.IsaReference}"
                                  + (c.SafetyTopic ? "\nSAFETY TOPIC: please contact this person promptly." : ""),
                ["Status"] = "New",
                ["Origin"] = "Web",
                ["Priority"] = c.SafetyTopic ? "High" : "Medium",
                ["Trace_ID__c"] = c.TraceId,
                ["Isa_Ticket_Ref__c"] = c.IsaReference,
                ["Language__c"] = c.Language,
                ["Escalated_By__c"] = c.EscalatedBy,
                ["Chapter_ID__c"] = c.ChapterId ?? "",
                ["Chapter_Role__c"] = c.ChapterRole ?? "",
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/services/data/{sf.ApiVersion}/sobjects/Case")
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var resp = await _http.CreateClient("salesforce").SendAsync(req, timeout.Token);
            if (!resp.IsSuccessStatusCode)
            {
                if ((int)resp.StatusCode == 401) _tokenExpiresUtc = DateTime.MinValue;   // sign in again next time
                return new CaseResult(false, null, "http_" + (int)resp.StatusCode);
            }
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(timeout.Token));
            return new CaseResult(true, doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null, null);
        }
        catch (OperationCanceledException) { return new CaseResult(false, null, "timeout"); }
        catch (Exception ex)
        {
            _log.LogWarning("Salesforce case failed: {Type}", ex.GetType().Name);   // type only, no message
            return new CaseResult(false, null, ex.GetType().Name);
        }
    }

    private async Task<(string? Token, string BaseUrl, string? Error)> GetTokenAsync(DevKeysStore.SalesforceUse sf, CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_token is not null && DateTime.UtcNow < _tokenExpiresUtc)
                return (_token, _instanceUrl ?? sf.OrgUrl, null);

            using var resp = await _http.CreateClient("salesforce").PostAsync($"{sf.OrgUrl}/services/oauth2/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = sf.ClientId,
                    ["client_secret"] = sf.ClientSecret,
                }), ct);
            if (!resp.IsSuccessStatusCode) return (null, sf.OrgUrl, "token_http_" + (int)resp.StatusCode);
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            _token = doc.RootElement.GetProperty("access_token").GetString();
            _instanceUrl = doc.RootElement.TryGetProperty("instance_url", out var iu) ? iu.GetString() : null;
            _tokenExpiresUtc = DateTime.UtcNow.AddMinutes(25);   // refreshed well before it can lapse
            return (_token, _instanceUrl ?? sf.OrgUrl, null);
        }
        finally { _tokenLock.Release(); }
    }
}
