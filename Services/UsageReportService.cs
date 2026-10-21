using System.Globalization;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services;

/// <summary>
/// Content Health Dashboard, step 2: reads data/usage.db (written by
/// UsageSummaryService) and data/content.db and turns them into the numbers the
/// dashboard shows. Read-only. Added 2026-10-21.
///
/// Everything returned is a total or a rate. Nothing here can return one person's
/// questions: the database holds no question text, and people appear only as hashes
/// that are counted, never listed.
/// </summary>
public class UsageReportService
{
    private readonly string _usagePath;
    private readonly string _contentPath;
    private readonly UsageSummaryService _usage;   // also makes sure usage.db exists

    public static readonly string[] Categories = { "Governance", "Operations", "Programs", "Products" };

    public UsageReportService(IConfiguration config, UsageSummaryService usage)
    {
        _usage = usage;
        _usagePath = config["Usage:Path"] ?? Path.Combine("data", "usage.db");
        _contentPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
    }

    public sealed record Count(string Label, int N);
    public sealed record PassageStat(string Passage, int Retrieved, int Cited);
    public sealed record CategoryCoverage(string Category, int Passages, int PassagesUsed, int Retrievals);
    public sealed record RouteSplit(string Category, int Direct, int Ai);
    public sealed record Group(string Name, int Questions, int Escalated, int Resolved, int NotQuite);
    public sealed record FeedbackDay(string Day, int Resolved, int NotQuite);

    public sealed record Snapshot(
        int Total, int Answered, int Stopped, int Escalated, int ZeroMatch,
        IReadOnlyList<Count> EscalationsByReason,
        IReadOnlyList<Count> EscalationsByTopic,
        IReadOnlyList<Count> EscalationsByChapter,
        IReadOnlyList<Count> EscalationsByHour,
        int UsersWhoEscalated, int UsersWhoEscalatedAgain,
        IReadOnlyList<Count> Routes,
        IReadOnlyList<PassageStat> MostRetrieved,
        int TotalPassages, int NeverRetrieved, IReadOnlyList<string> NeverRetrievedSample,
        IReadOnlyList<CategoryCoverage> Coverage,
        IReadOnlyList<RouteSplit> SearchVsGeneration,
        int Resolved, int NotQuite, IReadOnlyList<FeedbackDay> FeedbackByDay,
        IReadOnlyList<Group> Tenure,
        int RepeatedQuestions, int RepeatedAcrossPeople,
        long PromptTokens, long CompletionTokens, int QuestionsWithTokens,
        int TicketsOpened, int TicketsClosed, double? AvgHoursToClose,
        int OpenConflicts)
    {
        public static Snapshot Empty => new(0, 0, 0, 0, 0, new List<Count>(), new List<Count>(), new List<Count>(), new List<Count>(),
            0, 0, new List<Count>(), new List<PassageStat>(), 0, 0, new List<string>(),
            Categories.Select(c => new CategoryCoverage(c, 0, 0, 0)).ToList(), new List<RouteSplit>(),
            0, 0, new List<FeedbackDay>(), new List<Group>(), 0, 0, 0, 0, 0, 0, 0, null, 0);
    }

    private static SqliteConnection OpenRead(string path)
    {
        var c = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
        c.Open();
        return c;
    }

    private static List<T> Rows<T>(SqliteConnection c, string sql, Func<SqliteDataReader, T> map, string? cutoff = null)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        if (cutoff is not null) cmd.Parameters.AddWithValue("$c", cutoff);
        using var r = cmd.ExecuteReader();
        var list = new List<T>();
        while (r.Read()) list.Add(map(r));
        return list;
    }

    private static string Str(SqliteDataReader r, int i, string fallback = "") => r.IsDBNull(i) ? fallback : r.GetString(i);
    private static DateTime? Parse(string? s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d) ? d : null;

    public Snapshot Get(DateTime cutoffUtc)
    {
        if (!File.Exists(_usagePath)) return Snapshot.Empty;
        var cut = cutoffUtc.ToUniversalTime().ToString("O");
        try
        {
            using var c = OpenRead(_usagePath);

            var outcomes = Rows(c, "SELECT outcome, COUNT(*) FROM request_summary WHERE timestamp_utc >= $c GROUP BY outcome",
                r => new Count(r.GetString(0), r.GetInt32(1)), cut).ToDictionary(x => x.Label, x => x.N);
            int Out(string k) => outcomes.TryGetValue(k, out var n) ? n : 0;

            int Scalar(string sql)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("$c", cut);
                return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            }

            var zero = Scalar("SELECT COUNT(*) FROM request_summary WHERE timestamp_utc >= $c AND route = 'none'");
            const string esc = "FROM request_summary WHERE timestamp_utc >= $c AND outcome = 'escalated'";
            var byReason = Rows(c, $"SELECT COALESCE(reason,'(none given)'), COUNT(*) {esc} GROUP BY 1 ORDER BY 2 DESC",
                r => new Count(r.GetString(0), r.GetInt32(1)), cut);
            var byTopic = Rows(c, $"SELECT COALESCE(category,'No topic (nothing found)'), COUNT(*) {esc} GROUP BY 1 ORDER BY 2 DESC",
                r => new Count(r.GetString(0), r.GetInt32(1)), cut);
            var byChapter = Rows(c, $"SELECT COALESCE(chapter_id,'Unknown'), COUNT(*) {esc} GROUP BY 1 ORDER BY 2 DESC",
                r => new Count(r.GetString(0), r.GetInt32(1)), cut);

            var hours = new int[24];
            foreach (var t in Rows(c, $"SELECT timestamp_utc {esc}", r => Parse(r.GetString(0)), cut))
                if (t is { } d) hours[d.ToLocalTime().Hour]++;
            var byHour = Enumerable.Range(0, 24).Where(h => hours[h] > 0)
                .Select(h => new Count($"{h:00}:00", hours[h])).ToList();

            var perUser = Rows(c, $"SELECT COUNT(*) {esc} AND user_hash IS NOT NULL GROUP BY user_hash", r => r.GetInt32(0), cut);
            var routes = Rows(c, "SELECT route, COUNT(*) FROM request_summary WHERE timestamp_utc >= $c GROUP BY route ORDER BY 2 DESC",
                r => new Count(r.GetString(0), r.GetInt32(1)), cut);

            // ---- retrieval, coverage ----
            var retrievals = Rows(c, "SELECT document, section, page, COUNT(*), SUM(cited) FROM retrieval WHERE timestamp_utc >= $c GROUP BY 1,2,3",
                r => (Doc: r.GetString(0), Sec: r.GetString(1), Page: r.GetString(2), N: r.GetInt32(3), Cited: r.GetInt32(4)), cut);
            string Label(string d, string s, string p) => $"{d} → {s} (p. {p})";
            var most = retrievals.OrderByDescending(x => x.N).Take(5)
                .Select(x => new PassageStat(Label(x.Doc, x.Sec, x.Page), x.N, x.Cited)).ToList();
            var used = retrievals.ToDictionary(x => (x.Doc, x.Sec, x.Page), x => x.N);

            var all = new List<(string Doc, string Sec, string Page)>();
            try
            {
                using var cc = OpenRead(_contentPath);
                all = Rows(cc, "SELECT DISTINCT document, section, page FROM passages",
                    r => (Str(r, 0), Str(r, 1), Str(r, 2)));
            }
            catch { /* no content db: coverage stays empty */ }

            var never = all.Where(p => !used.ContainsKey(p)).ToList();
            var coverage = Categories.ToDictionary(k => k, k => (Passages: 0, Used: 0, Hits: 0));
            foreach (var p in all)
            {
                var cat = _usage.CategoryOf(p.Doc) ?? "Uncategorized";
                coverage.TryGetValue(cat, out var cur);
                coverage[cat] = (cur.Passages + 1, cur.Used + (used.ContainsKey(p) ? 1 : 0), cur.Hits + (used.TryGetValue(p, out var n) ? n : 0));
            }

            var split = Rows(c, "SELECT COALESCE(category,'No topic'), route, COUNT(*) FROM request_summary WHERE timestamp_utc >= $c AND outcome='answered' GROUP BY 1,2",
                r => (Cat: r.GetString(0), Route: r.GetString(1), N: r.GetInt32(2)), cut)
                .GroupBy(x => x.Cat)
                .Select(g => new RouteSplit(g.Key, g.Where(x => x.Route == "direct").Sum(x => x.N), g.Where(x => x.Route == "ai").Sum(x => x.N)))
                .OrderByDescending(x => x.Direct + x.Ai).ToList();

            // ---- feedback, tenure ----
            var fb = Rows(c, "SELECT timestamp_utc, kind FROM feedback WHERE timestamp_utc >= $c",
                r => (T: Parse(r.GetString(0)), Kind: r.GetString(1)), cut);
            int resolved = fb.Count(f => f.Kind == "resolved"), notQuite = fb.Count(f => f.Kind == "not_quite");
            var byDay = fb.Where(f => f.T is not null)
                .GroupBy(f => f.T!.Value.ToLocalTime().Date).OrderByDescending(g => g.Key).Take(7).OrderBy(g => g.Key)
                .Select(g => new FeedbackDay(g.Key.ToString("MMM d", CultureInfo.InvariantCulture),
                    g.Count(x => x.Kind == "resolved"), g.Count(x => x.Kind == "not_quite"))).ToList();

            const string grp = "CASE WHEN years_as_leader IS NULL THEN 'Unknown' WHEN years_as_leader = 0 THEN 'New leader (first year)' ELSE 'Returning leader' END";
            var qs = Rows(c, $"SELECT {grp}, COUNT(*), SUM(CASE WHEN outcome='escalated' THEN 1 ELSE 0 END) FROM request_summary WHERE timestamp_utc >= $c GROUP BY 1",
                r => (G: r.GetString(0), Q: r.GetInt32(1), E: r.GetInt32(2)), cut);
            var fbByGroup = Rows(c, $"SELECT {grp}, f.kind, COUNT(*) FROM feedback f JOIN request_summary r ON r.trace_id = f.trace_id WHERE f.timestamp_utc >= $c GROUP BY 1,2",
                r => (G: r.GetString(0), K: r.GetString(1), N: r.GetInt32(2)), cut);
            var tenure = qs.Select(x => new Group(x.G, x.Q, x.E,
                fbByGroup.Where(f => f.G == x.G && f.K == "resolved").Sum(f => f.N),
                fbByGroup.Where(f => f.G == x.G && f.K == "not_quite").Sum(f => f.N))).OrderBy(g => g.Name).ToList();

            // ---- repeated questions (hash only) ----
            var rep = Rows(c, "SELECT COUNT(*), COUNT(DISTINCT user_hash) FROM request_summary WHERE timestamp_utc >= $c AND question_hash IS NOT NULL GROUP BY question_hash HAVING COUNT(*) >= 2",
                r => (Times: r.GetInt32(0), People: r.GetInt32(1)), cut);

            // ---- hooks: tokens, tickets, conflicts ----
            long pt = 0, ct = 0; int withTok = 0;
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "SELECT COALESCE(SUM(prompt_tokens),0), COALESCE(SUM(completion_tokens),0), COUNT(prompt_tokens) FROM request_summary WHERE timestamp_utc >= $c";
                cmd.Parameters.AddWithValue("$c", cut);
                using var r = cmd.ExecuteReader();
                if (r.Read()) { pt = r.GetInt64(0); ct = r.GetInt64(1); withTok = r.GetInt32(2); }
            }
            var tickets = Rows(c, "SELECT timestamp_utc, ticket_closed_utc FROM request_summary WHERE timestamp_utc >= $c AND ticket_reference IS NOT NULL",
                r => (Open: Parse(Str(r, 0)), Closed: Parse(r.IsDBNull(1) ? null : r.GetString(1))), cut);
            var closed = tickets.Where(t => t.Open is not null && t.Closed is not null).ToList();
            double? avgHours = closed.Count == 0 ? null : closed.Average(t => (t.Closed!.Value - t.Open!.Value).TotalHours);
            int conflicts;
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM content_conflicts WHERE resolved_utc IS NULL";
                conflicts = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            }

            return new Snapshot(
                Out("answered") + Out("stopped") + Out("escalated"), Out("answered"), Out("stopped"), Out("escalated"), zero,
                byReason, byTopic, byChapter, byHour, perUser.Count, perUser.Count(n => n >= 2), routes,
                most, all.Count, never.Count, never.Take(5).Select(p => Label(p.Doc, p.Sec, p.Page)).ToList(),
                coverage.Select(kv => new CategoryCoverage(kv.Key, kv.Value.Passages, kv.Value.Used, kv.Value.Hits)).ToList(),
                split, resolved, notQuite, byDay, tenure, rep.Count, rep.Count(x => x.People >= 2),
                pt, ct, withTok, tickets.Count, closed.Count, avgHours, conflicts);
        }
        catch
        {
            return Snapshot.Empty;
        }
    }
}
