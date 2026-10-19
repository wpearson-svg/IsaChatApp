using System.Globalization;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature (see ONBOARDING-FEATURE.md): what leaders DO in
/// the journey, as opposed to the journey content.
///
/// Keeps, in data/progress.db:
///   - which steps each leader has completed, and how
///   - the full history of every step: each "done" and "not done" is
///     added as a new entry and never erased, with the date (the demo
///     date while the demo date is on), so it works as a record
///   - welcome-call bookings (one per leader; a booked time shows as
///     "Taken" to everyone else)
///   - saved seats at a group welcome
///   - introduction messages written to a contact (never emailed in
///     the prototype: the contacts are invented sample people)
///
/// This is a separate database from data/onboarding.db on purpose:
/// rebuilding the journey steps never erases anyone's progress. The file
/// is created automatically the first time Isa starts, and it is not in
/// Git (the .gitignore leaves out data/*.db). Deleting it resets every
/// leader's progress, which is handy before a demo rehearsal.
/// </summary>
public class ProgressService
{
    private readonly string _dbPath;

    public ProgressService(IConfiguration config)
    {
        _dbPath = config["ProgressDb:Path"] ?? Path.Combine("data", "progress.db");
        var folder = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        // Creates the tables the first time; does nothing after that.
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS step_progress(
              person_id    TEXT NOT NULL,
              step_id      TEXT NOT NULL,
              how          TEXT NOT NULL,   -- e.g. done, call, introduction, group
              completed_at TEXT NOT NULL,   -- real time it was marked, UTC
              PRIMARY KEY (person_id, step_id));
            CREATE TABLE IF NOT EXISTS call_bookings(
              contact_id TEXT NOT NULL,
              starts_utc TEXT NOT NULL,
              person_id  TEXT NOT NULL,
              booked_at  TEXT NOT NULL,
              PRIMARY KEY (contact_id, starts_utc));
            CREATE TABLE IF NOT EXISTS group_seats(
              person_id   TEXT PRIMARY KEY,
              session_utc TEXT NOT NULL,
              saved_at    TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS introductions(
              person_id  TEXT PRIMARY KEY,
              contact_id TEXT NOT NULL,
              message    TEXT NOT NULL,
              written_at TEXT NOT NULL);
            -- The record: one row per change, only ever added to.
            CREATE TABLE IF NOT EXISTS step_history(
              entry_id    INTEGER PRIMARY KEY AUTOINCREMENT,
              person_id   TEXT NOT NULL,
              step_id     TEXT NOT NULL,
              action      TEXT NOT NULL,  -- done or undone
              how         TEXT NOT NULL,  -- e.g. done, call, introduction, group
              on_date     TEXT NOT NULL,  -- the leader's "today" (demo date when on), YYYY-MM-DD
              recorded_at TEXT NOT NULL); -- real time, UTC
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

    // ---- Steps -----------------------------------------------------------

    /// <summary>
    /// Every step a leader has completed: step ID mapped to how and when.
    /// The date is the latest "done" entry in the history (null only for
    /// steps completed before the history existed).
    /// </summary>
    public IReadOnlyDictionary<string, StepDone> GetCompletedSteps(string personId)
    {
        var done = new Dictionary<string, StepDone>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT sp.step_id, sp.how,
                   (SELECT h.on_date FROM step_history h
                    WHERE h.person_id = sp.person_id AND h.step_id = sp.step_id AND h.action = 'done'
                    ORDER BY h.entry_id DESC LIMIT 1)
            FROM step_progress sp
            WHERE sp.person_id = $p;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            done[r.GetString(0)] = new StepDone(r.GetString(1), r.IsDBNull(2) ? null : ParseDate(r.GetString(2)));
        return done;
    }

    /// <summary>A step's full history for one leader, oldest first.</summary>
    public IReadOnlyList<StepHistoryEntry> GetHistory(string personId, string stepId)
    {
        var list = new List<StepHistoryEntry>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT action, how, on_date FROM step_history
            WHERE person_id = $p AND step_id = $s
            ORDER BY entry_id;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", stepId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new StepHistoryEntry(r.GetString(0), r.GetString(1), ParseDate(r.GetString(2))));
        return list;
    }

    /// <summary>
    /// Marks a step complete and adds a "done" entry to the history.
    /// "how" records which way, e.g. "done" or "call". "onDate" is the
    /// leader's today (WidgetSettings.Today: the demo date when it is on).
    /// </summary>
    public void CompleteStep(string personId, string stepId, string how, DateOnly onDate)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO step_progress(person_id, step_id, how, completed_at)
            VALUES($p, $s, $h, $t)
            ON CONFLICT(person_id, step_id) DO UPDATE SET how = $h, completed_at = $t;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", stepId);
        cmd.Parameters.AddWithValue("$h", how);
        cmd.Parameters.AddWithValue("$t", Now());
        cmd.ExecuteNonQuery();
        AddHistory(conn, personId, stepId, "done", how, onDate);
    }

    /// <summary>Marks a step not done again, and adds an "undone" entry to the history (the "done" entry stays).</summary>
    public void UndoStep(string personId, string stepId, DateOnly onDate)
    {
        using var conn = Open();
        var how = CurrentHow(conn, personId, stepId);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM step_progress WHERE person_id = $p AND step_id = $s;";
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", stepId);
        cmd.ExecuteNonQuery();
        if (how is not null) AddHistory(conn, personId, stepId, "undone", how, onDate);
    }

    // How a step is currently completed, or null if it isn't.
    private static string? CurrentHow(SqliteConnection conn, string personId, string stepId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT how FROM step_progress WHERE person_id = $p AND step_id = $s;";
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", stepId);
        return cmd.ExecuteScalar() as string;
    }

    private static void AddHistory(SqliteConnection conn, string personId, string stepId, string action, string how, DateOnly onDate)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO step_history(person_id, step_id, action, how, on_date, recorded_at)
            VALUES($p, $s, $a, $h, $d, $t);
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", stepId);
        cmd.Parameters.AddWithValue("$a", action);
        cmd.Parameters.AddWithValue("$h", how);
        cmd.Parameters.AddWithValue("$d", onDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$t", Now());
        cmd.ExecuteNonQuery();
    }

    private static DateOnly ParseDate(string text) =>
        DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ---- Contact step: welcome call --------------------------------------

    /// <summary>Booked call times for a contact: time key (see ContactService.ToKey) mapped to who booked it.</summary>
    public IReadOnlyDictionary<string, string> GetBookedCalls(string contactId)
    {
        var booked = new Dictionary<string, string>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT starts_utc, person_id FROM call_bookings WHERE contact_id = $c;";
        cmd.Parameters.AddWithValue("$c", contactId);
        using var r = cmd.ExecuteReader();
        while (r.Read()) booked[r.GetString(0)] = r.GetString(1);
        return booked;
    }

    /// <summary>The time key of this leader's booked call, or null.</summary>
    public string? GetMyCall(string personId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT starts_utc FROM call_bookings WHERE person_id = $p LIMIT 1;";
        cmd.Parameters.AddWithValue("$p", personId);
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>
    /// Books a call time. Returns false if someone else took it first.
    /// A leader holds at most one booking, so any earlier one is released.
    /// </summary>
    public bool BookCall(string contactId, string startsKey, string personId)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();

        using (var check = conn.CreateCommand())
        {
            check.Transaction = tx;
            check.CommandText = "SELECT person_id FROM call_bookings WHERE contact_id = $c AND starts_utc = $s;";
            check.Parameters.AddWithValue("$c", contactId);
            check.Parameters.AddWithValue("$s", startsKey);
            var owner = check.ExecuteScalar() as string;
            if (owner is not null && owner != personId) return false;
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = """
                DELETE FROM call_bookings WHERE person_id = $p;
                INSERT INTO call_bookings(contact_id, starts_utc, person_id, booked_at) VALUES($c, $s, $p, $t);
                """;
            cmd.Parameters.AddWithValue("$c", contactId);
            cmd.Parameters.AddWithValue("$s", startsKey);
            cmd.Parameters.AddWithValue("$p", personId);
            cmd.Parameters.AddWithValue("$t", Now());
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return true;
    }

    // ---- Contact step: group welcome --------------------------------------

    /// <summary>The time key of this leader's saved group-welcome seat, or null.</summary>
    public string? GetMySeat(string personId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT session_utc FROM group_seats WHERE person_id = $p;";
        cmd.Parameters.AddWithValue("$p", personId);
        return cmd.ExecuteScalar() as string;
    }

    public void SaveSeat(string personId, string sessionKey)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO group_seats(person_id, session_utc, saved_at) VALUES($p, $s, $t)
            ON CONFLICT(person_id) DO UPDATE SET session_utc = $s, saved_at = $t;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", sessionKey);
        cmd.Parameters.AddWithValue("$t", Now());
        cmd.ExecuteNonQuery();
    }

    // ---- Contact step: introduction ---------------------------------------

    /// <summary>The introduction this leader wrote, or null.</summary>
    public string? GetMyIntroduction(string personId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT message FROM introductions WHERE person_id = $p;";
        cmd.Parameters.AddWithValue("$p", personId);
        return cmd.ExecuteScalar() as string;
    }

    public void SaveIntroduction(string personId, string contactId, string message)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO introductions(person_id, contact_id, message, written_at) VALUES($p, $c, $m, $t)
            ON CONFLICT(person_id) DO UPDATE SET contact_id = $c, message = $m, written_at = $t;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$c", contactId);
        cmd.Parameters.AddWithValue("$m", message);
        cmd.Parameters.AddWithValue("$t", Now());
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// "Change my choice" on the contact step: releases the leader's call
    /// booking, group seat and introduction, and marks the step not done,
    /// so they can choose again. The history keeps both the "done" and
    /// the "undone" entries.
    /// </summary>
    public void ClearContactChoice(string personId, string stepId, DateOnly onDate)
    {
        using var conn = Open();
        var how = CurrentHow(conn, personId, stepId);
        if (how is not null) AddHistory(conn, personId, stepId, "undone", how, onDate);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            DELETE FROM call_bookings WHERE person_id = $p;
            DELETE FROM group_seats   WHERE person_id = $p;
            DELETE FROM introductions WHERE person_id = $p;
            DELETE FROM step_progress WHERE person_id = $p AND step_id = $s;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$s", stepId);
        cmd.ExecuteNonQuery();
    }
}

/// <summary>A completed step: how it was completed, and the date (the leader's today at the time).</summary>
public record StepDone(string How, DateOnly? DoneOn);

/// <summary>One entry in a step's history: "done" or "undone", how, and the date.</summary>
public record StepHistoryEntry(string Action, string How, DateOnly OnDate);
