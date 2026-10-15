using Microsoft.Data.Sqlite;
using IsaChatApp.Services.Orchestrator;

namespace IsaChatApp.Services.Orchestrator.Stages;

public class TranslationStage : IStage
{
    public string Name => "translation";
    public Task<StageResult> RunAsync(QueryContext ctx)
        => Task.FromResult(new StageResult(StageOutcome.Pass, null, $"lang={ctx.Language}"));
}

public class InputComplianceStage : IStage
{
    public string Name => "input_compliance";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        var q = ctx.Question.ToLowerInvariant();

        if (q.Contains("pizza") || q.Contains("ignore your rules") || q.Contains("show me every document"))
            return Task.FromResult(new StageResult(
                StageOutcome.Stop,
                "This question is outside the scope of chapter support.",
                "out_of_scope"));

        if (q.Contains("harass") || q.Contains("harassment"))
            return Task.FromResult(new StageResult(
                StageOutcome.Escalate,
                "Sensitive topic — escalating to a person",
                "sensitive"));

        return Task.FromResult(new StageResult(StageOutcome.Pass, null, "ok"));
    }
}

/// <summary>T11b — Database Search stub.</summary>
public class DatabaseSearchStage : IStage
{
    public string Name => "database_search";
    private readonly string _contentDbPath;

    public DatabaseSearchStage(IConfiguration config)
    {
        _contentDbPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
    }

    public async Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (!File.Exists(_contentDbPath))
            return new StageResult(StageOutcome.Pass, null, "no content db yet");

        await using var conn = new SqliteConnection($"Data Source={_contentDbPath};Mode=ReadOnly");
        await conn.OpenAsync();

        var passages = new List<Passage>();

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT document, section, page, text
                FROM passages WHERE text MATCH $q LIMIT 5;
                """;
            cmd.Parameters.AddWithValue("$q", ctx.Question);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                passages.Add(new Passage(reader.GetString(0), reader.GetString(1),
                    reader.GetString(2), reader.GetString(3)));
        }
        catch { /* table may not exist yet */ }

        if (passages.Count > 0)
        {
            ctx.Passages = passages;
            ctx.DirectHit = true;
            ctx.Sources = passages.Select(p => new SourceRef(p.Document, p.Section, p.Page)).ToList();
            return new StageResult(StageOutcome.Pass, null, $"direct_hit; count={passages.Count}");
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                SELECT p.document, p.section, p.page, p.text
                FROM meaning_map m
                JOIN passages p ON p.id = m.passage_id
                WHERE lower(m.question) = lower($q) LIMIT 5;
                """;
            cmd.Parameters.AddWithValue("$q", ctx.Question);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                passages.Add(new Passage(reader.GetString(0), reader.GetString(1),
                    reader.GetString(2), reader.GetString(3)));
        }
        catch { /* meaning_map may not exist */ }

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

public class KnowledgeGraphStage : IStage
{
    public string Name => "knowledge_graph";
    public Task<StageResult> RunAsync(QueryContext ctx)
        => Task.FromResult(new StageResult(StageOutcome.Pass, null, "related=[]"));
}

public class RagStage : IStage
{
    public string Name => "rag";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (ctx.DirectHit)
        {
            if (ctx.Passages.Count > 0 && string.IsNullOrEmpty(ctx.AnswerText))
                ctx.AnswerText = ctx.Passages[0].Text;
            return Task.FromResult(new StageResult(StageOutcome.Pass, null, "skipped direct_hit", Skipped: true));
        }

        if (ctx.Passages.Count == 0)
            return Task.FromResult(new StageResult(StageOutcome.Pass, null, "no_context"));

        ctx.AnswerText = ctx.Passages[0].Text;
        ctx.Sources = ctx.Passages.Select(p => new SourceRef(p.Document, p.Section, p.Page)).ToList();
        return Task.FromResult(new StageResult(StageOutcome.Pass, null,
            $"passages={ctx.Passages.Count}"));
    }
}

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

public class OutputComplianceStage : IStage
{
    public string Name => "output_compliance";
    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.AnswerText) && ctx.Passages.Count == 0)
            return Task.FromResult(new StageResult(
                StageOutcome.Escalate, "No content found for this question", "escalate_no_content"));
        return Task.FromResult(new StageResult(StageOutcome.Pass, null, "ok"));
    }
}

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
