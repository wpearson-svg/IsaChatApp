using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace IsaChatApp.Services.Tts;

/// <summary>
/// Isa's spoken voice for the Library's Listen button (27 October 2026). Asks a Kokoro-82M speech
/// service that runs on this machine (docker compose, port 8880) to read one sentence in the chosen
/// voice (bf_lily), and keeps every sentence it makes in data/tts-cache so each is only made once.
/// Nothing leaves this machine. It never throws: when the speech service is off or slow it returns
/// null and the player falls back to the device's own voice.
/// Settings (appsettings.json "Tts"): Enabled, Endpoint, Voice, Model, PreGenerate.
/// </summary>
public sealed class KokoroTtsService
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _config;
    private readonly ILogger<KokoroTtsService> _log;
    private readonly string _cacheDir;
    private readonly SemaphoreSlim _one = new(1, 1);   // one sentence is made at a time; it is a CPU voice
    private int _listenersWaiting;   // sentences a listener is waiting for: the background pre-making steps aside for them

    public KokoroTtsService(IHttpClientFactory clients, IConfiguration config, IWebHostEnvironment env, ILogger<KokoroTtsService> log)
    {
        _clients = clients;
        _config = config;
        _log = log;
        _cacheDir = Path.Combine(env.ContentRootPath, "data", "tts-cache");
    }

    public bool Enabled => _config.GetValue("Tts:Enabled", false);
    public string Voice => _config["Tts:Voice"] ?? "bf_lily";

    /// <summary>The audio file (mp3) for this text, from the cache or newly made. Null if it can't be made.</summary>
    /// <param name="noCache">Speed tests only: make it fresh into a temporary file the caller deletes, so the time is real.</param>
    public async Task<string?> GetOrCreateAsync(string text, CancellationToken ct = default, bool background = false, bool noCache = false)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(text)) return null;
        var path = noCache ? Path.Combine(Path.GetTempPath(), "isa-tts-" + Guid.NewGuid().ToString("N") + ".mp3") : PathFor(text);
        if (!noCache && File.Exists(path)) return path;

        // Someone listening comes first: the background pre-making waits while any listener is waiting,
        // so Listen is never stuck behind it (it was making every second sentence slow).
        if (background) { while (Volatile.Read(ref _listenersWaiting) > 0) await Task.Delay(250, ct); }
        else Interlocked.Increment(ref _listenersWaiting);
        var counted = !background;

        try { await _one.WaitAsync(ct); }
        catch { if (counted) Interlocked.Decrement(ref _listenersWaiting); throw; }
        if (counted) { Interlocked.Decrement(ref _listenersWaiting); counted = false; }
        try
        {
            if (!noCache && File.Exists(path)) return path;   // made while this call waited its turn
            var endpoint = (_config["Tts:Endpoint"] ?? "http://localhost:8880").TrimEnd('/');
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(60));
            using var response = await _clients.CreateClient("tts").PostAsJsonAsync(
                $"{endpoint}/v1/audio/speech",
                new { model = _config["Tts:Model"] ?? "kokoro", input = text.Trim(), voice = Voice, response_format = "mp3", speed = 1.0 },
                timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogInformation("Speech service answered {Status}", (int)response.StatusCode);
                return null;
            }
            var bytes = await response.Content.ReadAsByteArrayAsync(timeout.Token);
            if (bytes.Length < 200) return null;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temp = path + ".tmp";
            await File.WriteAllBytesAsync(temp, bytes, ct);
            File.Move(temp, path, overwrite: true);
            return path;
        }
        catch (Exception ex)
        {
            _log.LogInformation(ex, "Speech service did not answer");
            return null;
        }
        finally { _one.Release(); }
    }

    /// <summary>
    /// Streaming test: opens Kokoro's audio stream (raw 24 kHz 16-bit mono, sent as it is made) for this text.
    /// The caller copies it on and disposes the response. Null if the voice is off or does not answer.
    /// </summary>
    public async Task<HttpResponseMessage?> StartStreamAsync(string text, CancellationToken ct = default)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            var endpoint = (_config["Tts:Endpoint"] ?? "http://localhost:8880").TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/v1/audio/speech")
            {
                Content = JsonContent.Create(new
                {
                    model = _config["Tts:Model"] ?? "kokoro",
                    input = text.Trim(),
                    voice = Voice,
                    response_format = "pcm",
                    speed = 1.0,
                    stream = true,
                }),
            };
            var response = await _clients.CreateClient("tts").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) { response.Dispose(); return null; }
            return response;
        }
        catch (Exception ex)
        {
            _log.LogInformation(ex, "Speech service did not answer the streaming request");
            return null;
        }
    }

    /// <summary>Is the speech service answering right now. Never throws.</summary>
    public async Task<bool> IsUpAsync(CancellationToken ct = default)
    {
        if (!Enabled) return false;
        try
        {
            var endpoint = (_config["Tts:Endpoint"] ?? "http://localhost:8880").TrimEnd('/');
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(3));
            using var r = await _clients.CreateClient("tts").GetAsync($"{endpoint}/v1/models", timeout.Token);
            return (int)r.StatusCode < 500;
        }
        catch { return false; }
    }

    private string PathFor(string text)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Voice + "|" + text.Trim()))).ToLowerInvariant();
        return Path.Combine(_cacheDir, Voice, hash[..24] + ".mp3");
    }
}
