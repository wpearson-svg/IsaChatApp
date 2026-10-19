using System.Globalization;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature, week 8 foundation (see ONBOARDING-FEATURE.md).
///
/// Answers one question: "What role does this person hold on this date,
/// and what day of their term is it?"
///
/// It reads the team's directory database, data/directory.db, which
/// directory/build_directory_db.py builds from the CSV files in
/// directory/ (the one master list of test people). It opens that file
/// read-only, so it can never change the directory.
///
/// The date to count from is passed in by the caller. Screens pass
/// WidgetSettings.Today, which is the demo date (3 July 2026) unless the
/// demo date is switched off on the Config page.
///
/// Day 1 is the term start date itself, so a term that started on
/// 1 July 2026 is on day 3 on 3 July 2026.
/// </summary>
public class LeaderTermService
{
    private readonly string _dbPath;

    public LeaderTermService(IConfiguration config)
    {
        _dbPath = config["DirectoryDb:Path"] ?? Path.Combine("data", "directory.db");
    }

    /// <summary>
    /// False until data/directory.db has been built. To build it, run
    /// this once from the IsaChatApp folder:
    /// python directory\build_directory_db.py
    /// </summary>
    public bool DirectoryExists => File.Exists(_dbPath);

    /// <summary>Every role one person holds on the given date. A person can hold more than one.</summary>
    public IReadOnlyList<LeaderTerm> GetCurrentTerms(string personId, DateOnly onDate)
        => Query(onDate, personId);

    /// <summary>Every role held by anyone on the given date. Used by the check table on the Config page.</summary>
    public IReadOnlyList<LeaderTerm> GetAllCurrentTerms(DateOnly onDate)
        => Query(onDate, personId: null);

    private IReadOnlyList<LeaderTerm> Query(DateOnly onDate, string? personId)
    {
        var list = new List<LeaderTerm>();
        if (!DirectoryExists) return list;

        using var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();

        // A role counts as "current" when its term started on or before
        // the date and ends on or after it. Dates are stored as
        // YYYY-MM-DD text, which sorts in date order, so plain text
        // comparison works. When personId is null, everyone is listed.
        cmd.CommandText = """
            SELECT r.person_id, p.latin_name, r.role, r.chapter_id, c.chapter_name,
                   r.term_start, r.term_end, p.time_zone
            FROM role_assignments r
            JOIN people p   ON p.person_id  = r.person_id
            JOIN chapters c ON c.chapter_id = r.chapter_id
            WHERE r.term_start <= $date
              AND r.term_end   >= $date
              AND ($person IS NULL OR r.person_id = $person)
            ORDER BY r.person_id, r.role;
            """;
        cmd.Parameters.AddWithValue("$date", onDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$person", (object?)personId ?? DBNull.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var start = ParseDate(reader.GetString(5));
            var end = ParseDate(reader.GetString(6));
            list.Add(new LeaderTerm(
                PersonId: reader.GetString(0),
                Name: reader.GetString(1),
                Role: reader.GetString(2),
                ChapterId: reader.GetString(3),
                ChapterName: reader.GetString(4),
                TermStart: start,
                TermEnd: end,
                TimeZone: reader.GetString(7),
                // DayNumber counts days since a fixed starting point, so the
                // difference is the number of days between the two dates.
                // Adding 1 makes the start date itself day 1.
                DayOfTerm: onDate.DayNumber - start.DayNumber + 1));
        }
        return list;
    }

    private static DateOnly ParseDate(string text)
        => DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}

/// <summary>One role a person holds, with the day of term on the date asked about.</summary>
public record LeaderTerm(
    string PersonId,
    string Name,
    string Role,
    string ChapterId,
    string ChapterName,
    DateOnly TermStart,
    DateOnly TermEnd,
    string TimeZone,
    int DayOfTerm);
