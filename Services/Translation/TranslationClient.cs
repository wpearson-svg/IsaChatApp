using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Translation;

/// <summary>
/// Calls the translation service (Eliyas's E1 service, ai-services/translation: NLLB-200). It knows English ("eng")
/// and Amharic ("amh") only. A service that is down, slow, or answers fallback=true (its model is not loaded) gives
/// a null Text with a short note, never an exception and never the untranslated text passed off as translated.
/// The text sent is never logged. Settings: Translation:Enabled (default true), Translation:Endpoint
/// (default http://localhost:8091), Translation:TimeoutSeconds (default 30, per call).
/// </summary>
public sealed class TranslationClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<TranslationClient> _log;

    public TranslationClient(IHttpClientFactory factory, IConfiguration config, ILogger<TranslationClient> log)
    {
        _factory = factory;
        _config = config;
        _log = log;
    }

    /// <summary>Isa's two-letter language code to the translation service's code. The service must know the code too
    /// (ai-services/translation/app.py: its LANG map and the language check on the request). Today the service
    /// knows English and Amharic; "es" works once "spa" is added there. A code the service rejects (HTTP 422) comes back
    /// as note "language_not_supported_by_service" and the text goes on unchanged.</summary>
    public static readonly IReadOnlyDictionary<string, string> ServiceCodes =
        new Dictionary<string, string> { ["am"] = "amh", ["es"] = "spa" };

    public static string? ServiceCode(string? isaCode) =>
        isaCode is not null && ServiceCodes.TryGetValue(isaCode, out var c) ? c : null;

    public bool Enabled => _config.GetValue("Translation:Enabled", true);

    /// <param name="Text">The translation, or null when it could not be done.</param>
    /// <param name="Note">"ok", "fallback" or "service_unavailable".</param>
    public sealed record Result(string? Text, string Note, long Ms);

    /// <summary>Is the service reachable, and has its model loaded? For the Dev Dashboard's translation test.</summary>
    public async Task<(bool Reachable, bool ModelReady, string ModelId)> HealthAsync(CancellationToken ct = default)
    {
        var endpoint = (_config["Translation:Endpoint"] ?? "http://localhost:8091").TrimEnd('/');
        try
        {
            var client = _factory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            using var doc = JsonDocument.Parse(await client.GetStringAsync($"{endpoint}/health", ct));
            var root = doc.RootElement;
            return (true,
                root.TryGetProperty("model_ready", out var r) && r.ValueKind == JsonValueKind.True,
                root.TryGetProperty("model_id", out var m) ? m.GetString() ?? "" : "");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return (false, false, "");
        }
    }

    /// <summary>One call. Keep the text short (a sentence or two): the model truncates long input.</summary>
    public async Task<Result> TranslateAsync(string text, string from, string to, CancellationToken ct = default)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var endpoint = (_config["Translation:Endpoint"] ?? "http://localhost:8091").TrimEnd('/');
        try
        {
            var client = _factory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(_config.GetValue("Translation:TimeoutSeconds", 30));
            using var resp = await client.PostAsJsonAsync($"{endpoint}/translate",
                new { text, source_lang = from, target_lang = to }, ct);
            if ((int)resp.StatusCode == 422)
                return new Result(null, "language_not_supported_by_service", clock.ElapsedMilliseconds);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<Response>(cancellationToken: ct);
            if (body is null || body.Fallback || string.IsNullOrWhiteSpace(body.TranslatedText))
                return new Result(null, "fallback", clock.ElapsedMilliseconds);
            return new Result(body.TranslatedText, "ok", clock.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _log.LogWarning("Translation service did not answer ({Reason})", ex.GetType().Name);
            return new Result(null, "service_unavailable", clock.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// A whole message: split into short pieces (lines, then sentences, joined up to about 350 characters) so the
    /// model does not truncate, translated one piece at a time. If ANY piece fails the whole result is null, so a
    /// person never gets half a message in one language and half in another.
    /// </summary>
    public async Task<Result> TranslateMessageAsync(string message, string from, string to, CancellationToken ct = default)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var outLines = new List<string>();
        foreach (var line in message.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) { outLines.Add(""); continue; }
            var pieces = new List<string>();
            var cur = "";
            foreach (var sentence in Regex.Split(line.Trim(), @"(?<=[.!?])\s+"))
            {
                if (cur.Length > 0 && cur.Length + sentence.Length + 1 > 350) { pieces.Add(cur); cur = sentence; }
                else cur = cur.Length == 0 ? sentence : cur + " " + sentence;
            }
            if (cur.Length > 0) pieces.Add(cur);

            var translated = new List<string>();
            foreach (var piece in pieces)
            {
                var r = await TranslateAsync(piece, from, to, ct);
                if (r.Text is null) return new Result(null, r.Note, clock.ElapsedMilliseconds);
                translated.Add(r.Text);
            }
            outLines.Add(string.Join(" ", translated));
        }
        return new Result(string.Join("\n", outLines), "ok", clock.ElapsedMilliseconds);
    }

    private sealed record Response(
        [property: JsonPropertyName("translated_text")] string? TranslatedText,
        [property: JsonPropertyName("model_id")] string? ModelId,
        [property: JsonPropertyName("fallback")] bool Fallback);
}
