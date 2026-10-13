using System;
using System.Collections.Generic;
using System.Linq;

namespace IsaChatApp.Services;

/// <summary>
/// In-memory log of every completed AI reply ChatWidget has gotten back
/// from a model host -- one entry per successful (non-error) response.
/// Registered as a singleton in Program.cs, same pattern as
/// WidgetSettings: shared app-wide, not persisted to disk, and reset to
/// empty on every app restart.
///
/// This is what powers the Content Health Dashboard's model-usage log,
/// per-model token/response-time stats, and the "tokens this week"
/// figure -- see Components/Pages/ContentHealthDashboard.razor. It's
/// also where ChatWidget.Send() writes a row immediately after each
/// successful /api/generate call (see the UsageLog.Record(...) call
/// there), using the token counts Ollama itself reports
/// (prompt_eval_count / eval_count) and the wall-clock elapsed time of
/// that one call.
///
/// A List&lt;T&gt; behind a lock is enough here -- Blazor Server serializes
/// a given circuit's own calls onto one thread already, but multiple
/// people could have the widget open in different circuits at once, so
/// this still needs to be safe for concurrent writers.
/// </summary>
public class UsageLogService
{
    /// <summary>One completed model call. PromptTokens/CompletionTokens are
    /// null if the host didn't report them (not every Ollama build does).</summary>
    public sealed record UsageEntry(
        DateTime TimestampUtc,
        string Model,
        string Host,
        int? PromptTokens,
        int? CompletionTokens,
        double ElapsedMs)
    {
        public long TotalTokens => (PromptTokens ?? 0) + (CompletionTokens ?? 0);
    }

    private readonly List<UsageEntry> _entries = new();
    private readonly object _lock = new();

    public void Record(string model, string host, int? promptTokens, int? completionTokens, double elapsedMs)
    {
        lock (_lock)
        {
            _entries.Add(new UsageEntry(DateTime.UtcNow, model, host, promptTokens, completionTokens, elapsedMs));
        }
    }

    /// <summary>A snapshot copy, newest call last (chronological) -- callers needing
    /// newest-first can just walk it backwards, same convention as ChatWidget.Messages.</summary>
    public IReadOnlyList<UsageEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }
    }

    /// <summary>Dev convenience for the dashboard's "Clear log" button -- not tied to
    /// anything else; the log resets on app restart regardless.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }
}
