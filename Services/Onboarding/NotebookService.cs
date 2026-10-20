using System.Globalization;
using System.Text.Json;
using IsaChatApp.Services.Orchestrator;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Onboarding feature, Feature 2 (see ONBOARDING-FEATURE.md): the
/// succession notebook. Each chapter has one notebook that passes from
/// president to president.
///
/// Keeps, in data/notebook.db (its own file, so resetting journey progress
/// in data/progress.db never wipes saved notes, and the other way round):
///   - saved answers: a question asked of Isa, her answer, its sources,
///     the date, and an optional "what we decided" line
///   - notes typed straight into the notebook: contacts, what worked,
///     what's unfinished, decisions, anything else
///
/// Who can do what (decided by Matthew, 20 October 2026):
///   - WRITE: the chapter's president, and the outgoing president for 30
///     days after their term ends (the handoff window, matching the
///     guidebook's turnover duties).
///   - READ EVERYTHING: the same people. A saved answer includes the
///     leader's own question, so by default only presidents see it.
///   - READ SHARED ENTRIES: any other current board member of the chapter
///     (a Leadership role) sees only the entries their writer chose to
///     share with the board, note by note. Nothing is shared unless the
///     writer says yes, the same rule as the day 30/60/90 check-ins. This
///     includes a former president after the handoff window, for example
///     an Immediate Past President (decided 20 October 2026).
///   - Anyone who has left the board sees nothing.
///   - Planned next: "Ask the outgoing president", question-and-answer
///     threads between the two presidents that stay open while the former
///     president is on the board.
///
/// Entries are never erased. "Remove" hides an entry from the notebook
/// and records the date, so the notebook keeps an honest record, like the
/// step history in ProgressService.
///
/// Typed notes must pass Isa's sign-in check and Input Compliance gate
/// before they are saved (decided 20 October 2026), so a note can't become
/// a side door around Isa's rules. AddNoteAsync runs that check through the
/// orchestrator (Orchestrator.CheckTextAsync), which writes it to the event
/// log; a note that doesn't pass is not saved.
///
/// The file is created automatically the first time Isa starts and is not
/// in Git (the .gitignore leaves out data/*.db). Deleting it empties every
/// chapter's notebook, which is handy before a demo rehearsal.
/// </summary>
public class NotebookService
{
    /// <summary>How long after their term ends an outgoing president can still add notes.</summary>
    public const int HandoffDays = 30;

    private readonly string _dbPath;
    private readonly LeaderTermService _terms;
    private readonly Orchestrator.Orchestrator _pipeline;

    public NotebookService(IConfiguration config, LeaderTermService terms, Orchestrator.Orchestrator pipeline)
    {
        _terms = terms;
        _pipeline = pipeline;
        _dbPath = config["NotebookDb:Path"] ?? Path.Combine("data", "notebook.db");
        var folder = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        // Creates the table the first time; does nothing after that.
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS notebook_entries(
              entry_id     INTEGER PRIMARY KEY AUTOINCREMENT,
              chapter_id   TEXT NOT NULL,
              author_id    TEXT NOT NULL,     -- person_id of the writer
              author_name  TEXT NOT NULL,     -- name at the time, so the record reads the same later
              author_role  TEXT NOT NULL,     -- e.g. President, Outgoing president
              kind         TEXT NOT NULL,     -- saved_answer or note
              category     TEXT,              -- notes only: decision, contact, worked, unfinished, other
              question     TEXT,              -- saved answers only
              answer       TEXT,              -- saved answers only
              sources_json TEXT,              -- saved answers only: [{document, section, page}]
              note         TEXT,              -- a typed note, or the "what we decided" line on a saved answer
              shared       INTEGER NOT NULL DEFAULT 0,  -- 1 = the writer shared it with the whole board
              on_date      TEXT NOT NULL,     -- the writer's "today" (demo date when on), YYYY-MM-DD
              recorded_at  TEXT NOT NULL,     -- real time, UTC
              removed_on   TEXT);             -- set when the writer removes it; the row stays
            CREATE INDEX IF NOT EXISTS notebook_by_chapter ON notebook_entries(chapter_id);
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

    // ---- Who can do what -------------------------------------------------

    /// <summary>
    /// What this person may do with their chapter's notebook on this date.
    /// Presidents (and the outgoing president in the handoff window) can
    /// read everything and write; other board members can read shared
    /// entries only; anyone else has no access.
    /// </summary>
    public NotebookAccess GetAccess(string personId, DateOnly onDate)
    {
        var current = _terms.GetCurrentTerms(personId, onDate);

        var president = current.FirstOrDefault(t => t.Role == "President");
        if (president is not null)
            return new NotebookAccess(president.ChapterId, president.ChapterName, president.Name, "President", CanWrite: true, SeesAll: true);

        var outgoing = _terms.GetRecentlyEndedTerms(personId, onDate, HandoffDays)
            .FirstOrDefault(t => t.Role == "President");
        if (outgoing is not null)
            return new NotebookAccess(outgoing.ChapterId, outgoing.ChapterName, outgoing.Name, "Outgoing president", CanWrite: true, SeesAll: true,
                HandoffEnds: outgoing.TermEnd.AddDays(HandoffDays));

        var board = current.FirstOrDefault(t => t.Permission == "Leadership");
        if (board is not null)
            return new NotebookAccess(board.ChapterId, board.ChapterName, board.Name, board.Role, CanWrite: false, SeesAll: false);

        return NotebookAccess.None;
    }

    // ---- Reading -----------------------------------------------------------

    /// <summary>
    /// The entries this person may see in their chapter's notebook, newest
    /// first. Removed entries are left out.
    /// </summary>
    public IReadOnlyList<NotebookEntry> GetEntries(string personId, DateOnly onDate)
    {
        var access = GetAccess(personId, onDate);
        var list = new List<NotebookEntry>();
        if (access.ChapterId is null) return list;

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        // Presidents see every entry; other board members see only shared
        // entries. ($all is 1 or 0.)
        cmd.CommandText = """
            SELECT entry_id, chapter_id, author_id, author_name, author_role, kind, category,
                   question, answer, sources_json, note, shared, on_date
            FROM notebook_entries
            WHERE chapter_id = $c
              AND removed_on IS NULL
              AND ($all = 1 OR shared = 1)
            ORDER BY on_date DESC, entry_id DESC;
            """;
        cmd.Parameters.AddWithValue("$c", access.ChapterId);
        cmd.Parameters.AddWithValue("$all", access.SeesAll ? 1 : 0);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(ReadEntry(r));
        return list;
    }

    private static NotebookEntry ReadEntry(SqliteDataReader r) => new(
        EntryId: r.GetInt64(0),
        ChapterId: r.GetString(1),
        AuthorId: r.GetString(2),
        AuthorName: r.GetString(3),
        AuthorRole: r.GetString(4),
        Kind: r.GetString(5),
        Category: r.IsDBNull(6) ? null : r.GetString(6),
        Question: r.IsDBNull(7) ? null : r.GetString(7),
        Answer: r.IsDBNull(8) ? null : r.GetString(8),
        Sources: r.IsDBNull(9) ? Array.Empty<SourceRef>() : JsonSerializer.Deserialize<List<SourceRef>>(r.GetString(9)) ?? new(),
        Note: r.IsDBNull(10) ? null : r.GetString(10),
        Shared: r.GetInt64(11) == 1,
        OnDate: DateOnly.ParseExact(r.GetString(12), "yyyy-MM-dd", CultureInfo.InvariantCulture));

    // ---- Writing -----------------------------------------------------------

    /// <summary>
    /// Saves one of Isa's answers to the chapter's notebook: the question,
    /// the answer, its sources and the date, plus an optional "what we
    /// decided" line. The question and answer already went through Isa's
    /// whole pipeline when they were asked and answered; the "what we
    /// decided" line is new typed text, so it is checked the same way as a
    /// typed note before anything is saved.
    /// </summary>
    public async Task<NoteResult> SaveAnswerAsync(string personId, DateOnly onDate, string question, string answer,
        IReadOnlyList<SourceRef> sources, string? decided, bool shareWithBoard, string? bearerToken)
    {
        var access = GetAccess(personId, onDate);
        if (!access.CanWrite) return NoteResult.Refused("You can't add to this notebook.");

        var line = Clean(decided);
        if (line is not null)
        {
            var problem = await CheckAsync(line, "notebook_decision", bearerToken,
                "This sounds like something to talk through with a person, so it wasn't saved. Your Chapter Experience contact can help.",
                "That line is outside what the notebook is for, so nothing was saved. Try a short sentence about what you decided.");
            if (problem is not null) return NoteResult.Refused(problem);
        }

        return NoteResult.Saved(Insert(access, personId, onDate, "saved_answer", category: null,
            question, answer, JsonSerializer.Serialize(sources), line, shareWithBoard));
    }

    /// <summary>
    /// Adds a typed note (contacts, what worked, what's unfinished, a
    /// decision, or anything else), after checking it with Isa's sign-in
    /// check and Input Compliance gate. <paramref name="bearerToken"/> is the
    /// signed-in leader's token, as the chat sends it to the pipeline.
    /// </summary>
    public async Task<NoteResult> AddNoteAsync(string personId, DateOnly onDate, string category, string text,
        bool shareWithBoard, string? bearerToken)
    {
        var access = GetAccess(personId, onDate);
        var note = Clean(text);
        if (!access.CanWrite) return NoteResult.Refused("You can't add to this notebook.");
        if (note is null) return NoteResult.Refused("Write a note first.");
        if (!Categories.Contains(category)) return NoteResult.Refused("Pick what kind of note this is.");

        var problem = await CheckAsync(note, "notebook_note", bearerToken,
            "This sounds like something to talk through with a person, so it wasn't saved. Your Chapter Experience contact can help.",
            "This note is outside what the notebook is for, so it wasn't saved. Notes are for running your chapter.");
        if (problem is not null) return NoteResult.Refused(problem);

        return NoteResult.Saved(Insert(access, personId, onDate, "note", category, null, null, null, note, shareWithBoard));
    }

    // Checks typed text with Isa's sign-in check and Input Compliance gate
    // (through the orchestrator, which logs it). Returns null if it passed,
    // or the message for the leader if it didn't.
    private async Task<string?> CheckAsync(string text, string purpose, string? bearerToken,
        string needsPerson, string outOfScope)
    {
        var check = await _pipeline.CheckTextAsync(new QueryContext
        {
            TraceId = Guid.NewGuid().ToString(),
            Question = text,
            IncomingToken = bearerToken,
        }, purpose);

        return check.Outcome switch
        {
            StageOutcome.Pass => null,
            StageOutcome.Escalate => needsPerson,
            _ when check.DataText.StartsWith("error=") => "Something went wrong on our side, so nothing was saved. Try again.",
            _ => outOfScope,
        };
    }

    /// <summary>The note categories, in the order the notebook shows them.</summary>
    public static readonly IReadOnlyList<string> Categories = new[] { "decision", "contact", "worked", "unfinished", "other" };

    private long Insert(NotebookAccess access, string personId, DateOnly onDate, string kind, string? category,
        string? question, string? answer, string? sourcesJson, string? note, bool shared)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notebook_entries(chapter_id, author_id, author_name, author_role, kind, category,
                                         question, answer, sources_json, note, shared, on_date, recorded_at)
            VALUES($c, $p, $n, $r, $k, $cat, $q, $a, $s, $note, $sh, $d, $t);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$c", access.ChapterId);
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$n", access.PersonName);
        cmd.Parameters.AddWithValue("$r", access.RoleLabel);
        cmd.Parameters.AddWithValue("$k", kind);
        cmd.Parameters.AddWithValue("$cat", (object?)category ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$q", (object?)question ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$a", (object?)answer ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$s", (object?)sourcesJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$note", (object?)note ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sh", shared ? 1 : 0);
        cmd.Parameters.AddWithValue("$d", Day(onDate));
        cmd.Parameters.AddWithValue("$t", Now());
        return (long)cmd.ExecuteScalar()!;
    }

    /// <summary>
    /// The writer turns "Share with the board" on or off for one of their
    /// own entries, while they can still write to the notebook. Returns
    /// false for anyone else, or once their handoff window has ended.
    /// </summary>
    public bool SetShared(string personId, long entryId, bool share, DateOnly onDate)
    {
        if (!GetAccess(personId, onDate).CanWrite) return false;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notebook_entries SET shared = $sh WHERE entry_id = $e AND author_id = $p AND removed_on IS NULL;";
        cmd.Parameters.AddWithValue("$sh", share ? 1 : 0);
        cmd.Parameters.AddWithValue("$e", entryId);
        cmd.Parameters.AddWithValue("$p", personId);
        return cmd.ExecuteNonQuery() == 1;
    }

    /// <summary>
    /// The writer removes one of their own entries. It disappears from the
    /// notebook but the row stays, with the date it was removed. Returns
    /// false for anyone else, or once their handoff window has ended.
    /// </summary>
    public bool Remove(string personId, long entryId, DateOnly onDate)
    {
        if (!GetAccess(personId, onDate).CanWrite) return false;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE notebook_entries SET removed_on = $d WHERE entry_id = $e AND author_id = $p AND removed_on IS NULL;";
        cmd.Parameters.AddWithValue("$d", Day(onDate));
        cmd.Parameters.AddWithValue("$e", entryId);
        cmd.Parameters.AddWithValue("$p", personId);
        return cmd.ExecuteNonQuery() == 1;
    }

    // Trims a note; an empty one counts as no note.
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

/// <summary>The result of adding a note: saved (with its ID), or not saved and why, in words for the leader.</summary>
public record NoteResult(long? EntryId, string? Problem)
{
    public static NoteResult Saved(long id) => new(id, null);
    public static NoteResult Refused(string why) => new(null, why);
    public bool IsSaved => EntryId is not null;
}

/// <summary>What one person may do with their chapter's notebook on a given date.</summary>
/// <param name="RoleLabel">How the notebook names their role, e.g. "President" or "Outgoing president".</param>
/// <param name="SeesAll">True for presidents: every entry. False for other board members: shared entries only.</param>
/// <param name="HandoffEnds">For an outgoing president: the last day they can still add notes.</param>
public record NotebookAccess(string? ChapterId, string? ChapterName, string? PersonName, string? RoleLabel,
    bool CanWrite, bool SeesAll, DateOnly? HandoffEnds = null)
{
    public static readonly NotebookAccess None = new(null, null, null, null, false, false);
    public bool CanRead => ChapterId is not null;
}

/// <summary>One entry in a chapter's succession notebook.</summary>
public record NotebookEntry(
    long EntryId,
    string ChapterId,
    string AuthorId,
    string AuthorName,
    string AuthorRole,
    string Kind,            // saved_answer or note
    string? Category,       // notes only
    string? Question,       // saved answers only
    string? Answer,         // saved answers only
    IReadOnlyList<SourceRef> Sources,
    string? Note,           // a typed note, or the "what we decided" line
    bool Shared,
    DateOnly OnDate);
