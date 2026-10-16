using System.Text;
using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Orchestrator.Stages;

// T11c -- the three stages Willis owns: complex_ai, output_compliance, ticketing.
// Stub rules (contract section 8): right shape every time, does not have to be smart,
// and stages never write to the event log (the orchestrator does that from what they return).

/// <summary>
/// Stage 8. Builds an answer when the search did not find a direct text match (test path 2).
/// STUB: no model is called. The real version sends the question + passages to the chosen model.
/// </summary>
public class ComplexAiStage : IStage
{
    public string Name => "complex_ai";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        // Direct hit: the answer is already the matching passage. Pass with the note "skipped".
        if (ctx.DirectHit)
            return Task.FromResult(new StageResult(StageOutcome.Pass, null, "direct hit", Skipped: true));

        // Nothing was found, so there is nothing to build from. output_compliance will escalate.
        if (ctx.Passages.Count == 0)
            return Task.FromResult(new StageResult(StageOutcome.Pass, null, "stub; passages=0; nothing to build from"));

        // Stand-in for the model: use the top passages (no duplicates) as the answer text.
        var used = ctx.Passages.Select(p => p.Text).Distinct().Take(1).ToList();
        ctx.AnswerText = string.Join("\n\n", used);
        ctx.Sources = ctx.Sources.Distinct().ToList();

        return Task.FromResult(new StageResult(StageOutcome.Pass, null, $"stub; model=none; built_from={used.Count}"));
    }
}

/// <summary>
/// Stage 9. Last check before the user sees an answer.
/// Escalates when there is no approved content behind the answer, otherwise tidies the text.
/// </summary>
public class OutputComplianceStage : IStage
{
    public string Name => "output_compliance";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        // Check 1: there is an answer and it comes from approved content (test path 4, cryptocurrency).
        if (string.IsNullOrWhiteSpace(ctx.AnswerText) || ctx.Passages.Count == 0)
            return Task.FromResult(new StageResult(StageOutcome.Escalate,
                "no approved content found", "check=grounding; passages=0"));

        // Check 2: an answer must name at least one source.
        if (ctx.Sources.Count == 0)
            return Task.FromResult(new StageResult(StageOutcome.Escalate,
                "answer has no source", "check=sources; sources=0"));

        ctx.AnswerText = "Based on the approved content: " + Tidy(ctx.AnswerText!, ctx.Passages[0].Page);
        return Task.FromResult(new StageResult(StageOutcome.Pass, null,
            $"checks=grounding,sources; sources={ctx.Sources.Count}"));
    }

    // PDF text arrives with hard line breaks and the page number stuck on the end. Make it read as prose.
    private static string Tidy(string text, string page)
    {
        var sb = new StringBuilder();
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) { if (sb.Length > 0 && sb[^1] != '\n') sb.Append("\n\n"); continue; }
            var isBullet = line.StartsWith("•") || line.StartsWith("- ");
            if (sb.Length > 0 && sb[^1] != '\n') sb.Append(isBullet ? "\n" : " ");
            sb.Append(line);
        }
        var t = Regex.Replace(sb.ToString().Trim(), @"^(•|-)\s*", "");   // no leading bullet mark
        // Remove a trailing page number only when it equals the passage's own page.
        if (page.Length > 0 && page != "n/a")
            t = Regex.Replace(t, @"\s+" + Regex.Escape(page) + "$", "");
        return t;
    }
}

/// <summary>
/// Stage 10. Runs only after an Escalate. Returns a fake ticket reference (TKT-0001 style, counting up).
/// STUB: the real version creates the ticket in the client's ticketing system through the adapter.
/// The count restarts when the app restarts.
/// </summary>
public class TicketingStage : IStage
{
    private static int _counter;
    public string Name => "ticketing";

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (ctx.EscalatedBy is null)
            return Task.FromResult(new StageResult(StageOutcome.Pass, null, "not escalated", Skipped: true));

        var ticket = $"TKT-{Interlocked.Increment(ref _counter):0000}";
        ctx.TicketReference = ticket;

        // An escalated run shows no answer and no sources, whatever an earlier stage had built.
        ctx.Sources = new();
        ctx.AnswerText = ctx.EscalatedBy == "input_compliance"
            ? $"This looks like a sensitive matter, so I have passed it to a person who can help. Your reference number is {ticket}."
            : $"I could not find an approved answer to this, so I have sent it to a person to follow up. Your reference number is {ticket}.";

        return Task.FromResult(new StageResult(StageOutcome.Pass, null, $"ticket={ticket}; from={ctx.EscalatedBy}"));
    }
}
