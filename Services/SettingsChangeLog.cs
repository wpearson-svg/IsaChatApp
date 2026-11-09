using System.Text.Json;

namespace IsaChatApp.Services;

/// <summary>
/// The Dev Dashboard's change log: every save or reset of a setting on the dashboard is written as one line
/// (when, who, which setting, the old value and the new value) to settings-change-log.jsonl in the project folder.
/// The file stays on this computer (it is not in Git). Secret values are never passed in here: callers log
/// "changed" instead of the value. A failure to write never stops a save.
/// </summary>
public sealed class SettingsChangeLog
{
    public const string FileName = "settings-change-log.jsonl";

    public sealed record Entry(DateTime TimestampUtc, string Who, string Area, string Setting, string? Old, string? New);

    private readonly string _filePath;
    private readonly ILogger<SettingsChangeLog> _log;
    private readonly object _lock = new();

    public SettingsChangeLog(IHostEnvironment env, ILogger<SettingsChangeLog> log)
    {
        _filePath = Path.Combine(env.ContentRootPath, FileName);
        _log = log;
    }

    /// <summary>Writes one entry. Does nothing when the old and new values are the same.</summary>
    public void Add(string who, string area, string setting, string? oldValue, string? newValue)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal)) return;
        try
        {
            var entry = new Entry(DateTime.UtcNow, string.IsNullOrWhiteSpace(who) ? "unknown" : who, area, setting, oldValue, newValue);
            var line = JsonSerializer.Serialize(entry);
            lock (_lock) File.AppendAllText(_filePath, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not write the settings change log");
        }
    }

    /// <summary>The most recent entries, newest first.</summary>
    public IReadOnlyList<Entry> Recent(int count = 50)
    {
        try
        {
            if (!File.Exists(_filePath)) return Array.Empty<Entry>();
            string[] lines;
            lock (_lock) lines = File.ReadAllLines(_filePath);
            var list = new List<Entry>();
            for (var i = lines.Length - 1; i >= 0 && list.Count < count; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                try
                {
                    var entry = JsonSerializer.Deserialize<Entry>(lines[i]);
                    if (entry is not null) list.Add(entry);
                }
                catch { /* a damaged line is skipped */ }
            }
            return list;
        }
        catch
        {
            return Array.Empty<Entry>();
        }
    }
}
