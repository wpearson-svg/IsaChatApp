using System.Globalization;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature (see ONBOARDING-FEATURE.md): the "Meet your
/// Chapter Experience contact" step.
///
/// Answers three questions:
///   1. Who is this chapter's Chapter Experience contact?
///   2. Which welcome-call times can the contact offer?
///   3. When is the next group welcome for new presidents?
///
/// The contacts are INVENTED sample people (never real ISACA staff).
/// They live in onboarding/contacts.csv and are built into
/// data/onboarding.db by onboarding/build_onboarding_db.py, which this
/// class opens read-only.
///
/// The calendar is fake on purpose (decided in the design): call times
/// and group sessions are worked out from "today" (the demo date) with a
/// simple fixed pattern, so they always lie a few days ahead. A real
/// calendar connection is ISACA's decision later. Which times have been
/// booked is kept by ProgressService, not here.
/// </summary>
public class ContactService
{
    private readonly string _dbPath;

    public ContactService(IConfiguration config)
    {
        _dbPath = config["OnboardingDb:Path"] ?? Path.Combine("data", "onboarding.db");
    }

    /// <summary>
    /// The contact for a chapter, or null if there is none or the
    /// contacts haven't been built into data/onboarding.db yet (run
    /// python onboarding\build_onboarding_db.py).
    /// </summary>
    public ChapterContact? GetContactForChapter(string chapterId)
    {
        if (!File.Exists(_dbPath)) return null;
        try
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT c.contact_id, c.name, c.team, c.region_label, c.time_zone, c.email
                FROM contact_chapters cc
                JOIN contacts c ON c.contact_id = cc.contact_id
                WHERE cc.chapter_id = $chapter;
                """;
            cmd.Parameters.AddWithValue("$chapter", chapterId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new ChapterContact(r.GetString(0), r.GetString(1), r.GetString(2),
                                      r.GetString(3), r.GetString(4), r.GetString(5));
        }
        catch (SqliteException)
        {
            // Older onboarding.db without the contacts tables.
            return null;
        }
    }

    // The fake calendar pattern, in the CONTACT's own time zone: one
    // call time on each of the next three weekdays after today.
    private static readonly TimeOnly[] CallTimes = { new(10, 0), new(14, 0), new(9, 30) };

    /// <summary>Three welcome-call start times (in UTC) on the next three weekdays after today.</summary>
    public IReadOnlyList<DateTime> GetCallSlotsUtc(ChapterContact contact, DateOnly today)
    {
        var slots = new List<DateTime>();
        var day = today;
        while (slots.Count < CallTimes.Length)
        {
            day = day.AddDays(1);
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            slots.Add(ToUtc(day, CallTimes[slots.Count], contact.TimeZone));
        }
        return slots;
    }

    /// <summary>The next group welcome: the first Wednesday after today, 10:00 in the contact's time zone, in UTC.</summary>
    public DateTime GetGroupSessionUtc(ChapterContact contact, DateOnly today)
    {
        var day = today.AddDays(1);
        while (day.DayOfWeek != DayOfWeek.Wednesday) day = day.AddDays(1);
        return ToUtc(day, new TimeOnly(10, 0), contact.TimeZone);
    }

    // ---- Time zone helpers (used by the Today window too) -------------

    /// <summary>A date and clock time in one time zone, turned into UTC.</summary>
    public static DateTime ToUtc(DateOnly date, TimeOnly time, string timeZone)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, FindZone(timeZone));
    }

    /// <summary>A UTC time shown in someone's own time zone, e.g. "Tue 7 Jul, 11:00".</summary>
    public static string FormatInZone(DateTime utc, string timeZone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), FindZone(timeZone));
        return local.ToString("ddd d MMM, HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>UTC as stored in the progress database, e.g. "2026-07-06T02:00Z".</summary>
    public static string ToKey(DateTime utc) =>
        utc.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture);

    /// <summary>The reverse of ToKey.</summary>
    public static DateTime FromKey(string key) =>
        DateTime.ParseExact(key, "yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    // Time zone names like "Asia/Tokyo" come from our synthetic person
    // records. .NET understands them on Windows too. If one is ever
    // misspelled, UTC is used rather than crashing the screen.
    private static TimeZoneInfo FindZone(string timeZone)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZone); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }
}

/// <summary>An invented sample Chapter Experience contact.</summary>
public record ChapterContact(string ContactId, string Name, string Team, string RegionLabel, string TimeZone, string Email)
{
    /// <summary>First name, for the greeting in the introduction message.</summary>
    public string FirstName => Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? Name;

    /// <summary>Initials for the round badge, e.g. "Mei Ling Tan" becomes "MT".</summary>
    public string Initials
    {
        get
        {
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            var first = char.ToUpperInvariant(parts[0][0]);
            return parts.Length == 1 ? first.ToString() : $"{first}{char.ToUpperInvariant(parts[^1][0])}";
        }
    }
}
