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
}

/// <summary>One step of the 90-day journey.</summary>
public record JourneyStep(
    string StepId,
    string Role,
    int DayFrom,
    int DayTo,
    string Title,
    string Details,
    string Source);
