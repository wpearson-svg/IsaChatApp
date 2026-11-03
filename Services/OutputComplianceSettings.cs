using System.Text.Json;
using System.Text.Json.Nodes;

namespace IsaChatApp.Services;

/// <summary>
/// The Output Compliance numbers a developer can change on the Dev Dashboard.
/// Defaults come from appsettings.json (OutputCompliance:NliThreshold,
/// OutputCompliance:ConfidenceThreshold); a saved change is written to
/// output-compliance-settings.json in the project root and wins over appsettings.
/// The stage reads these on every answer, so a change applies to the next question
/// without restarting Isa.
/// </summary>
public sealed class OutputComplianceSettings
{
    public const double DefaultNli = 0.50;
    public const double DefaultConfidence = 0.70;

    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
    private readonly object _lock = new();
    private readonly string _filePath;
    private readonly double _nliDefault;
    private readonly double _confDefault;
    private double? _nli;
    private double? _conf;

    public OutputComplianceSettings(IConfiguration config, IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "output-compliance-settings.json");
        _nliDefault = Clamp(config.GetValue("OutputCompliance:NliThreshold", DefaultNli));
        _confDefault = Clamp(config.GetValue("OutputCompliance:ConfidenceThreshold", DefaultConfidence));
        Load();
    }

    /// <summary>An answer sentence scoring below this against the cited passage fails the NLI check (0 to 1).</summary>
    public double NliThreshold { get { lock (_lock) return _nli ?? _nliDefault; } }

    /// <summary>An answer with a confidence score below this fails the confidence check (0 to 1).</summary>
    public double ConfidenceThreshold { get { lock (_lock) return _conf ?? _confDefault; } }

    public double NliDefault => _nliDefault;
    public double ConfidenceDefault => _confDefault;

    public void Save(double nli, double confidence)
    {
        lock (_lock)
        {
            _nli = Clamp(nli);
            _conf = Clamp(confidence);
            var root = new JsonObject
            {
                ["nliThreshold"] = _nli,
                ["confidenceThreshold"] = _conf,
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
        }
        catch
        {
            // A damaged file just means the appsettings values are used.
        }
    }

    private static double Clamp(double v) => Math.Round(Math.Min(1.0, Math.Max(0.0, v)), 2);
}
