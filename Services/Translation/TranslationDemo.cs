using System.Text.Json;
using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Translation;

/// <summary>
/// The predetermined Amharic questions for the test and the presentation (Translation:DemoFile, default
/// content/translation-demo.json). Each entry holds the question as people type it in Latin letters, the same
/// question in Ethiopic script (what is sent to the translation service), and the reply shown back in Latin letters.
/// Only a question that matches an entry (ignoring case, punctuation and spacing) takes this path; every other
/// question is handled as before. The file is read on each use, so edits apply without a restart.
/// </summary>
public sealed class TranslationDemo
{
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<TranslationDemo> _log;

    public TranslationDemo(IConfiguration config, IHostEnvironment env, ILogger<TranslationDemo> log)
    {
        _config = config;
        _env = env;
        _log = log;
    }

    public sealed record Entry(string Question, string Ethiopic, string Reply);

    public Entry? Find(string question)
    {
        try
        {
            var path = Path.Combine(_env.ContentRootPath, _config["Translation:DemoFile"] ?? "content/translation-demo.json");
            if (!File.Exists(path)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("entries", out var entries)) return null;
            var key = Normalize(question);
            foreach (var e in entries.EnumerateArray())
            {
                var q = e.TryGetProperty("question", out var qp) ? qp.GetString() ?? "" : "";
                var eth = e.TryGetProperty("ethiopic", out var ep) ? ep.GetString() ?? "" : "";
                var reply = e.TryGetProperty("reply", out var rp) ? rp.GetString() ?? "" : "";
                if (q.Length > 0 && eth.Length > 0 && reply.Length > 0 && Normalize(q) == key)
                    return new Entry(q, eth, reply);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _log.LogWarning("Translation demo file could not be read ({Reason})", ex.GetType().Name);
        }
        return null;
    }

    private static string Normalize(string s) =>
        Regex.Replace(s ?? "", @"[^\p{L}\p{N}]+", " ").Trim().ToLowerInvariant();
}
