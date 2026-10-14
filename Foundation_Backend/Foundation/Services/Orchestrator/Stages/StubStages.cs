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

/// <summary>T11b — Database Search stub. FTS on content DB + meaning-map fallback.</summary>
public class DatabaseSearchStage : IStage
{
    public string Name => "database_search";
    private readonly string _contentDbPath;
    private readonly IConfiguration _config;

    public DatabaseSearchStage(IConfiguration config)
    {
        _config = config;
        _contentDbPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
    }

    public async Task<StageResult> RunAsync(QueryContext ctx)
    {
        // Open read-only
        if (!File.Exists(_contentDbPath))
        {
            return new StageResult(StageOutcome.Pass, null, "no content db yet");
        }

        await using var conn = new SqliteConnection($"Data Source={_contentDbPath};Mode=ReadOnly");
        await conn.OpenAsync();

        // Try FTS if a passages/content table exists (A'keyrah T8 will define schema)
        var passages = new List<Passage>();
        try
        {
            await using var cmd = conn.CreateCommand();
            // Adjust table/column names to match A'keyrah's T8 schema when ready
            cmd.CommandText = """
                SELECT document, section, page, text
                FROM passages
                WHERE text MATCH $q
                LIMIT 5;
                """;
            cmd.Parameters.AddWithValue("$q", ctx.Question);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                passages.Add(new Passage(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)
                ));
            }
        }
        catch
        {
            // Table may not exist yet — fall through to meaning map
        }

        if (passages.Count > 0)
        {
            ctx.Passages = passages;
            ctx.DirectHit = true;
            ctx.Sources = passages.Select(p => new SourceRef(p.Document, p.Section, p.Page)).ToList();
            return new StageResult(StageOutcome.Pass, null, $"direct_hit; count={passages.Count}");
        }

        // Meaning-map fallback (A'keyrah T7/T8) — stand-in for vector search
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT p.document, p.section, p.page, p.text
                FROM meaning_map m
                JOIN passages p ON p.id = m.passage_id
                WHERE lower(m.question) = lower($q)
                LIMIT 5;
                """;
            cmd.Parameters.AddWithValue("$q", ctx.Question);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                passages.Add(new Passage(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)
                ));
            }
        }
        catch
        {
            // meaning_map may not exist yet
        }

        if (passages.Count > 0)
        {
            ctx.Passages = passages;
            ctx.DirectHit = false;
            ctx.Sources = passages.Select(p => new SourceRef(p.Document, p.Section, p.Page)).ToList();
            return new StageResult(StageOutcome.Pass, null, $"meaning_map; count={passages.Count}");
        }

        return new StageResult(StageOutcome.Pass, null, "no_passages");
    }
}

/// <summary>T11b — Knowledge Graph stub. Returns empty related list for Foundation.</summary>
public class KnowledgeGraphStage : IStage
{
    public string Name => "knowledge_graph";

    public Task<StageResult> RunAsync(QueryContext ctx)
        => Task.FromResult(new StageResult(StageOutcome.Pass, null, "related=[]"));
}

/// <summary>T11b — RAG stub. Joins passages into context. Skips on direct hit.</summary>
public class RagStage : IStage
{
    public string Name => "rag";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (ctx.DirectHit)
        {
            // On direct hit, AI stages may be skipped
            if (ctx.Passages.Count > 0 && string.IsNullOrEmpty(ctx.AnswerText))
            {
                var top = ctx.Passages[0];
                ctx.AnswerText = top.Text;
            }
            return Task.FromResult(new StageResult(StageOutcome.Pass, null, "skipped direct_hit", Skipped: true));
        }

        if (ctx.Passages.Count == 0)
        {
            return Task.FromResult(new StageResult(StageOutcome.Pass, null, "no_context"));
        }

        var contextText = string.Join("\n\n",
            ctx.Passages.Select(p => $"[{p.Document} / {p.Section} / p.{p.Page}]\n{p.Text}"));

        // Foundation: stub answer from first passage. Real AI comes in Pipeline Built.
        ctx.AnswerText = ctx.Passages[0].Text;
        ctx.Sources = ctx.Passages.Select(p => new SourceRef(p.Document, p.Section, p.Page)).ToList();

        return Task.FromResult(new StageResult(
            StageOutcome.Pass,
            null,
            $"context_chars={contextText.Length}; passages={ctx.Passages.Count}"
        ));
    }
}

/// <summary>Stub complex_ai — passes through. Willis owns real T11c.</summary>
public class ComplexAiStage : IStage
{
    public string Name => "complex_ai";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (ctx.DirectHit)
            return Task.FromResult(new StageResult(StageOutcome.Pass, null, "skipped", Skipped: true));

        return Task.FromResult(new StageResult(StageOutcome.Pass, null, "stub"));
    }
}

/// <summary>Stub output_compliance — escalates if no answer. Willis owns real T11c.</summary>
public class OutputComplianceStage : IStage
{
    public string Name => "output_compliance";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.AnswerText) && ctx.Passages.Count == 0)
        {
            return Task.FromResult(new StageResult(
                StageOutcome.Escalate,
                "No content found for this question",
                "escalate_no_content"
            ));
        }
        return Task.FromResult(new StageResult(StageOutcome.Pass, null, "ok"));
    }
}

/// <summary>Stub ticketing — returns fake ticket ref. Willis owns real T11c.</summary>
public class TicketingStage : IStage
{
    public string Name => "ticketing";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        var ticket = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        ctx.TicketReference = ticket;
        ctx.AnswerText ??= "Your request has been escalated. Use the reference number below.";
        return Task.FromResult(new StageResult(StageOutcome.Pass, null, $"ticket={ticket}"));
    }
}

/// <summary>Stub translation — pass-through. A'keyrah owns T11a.</summary>
public class TranslationStage : IStage
{
    public string Name => "translation";
    public Task<StageResult> RunAsync(QueryContext ctx)
        => Task.FromResult(new StageResult(StageOutcome.Pass, null, $"lang={ctx.Language}"));
}

/// <summary>
/// Stub input_compliance — stops pizza / jailbreak; escalates harassment.
/// A'keyrah owns real T11a. This supports the four test paths.
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
                "This question is outside the scope of chapter support.",
                "out_of_scope"
            ));
        }

        if (q.Contains("harass") || q.Contains("harassment"))
        {
            return Task.FromResult(new StageResult(
                StageOutcome.Escalate,
                "Sensitive topic — escalating to a person",
                "sensitive"
            ));
        }

        return Task.FromResult(new StageResult(StageOutcome.Pass, null, "ok"));
    }
}
