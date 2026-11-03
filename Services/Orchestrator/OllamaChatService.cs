using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace IsaChatApp.Services.Orchestrator;

/// <summary>
/// Calls the local model (phi4-mini through Ollama) for the complex_ai stage.
/// Ported from Eliyas's Pipeline Built package (22 Oct 2026), with changes: it never throws,
/// it classifies a failure as timeout or unavailable (stage contract section 9), asks for
/// temperature 0 and a capped reply length so timing is steady, keeps the model loaded
/// between questions, and reads Ollama's token counts for the Content Health dashboard.
/// Settings (appsettings.json "Ollama"): Endpoint, Model, TimeoutSeconds (default 30), NumPredict, KeepAlive.
/// </summary>
public sealed class OllamaChatService
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _config;
    private readonly ILogger<OllamaChatService> _log;

    /// <summary>
    /// Presentation mode (Isa Configuration, or Presentation:Mode in appsettings.json). On: the model is loaded
    /// at start and kept for Ollama:KeepAlive (60 minutes). Off (the default): it loads when a question needs it
    /// and is let go after Ollama:IdleKeepAlive (2 minutes), so it does not hold memory the rest of the time.
    /// </summary>
    public bool PresentationMode { get; private set; }

    /// <summary>The keep-alive for a request right now, by mode.</summary>
    private string KeepAliveNow() => PresentationMode
        ? _config["Ollama:KeepAlive"] ?? "60m"
        : _config["Ollama:IdleKeepAlive"] ?? "2m";

    /// <summary>Switches presentation mode. On loads the model in the background; off unloads it at once.</summary>
    public void SetPresentationMode(bool on)
    {
        if (PresentationMode == on) return;
        PresentationMode = on;
        _ = Task.Run(async () => { if (on) await WarmAsync(); else await UnloadAsync(); });
    }

    public OllamaChatService(IHttpClientFactory clients, IConfiguration config, ILogger<OllamaChatService> log)
    {
        _clients = clients;
        _config = config;
        _log = log;
        PresentationMode = config.GetValue("Presentation:Mode", false);
    }

    /// <summary>
    /// Warm-up for the presentation: loads the model into memory with a one-token request,
    /// using the normal keep-alive. Never throws. Returns true when Ollama answered.
    /// </summary>
    public async Task<bool> WarmAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = _config["Ollama:Endpoint"] ?? "http://localhost:11434";
        var model = _config["Ollama:Model"] ?? "phi4-mini";
        var keepAlive = KeepAliveNow();
        var clock = Stopwatch.StartNew();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(90));
            using var response = await _clients.CreateClient("ollama").PostAsJsonAsync(
                $"{endpoint.TrimEnd('/')}/api/generate",
                new { model, prompt = "Reply with OK.", stream = false, keep_alive = keepAlive, options = new { temperature = 0, num_predict = 3 } },
                timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            // The model's own reply is written to the Isa log, so a warm-up can be checked afterwards:
            // look for "Model warm-up" in isa-run.log. Then it asks Ollama whether the model is loaded and until when.
            var reply = "";
            try { using var j = JsonDocument.Parse(body); reply = j.RootElement.TryGetProperty("response", out var r) ? r.GetString() ?? "" : ""; } catch { }
            var status = await StatusAsync(cancellationToken);
            _log.LogInformation("Model warm-up: http={Status} model={Model} reply='{Reply}' took={Ms}ms keep_alive_sent={KeepAlive} loaded_now={Loaded} ollama_expires_at={Expires} now_utc={Now:O}",
                (int)response.StatusCode, model, reply.Trim(), clock.ElapsedMilliseconds, keepAlive, status.ModelLoaded, status.ExpiresAt, DateTime.UtcNow);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.LogInformation(ex, "Model warm-up did not complete after {Ms}ms (model={Model}, endpoint={Endpoint})", clock.ElapsedMilliseconds, model, endpoint);
            return false;
        }
    }

    /// <summary>
    /// Unloads the model from memory now (keep_alive 0), so it does not sit there for the rest of the
    /// keep-alive time after Isa stops. Never throws. Returns true when Ollama answered.
    /// </summary>
    public async Task<bool> UnloadAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = _config["Ollama:Endpoint"] ?? "http://localhost:11434";
        var model = _config["Ollama:Model"] ?? "phi4-mini";
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await _clients.CreateClient("ollama").PostAsJsonAsync(
                $"{endpoint.TrimEnd('/')}/api/generate",
                new { model, keep_alive = 0 },
                timeout.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.LogInformation(ex, "Model unload did not complete");
            return false;
        }
    }

    /// <summary>Is Ollama reachable, is the model installed, is it loaded in memory right now. Never throws.</summary>
    public async Task<OllamaStatus> StatusAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = (_config["Ollama:Endpoint"] ?? "http://localhost:11434").TrimEnd('/');
        var model = _config["Ollama:Model"] ?? "phi4-mini";
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(4));
            var client = _clients.CreateClient("ollama");
            bool Has(JsonElement root) =>
                root.TryGetProperty("models", out var arr) && arr.ValueKind == JsonValueKind.Array &&
                arr.EnumerateArray().Any(m => m.TryGetProperty("name", out var n) && (n.GetString() ?? "").StartsWith(model, StringComparison.OrdinalIgnoreCase));

            using var tags = JsonDocument.Parse(await client.GetStringAsync($"{endpoint}/api/tags", timeout.Token));
            var installed = Has(tags.RootElement);
            var loaded = false;
            string? expires = null;
            try
            {
                using var ps = JsonDocument.Parse(await client.GetStringAsync($"{endpoint}/api/ps", timeout.Token));
                loaded = Has(ps.RootElement);
                // Ollama's own answer to "until when is it kept": the expires_at it applied, not what Isa asked for.
                if (loaded && ps.RootElement.TryGetProperty("models", out var arr))
                    foreach (var m in arr.EnumerateArray())
                        if (m.TryGetProperty("name", out var n) && (n.GetString() ?? "").StartsWith(model, StringComparison.OrdinalIgnoreCase)
                            && m.TryGetProperty("expires_at", out var e)) expires = e.GetString();
            }
            catch { /* older Ollama without /api/ps: treat as not loaded */ }
            return new OllamaStatus(true, installed, loaded, expires);
        }
        catch
        {
            return new OllamaStatus(false, false, false, null);
        }
    }

    /// <param name="endpointOverride">Test only: send the call somewhere else (an address with nothing listening) to test the "model unavailable" path.</param>
    public async Task<OllamaAnswer> GenerateAsync(string prompt, string? endpointOverride = null, CancellationToken cancellationToken = default)
    {
        var endpoint = endpointOverride ?? _config["Ollama:Endpoint"] ?? "http://localhost:11434";
        var model = _config["Ollama:Model"] ?? "phi4-mini";
        var timeoutSeconds = Math.Clamp(_config.GetValue<int?>("Ollama:TimeoutSeconds") ?? 30, 5, 180);
        var numPredict = Math.Clamp(_config.GetValue<int?>("Ollama:NumPredict") ?? 300, 50, 2000);
        var keepAlive = KeepAliveNow();

        var client = _clients.CreateClient("ollama");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var clock = Stopwatch.StartNew();

        try
        {
            using var response = await client.PostAsJsonAsync(
                $"{endpoint.TrimEnd('/')}/api/generate",
                new { model, prompt, stream = false, keep_alive = keepAlive, options = new { temperature = 0, num_predict = numPredict } },
                timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var text = root.TryGetProperty("response", out var r) ? r.GetString() ?? "" : "";
            int? promptTokens = root.TryGetProperty("prompt_eval_count", out var pe) && pe.TryGetInt32(out var p) ? p : null;
            int? completionTokens = root.TryGetProperty("eval_count", out var ec) && ec.TryGetInt32(out var c) ? c : null;
            clock.Stop();
            long Ms(string name) => root.TryGetProperty(name, out var d) && d.TryGetInt64(out var ns) ? ns / 1_000_000 : -1;
            _log.LogInformation("Ollama answer: took={Ms}ms load={Load}ms prompt_eval={PromptEval}ms eval={Eval}ms prompt_tokens={PT} keep_alive_sent={KeepAlive} presentation_mode={Mode}",
                clock.ElapsedMilliseconds, Ms("load_duration"), Ms("prompt_eval_duration"), Ms("eval_duration"), promptTokens, keepAlive, PresentationMode);
            return new OllamaAnswer(model, text.Trim(), clock.ElapsedMilliseconds, null, promptTokens, completionTokens);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            clock.Stop();
            // What Ollama says about the model right now, so a timeout can be told apart from "the model was unloaded".
            var st = await StatusAsync();
            _log.LogWarning("Ollama timed out after {Ms}ms: keep_alive_sent={KeepAlive} presentation_mode={Mode} loaded_now={Loaded} ollama_expires_at={Expires} now_utc={Now:O}",
                clock.ElapsedMilliseconds, keepAlive, PresentationMode, st.ModelLoaded, st.ExpiresAt, DateTime.UtcNow);
            return new OllamaAnswer(model, "", clock.ElapsedMilliseconds, "timeout", null, null);
        }
        catch (Exception ex)
        {
            clock.Stop();
            _log.LogWarning(ex, "Ollama request failed for model {Model} at {Endpoint}", model, endpoint);
            // A short, safe hint for the event log: the HTTP status (404 usually means the model name is not installed) or the exception type.
            var detail = ex is HttpRequestException h && h.StatusCode is { } sc ? $"http_{(int)sc}" : ex.GetType().Name;
            return new OllamaAnswer(model, "", clock.ElapsedMilliseconds, "unavailable", null, null, detail);
        }
    }
}

/// <param name="Detail">For an "unavailable" error: the HTTP status or exception type, written to the event log to help find the cause.</param>
/// <param name="Error">null when it worked; otherwise "timeout" or "unavailable" (the contract's error values).</param>
public sealed record OllamaAnswer(string Model, string Text, long ElapsedMs, string? Error, int? PromptTokens, int? CompletionTokens, string? Detail = null);

public sealed record OllamaStatus(bool Reachable, bool ModelInstalled, bool ModelLoaded, string? ExpiresAt = null);
