using System.Globalization;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services;

/// <summary>
/// Numbers for the Dev Dashboard's Health alerts and Front end tabs. Read only: counts from events.db
/// (what the stages logged) and totals from usage.db. No question text exists in either database.
/// Alert thresholds: DevHealth:ModelErrorsPerHour (default 3), DevHealth:SalesforceFailuresInARow (default 3).
/// </summary>
public class DevHealthService
{
    private readonly string _eventsPath;
    private readonly string _usagePath;
    private readonly IConfiguration _config;

    public DevHealthService(IConfiguration config, EventLogService _eventsEnsureCreated, UsageSummaryService _usageEnsureCreated)
    {
        _config = config;
        _eventsPath = config["EventLog:Path"] ?? Path.Combine("data", "events.db");
        _usagePath = config["Usage:Path"] ?? Path.Combine("data", "usage.db");
    }

    public sealed record Trouble(string What, int LastHour, int Last24Hours);
    public sealed record Alert(string Level, string Text);   // Level: red | amber
    public sealed record HealthReport(IReadOnlyList<Trouble> Trouble, IReadOnlyList<Alert> Alerts, string? Error);

    public sealed record Count(string Label, int N);
    public sealed record Timing(string Route, int Questions, double MedianSeconds, double SlowestSeconds);
    public sealed record FrontEndReport(int Questions, IReadOnlyList<Count> Languages, int Resolved, int NotQuite, int TalkToPerson,
        IReadOnlyList<Timing> Timings, string? Error);

    private static SqliteConnection OpenRead(string path)
    {
        var c = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        c.Open();
        return c;
    }

    private static int Scalar(SqliteConnection c, string sql, params (string, object)[] ps)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
    }

    private static string Iso(TimeSpan ago) => DateTime.UtcNow.Subtract(ago).ToString("O");

    /// <summary>What went wrong lately, from the event log, plus the alerts that follow from it.</summary>
    public HealthReport GetHealth()
    {
        if (!File.Exists(_eventsPath)) return new HealthReport([], [], "No event log yet.");
        try
        {
            using var c = OpenRead(_eventsPath);
            int Hits(string stage, string like, TimeSpan ago) =>
                Scalar(c, "SELECT COUNT(*) FROM events WHERE stage=$s AND timestamp>=$t AND data LIKE $l",
                    ("$s", stage), ("$t", Iso(ago)), ("$l", like));
            int AnyStage(string like, TimeSpan ago) =>
                Scalar(c, "SELECT COUNT(*) FROM events WHERE timestamp>=$t AND data LIKE $l", ("$t", Iso(ago)), ("$l", like));

            var h = TimeSpan.FromHours(1); var d = TimeSpan.FromHours(24);
            var rows = new List<Trouble>();
            void Add(string what, Func<TimeSpan, int> f) => rows.Add(new Trouble(what, f(h), f(d)));
            Add("Model timed out", a => Hits("complex_ai", "%error=timeout%", a));
            Add("Model unavailable", a => Hits("complex_ai", "%error=unavailable%", a));
            Add("Model gave an empty answer", a => Hits("complex_ai", "%error=empty%", a));
            Add("Case not created in Salesforce", a => Hits("ticketing", "%salesforce=failed%", a));
            Add("Answer trimmed by the accuracy check", a => AnyStage("%step_down=trimmed%", a));
            Add("Answer replaced with ISACA's exact words", a => AnyStage("%step_down=isaca_words%", a));

            var alerts = new List<Alert>();
            var modelLimit = Math.Max(1, _config.GetValue("DevHealth:ModelErrorsPerHour", 3));
            var modelErrors = rows.Take(3).Sum(r => r.LastHour);
            if (modelErrors >= modelLimit)
                alerts.Add(new Alert("red", $"The model failed {modelErrors} times in the last hour (limit {modelLimit}). Check Ollama on the Services tab."));

            var inARow = Math.Max(1, _config.GetValue("DevHealth:SalesforceFailuresInARow", 3));
            var lastTickets = new List<bool>();
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "SELECT data LIKE '%salesforce=failed%' FROM events WHERE stage='ticketing' AND data LIKE '%salesforce=%' AND data NOT LIKE '%salesforce=off%' ORDER BY id DESC LIMIT $n";
                cmd.Parameters.AddWithValue("$n", inARow);
                using var r = cmd.ExecuteReader();
                while (r.Read()) lastTickets.Add(r.GetInt32(0) == 1);
            }
            if (lastTickets.Count == inARow && lastTickets.All(x => x))
                alerts.Add(new Alert("red", $"The last {inARow} Salesforce Cases all failed. Check the Salesforce settings and the record type."));

            var sync = TicketCloseSyncService.LastRunUtc;
            if (sync is null)
                alerts.Add(new Alert("amber", "The ticket close sync has not run since Isa started (it waits about a minute, then repeats). Time to resolution depends on it."));
            else if (!TicketCloseSyncService.LastRunOk)
                alerts.Add(new Alert("amber", $"The last ticket close sync ({sync.Value.ToLocalTime():t}) could not read Salesforce."));

            return new HealthReport(rows, alerts, null);
        }
        catch (Exception ex)
        {
            return new HealthReport([], [], "Could not read the event log: " + ex.GetType().Name);
        }
    }

    /// <summary>How people are using the chat, from usage.db (last N days).</summary>
    public FrontEndReport GetFrontEnd(int days)
    {
        if (!File.Exists(_usagePath)) return new FrontEndReport(0, [], 0, 0, 0, [], "No usage data yet.");
        try
        {
            using var c = OpenRead(_usagePath);
            var cut = DateTime.UtcNow.AddDays(-days).ToString("O");

            List<T> Q<T>(string sql, Func<SqliteDataReader, T> map)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("$c", cut);
                using var r = cmd.ExecuteReader();
                var l = new List<T>();
                while (r.Read()) l.Add(map(r));
                return l;
            }

            var total = Scalar(c, "SELECT COUNT(*) FROM request_summary WHERE timestamp_utc>=$c", ("$c", cut));
            var langs = Q("SELECT COALESCE(language,'unknown'), COUNT(*) FROM request_summary WHERE timestamp_utc>=$c GROUP BY 1 ORDER BY 2 DESC",
                r => new Count(r.GetString(0), r.GetInt32(1)));
            var fb = Q("SELECT kind, COUNT(*) FROM feedback WHERE timestamp_utc>=$c GROUP BY 1", r => (r.GetString(0), r.GetInt32(1))).ToDictionary(x => x.Item1, x => x.Item2);
            int F(string k) => fb.TryGetValue(k, out var n) ? n : 0;

            var times = Q("SELECT route, total_ms FROM request_summary WHERE timestamp_utc>=$c AND total_ms IS NOT NULL AND route IN ('direct','ai')",
                r => (Route: r.GetString(0), Ms: r.GetDouble(1)));
            var timings = times.GroupBy(t => t.Route).Select(g =>
            {
                var s = g.Select(x => x.Ms / 1000.0).OrderBy(x => x).ToList();
                var median = s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
                return new Timing(g.Key == "direct" ? "Found in approved text" : "Built by the model", s.Count, median, s[^1]);
            }).OrderBy(t => t.Route).ToList();

            return new FrontEndReport(total, langs, F("resolved"), F("not_quite"), F("talk_to_person"), timings, null);
        }
        catch (Exception ex)
        {
            return new FrontEndReport(0, [], 0, 0, 0, [], "Could not read usage data: " + ex.GetType().Name);
        }
    }
}
