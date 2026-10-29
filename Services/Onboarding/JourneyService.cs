using System.Globalization;
using IsaChatApp.Services.Orchestrator;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature (see ONBOARDING-FEATURE.md): the 90-day journey steps.
///
/// Answers one question: "Which journey steps should this role see on
/// this day of their term?"
///
/// The steps live in onboarding/journey_steps.csv (the master copy).
/// onboarding/build_onboarding_db.py checks that file and builds
/// data/onboarding.db from it. This class opens that database read-only.
///
/// A step is shown from its "day from" to its "day to", counting the term
/// start date as day 1. The day of term itself comes from
/// LeaderTermService.
/// </summary>
public class JourneyService
{
    private readonly string _dbPath;

    public JourneyService(IConfiguration config)
    {
        _dbPath = config["OnboardingDb:Path"] ?? Path.Combine("data", "onboarding.db");
    }

    /// <summary>
    /// False until data/onboarding.db has been built. To build it, run
    /// this from the IsaChatApp folder:
    /// python onboarding\build_onboarding_db.py
    /// </summary>
    public bool StepsExist => File.Exists(_dbPath);

    /// <summary>The steps a role should see on one day of their term, in order.</summary>
    public IReadOnlyList<JourneyStep> GetStepsForDay(string role, int dayOfTerm)
        => Query(role, dayOfTerm);

    /// <summary>Every step for a role, in order. For screens that show the whole journey.</summary>
    public IReadOnlyList<JourneyStep> GetAllSteps(string role)
        => Query(role, dayOfTerm: null);

    private IReadOnlyList<JourneyStep> Query(string role, int? dayOfTerm)
    {
        var list = new List<JourneyStep>();
        if (!StepsExist) return list;

        using var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();

        // When dayOfTerm is null, every step for the role is returned.
        // Otherwise only the steps whose day range includes that day.
        cmd.CommandText = """
            SELECT step_id, role, day_from, day_to, title, details, source
            FROM journey_steps
            WHERE role = $role
              AND ($day IS NULL OR (day_from <= $day AND day_to >= $day))
            ORDER BY day_from, step_id;
            """;
        cmd.Parameters.AddWithValue("$role", role);
        cmd.Parameters.AddWithValue("$day", (object?)dayOfTerm ?? DBNull.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new JourneyStep(
                StepId: reader.GetString(0),
                Role: reader.GetString(1),
                DayFrom: reader.GetInt32(2),
                DayTo: reader.GetInt32(3),
                Title: reader.GetString(4),
                Details: reader.GetString(5),
                Source: reader.GetString(6)));
        }
        return list;
    }

    /// <summary>
    /// Key calendar dates from ISACA documents (onboarding/key_dates.csv),
    /// each worked out for the year around the given date: either open
    /// now (today falls between its start and end) or starting in a
    /// number of days. Used by the "Today" note. The dates come from the
    /// database, never from the AI model, so the counts are always exact.
    /// </summary>
    public IReadOnlyList<KeyDateOnDay> GetKeyDates(DateOnly today)
    {
        var list = new List<KeyDateOnDay>();
        if (!StepsExist) return list;

        using var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT date_id, title, starts, ends, source FROM key_dates ORDER BY starts;";
        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var starts = reader.GetString(2);
                var ends = reader.IsDBNull(3) ? starts : reader.GetString(3);
                list.Add(OnDay(reader.GetString(0), reader.GetString(1), starts, ends, reader.GetString(4), today));
            }
        }
        catch (SqliteException)
        {
            // data/onboarding.db was built before key dates existed:
            // run python onboarding\build_onboarding_db.py again.
        }
        return list.OrderBy(k => k.IsOpen ? 0 : 1).ThenBy(k => k.DaysUntil).ToList();
    }

    /// <summary>
    /// The outgoing president's handoff checklist, in order: the President
    /// Guidebook's turnover items word for word (onboarding/handoff_steps.csv).
    /// </summary>
    public IReadOnlyList<HandoffStep> GetHandoffSteps()
    {
        var list = new List<HandoffStep>();
        if (!StepsExist) return list;

        using var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT step_id, title, text, source, notebook_kind FROM handoff_steps ORDER BY step_id;";
        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(new HandoffStep(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4)));
        }
        catch (SqliteException)
        {
            // data/onboarding.db was built before the handoff checklist existed:
            // run the onboarding build script again.
        }
        return list;
    }

    /// <summary>
    /// The "worth saving?" label for an answer, or null. A fixed rule, not
    /// the AI model: if the source the answer cites (the first source) is a
    /// document section tagged in onboarding/save_hints.csv, the save button
    /// shows that tag ("deadline" or "decision") and its reason. Only the
    /// cited source counts, so a passage Isa merely looked at never adds a label.
    /// </summary>
    public SaveHint? GetSaveHint(IReadOnlyList<SourceRef> sources)
    {
        if (!StepsExist || sources.Count == 0) return null;
        var cited = sources[0];

        using var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT hint_id, label, reason FROM save_hints
            WHERE document = $document AND section = $section AND page = $page;
            """;
        cmd.Parameters.AddWithValue("$document", cited.Document.Trim());
        cmd.Parameters.AddWithValue("$section", cited.Section.Trim());
        cmd.Parameters.AddWithValue("$page", cited.Page.Trim());
        try
        {
            using var reader = cmd.ExecuteReader();
            return reader.Read()
                ? new SaveHint(reader.GetString(0), reader.GetString(1), reader.GetString(2), cited.Document)
                : null;
        }
        catch (SqliteException)
        {
            // data/onboarding.db was built before the save hints existed:
            // run the onboarding build script again. No label until then.
            return null;
        }
    }

    // Places a yearly date range ("11-01" to "12-31") around today: this
    // year's range if today is inside it or before it, otherwise next year's.
    private static KeyDateOnDay OnDay(string id, string title, string starts, string ends, string source, DateOnly today)
    {
        DateOnly In(int year, string monthDay) => new(year,
            int.Parse(monthDay[..2], CultureInfo.InvariantCulture), int.Parse(monthDay[3..], CultureInfo.InvariantCulture));

        var start = In(today.Year, starts);
        var end = In(today.Year, ends);
        if (end < start) end = end.AddYears(1);          // a range that crosses New Year
        if (today > end) { start = start.AddYears(1); end = end.AddYears(1); }

        var isOpen = today >= start && today <= end;
        return new KeyDateOnDay(id, title, start, end, source, isOpen,
            DaysUntil: isOpen ? 0 : start.DayNumber - today.DayNumber,
            DaysLeft: isOpen ? end.DayNumber - today.DayNumber : 0);
    }
}

/// <summary>A "worth saving?" label for one answer (onboarding/save_hints.csv).</summary>
/// <param name="Label">deadline or decision.</param>
/// <param name="Reason">Plain words that finish "This answer cites ...".</param>
/// <param name="Document">The cited document, named in the explanation.</param>
public record SaveHint(string HintId, string Label, string Reason, string Document);

/// <summary>One item of the outgoing president's handoff checklist.</summary>
/// <param name="Title">A short title in Isa's own words.</param>
/// <param name="Text">The President Guidebook's own words, unchanged.</param>
/// <param name="NotebookKind">If set, the item offers "Add to the notebook" with this note kind.</param>
public record HandoffStep(string StepId, string Title, string Text, string Source, string? NotebookKind);

/// <summary>A key calendar date, placed around a given day.</summary>
/// <param name="DaysUntil">Days until it starts (0 when it is open).</param>
/// <param name="DaysLeft">While open: days until it ends (0 on its last day).</param>
public record KeyDateOnDay(string DateId, string Title, DateOnly Starts, DateOnly Ends, string Source,
    bool IsOpen, int DaysUntil, int DaysLeft);

/// <summary>One step of the 90-day journey.</summary>
public record JourneyStep(
    string StepId,
    string Role,
    int DayFrom,
    int DayTo,
    string Title,
    string Details,
    string Source);
