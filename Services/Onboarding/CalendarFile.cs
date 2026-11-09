using System.Globalization;
using System.Text;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// "Add to my calendar" in the Today window's calendar (November 6, 2026).
///
/// Makes a standard calendar file (.ics, the iCalendar format) for one item:
/// a journey step, an ISACA key date, or the leader's booked welcome call or
/// group seat. The leader's browser downloads it, and opening it adds the
/// event to Outlook, Google Calendar or Apple Calendar.
///
/// Isa connects to no calendar service and sends nothing. The leader chooses
/// to add the event, and their own calendar does any reminding, so the rule
/// that Isa never contacts a leader while they are away still holds.
///
/// The event carries only the item's title, dates and a short line pointing
/// back to Isa: no notebook notes, no questions to Isa, no email addresses.
/// It is a one-time copy: if Isa's dates change later, the leader's calendar
/// does not update (said in the supplement document, not beside the button).
/// </summary>
public static class CalendarFile
{
    /// <summary>An all-day event from <paramref name="first"/> to <paramref name="last"/>, both included.</summary>
    public static string AllDay(string uid, string title, string description, DateOnly first, DateOnly last) =>
        Wrap(uid, title, description,
            $"DTSTART;VALUE=DATE:{D(first)}",
            // In the format the end day is the day AFTER the event (not included).
            $"DTEND;VALUE=DATE:{D(last.AddDays(1))}");

    /// <summary>A timed event, in UTC, so every calendar shows it in its own time zone.</summary>
    public static string Timed(string uid, string title, string description, DateTime startUtc, int minutes) =>
        Wrap(uid, title, description,
            $"DTSTART:{T(startUtc)}",
            $"DTEND:{T(startUtc.AddMinutes(minutes))}");

    /// <summary>A file name the leader will recognise, from the title: "Isa - The annual compliance window.ics".</summary>
    public static string FileName(string title)
    {
        var safe = new string(title.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();
        if (safe.Length > 60) safe = safe[..60].Trim();
        return $"Isa - {safe}.ics";
    }

    private static string Wrap(string uid, string title, string description, string start, string end)
    {
        var lines = new[]
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Isa chapter support prototype//EN",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{uid}@isa.example.org",
            $"DTSTAMP:{T(DateTime.UtcNow)}",
            start,
            end,
            $"SUMMARY:{Escape(title)}",
            $"DESCRIPTION:{Escape(description)}",
            "TRANSP:TRANSPARENT",   // all-day reminders don't mark the leader as busy
            "END:VEVENT",
            "END:VCALENDAR",
        };
        var sb = new StringBuilder();
        foreach (var line in lines) sb.Append(Fold(line)).Append("\r\n");   // the format needs CRLF line ends
        return sb.ToString();
    }

    private static string D(DateOnly d) => d.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
    private static string T(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    // Commas, semicolons, backslashes and line breaks have meanings in the format, so they are escaped.
    private static string Escape(string text) => text
        .Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,")
        .Replace("\r\n", "\\n").Replace("\n", "\\n");

    // The format limits a line to 75 bytes; longer ones continue on the next line after a space.
    private static string Fold(string line)
    {
        var bytes = Encoding.UTF8.GetByteCount(line);
        if (bytes <= 75) return line;
        var sb = new StringBuilder();
        var count = 0;
        foreach (var ch in line)
        {
            var size = Encoding.UTF8.GetByteCount(ch.ToString());
            if (count + size > 74) { sb.Append("\r\n "); count = 1; }
            sb.Append(ch);
            count += size;
        }
        return sb.ToString();
    }
}
