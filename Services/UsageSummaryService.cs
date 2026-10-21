using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IsaChatApp.Services.Orchestrator;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services;

/// <summary>
/// Content Health Dashboard, step 1: one summary row per question, so every
/// dashboard number is a simple query on one table instead of parsing event
/// text. Added 2026-10-21.
///
/// Lives in its own file (data/usage.db), apart from the event log, so
/// clearing one never touches the other. Only the Orchestrator writes
/// request_summary (at the end of a run); the chat writes feedback.
///
/// Privacy rules (same spirit as the event log and the onboarding design):
///  - The question text is NEVER stored (the event log already promises this).
///  - The person is stored only as a salted hash, so "repeat escalation" and
///    "many different users" can be counted, but no row names anyone.
///    The dashboard must show totals and rates only, never one person's rows.
///  - Writing here must never break an answer: every failure is swallowed.
///
/// Fields that don't exist yet are left empty (NULL), not invented:
///  tokens (no real model yet), region (the directory has no region field).
/// </summary>
public class UsageSummaryService
{
    private readonly string _dbPath;
    private readonly string _contentDbPath;
    private readonly string _salt;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, string> _categoryByDocument = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<UsageSummaryService> _log;

    public UsageSummaryService(IConfiguration config, ILogger<UsageSummaryService> log)
    {
        _log = log;
        _dbPath = config["Usage:Path"] ?? Path.Combine("data", "usage.db");
        _contentDbPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
        _salt = config["Usage:HashSalt"] ?? "isa-dev-salt-change-me";
        var dir = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        try { EnsureSchema(); } catch (Exception ex) { _log.LogWarning(ex, "usage.db schema failed"); }
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var p = conn.CreateCommand();
        p.CommandText = "PRAGMA journal_mode=WAL;";
        p.ExecuteNonQuery();
        return conn;
    }

    private void EnsureSchema()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS request_summary (
                trace_id          TEXT PRIMARY KEY,
                timestamp_utc     TEXT NOT NULL,
                user_hash         TEXT,
                chapter_id        TEXT,
                region            TEXT,            -- from a "region" claim when the directory has one
                chapter_role      TEXT,
                years_as_leader   INTEGER,
                term_start        TEXT,            -- lets the dashboard work out tenure
                language          TEXT,
                outcome           TEXT NOT NULL,   -- answered | stopped | escalated
                route             TEXT NOT NULL,   -- direct | ai | none | blocked
                stopped_by        TEXT,            -- stage that stopped or escalated
                reason            TEXT,
                retrieved_count   INTEGER NOT NULL DEFAULT 0,   -- 0 = zero-match
                cited             TEXT,            -- the passage the answer came from
                category          TEXT,            -- from data/content_categories.csv
                question_hash     TEXT,            -- salted hash of the normalized question (never the text)
                search_score      REAL,            -- hook: database_search best score
                confidence        REAL,
                prompt_tokens     INTEGER,         -- hook: real model
                completion_tokens INTEGER,         -- hook: real model
                total_ms          REAL,
                ticket_reference  TEXT,
                ticket_closed_utc TEXT             -- hook: ticketing (time-to-resolution)
            );
            CREATE INDEX IF NOT EXISTS idx_rs_time ON request_summary(timestamp_utc);
            CREATE INDEX IF NOT EXISTS idx_rs_user ON request_summary(user_hash);

            -- One row per passage found for a question (rank 1 = best). Powers
            -- most/least-retrieved: least-retrieved = passages in content.db with no row here.
            CREATE TABLE IF NOT EXISTS retrieval (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                trace_id      TEXT NOT NULL,
                timestamp_utc TEXT NOT NULL,
                document      TEXT NOT NULL,
                section       TEXT NOT NULL,
                page          TEXT NOT NULL,
                category      TEXT,
                rank          INTEGER NOT NULL,
                cited         INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS idx_ret_trace ON retrieval(trace_id);
            CREATE INDEX IF NOT EXISTS idx_ret_doc ON retrieval(document, section, page);

            CREATE TABLE IF NOT EXISTS feedback (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                trace_id      TEXT NOT NULL,
                timestamp_utc TEXT NOT NULL,
                kind          TEXT NOT NULL        -- resolved | not_quite | talk_to_person
            );
            CREATE INDEX IF NOT EXISTS idx_fb_trace ON feedback(trace_id);

            -- Hook: pairs of passages that give different answers. Filled by hand today
            -- (FlagConflictAsync); the NLI recheck can fill it later.
            CREATE TABLE IF NOT EXISTS content_conflicts (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp_utc TEXT NOT NULL,
                passage_a     TEXT NOT NULL,       -- "Document|Section|Page"
                passage_b     TEXT NOT NULL,
                note          TEXT,
                flagged_by    TEXT NOT NULL,       -- manual | nli
                resolved_utc  TEXT
            );
            """;
        cmd.ExecuteNonQuery();

        // Older usage.db files (from the first version) get the new columns added.
        foreach (var (col, type) in new[] { ("term_start","TEXT"), ("question_hash","TEXT"), ("search_score","REAL"), ("ticket_closed_utc","TEXT") })
        {
            using var chk = conn.CreateCommand();
            chk.CommandText = "SELECT COUNT(*) FROM pragma_table_info('request_summary') WHERE name=$n";
            chk.Parameters.AddWithValue("$n", col);
            if ((long)chk.ExecuteScalar()! == 0)
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = $"ALTER TABLE request_summary ADD COLUMN {col} {type}";
                alter.ExecuteNonQuery();
            }
        }

        // after the migration, so an older usage.db has the column before it is indexed
        using var idx = conn.CreateCommand();
        idx.CommandText = "CREATE INDEX IF NOT EXISTS idx_rs_qhash ON request_summary(question_hash);";
        idx.ExecuteNonQuery();
    }

    /// <summary>
    /// Salted hash of the question with case, spacing and punctuation ignored, so the
    /// dashboard can count how often the SAME question comes back (and from how many
    /// different people) without ever storing the question. Exact repeats only; real
    /// clustering by meaning waits on the embeddings. Not reversible without the salt.
    /// </summary>
    public string QuestionHash(string question)
    {
        var norm = Regex.Replace(question.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_salt + "|q|" + norm));
        return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
    }

    /// <summary>Stable, anonymous id: same person -> same hash, but not reversible without the salt.</summary>
    public string Hash(string personId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(_salt + "|" + personId));
        return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
    }

    /// <summary>
    /// The dashboard category of a document (Governance, Operations, Programs, Products).
    /// Read from data/content_categories.csv (document,category), an editable list kept
    /// next to the content database. A document not in the list falls back to its folder
    /// name, so nothing is ever blank. Added 2026-10-21.
    /// </summary>
    public string? CategoryOf(string document)
    {
        lock (_categoryByDocument)
        {
            if (_categoryByDocument.Count == 0)
            {
                // 1. folders from the content database (fallback)
                try
                {
                    using var c = new SqliteConnection($"Data Source={_contentDbPath};Mode=ReadOnly");
                    c.Open();
                    using var cmd = c.CreateCommand();
                    cmd.CommandText = "SELECT title, folder FROM documents";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        if (!r.IsDBNull(0) && !r.IsDBNull(1)) _categoryByDocument[r.GetString(0)] = r.GetString(1);
                }
                catch { /* no content db yet */ }

                // 2. the editable category list wins over the folder
                try
                {
                    var csv = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(_contentDbPath)) ?? "data", "content_categories.csv");
                    if (File.Exists(csv))
                        foreach (var line in File.ReadLines(csv).Skip(1))
                        {
                            var i = line.LastIndexOf(',');
                            if (i <= 0) continue;
                            var doc = line[..i].Trim().Trim('"');
                            var cat = line[(i + 1)..].Trim().Trim('"');
                            if (doc.Length > 0 && cat.Length > 0) _categoryByDocument[doc] = cat;
                        }
                }
                catch { /* keep the folder fallback */ }
            }
            return _categoryByDocument.TryGetValue(document, out var f) ? f : null;
        }
    }

    private static string Key(string doc, string section, string page) => $"{doc}|{section}|{page}";

    /// <summary>Called by the Orchestrator once a run is over. Never throws.</summary>
    public async Task RecordRunAsync(QueryContext ctx, string status, double totalMs, string? originalQuestion = null)
    {
        try
        {
            ctx.Profile.TryGetValue("chapter_id", out var chapter);
            ctx.Profile.TryGetValue("chapter_role", out var role);
            ctx.Profile.TryGetValue("region", out var region);
            ctx.Profile.TryGetValue("term_start", out var termStart);
            int? years = ctx.Profile.TryGetValue("years_as_leader", out var y) && int.TryParse(y, out var yi) ? yi : null;
            var person = ctx.UserId ?? (ctx.Profile.TryGetValue("person_id", out var pid) ? pid : null);

            var accessStage = ctx.WorkSteps.FirstOrDefault(s => s.Stage == "access_control");
            var aiRan = ctx.WorkSteps.Any(s => s.Stage == "complex_ai" && !s.Skipped);
            var searchRan = ctx.WorkSteps.Any(s => s.Stage == "database_search" && !s.Skipped);
            // route: how the answer (or non-answer) came about
            var route = ctx.AuthFailed || ctx.AccessDenied || !searchRan ? "blocked"
                      : ctx.DirectHit ? "direct"
                      : aiRan && ctx.Passages.Count > 0 ? "ai"
                      : "none";

            string? stoppedBy = status == "escalated" ? ctx.EscalatedBy
                              : status == "stopped" ? ctx.WorkSteps.LastOrDefault(s => s.Outcome == "stop")?.Stage
                              : null;
            var reason = status == "escalated" ? ctx.EscalationReason : status == "stopped" ? ctx.StopReason : null;

            double? confidence = null;
            var outStep = ctx.WorkSteps.FirstOrDefault(s => s.Stage == "output_compliance" && !s.Skipped);
            if (outStep is not null)
            {
                var m = Regex.Match(outStep.DataText, @"confidence=([0-9.]+)");
                if (m.Success && double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var cv)) confidence = cv;
            }

            var cited = ctx.CitedSource is { } cs ? Key(cs.Document, cs.Section, cs.Page) : null;
            var catDoc = ctx.CitedSource?.Document ?? ctx.Passages.FirstOrDefault()?.Document;
            var category = catDoc is null ? null : CategoryOf(catDoc);
            var qhash = string.IsNullOrWhiteSpace(originalQuestion) ? null : QuestionHash(originalQuestion);
            var now = DateTime.UtcNow.ToString("O");

            await _lock.WaitAsync();
            try
            {
                await using var conn = Open();
                await using var tx = conn.BeginTransaction();
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = """
                        INSERT OR REPLACE INTO request_summary
                        (trace_id, timestamp_utc, user_hash, chapter_id, region, chapter_role, years_as_leader, term_start,
                         language, outcome, route, stopped_by, reason, retrieved_count, cited, category, question_hash,
                         search_score, confidence, prompt_tokens, completion_tokens, total_ms, ticket_reference)
                        VALUES ($t,$ts,$u,$ch,$reg,$role,$yrs,$tstart,$lang,$out,$route,$by,$why,$retn,$cited,$cat,$qh,
                                $ss,$conf,$ptok,$ctok,$ms,$ticket);
                        """;
                    void P(string n, object? v) => cmd.Parameters.AddWithValue(n, v ?? DBNull.Value);
                    P("$t", ctx.TraceId); P("$ts", now);
                    P("$u", person is null ? null : Hash(person)); P("$ch", chapter); P("$reg", region); P("$role", role);
                    P("$yrs", years); P("$tstart", termStart);
                    P("$lang", ctx.Language); P("$out", status); P("$route", route); P("$by", stoppedBy); P("$why", reason);
                    P("$retn", ctx.Passages.Count); P("$cited", cited); P("$cat", category); P("$qh", qhash);
                    P("$ss", ctx.SearchScore); P("$conf", confidence);
                    P("$ptok", ctx.PromptTokens); P("$ctok", ctx.CompletionTokens);
                    P("$ms", totalMs); P("$ticket", ctx.TicketReference);
                    await cmd.ExecuteNonQueryAsync();
                }

                // one row per passage found
                var rank = 0;
                foreach (var p in ctx.Passages)
                {
                    rank++;
                    await using var r = conn.CreateCommand();
                    r.Transaction = tx;
                    r.CommandText = """
                        INSERT INTO retrieval (trace_id, timestamp_utc, document, section, page, category, rank, cited)
                        VALUES ($t,$ts,$d,$s,$p,$c,$k,$ci);
                        """;
                    r.Parameters.AddWithValue("$t", ctx.TraceId);
                    r.Parameters.AddWithValue("$ts", now);
                    r.Parameters.AddWithValue("$d", p.Document);
                    r.Parameters.AddWithValue("$s", p.Section);
                    r.Parameters.AddWithValue("$p", p.Page);
                    r.Parameters.AddWithValue("$c", (object?)CategoryOf(p.Document) ?? DBNull.Value);
                    r.Parameters.AddWithValue("$k", rank);
                    r.Parameters.AddWithValue("$ci", cited is not null && Key(p.Document, p.Section, p.Page) == cited ? 1 : 0);
                    await r.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
            }
            finally { _lock.Release(); }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not write request_summary for {Trace}", ctx.TraceId);
        }
    }

    /// <summary>One feedback click: "resolved" (Yes, this answered my question), "not_quite", or "talk_to_person". Never throws.</summary>
    public async Task RecordFeedbackAsync(string traceId, string kind)
    {
        if (string.IsNullOrWhiteSpace(traceId) || kind is not ("resolved" or "not_quite" or "talk_to_person")) return;
        try
        {
            await _lock.WaitAsync();
            try
            {
                await using var conn = Open();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO feedback (trace_id, timestamp_utc, kind) VALUES ($t,$ts,$k);";
                cmd.Parameters.AddWithValue("$t", traceId);
                cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("O"));
                cmd.Parameters.AddWithValue("$k", kind);
                await cmd.ExecuteNonQueryAsync();
            }
            finally { _lock.Release(); }
        }
        catch (Exception ex) { _log.LogWarning(ex, "Could not write feedback for {Trace}", traceId); }
    }

    // ---------- Hooks for later (nothing calls these yet) ----------

    /// <summary>Hook for ticketing: call when a ticket is closed, so time-to-resolution can be shown.</summary>
    public async Task CloseTicketAsync(string ticketReference)
    {
        try
        {
            await _lock.WaitAsync();
            try
            {
                await using var conn = Open();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE request_summary SET ticket_closed_utc=$ts WHERE ticket_reference=$r AND ticket_closed_utc IS NULL;";
                cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("O"));
                cmd.Parameters.AddWithValue("$r", ticketReference);
                await cmd.ExecuteNonQueryAsync();
            }
            finally { _lock.Release(); }
        }
        catch (Exception ex) { _log.LogWarning(ex, "Could not close ticket {Ref}", ticketReference); }
    }

    /// <summary>Hook for conflicting-answer flags: a person (flaggedBy "manual") or the NLI recheck ("nli") marks two passages that disagree.</summary>
    public async Task FlagConflictAsync(string passageA, string passageB, string? note, string flaggedBy = "manual")
    {
        try
        {
            await _lock.WaitAsync();
            try
            {
                await using var conn = Open();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO content_conflicts (timestamp_utc, passage_a, passage_b, note, flagged_by) VALUES ($ts,$a,$b,$n,$f);";
                cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("O"));
                cmd.Parameters.AddWithValue("$a", passageA);
                cmd.Parameters.AddWithValue("$b", passageB);
                cmd.Parameters.AddWithValue("$n", (object?)note ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$f", flaggedBy);
                await cmd.ExecuteNonQueryAsync();
            }
            finally { _lock.Release(); }
        }
        catch (Exception ex) { _log.LogWarning(ex, "Could not flag conflict"); }
    }
}
