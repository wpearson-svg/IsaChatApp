using System.Globalization;
using IsaChatApp.Services.Orchestrator;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature (ONBOARDING-FEATURE.md, "Board introduction email and
/// board roster", decided 20 October 2026; built 3 November 2026): the new
/// president's introduction to the board (step J-19) and the board's
/// optional four-question profile form, whose replies the president sees in
/// step J-25.
///
/// How it works:
///   - The president writes the introduction from a draft Isa fills in from
///     ISACA's records (name, chapter, term start). Isa never writes it
///     herself: the AI model only words ISACA's passages. Saving it completes
///     J-19. Nothing is emailed: in the prototype it is saved in Isa.
///   - Once it is saved, each current board member of that chapter sees one
///     line at the top of their chat inviting them to the optional profile
///     form. "No thanks" hides it for good; the president is never told who
///     said no (a member who hasn't answered is "No reply yet").
///   - The four answers: what they're good at, what they'd enjoy helping
///     with, about how much time each month (a choice), and how they prefer
///     to be contacted (a choice). All optional. Only the president who asked
///     sees them. A member can change or withdraw their answers any time.
///   - Typed text (the introduction and the two written answers) passes
///     Isa's sign-in check and Input Compliance gate before it is saved, like
///     notebook notes, so neither is a side door around Isa's rules.
///
/// Isa does not learn names or roles from conversations: the board comes
/// from ISACA's records (LeaderTermService). Stored in data/progress.db with
/// the rest of what leaders do in the journey (local, not in Git).
/// </summary>
public class BoardIntroService
{
    /// <summary>"Introduce yourself to your board".</summary>
    public const string IntroStepId = "J-19";

    /// <summary>The choices for "About how much time can you give each month?"</summary>
    public static readonly IReadOnlyList<string> TimeChoices =
        ["Less than 2 hours", "2 to 5 hours", "5 to 10 hours", "More than 10 hours"];

    /// <summary>The choices for "How do you prefer to be contacted?"</summary>
    public static readonly IReadOnlyList<string> ContactChoices =
        ["Email", "Phone", "A message on Engage", "A chat app"];

    private readonly string _dbPath;
    private readonly LeaderTermService _terms;
    private readonly ProgressService _progress;
    private readonly Orchestrator.Orchestrator _pipeline;

    public BoardIntroService(IConfiguration config, LeaderTermService terms, ProgressService progress, Orchestrator.Orchestrator pipeline)
    {
        _terms = terms;
        _progress = progress;
        _pipeline = pipeline;
        _dbPath = config["ProgressDb:Path"] ?? Path.Combine("data", "progress.db");
        var folder = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        // Creates the tables the first time; does nothing after that.
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS board_introductions(
              president_id TEXT NOT NULL,
              chapter_id   TEXT NOT NULL,
              message      TEXT NOT NULL,
              on_date      TEXT NOT NULL,   -- the president's "today" (demo date when on), YYYY-MM-DD
              written_at   TEXT NOT NULL,   -- real time, UTC
              PRIMARY KEY (president_id, chapter_id));
            CREATE TABLE IF NOT EXISTS board_profiles(
              person_id    TEXT NOT NULL,   -- the board member
              chapter_id   TEXT NOT NULL,
              president_id TEXT NOT NULL,   -- the president who asked; only they see the answers
              status       TEXT NOT NULL,   -- answered, declined or withdrawn
              good_at      TEXT,
              enjoy        TEXT,
              time_per_month TEXT,
              contact_by   TEXT,
              on_date      TEXT NOT NULL,   -- the member's "today" when last changed
              updated_at   TEXT NOT NULL,   -- real time, UTC
              PRIMARY KEY (person_id, chapter_id, president_id));
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

    // ---- The president's introduction (J-19) ----------------------------------

    /// <summary>The draft Isa offers, filled in from ISACA's records. The president edits it freely.</summary>
    public static string Draft(LeaderTerm president) =>
        "Dear board,\n\n" +
        $"I'm {president.Name}, the new president of the {president.ChapterName}. " +
        $"My term started on {president.TermStart.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}, " +
        "and I'm looking forward to working with each of you this year.\n\n" +
        "To help us share the work, I'd like to know a little about what you enjoy and how you'd like to help. " +
        "If you'd like, please answer four short questions in Isa. It's optional and takes a few minutes.\n\n" +
        $"Best regards,\n{president.Name}";

    /// <summary>The introduction this president saved for their chapter, or null.</summary>
    public BoardIntroduction? GetIntroduction(string presidentId, string chapterId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT message, on_date FROM board_introductions WHERE president_id = $p AND chapter_id = $c;";
        cmd.Parameters.AddWithValue("$p", presidentId);
        cmd.Parameters.AddWithValue("$c", chapterId);
        using var r = cmd.ExecuteReader();
        return r.Read() ? new BoardIntroduction(r.GetString(0), ParseDay(r.GetString(1))) : null;
    }

    /// <summary>
    /// Saves the president's introduction after Isa's sign-in check and Input
    /// Compliance gate, and completes J-19. Returns null if saved, or the
    /// message for the leader if it wasn't.
    /// </summary>
    public async Task<string?> SaveIntroductionAsync(string presidentId, DateOnly onDate, string text, string? bearerToken)
    {
        var president = _terms.GetCurrentTerms(presidentId, onDate).FirstOrDefault(t => t.Role == "President");
        var message = text.Trim();
        if (president is null) return "Only the chapter's president can introduce themselves to the board here.";
        if (message.Length == 0) return "Write your introduction first.";

        var problem = await CheckAsync(message, "board_introduction", bearerToken,
            "This sounds like something to talk through with a person, so it wasn't saved. Your Chapter Experience contact can help.",
            "Part of this is outside what Isa can help with, so it wasn't saved. Try a short hello to your board.");
        if (problem is not null) return problem;

        using (var conn = Open())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                INSERT INTO board_introductions(president_id, chapter_id, message, on_date, written_at) VALUES($p, $c, $m, $d, $t)
                ON CONFLICT(president_id, chapter_id) DO UPDATE SET message = $m, on_date = $d, written_at = $t;
                """;
            cmd.Parameters.AddWithValue("$p", presidentId);
            cmd.Parameters.AddWithValue("$c", president.ChapterId);
            cmd.Parameters.AddWithValue("$m", message);
            cmd.Parameters.AddWithValue("$d", Day(onDate));
            cmd.Parameters.AddWithValue("$t", Now());
            cmd.ExecuteNonQuery();
        }
        _progress.CompleteStep(presidentId, IntroStepId, "introduction", onDate);
        return null;
    }

    // ---- The board member's side: the optional profile form -------------------

    /// <summary>
    /// The profile request waiting for this board member, if any: their
    /// chapter's current president has saved an introduction, and the member
    /// hasn't answered or said no. The president never gets one for themself.
    /// </summary>
    public ProfileRequest? GetRequest(string personId, DateOnly onDate, bool includeAnswered = false)
    {
        foreach (var term in _terms.GetCurrentTerms(personId, onDate).Where(t => t.Permission == "Leadership" && t.Role != "President"))
        {
            var president = _terms.GetBoard(term.ChapterId, onDate).FirstOrDefault(m => m.Role == "President");
            if (president is null || president.PersonId == personId) continue;
            if (GetIntroduction(president.PersonId, term.ChapterId) is null) continue;

            var mine = GetProfile(personId, term.ChapterId, president.PersonId);
            if (!includeAnswered && mine is { Status: "answered" or "declined" }) continue;
            // The president's name as the notebook writes it (ISACA's records), not the board list's short form.
            var presidentName = _terms.GetCurrentTerms(president.PersonId, onDate).FirstOrDefault(t => t.Role == "President")?.Name ?? president.Name;
            return new ProfileRequest(term.ChapterId, term.ChapterName, president.PersonId, presidentName, term.Role, mine);
        }
        return null;
    }

    /// <summary>
    /// Saves a board member's answers after Isa's sign-in check and Input
    /// Compliance gate (the two written answers). Every answer is optional,
    /// but at least one must be given. Returns null if saved, or the message.
    /// </summary>
    public async Task<string?> SaveProfileAsync(string personId, DateOnly onDate, string? goodAt, string? enjoy,
        string? timePerMonth, string? contactBy, string? bearerToken)
    {
        var request = GetRequest(personId, onDate, includeAnswered: true);
        if (request is null) return "There's no profile request for you right now.";
        goodAt = Clean(goodAt);
        enjoy = Clean(enjoy);
        timePerMonth = TimeChoices.Contains(timePerMonth) ? timePerMonth : null;
        contactBy = ContactChoices.Contains(contactBy) ? contactBy : null;
        if (goodAt is null && enjoy is null && timePerMonth is null && contactBy is null)
            return "Answer at least one question, or choose No thanks.";

        var written = string.Join("\n", new[] { goodAt, enjoy }.Where(x => x is not null));
        if (written.Length > 0)
        {
            var problem = await CheckAsync(written, "board_profile", bearerToken,
                "This sounds like something to talk through with a person, so it wasn't saved. The Chapter Experience team can help.",
                "Part of this is outside what the profile is for, so it wasn't saved. Try a few words about your skills and interests.");
            if (problem is not null) return problem;
        }

        Upsert(personId, request, "answered", goodAt, enjoy, timePerMonth, contactBy, onDate);
        return null;
    }

    /// <summary>"No thanks": the invitation goes away for good. The president only ever sees "No reply yet".</summary>
    public void Decline(string personId, DateOnly onDate)
    {
        var request = GetRequest(personId, onDate);
        if (request is not null) Upsert(personId, request, "declined", null, null, null, null, onDate);
    }

    /// <summary>Withdraws a member's answers: the row stays, the answers are cleared.</summary>
    public void Withdraw(string personId, DateOnly onDate)
    {
        var request = GetRequest(personId, onDate, includeAnswered: true);
        if (request is not null) Upsert(personId, request, "withdrawn", null, null, null, null, onDate);
    }

    private void Upsert(string personId, ProfileRequest request, string status, string? goodAt, string? enjoy,
        string? timePerMonth, string? contactBy, DateOnly onDate)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO board_profiles(person_id, chapter_id, president_id, status, good_at, enjoy, time_per_month, contact_by, on_date, updated_at)
            VALUES($p, $c, $pr, $s, $g, $e, $tm, $cb, $d, $t)
            ON CONFLICT(person_id, chapter_id, president_id) DO UPDATE SET
              status = $s, good_at = $g, enjoy = $e, time_per_month = $tm, contact_by = $cb, on_date = $d, updated_at = $t;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$c", request.ChapterId);
        cmd.Parameters.AddWithValue("$pr", request.PresidentId);
        cmd.Parameters.AddWithValue("$s", status);
        cmd.Parameters.AddWithValue("$g", (object?)goodAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$e", (object?)enjoy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$tm", (object?)timePerMonth ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cb", (object?)contactBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$d", Day(onDate));
        cmd.Parameters.AddWithValue("$t", Now());
        cmd.ExecuteNonQuery();
    }

    private BoardProfile? GetProfile(string personId, string chapterId, string presidentId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT status, good_at, enjoy, time_per_month, contact_by, on_date FROM board_profiles
            WHERE person_id = $p AND chapter_id = $c AND president_id = $pr;
            """;
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$c", chapterId);
        cmd.Parameters.AddWithValue("$pr", presidentId);
        using var r = cmd.ExecuteReader();
        return r.Read()
            ? new BoardProfile(r.GetString(0), Str(r, 1), Str(r, 2), Str(r, 3), Str(r, 4), ParseDay(r.GetString(5)))
            : null;
    }

    // ---- The president's side: replies so far (J-25) --------------------------

    /// <summary>
    /// The president's board, each member with their answers if they gave
    /// them. Anyone who hasn't answered (including anyone who said no or
    /// withdrew) shows as no reply, so the president never learns who said no.
    /// </summary>
    public IReadOnlyList<BoardReply> GetReplies(string presidentId, DateOnly onDate)
    {
        var president = _terms.GetCurrentTerms(presidentId, onDate).FirstOrDefault(t => t.Role == "President");
        if (president is null) return [];
        return _terms.GetBoard(president.ChapterId, onDate)
            .Where(m => m.PersonId != presidentId)
            .Select(m =>
            {
                var p = GetProfile(m.PersonId, president.ChapterId, presidentId);
                return new BoardReply(m, p is { Status: "answered" } ? p : null);
            })
            .ToList();
    }

    // Checks typed text with Isa's sign-in check and Input Compliance gate
    // (through the orchestrator, which logs it). Returns null if it passed,
    // or the message for the leader if it didn't.
    private async Task<string?> CheckAsync(string text, string purpose, string? bearerToken, string needsPerson, string outOfScope)
    {
        var ctx = new QueryContext
        {
            TraceId = Guid.NewGuid().ToString(),
            Question = text,
            IncomingToken = bearerToken,
        };
        var check = await _pipeline.CheckTextAsync(ctx, purpose);

        return check.Outcome switch
        {
            StageOutcome.Pass => null,
            // The sign-in check refused it: say so, rather than blaming the words (3 November 2026).
            _ when ctx.AuthFailed => "You're not signed in any more, so nothing was saved. Please sign in again and try once more.",
            StageOutcome.Escalate => needsPerson,
            _ when check.DataText.StartsWith("error=") => "Something went wrong on our side, so nothing was saved. Try again.",
            _ => outOfScope,
        };
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static string? Str(SqliteDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    private static DateOnly ParseDay(string s) => DateOnly.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}

/// <summary>A president's saved introduction to the board, and the day it was saved.</summary>
public record BoardIntroduction(string Message, DateOnly OnDate);

/// <summary>A board member's answers (Status: answered, declined or withdrawn).</summary>
public record BoardProfile(string Status, string? GoodAt, string? Enjoy, string? TimePerMonth, string? ContactBy, DateOnly OnDate);

/// <summary>An invitation to the profile form: which chapter, which president asked, the member's role, and any earlier answers.</summary>
public record ProfileRequest(string ChapterId, string ChapterName, string PresidentId, string PresidentName, string Role, BoardProfile? Mine);

/// <summary>One board member in J-25, with their answers, or null for "No reply yet".</summary>
public record BoardReply(BoardMember Member, BoardProfile? Profile);
