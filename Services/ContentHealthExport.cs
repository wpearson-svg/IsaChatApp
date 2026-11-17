namespace IsaChatApp.Services;

/// <summary>
/// The Content Health Dashboard as a spreadsheet: the "Export to xlsx" button (16 November 2026, Willis).
/// One sheet per card on the page, for the date range picked at the top, plus a Summary sheet first.
///
/// It exports exactly what the page shows, so the same privacy rules hold: totals and rates only, no question text
/// (none is stored), and people only counted, never listed. The model usage sheets come from the in-memory log,
/// which resets when Isa restarts, like on the page.
/// </summary>
public static class ContentHealthExport
{
    /// <summary>The plain words for a route, the same on the page (ContentHealthSections) and in the export.</summary>
    public static string RouteLabel(string route) => route switch
    {
        "direct" => "Found in approved text",
        "ai" => "Built by the model",
        "none" => "Nothing found",
        "blocked" => "Stopped before search",
        _ => route
    };

    /// <summary>The range keys the page and the API take; null if the key isn't one of them. Empty means a week.</summary>
    public static string? RangeKey(string? range) => string.IsNullOrWhiteSpace(range) ? "week"
        : range.Trim().ToLowerInvariant() is var k && k is "today" or "week" or "30d" or "all" ? k : null;

    /// <summary>Earliest time included for a range key, the same rule as the dashboard page.</summary>
    public static DateTime Cutoff(string rangeKey, DateTime nowUtc) => rangeKey switch
    {
        "today" => nowUtc.Date,
        "week" => nowUtc.AddDays(-7),
        "30d" => nowUtc.AddDays(-30),
        _ => DateTime.MinValue,
    };

    public static string Label(string rangeKey) => rangeKey switch
    {
        "today" => "today", "week" => "last 7 days", "30d" => "last 30 days", _ => "all time"
    };

    public static string FileName(string rangeKey, DateTime nowUtc) => $"isa-content-health-{rangeKey}-{nowUtc:yyyy-MM-dd}.xlsx";

    public static byte[] Build(UsageReportService.Snapshot s, IReadOnlyList<UsageLogService.UsageEntry> modelCalls,
                               string rangeLabel, DateTime nowUtc)
    {
        static IReadOnlyList<object?> R(params object?[] cells) => cells;
        static double? Rate(int a, int b) => b == 0 ? null : Math.Round(100.0 * a / b, 1);

        var x = new XlsxWriter();

        x.Add("Summary", new[] { "Measure", "Value" }, new[]
        {
            R("Isa Content Health", null),
            R("Date range", rangeLabel),
            R("Exported (UTC)", nowUtc.ToString("yyyy-MM-dd HH:mm")),
            R("Totals and rates only: no question text, people counted anonymously.", null),
            R(null, null),
            R("Questions asked", s.Total),
            R("Answered", s.Answered),
            R("Stopped (outside scope)", s.Stopped),
            R("Passed to a person", s.Escalated),
            R("Found nothing (zero-match)", s.ZeroMatch),
            R("Zero-match, % of questions", Rate(s.ZeroMatch, s.Total)),
            R("People passed to a person", s.UsersWhoEscalated),
            R("...of them, more than once", s.UsersWhoEscalatedAgain),
            R("Tickets opened", s.TicketsOpened),
            R("Tickets closed", s.TicketsClosed),
            R("Average hours to close", s.AvgHoursToClose is { } h ? Math.Round(h, 1) : null),
            R("Passages in content", s.TotalPassages),
            R("Passages never retrieved", s.NeverRetrieved),
            R("Open conflicting-answer flags", s.OpenConflicts),
            R("Questions where the model reported tokens", s.QuestionsWithTokens),
            R("Average tokens per query", s.QuestionsWithTokens == 0 ? null : (s.PromptTokens + s.CompletionTokens) / s.QuestionsWithTokens),
            R("Documents", s.Age.Documents),
            R("Documents with a last-updated date", s.Age.Dated),
            R($"Documents overdue for review (over {s.Age.ReviewAfterDays} days)", s.Age.Overdue),
            R("Rated \"yes, this answered my question\"", s.Resolved),
            R("Rated \"not quite\"", s.NotQuite),
            R("Self-resolution, % of rated answers", Rate(s.Resolved, s.Resolved + s.NotQuite)),
            R("Topics asked about more than once", s.RepeatedTopics),
            R("...by 2 or more different people", s.RepeatedTopicsAcrossPeople),
        });

        x.Add("Escalations", new[] { "Broken down by", "Group", "Passed to a person" },
            s.EscalationsByReason.Select(c => R("Reason", c.Label, c.N))
             .Concat(s.EscalationsByTopic.Select(c => R("Topic", c.Label, c.N)))
             .Concat(s.EscalationsByHour.Select(c => R("Hour of day (this computer's time)", c.Label, c.N)))
             .Concat(s.EscalationsByRegion.Select(c => R("Region", c.Label, c.N)))
             .Concat(s.EscalationsByChapter.Select(c => R("Chapter", c.Label, c.N))));

        x.Add("How questions were handled", new[] { "Route", "Questions" },
            s.Routes.Select(r => R(RouteLabel(r.Label), r.N)));

        x.Add("Most-retrieved passages", new[] { "Passage", "Retrieved", "Cited" },
            s.MostRetrieved.Select(p => R(p.Passage, p.Retrieved, p.Cited)));

        var never = s.NeverRetrievedSample.Select(p => R(p)).ToList();
        if (s.NeverRetrieved > s.NeverRetrievedSample.Count)
            never.Add(R($"...and {s.NeverRetrieved - s.NeverRetrievedSample.Count} more (the dashboard lists the first {s.NeverRetrievedSample.Count})"));
        x.Add("Never-retrieved passages", new[] { "Passage" }, never);

        x.Add("Coverage by category", new[] { "Category", "Passages", "Used", "Retrievals" },
            s.Coverage.Select(c => R(c.Category, c.Passages, c.PassagesUsed, c.Retrievals)));

        x.Add("Search vs generation", new[] { "Topic", "Found in approved text", "Built by the model" },
            s.SearchVsGeneration.Select(r => R(r.Category, r.Direct, r.Ai)));

        x.Add("Content age", new[] { "Document", "Days since update" },
            s.Age.Oldest.Select(d => R(d.Title, d.Days)));

        x.Add("Feedback by day", new[] { "Day", "Yes", "Not quite" },
            s.FeedbackByDay.Select(d => R(d.Day, d.Resolved, d.NotQuite)));

        x.Add("New vs returning leaders", new[] { "Group", "Questions", "Passed on", "Yes", "Not quite" },
            s.Tenure.Select(g => R(g.Name, g.Questions, g.Escalated, g.Resolved, g.NotQuite)));

        x.Add("Question clusters", new[] { "Best-matching passage", "Questions", "People", "Passed on" },
            s.Clusters.Select(g => R(g.Passage, g.Questions, g.People, g.Escalated)));

        x.Add("Model usage", new[] { "Model", "Calls", "Prompt tokens", "Completion tokens", "Total tokens", "Average response (ms)" },
            modelCalls.GroupBy(e => e.Model).OrderByDescending(g => g.Count())
                .Select(g => R(g.Key, g.Count(), g.Sum(e => (long)(e.PromptTokens ?? 0)), g.Sum(e => (long)(e.CompletionTokens ?? 0)),
                               g.Sum(e => e.TotalTokens), Math.Round(g.Average(e => e.ElapsedMs)))));

        x.Add("Model log", new[] { "Time (UTC)", "Model", "Host", "Prompt tokens", "Completion tokens", "Total tokens", "Response (ms)" },
            modelCalls.OrderByDescending(e => e.TimestampUtc)
                .Select(e => R(e.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss"), e.Model, e.Host, e.PromptTokens, e.CompletionTokens,
                               e.TotalTokens, Math.Round(e.ElapsedMs))));

        return x.ToBytes();
    }
}
