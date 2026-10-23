using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Orchestrator.Stages;

/// <summary>Placeholder stages for stages owned by others (T11a / T11c).</summary>
public class PassThroughStage : IStage
{
    public string Name { get; }
    public PassThroughStage(string name) => Name = name;

    public Task<StageResult> RunAsync(QueryContext ctx)
        => Task.FromResult(new StageResult(StageOutcome.Pass, null, "placeholder"));
}

/// <summary>
/// Stage 5. Full-text search only (contract section 1). Found = a direct hit. Nothing found = NotFound
/// (handled, not a Stop and not an error), and the run goes on to the miss path. The meaning-map
/// stand-in now lives in the rag stage, not here. A real database problem is NOT hidden: it
/// throws, and the orchestrator turns it into a plain "something went wrong" stop.
/// Sets ctx.SearchScore (0 to 1) from the best match for the Content Health dashboard.
/// </summary>
public class DatabaseSearchStage : IStage
{
    public string Name => "database_search";
    private readonly string _contentDbPath;

    public DatabaseSearchStage(IConfiguration config)
    {
        _contentDbPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
    }

    private static readonly HashSet<string> FtsStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","how","many","what","does","must","each","year","have","our","and","who","are","for",
        "when","can","with","that","this","you","is","do","we","to","a","of","in","chapter","hold","policy","period","annual"
    };

    // Raw question text breaks FTS5 syntax ("?" etc.), so keep letters/digits, drop filler, and AND the rest.
    // Each term is quoted so a typed word like AND, OR or NOT is searched as a word, not read as an operator.
    private static string? BuildFtsQuery(string question)
    {
        var terms = System.Text.RegularExpressions.Regex.Matches(question ?? "", "[A-Za-z0-9]+")
            .Select(m => m.Value)
            .Where(w => !FtsStopWords.Contains(w))
            .ToList();
        return terms.Count == 0 ? null : string.Join(" AND ", terms.Select(t => "\"" + t + "\""));
    }

    public async Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (!File.Exists(_contentDbPath))
            return new StageResult(StageOutcome.NotFound, "no content db", "no content db yet; hit=no; passages=0");

        var ftsQuery = BuildFtsQuery(ctx.Question);
        if (ftsQuery == null)
            return new StageResult(StageOutcome.NotFound, "no match", "hit=no; passages=0; note=no searchable terms");

        await using var conn = new SqliteConnection($"Data Source={_contentDbPath};Mode=ReadOnly");
        await conn.OpenAsync();

        var passages = new List<Passage>();
        double? best = null;
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT document, section, page, text, rank
                FROM passages
                WHERE passages MATCH $q
                ORDER BY rank
                LIMIT 5;
                """;
            cmd.Parameters.AddWithValue("$q", ftsQuery);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                passages.Add(new Passage(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
                if (best is null)
                {
                    // FTS5 rank is negative and more negative = better. Squash it into 0..1.
                    var x = Math.Max(0, -reader.GetDouble(4));
                    best = x / (1 + x);
                }
            }
        }

        if (passages.Count == 0)
            return new StageResult(StageOutcome.NotFound, "no match", "hit=no; passages=0");

        ctx.Passages = passages;
        ctx.DirectHit = true;
        ctx.SearchScore = best;
        ctx.Sources = passages.Select(p => new SourceRef(p.Document, p.Section, p.Page)).ToList();
        ctx.CitedSource = ctx.Sources[0];   // the passage the answer comes from (contract: cited_source)
        return new StageResult(StageOutcome.Pass, null, $"direct_hit; count={passages.Count}; hit=yes; passages={passages.Count}");
    }
}

/// <summary>T11b — Knowledge Graph stub (a stub inside the RAG step). Skipped on a direct hit.</summary>
public class KnowledgeGraphStage : IStage
{
    public string Name => "knowledge_graph";

    public Task<StageResult> RunAsync(QueryContext ctx)
        => Task.FromResult(ctx.DirectHit
            ? new StageResult(StageOutcome.Pass, null, "direct hit", Skipped: true)
            : new StageResult(StageOutcome.Pass, null, "related=[]"));
}

/// <summary>
/// Stage 7. RAG STUB (contract section 1): on a miss, hands fixed stand-in passages to the AI step.
/// It searches nothing real. The stand-in is the meaning map in content.db (a question mapped to a
/// real passage), moved here from database_search (Pipeline Built). No passages = Escalate, which
/// skips complex_ai and goes to ticketing. Skipped on a direct hit.
/// </summary>
public class RagStage : IStage
{
    public string Name => "rag";
    private readonly string _contentDbPath;

    public RagStage(IConfiguration config)
    {
        _contentDbPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
    }

    public async Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (ctx.DirectHit)
        {
            if (ctx.Passages.Count > 0 && string.IsNullOrEmpty(ctx.AnswerText))
                AnswerExtract.Apply(ctx);   // best-matching sentence, its source first
            return new StageResult(StageOutcome.Pass, null, "direct hit", Skipped: true);
        }

        if (ctx.Passages.Count == 0)
            ctx.Passages = await StandInAsync(ctx.Question);

        if (ctx.Passages.Count == 0)
            return new StageResult(StageOutcome.Escalate, "no approved content found", "no_context; passages=0; retrieved=none");

        ctx.Sources = ctx.Passages.Select(p => new SourceRef(p.Document, p.Section, p.Page)).Distinct().ToList();
        var retrieved = string.Join(", ", ctx.Passages.Select(p => $"{p.Document} {p.Section}".Replace(';', ',')).Distinct());
        return new StageResult(StageOutcome.Pass, null,
            $"passages={ctx.Passages.Count}; retrieved={retrieved}; stub=meaning_map");
    }

    private static string Normalize(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value ?? "", "[^a-z0-9]+", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim().ToLowerInvariant();

    // The meaning map: a plain-language question mapped to the passage that answers it.
    private async Task<List<Passage>> StandInAsync(string question)
    {
        var found = new List<Passage>();
        if (!File.Exists(_contentDbPath)) return found;
        try
        {
            await using var conn = new SqliteConnection($"Data Source={_contentDbPath};Mode=ReadOnly");
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT m.question, p.document, p.section, p.page, p.text
                FROM meaning_map m
                JOIN passages p ON p.id = m.passage_id;
                """;
            var wanted = Normalize(question);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                if (Normalize(reader.GetString(0)) == wanted)
                    found.Add(new Passage(reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        }
        catch (SqliteException)
        {
            // No meaning map in this database: a normal miss. rag escalates.
        }
        return found.DistinctBy(p => (p.Document, p.Section, p.Page)).Take(5).ToList();
    }
}

/// <summary>Stub translation — pass-through (detects the language, does not translate yet).</summary>
public class TranslationStage : IStage
{
    public string Name => "translation";
    public Task<StageResult> RunAsync(QueryContext ctx)
        => Task.FromResult(ctx.Language == "en"
            // English needs no translation: the contract's "skipped, english" (shown gray in developer view).
            ? new StageResult(StageOutcome.Pass, null, "english", Skipped: true)
            // Any other language (or "und"): still a stub that passes the text through unchanged.
            : new StageResult(StageOutcome.Pass, null, $"from={ctx.Language}"));
}

/// <summary>
/// Stub input_compliance — stops pizza / jailbreak; escalates harassment.
/// Stub rules for the four test paths; a real rules check is still to come.
/// </summary>
public class InputComplianceStage : IStage
{
    public string Name => "input_compliance";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        var q = ctx.Question.ToLowerInvariant();

        if (q.Contains("pizza") || q.Contains("ignore your rules") || q.Contains("show me every document"))
        {
            return Task.FromResult(new StageResult(
                StageOutcome.Stop,
                IsaChatApp.Services.ShowTheWork.Wording.OutsideScope,
                "out_of_scope"
            ));
        }

        if (q.Contains("harass") || q.Contains("harassment"))
        {
            ctx.SafetyTopic = true;   // a safety topic: the hand-off adds the emergency-services line
            return Task.FromResult(new StageResult(
                StageOutcome.Escalate,
                "Sensitive topic — escalating to a person",
                "sensitive"
            ));
        }

        return Task.FromResult(new StageResult(StageOutcome.Pass, null, "ok"));
    }
}
