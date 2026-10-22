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

        // Added 22 October 2026 for the notebook index, entry pages and printed
        // decision records (Matthew's approved mockup, Claude outputs/Notebook
        // records mockups.html). Added to an existing file without touching
        // the entries already in it:
        //   topic      Elections, Finances, Meetings, Compliance, Membership, Events or Other
        //   shared_on  the writer's "today" when they shared the entry with the board
        //   parent_id  for a follow-up: the entry it follows up on
        AddColumnIfMissing(conn, "topic", "TEXT");
        AddColumnIfMissing(conn, "shared_on", "TEXT");
        AddColumnIfMissing(conn, "parent_id", "INTEGER");
        FillInOlderEntries(conn);

        // "Email this record": in the prototype nothing is emailed; each
        // request is kept here instead, like the introduction on the contact
        // step. A real version would hand these to ISACA's mail system.
        using var emails = conn.CreateCommand();
        emails.CommandText = """
            CREATE TABLE IF NOT EXISTS notebook_emails(
              email_id     INTEGER PRIMARY KEY AUTOINCREMENT,
              entry_id     INTEGER NOT NULL,  -- the record that would be sent
              sender_id    TEXT NOT NULL,     -- person_id of the leader who asked
              to_address   TEXT NOT NULL,
              copy         TEXT NOT NULL,     -- board or mine
              message      TEXT,              -- optional, checked like a note
              on_date      TEXT NOT NULL,     -- the leader's "today", YYYY-MM-DD
              recorded_at  TEXT NOT NULL);    -- real time, UTC
            """;
        emails.ExecuteNonQuery();
    }

    // Entries saved before 22 October 2026 have no topic or shared date.
    // Give them a topic by the same word rule, and take the day an older
    // shared entry was saved as the day it was shared (the closest we know).
    // Runs only on entries still missing them, so it does nothing after the first start.
    private static void FillInOlderEntries(SqliteConnection conn)
    {
        var missing = new List<(long Id, string? Question, string? Note)>();
        using (var read = conn.CreateCommand())
        {
            read.CommandText = "SELECT entry_id, question, note FROM notebook_entries WHERE topic IS NULL;";
            using var r = read.ExecuteReader();
            while (r.Read())
                missing.Add((r.GetInt64(0), r.IsDBNull(1) ? null : r.GetString(1), r.IsDBNull(2) ? null : r.GetString(2)));
        }
        foreach (var (id, question, note) in missing)
        {
            using var set = conn.CreateCommand();
            set.CommandText = "UPDATE notebook_entries SET topic = $t WHERE entry_id = $e;";
            set.Parameters.AddWithValue("$t", TopicFor(question, note));
            set.Parameters.AddWithValue("$e", id);
            set.ExecuteNonQuery();
        }
        using var shared = conn.CreateCommand();
        shared.CommandText = "UPDATE notebook_entries SET shared_on = on_date WHERE shared = 1 AND shared_on IS NULL;";
        shared.ExecuteNonQuery();
    }

    private static void AddColumnIfMissing(SqliteConnection conn, string column, string type)
    {
        using var check = conn.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM pragma_table_info('notebook_entries') WHERE name = $n;";
        check.Parameters.AddWithValue("$n", column);
        if ((long)check.ExecuteScalar()! > 0) return;
        using var add = conn.CreateCommand();
        add.CommandText = $"ALTER TABLE notebook_entries ADD COLUMN {column} {type};";
        add.ExecuteNonQuery();
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
            return new NotebookAccess(president.ChapterId, president.ChapterName, president.Name, "President", CanWrite: true, SeesAll: true,
                TimeZone: president.TimeZone);

        var outgoing = _terms.GetRecentlyEndedTerms(personId, onDate, HandoffDays)
            .FirstOrDefault(t => t.Role == "President");
        if (outgoing is not null)
            return new NotebookAccess(outgoing.ChapterId, outgoing.ChapterName, outgoing.Name, "Outgoing president", CanWrite: true, SeesAll: true,
                HandoffEnds: outgoing.TermEnd.AddDays(HandoffDays), TimeZone: outgoing.TimeZone);

        var board = current.FirstOrDefault(t => t.Permission == "Leadership");
        if (board is not null)
            return new NotebookAccess(board.ChapterId, board.ChapterName, board.Name, board.Role, CanWrite: false, SeesAll: false,
                TimeZone: board.TimeZone);

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
        // entries. ($all is 1 or 0.) Follow-ups come back too, with their
        // ParentId set; the panel shows them under the entry they follow.
        // A follow-up whose entry was removed goes with it.
        cmd.CommandText = """
            SELECT e.entry_id, e.chapter_id, e.author_id, e.author_name, e.author_role, e.kind, e.category,
                   e.question, e.answer, e.sources_json, e.note, e.shared, e.on_date,
                   e.topic, e.shared_on, e.parent_id, e.recorded_at
            FROM notebook_entries e
            LEFT JOIN notebook_entries p ON p.entry_id = e.parent_id
            WHERE e.chapter_id = $c
              AND e.removed_on IS NULL
              AND (e.parent_id IS NULL OR p.removed_on IS NULL)
              AND ($all = 1 OR e.shared = 1)
            ORDER BY e.on_date DESC, e.entry_id DESC;
            """;
        cmd.Parameters.AddWithValue("$c", access.ChapterId);
        cmd.Parameters.AddWithValue("$all", access.SeesAll ? 1 : 0);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var entry = ReadEntry(r);
            // A saved answer includes the president's own question, which is
            // private. Other board members see a shared entry without it
            // (the "Board copy" rule, Matthew's approved mockup, 22 October 2026).
            list.Add(access.SeesAll ? entry : entry with { Question = null });
        }
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
        OnDate: DateOnly.ParseExact(r.GetString(12), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Topic: r.IsDBNull(13) ? "Other" : r.GetString(13),
        SharedOn: r.IsDBNull(14) ? null : DateOnly.ParseExact(r.GetString(14), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        ParentId: r.IsDBNull(15) ? null : r.GetInt64(15),
        RecordedAtUtc: DateTime.SpecifyKind(DateTime.Parse(r.GetString(16), CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), DateTimeKind.Utc));

    // ---- Topics --------------------------------------------------------------

    /// <summary>The topics an entry can have, in the order the notebook shows them.</summary>
    public static readonly IReadOnlyList<string> Topics =
        new[] { "Elections", "Finances", "Meetings", "Compliance", "Membership", "Events", "Other" };

    // The words that pick a topic. A fixed, visible rule (not AI judgment),
    // like the save-offer rules: the first topic whose word appears wins.
    // The writer can change the topic on the entry page.
    private static readonly (string Topic, string[] Words)[] TopicWords =
    {
        ("Elections",  new[] { "election", " vote", "voting", "ballot", "nominat", "candidate" }),
        ("Finances",   new[] { "budget", "bank", "money", "finance", "financial", "treasur", "spend", "spent", " dues", " fees", "sponsor", "invoice", "audit" }),
        ("Compliance", new[] { "compliance", "comply", "requirement", "affiliation", "bylaw" }),
        ("Meetings",   new[] { "meeting", "agenda", "minutes", "quorum" }),
        ("Membership", new[] { "membership", "new member", "volunteer", "recruit", "renewal" }),
        ("Events",     new[] { "event", "webinar", "conference", " cpe", "venue", "speaker", "workshop" }),
    };

    /// <summary>The topic the fixed word rule gives this text, or "Other".</summary>
    public static string TopicFor(params string?[] texts)
    {
        // A leading space makes " vote", " dues" and the like match only at the start of a word.
        var all = " " + string.Join(" ", texts.Where(t => t is not null)).ToLowerInvariant();
        foreach (var (topic, words) in TopicWords)
            if (words.Any(w => all.Contains(w))) return topic;
        return "Other";
    }

    /// <summary>
    /// When an entry was recorded, as the leader reads it: the entry's day
    /// (the demo date while it is on) and the time of day it was really
    /// saved, in the reader's time zone, e.g. "28 June 2026, 14:32 (Asia/Tokyo)".
    /// </summary>
    public static string RecordedLabel(NotebookEntry e, string? timeZone)
    {
        var zone = FindZone(timeZone);
        var local = TimeZoneInfo.ConvertTimeFromUtc(e.RecordedAtUtc, zone);
        return $"{e.OnDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}, {local:HH:mm} ({(zone == TimeZoneInfo.Utc ? "UTC" : timeZone)})";
    }

    private static TimeZoneInfo FindZone(string? timeZone)
    {
        if (string.IsNullOrEmpty(timeZone)) return TimeZoneInfo.Utc;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZone); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    /// <summary>The number printed on a decision record, e.g. "NB-C001-0007".</summary>
    public static string RecordNumber(NotebookEntry e) =>
        $"NB-{e.ChapterId.Replace("-", "")}-{e.EntryId:0000}";

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
            question, answer, JsonSerializer.Serialize(sources), line, shareWithBoard,
            topic: TopicFor(question, line), parentId: null));
    }

    /// <summary>
    /// Adds a typed note (contacts, what worked, what's unfinished, a
    /// decision, or anything else), after checking it with Isa's sign-in
    /// check and Input Compliance gate. <paramref name="bearerToken"/> is the
    /// signed-in leader's token, as the chat sends it to the pipeline.
    /// </summary>
    /// <remarks>
    /// With <paramref name="parentId"/>, the note is a follow-up on an earlier
    /// entry in the same chapter's notebook (for example "the venue is now
    /// booked" under a decision). A follow-up takes its entry's topic and is
    /// shown under that entry, not as a line of its own in the index.
    /// </remarks>
    public async Task<NoteResult> AddNoteAsync(string personId, DateOnly onDate, string category, string text,
        bool shareWithBoard, string? bearerToken, long? parentId = null)
    {
        var access = GetAccess(personId, onDate);
        var note = Clean(text);
        if (!access.CanWrite) return NoteResult.Refused("You can't add to this notebook.");
        if (note is null) return NoteResult.Refused("Write a note first.");
        if (!Categories.Contains(category)) return NoteResult.Refused("Pick what kind of note this is.");

        string topic;
        if (parentId is long pid)
        {
            var parent = GetEntries(personId, onDate).FirstOrDefault(e => e.EntryId == pid && e.ParentId is null);
            if (parent is null) return NoteResult.Refused("That entry isn't in the notebook any more.");
            topic = parent.Topic;
        }
        else
        {
            topic = TopicFor(note);
        }

        var problem = await CheckAsync(note, "notebook_note", bearerToken,
            "This sounds like something to talk through with a person, so it wasn't saved. Your Chapter Experience contact can help.",
            "This note is outside what the notebook is for, so it wasn't saved. Notes are for running your chapter.");
        if (problem is not null) return NoteResult.Refused(problem);

        return NoteResult.Saved(Insert(access, personId, onDate, "note", category, null, null, null, note, shareWithBoard,
            topic, parentId));
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
        string? question, string? answer, string? sourcesJson, string? note, bool shared, string topic, long? parentId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notebook_entries(chapter_id, author_id, author_name, author_role, kind, category,
                                         question, answer, sources_json, note, shared, on_date, recorded_at,
                                         topic, shared_on, parent_id)
            VALUES($c, $p, $n, $r, $k, $cat, $q, $a, $s, $note, $sh, $d, $t, $topic, $sharedOn, $parent);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$topic", topic);
        cmd.Parameters.AddWithValue("$sharedOn", shared ? Day(onDate) : DBNull.Value);
        cmd.Parameters.AddWithValue("$parent", (object?)parentId ?? DBNull.Value);
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
        // shared_on records the day it was shared, for the decision record.
        cmd.CommandText = """
            UPDATE notebook_entries SET shared = $sh, shared_on = $on
            WHERE entry_id = $e AND author_id = $p AND removed_on IS NULL;
            """;
        cmd.Parameters.AddWithValue("$sh", share ? 1 : 0);
        cmd.Parameters.AddWithValue("$on", share ? Day(onDate) : DBNull.Value);
        cmd.Parameters.AddWithValue("$e", entryId);
        cmd.Parameters.AddWithValue("$p", personId);
        return cmd.ExecuteNonQuery() == 1;
    }

    /// <summary>
    /// The writer changes the topic of one of their own entries, while they
    /// can still write to the notebook. Its follow-ups move with it.
    /// </summary>
    public bool SetTopic(string personId, long entryId, string topic, DateOnly onDate)
    {
        if (!Topics.Contains(topic) || !GetAccess(personId, onDate).CanWrite) return false;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE notebook_entries SET topic = $t
            WHERE removed_on IS NULL
              AND (entry_id = $e AND author_id = $p
                   OR parent_id = $e AND EXISTS (SELECT 1 FROM notebook_entries WHERE entry_id = $e AND author_id = $p));
            """;
        cmd.Parameters.AddWithValue("$t", topic);
        cmd.Parameters.AddWithValue("$e", entryId);
        cmd.Parameters.AddWithValue("$p", personId);
        return cmd.ExecuteNonQuery() > 0;
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

    /// <summary>
    /// "Email this record". The leader chooses to send one decision record,
    /// as a Board copy (without their own question) or My copy (everything).
    /// In the prototype nothing is emailed: the request is saved in Isa,
    /// like the introduction on the contact step. An optional message is new
    /// typed text, so it is checked like a note first. Only presidents may
    /// send My copy, and a Board copy only exists for a shared entry.
    /// </summary>
    public async Task<NoteResult> SaveEmailRequestAsync(string personId, DateOnly onDate, long entryId, string to,
        string copy, string? message, string? bearerToken)
    {
        var access = GetAccess(personId, onDate);
        var entry = GetEntries(personId, onDate).FirstOrDefault(e => e.EntryId == entryId && e.ParentId is null);
        var address = Clean(to);
        if (entry is null) return NoteResult.Refused("That entry isn't in the notebook any more.");
        if (copy == "mine" && !access.SeesAll) return NoteResult.Refused("Only a president can send the full record.");
        if (copy == "board" && !entry.Shared) return NoteResult.Refused("This entry isn't shared with the board, so it has no Board copy.");
        if (copy is not ("board" or "mine")) return NoteResult.Refused("Choose Board copy or My copy.");
        if (address is null || !System.Text.RegularExpressions.Regex.IsMatch(address, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return NoteResult.Refused("Enter an email address, like name@example.org.");

        var text = Clean(message);
        if (text is not null)
        {
            var problem = await CheckAsync(text, "notebook_email", bearerToken,
                "This sounds like something to talk through with a person, so nothing was saved. Your Chapter Experience contact can help.",
                "That message is outside what the notebook is for, so nothing was saved. Try a short note about the record.");
            if (problem is not null) return NoteResult.Refused(problem);
        }

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO notebook_emails(entry_id, sender_id, to_address, copy, message, on_date, recorded_at)
            VALUES($e, $p, $to, $copy, $m, $d, $t);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$e", entryId);
        cmd.Parameters.AddWithValue("$p", personId);
        cmd.Parameters.AddWithValue("$to", address);
        cmd.Parameters.AddWithValue("$copy", copy);
        cmd.Parameters.AddWithValue("$m", (object?)text ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$d", Day(onDate));
        cmd.Parameters.AddWithValue("$t", Now());
        return NoteResult.Saved((long)cmd.ExecuteScalar()!);
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
/// <param name="TimeZone">Their time zone from the directory, e.g. "Asia/Tokyo", for showing when entries were recorded.</param>
public record NotebookAccess(string? ChapterId, string? ChapterName, string? PersonName, string? RoleLabel,
    bool CanWrite, bool SeesAll, DateOnly? HandoffEnds = null, string? TimeZone = null)
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
    DateOnly OnDate,
    string Topic,           // Elections, Finances, Meetings, Compliance, Membership, Events or Other
    DateOnly? SharedOn,     // the day it was shared with the board, if it is
    long? ParentId,         // for a follow-up: the entry it follows up on
    DateTime RecordedAtUtc) // the real time it was saved
{
    /// <summary>
    /// How the notebook names this entry: a saved answer with a "what we
    /// decided" line counts as a decision, like a typed decision note.
    /// </summary>
    public string KindLabel => Kind == "saved_answer"
        ? (string.IsNullOrEmpty(Note) ? "Saved answer" : "Decision")
        : Category switch
        {
            "decision" => "Decision",
            "contact" => "Contact",
            "worked" => "What worked",
            "unfinished" => "Unfinished",
            _ => "Other",
        };

    /// <summary>The line the index shows: the decision or note, or the question for a saved answer without one.</summary>
    public string Title => !string.IsNullOrEmpty(Note) ? Note! : Question ?? Answer ?? "";
}
