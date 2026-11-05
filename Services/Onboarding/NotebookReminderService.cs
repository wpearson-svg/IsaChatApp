using System.Globalization;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature (ONBOARDING-FEATURE.md, "Succession notebook
/// reminders", decided 20 October 2026; built 3 November 2026): the
/// check-in that asks the president, inside Isa, "Anything from this week
/// your successor should know?"
///
/// The rules (fixed, no AI):
///   - For the chapter's current president only (the outgoing president has
///     the handoff checklist instead). Inside Isa only, never by email.
///   - Starts in week 2 (day 8), so week 1 stays light (Matthew, 3 November
///     2026). Then once per period: every week by default, or every two
///     weeks or every four weeks if the president chooses (in the notebook's
///     "⋯" menu). In the last three months of a term it is every week
///     whatever the setting, as the handover gets close.
///   - If the president already saved something to the notebook in the
///     period, it still asks, but for anything more: "You saved 2 notes this
///     week. Anything else your successor should know?" (Matthew, 3 November
///     2026; this replaced "skip the check-in").
///   - Answers: a one-line note (saved to the notebook through
///     NotebookService.AddNoteAsync, so it passes the sign-in check and
///     Input Compliance), "Nothing this week" / "That's everything", "Ask me
///     later" (back the next time Isa is opened; not stored), or × (closed
///     until the next period). Each answer is recorded once per period.
///
/// Periods are counted from the term start date, on the leader's "today"
/// (the demo date when it is on). Stored in data/progress.db.
/// The prompt after each board meeting is designed but not built: Isa has
/// no meeting dates (docs/Supplement_Items.md).
/// </summary>
public class NotebookReminderService
{
    /// <summary>The first day of term with a check-in: day 8, the start of week 2.</summary>
    public const int FirstDay = 8;

    /// <summary>The choices: every week, every two weeks, every four weeks.</summary>
    public static readonly IReadOnlyList<int> Choices = [7, 14, 28];

    /// <summary>In the last three months of a term, check-ins are weekly whatever the setting.</summary>
    public const int WeeklyNearEndDays = 91;

    private readonly string _dbPath;
    private readonly LeaderTermService _terms;
    private readonly NotebookService _notebook;

    public NotebookReminderService(IConfiguration config, LeaderTermService terms, NotebookService notebook)
    {
        _terms = terms;
        _notebook = notebook;
        _dbPath = config["ProgressDb:Path"] ?? Path.Combine("data", "progress.db");
        var folder = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS notebook_checkins(
              person_id    TEXT NOT NULL,
              period_start TEXT NOT NULL,   -- the first day of the period, YYYY-MM-DD
              status       TEXT NOT NULL,   -- saved, nothing or closed
              on_date      TEXT NOT NULL,   -- the leader's "today" when they answered
              recorded_at  TEXT NOT NULL,   -- real time, UTC
              PRIMARY KEY (person_id, period_start));
            CREATE TABLE IF NOT EXISTS notebook_reminder_settings(
              person_id  TEXT PRIMARY KEY,
              every_days INTEGER NOT NULL,  -- 7, 14 or 28
              updated_at TEXT NOT NULL);
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    private static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    private static string Day(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ---- The setting ------------------------------------------------------------

    /// <summary>How often this president chose: 7, 14 or 28 days (every week by default).</summary>
    public int GetEveryDays(string personId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT every_days FROM notebook_reminder_settings WHERE person_id = $p;";
        cmd.Parameters.AddWithValue("$p", personId);
        return cmd.ExecuteScalar() is long n && Choices.Contains((int)n) ? (int)n : 7;
    }

    public void SetEveryDays(string personId, int everyDays)
    {
        if (!Choices.Contains(everyDays)) return;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notebook_reminder_settings(person_id, every_days, updated_at) VALUES($p, $n, $t)
            ON CONFLICT(person_id) DO UPDATE SET every_days = $n, updated_at = $t;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$n", everyDays);
        cmd.Parameters.AddWithValue("$t", Now());
        cmd.ExecuteNonQuery();
    }

    /// <summary>True for a current president, who can choose how often (the notebook shows the choice).</summary>
    public bool CanChoose(string personId, DateOnly onDate) =>
        _terms.GetCurrentTerms(personId, onDate).Any(t => t.Role == "President");

    /// <summary>True when the term ends within three months, so check-ins are weekly whatever the setting.</summary>
    public bool WeeklyNearEnd(string personId, DateOnly onDate) =>
        _terms.GetCurrentTerms(personId, onDate).FirstOrDefault(t => t.Role == "President") is { } t
        && t.TermEnd.DayNumber - onDate.DayNumber < WeeklyNearEndDays;

    // ---- The check-in -----------------------------------------------------------

    /// <summary>
    /// The check-in due now for this person, or null: not the chapter's
    /// president, still in week 1, or already answered for this period.
    /// </summary>
    public NotebookCheckIn? GetDue(string personId, DateOnly onDate)
    {
        var term = _terms.GetCurrentTerms(personId, onDate).FirstOrDefault(t => t.Role == "President");
        if (term is null || term.DayOfTerm < FirstDay) return null;
        if (!_notebook.GetAccess(personId, onDate).CanWrite) return null;

        var nearEnd = term.TermEnd.DayNumber - onDate.DayNumber < WeeklyNearEndDays;
        var every = nearEnd ? 7 : GetEveryDays(personId);
        var first = term.TermStart.AddDays(FirstDay - 1);
        var start = first.AddDays((onDate.DayNumber - first.DayNumber) / every * every);
        if (Answered(personId, start)) return null;

        // Anything this president saved to the notebook in this period.
        var saved = _notebook.GetEntries(personId, onDate)
            .Count(e => e.AuthorId == personId && e.Kind != "question" && e.OnDate >= start && e.OnDate <= onDate);
        var span = every switch { 7 => "this week", 14 => "the past two weeks", _ => "the past four weeks" };
        return new NotebookCheckIn(start, span, saved, every);
    }

    // Answered at any time during this period. Counting by the day of the answer, not the
    // period it was for, means changing how often never asks again straight away.
    private bool Answered(string personId, DateOnly periodStart)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM notebook_checkins WHERE person_id = $p AND on_date >= $s LIMIT 1;";
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", Day(periodStart));
        return cmd.ExecuteScalar() is not null;
    }

    /// <summary>Records the answer for this period: saved, nothing or closed. "Ask me later" isn't recorded.</summary>
    public void Respond(string personId, DateOnly onDate, DateOnly periodStart, string status)
    {
        if (status is not ("saved" or "nothing" or "closed")) return;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notebook_checkins(person_id, period_start, status, on_date, recorded_at) VALUES($p, $s, $st, $d, $t)
            ON CONFLICT(person_id, period_start) DO NOTHING;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", Day(periodStart));
        cmd.Parameters.AddWithValue("$st", status);
        cmd.Parameters.AddWithValue("$d", Day(onDate));
        cmd.Parameters.AddWithValue("$t", Now());
        cmd.ExecuteNonQuery();
    }
}

/// <summary>A check-in that is due: the period's first day, how the period is named ("this week"),
/// how many notes the president already saved in it, and how many days a period lasts.</summary>
public record NotebookCheckIn(DateOnly PeriodStart, string Span, int SavedCount, int EveryDays);
