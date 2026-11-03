using System.Collections.Concurrent;
using System.Text.Json;
using IsaChatApp.Services.Orchestrator;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services;

/// <summary>
/// Paths to a person (docs/Paths_To_A_Person.md, Matthew, November 2, 2026):
/// a hand-off the leader asks for themselves, from the "connect me" card
/// behind three doors ("Talk to a person", "Not quite", and later the
/// "answer, then point" button).
///
/// No side door around Isa's pipeline:
/// - The leader's optional note goes through the sign-in check and the Input
///   Compliance gate (Orchestrator.CheckTextAsync, purpose=handoff_note),
///   logged like every other check. With no note, the sign-in check still runs.
/// - The ticket is made by the pipeline's own TicketingStage (reference number,
///   Salesforce Case when switched on), unchanged; this service only calls it.
/// - A note on a safety topic gets the urgent safety handling and the
///   emergency-services line, as for any safety hand-off.
///
/// What the leader chose to share (the question, Isa's answer, its sources,
/// earlier questions, the note) is kept in data/person_requests.db for the
/// Chapter Experience team, because the Salesforce Case doesn't carry the
/// text yet. Only what the leader ticked is stored. The event log records
/// the door and counts, never the text. One hand-off per answer.
/// </summary>
public class PersonRequestService
{
    /// <summary>The doors, as logged and counted (by door, never by person).</summary>
    public static class Doors
    {
        public const string TalkToPerson = "talk_to_person";
        public const string NotQuite = "not_quite";
        public const string AnswerThenPoint = "answer_then_point";
    }

    private readonly Orchestrator.Orchestrator _pipeline;
    private readonly IStage _ticketing;
    private readonly EventLogService _eventLog;
    private readonly string _dbPath;

    // Answers already passed on in this run of Isa (answer trace ID -> reference),
    // so a second click shows the same reference instead of a second ticket.
    private readonly ConcurrentDictionary<string, string> _byAnswer = new();

    public PersonRequestService(Orchestrator.Orchestrator pipeline, IEnumerable<IStage> stages,
        EventLogService eventLog, IConfiguration config)
    {
        _pipeline = pipeline;
        _ticketing = stages.Single(s => s.Name == "ticketing");
        _eventLog = eventLog;
        _dbPath = config["PersonRequestsDb:Path"] ?? Path.Combine("data", "person_requests.db");
        var folder = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        EnsureTable();
    }

    /// <summary>What the leader chose to send. Null or empty means "not shared".</summary>
    public record Request(
        string Door,
        string? FromTraceId,
        string? Question,
        string? Answer,
        IReadOnlyList<SourceRef>? Sources,
        IReadOnlyList<string>? EarlierQuestions,
        string? Note);

    /// <summary>The outcome: Sent (with the reference and the message for the chat), or Refused (with why).</summary>
    public record Result(bool Sent, string? Ticket, string Message, bool AlreadySent = false);

    /// <summary>The reference for an answer that was already passed on, or null.</summary>
    public string? ExistingTicket(string? fromTraceId) =>
        fromTraceId is not null && _byAnswer.TryGetValue(fromTraceId, out var t) ? t : null;

    public async Task<Result> SendAsync(Request request, string? bearerToken)
    {
        if (ExistingTicket(request.FromTraceId) is { } existing)
            return new Result(true, existing, "", AlreadySent: true);

        var note = request.Note?.Trim() ?? "";
        var ctx = new QueryContext
        {
            TraceId = Guid.NewGuid().ToString(),
            Question = note,
            IncomingToken = bearerToken,
            Language = "en",
        };

        // The sign-in check and Input Compliance on the note (an empty note
        // only needs the sign-in check to pass).
        var check = await _pipeline.CheckTextAsync(ctx, "handoff_note");
        if (ctx.AuthFailed)
            return new Result(false, null, ShowTheWork.Wording.NotSignedIn);
        if (check.Outcome == StageOutcome.Stop)
            return new Result(false, null, check.DataText.StartsWith("error=")
                ? "Something went wrong on our side, so nothing was sent. Please try again."
                : "Your note is outside what the Chapter Experience team can help with, so nothing was sent. Please edit it and try again.");

        // An Escalate here means the note itself needs a person (for example a
        // safety topic): that's exactly where it's going, so carry on, keeping
        // ctx.SafetyTopic for the urgent handling and the safety line.
        ctx.EscalatedBy = "leader_request";
        ctx.EscalationReason = request.Door;
        var ticketing = await _ticketing.RunAsync(ctx);
        var ticket = ctx.TicketReference ?? "";
        await _eventLog.WriteAsync(ctx.TraceId, "ticketing",
            $"result={ticketing.Outcome.Tag()}; reason=leader_request; door={request.Door}; " +
            $"shared_question={(string.IsNullOrWhiteSpace(request.Question) ? "no" : "yes")}; " +
            $"earlier={request.EarlierQuestions?.Count ?? 0}; note_chars={note.Length}; " +
            $"urgent={(ctx.SafetyTopic ? "yes" : "no")}; from_trace={request.FromTraceId ?? "none"}; {ticketing.DataText}");

        Store(ctx, request, note, ticket);
        if (request.FromTraceId is not null) _byAnswer[request.FromTraceId] = ticket;

        return new Result(true, ticket, ShowTheWork.Wording.PersonRequestSent(ticket, ctx.SafetyTopic));
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    private void EnsureTable()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS person_requests (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                ticket        TEXT NOT NULL,
                trace_id      TEXT NOT NULL,
                from_trace_id TEXT,
                door          TEXT NOT NULL,
                person_id     TEXT,
                person_name   TEXT,
                chapter_id    TEXT,
                chapter_role  TEXT,
                question      TEXT,
                answer        TEXT,
                sources_json  TEXT,
                earlier_json  TEXT,
                note          TEXT,
                urgent        INTEGER NOT NULL,
                created_at    TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    // Only what the leader chose to share is written.
    private void Store(QueryContext ctx, Request request, string note, string ticket)
    {
        string? P(string key) => ctx.Profile.TryGetValue(key, out var v) ? v : null;
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO person_requests(ticket, trace_id, from_trace_id, door, person_id, person_name, chapter_id,
                                        chapter_role, question, answer, sources_json, earlier_json, note, urgent, created_at)
            VALUES($t, $tr, $from, $door, $p, $n, $c, $r, $q, $a, $s, $e, $note, $u, $at);
            """;
        cmd.Parameters.AddWithValue("$t", ticket);
        cmd.Parameters.AddWithValue("$tr", ctx.TraceId);
        cmd.Parameters.AddWithValue("$from", (object?)request.FromTraceId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$door", request.Door);
        cmd.Parameters.AddWithValue("$p", (object?)P("person_id") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$n", (object?)P("display_name") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$c", (object?)P("chapter_id") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$r", (object?)P("chapter_role") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$q", string.IsNullOrWhiteSpace(request.Question) ? DBNull.Value : request.Question);
        cmd.Parameters.AddWithValue("$a", string.IsNullOrWhiteSpace(request.Answer) ? DBNull.Value : request.Answer);
        cmd.Parameters.AddWithValue("$s", request.Sources is { Count: > 0 } ? JsonSerializer.Serialize(request.Sources) : DBNull.Value);
        cmd.Parameters.AddWithValue("$e", request.EarlierQuestions is { Count: > 0 } ? JsonSerializer.Serialize(request.EarlierQuestions) : DBNull.Value);
        cmd.Parameters.AddWithValue("$note", note.Length == 0 ? DBNull.Value : note);
        cmd.Parameters.AddWithValue("$u", ctx.SafetyTopic ? 1 : 0);
        cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("o"));
        cmd.ExecuteNonQuery();
    }
}
