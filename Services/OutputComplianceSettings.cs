using System.Text.Json;
using System.Text.Json.Nodes;

namespace IsaChatApp.Services;

/// <summary>
/// The answer-quality numbers a developer can change on the Dev Dashboard.
/// Defaults come from appsettings.json (OutputCompliance:NliThreshold,
/// OutputCompliance:ConfidenceThreshold, OutputCompliance:StrongMatch, VectorSearch:MinScore);
/// a saved change is written to output-compliance-settings.json in the project root and wins
/// over appsettings. The stages read these on every question, so a change applies to the next
/// question without restarting Isa.
/// </summary>
public sealed class OutputComplianceSettings
{
    public const double DefaultNli = 0.50;
    public const double DefaultConfidence = 0.70;
    public const double DefaultStrongMatch = 0.0;
    public const double DefaultSearchPassMark = 0.80;

    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly double _nliDefault;
    private readonly double _confDefault;
    private readonly double _strongDefault;
    private readonly double _searchDefault;
    private double? _nli;
    private double? _conf;
    private double? _strong;
    private double? _search;

    public OutputComplianceSettings(IConfiguration config, IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "output-compliance-settings.json");
        _nliDefault = Clamp(config.GetValue("OutputCompliance:NliThreshold", DefaultNli));
        _confDefault = Clamp(config.GetValue("OutputCompliance:ConfidenceThreshold", DefaultConfidence));
        _strongDefault = Clamp(config.GetValue("OutputCompliance:StrongMatch", DefaultStrongMatch));
        _searchDefault = Clamp(config.GetValue("VectorSearch:MinScore", DefaultSearchPassMark));
        Load();
    }

    /// <summary>An answer sentence scoring below this against the cited passage fails the NLI check (0 to 1).</summary>
    public double NliThreshold { get { lock (_lock) return _nli ?? _nliDefault; } }

    /// <summary>An answer with a confidence score below this fails the confidence check (0 to 1).</summary>
    public double ConfidenceThreshold { get { lock (_lock) return _conf ?? _confDefault; } }

    /// <summary>
    /// After an AI answer fails a check: a search score at or above this counts as a strong match, so Isa shows
    /// ISACA's own words from the passage; below it, the question goes to a person. 0 means every match is strong.
    /// </summary>
    public double StrongMatch { get { lock (_lock) return _strong ?? _strongDefault; } }

    /// <summary>A passage must score at least this against the question for search by meaning to use it (0 to 1).</summary>
    public double SearchPassMark { get { lock (_lock) return _search ?? _searchDefault; } }

    public double NliDefault => _nliDefault;
    public double ConfidenceDefault => _confDefault;
    public double StrongMatchDefault => _strongDefault;
    public double SearchPassMarkDefault => _searchDefault;

    public void Save(double nli, double confidence, double strongMatch, double searchPassMark)
    {
        lock (_lock)
        {
            _nli = Clamp(nli);
            _conf = Clamp(confidence);
            _strong = Clamp(strongMatch);
            _search = Clamp(searchPassMark);
            var root = new JsonObject
            {
                ["nliThreshold"] = _nli,
                ["confidenceThreshold"] = _conf,
                ["strongMatch"] = _strong,
                ["searchPassMark"] = _search,
            };
            var temp = _filePath + ".tmp";
            File.WriteAllText(temp, root.ToJsonString(Pretty));
            File.Move(temp, _filePath, overwrite: true);
        }
    }

    /// <summary>Back to the appsettings.json values (deletes the saved file).</summary>
    public void ResetToDefaults()
    {
        lock (_lock)
        {
            _nli = null;
            _conf = null;
            _strong = null;
            _search = null;
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var root = JsonNode.Parse(File.ReadAllText(_filePath)) as JsonObject;
            if (root?["nliThreshold"] is JsonNode n) _nli = Clamp(n.GetValue<double>());
            if (root?["confidenceThreshold"] is JsonNode c) _conf = Clamp(c.GetValue<double>());
            if (root?["strongMatch"] is JsonNode s) _strong = Clamp(s.GetValue<double>());
            if (root?["searchPassMark"] is JsonNode m) _search = Clamp(m.GetValue<double>());
        }
        catch
        {
            // A damaged file just means the appsettings values are used.
        }
    }

    private static double Clamp(double v) => Math.Round(Math.Min(1.0, Math.Max(0.0, v)), 2);
}
