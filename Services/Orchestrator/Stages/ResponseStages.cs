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

        // The real model names the passage it used (contract: cited_source). The stand-in
        // "names" the first passage, so output_compliance's AI side has a source to check.
        var first = ctx.Passages[0];
        ctx.CitedSource = new SourceRef(first.Document, first.Section, first.Page);

        return Task.FromResult(new StageResult(StageOutcome.Pass, null,
            $"stub; model=none; built_from={used.Count}; passages={ctx.Passages.Count}; source={first.Document} {first.Section}"));
    }
}

/// <summary>
/// Stage 9. Last check before the user sees an answer. Every answer passes through it,
/// including direct hits. Which section runs depends on where the answer came from
/// (stage contract section 8):
///
/// DATABASE side (a direct hit): the answer has a source, and the source is an approved
///   passage. No source, or a source that is not approved: Escalate.
/// AI side (written by the model): everything the database side does, plus the source
///   check (REAL: the passage the model named must be one of the retrieved passages), the
///   NLI check, the rule recheck and the confidence score (STUBS with fixed result shapes).
///   A failure on any of them escalates with "ai check failed: &lt;name&gt;".
///
/// The section that ran is written to ctx.OutputSection and to the event log.
/// Setting OutputCompliance:ForceFail (source, nli, rule or confidence) forces that
/// AI-side check to fail, so a demo or a test can show a bad response being caught.
/// </summary>
public class OutputComplianceStage : IStage
{
    public string Name => "output_compliance";

    private readonly double _threshold;
    private readonly string _forceFail;

    public OutputComplianceStage(IConfiguration config)
    {
        _threshold = config.GetValue("OutputCompliance:ConfidenceThreshold", 0.70);
        _forceFail = (config["OutputCompliance:ForceFail"] ?? "").Trim().ToLowerInvariant();
    }

    public Task<StageResult> RunAsync(QueryContext ctx)
    {
        var section = ctx.DirectHit ? "database" : "ai";
        ctx.OutputSection = section;

        StageResult Escalate(string reason, string data) =>
            new(StageOutcome.Escalate, reason, $"section={section}; {data}");

        // Safety net: there is no answer, or nothing approved behind it (test path 4, cryptocurrency).
        if (string.IsNullOrWhiteSpace(ctx.AnswerText) || ctx.Passages.Count == 0)
            return Task.FromResult(Escalate("no approved content found", "check=grounding; passages=0"));

        // Both sections: the answer has a source, and the source is an approved passage.
        // (Every passage comes from the approved content database, so "approved" means
        // the source is one of the passages this run found.)
        var cited = ctx.CitedSource ?? ctx.Sources.FirstOrDefault();
        var hasSource = cited is not null;
        var approved = hasSource && ctx.Sources.All(src => ctx.Passages.Any(p => OutputChecks.Same(src, p)));
        var basics = $"source={(hasSource ? "ok" : "missing")}; approved={(approved ? "yes" : "no")}";

        if (!hasSource || ctx.Sources.Count == 0)
            return Task.FromResult(Escalate("answer has no approved source", basics + "; sources=0"));
        if (!approved)
            return Task.FromResult(Escalate("answer has no approved source", basics + $"; sources={ctx.Sources.Count}"));

        var data = $"{basics}; sources={ctx.Sources.Count}";

        if (section == "ai")
        {
            // Source check (real).
            var source = OutputChecks.CheckSource(ctx.CitedSource, ctx.Passages);
            var retrieved = source.Retrieved && _forceFail != "source";
            data += $"; source_retrieved={(retrieved ? "yes" : "no")}";
            if (!retrieved)
                return Task.FromResult(Escalate("ai check failed: source not retrieved", data));

            // NLI check (stub).
            var nli = OutputChecks.Nli(ctx.AnswerText!, source.Cited!);
            var nliOk = nli.Verdict == "supported" && _forceFail != "nli";
            // Rule recheck (stub).
            var rule = OutputChecks.RuleRecheck(ctx.AnswerText!);
            var ruleOk = rule.Passed && _forceFail != "rule";
            // Confidence score (stub).
            var conf = OutputChecks.Confidence(ctx.AnswerText!, _threshold);
            var confOk = conf.Passed && _forceFail != "confidence";

            data += $"; rule_recheck={(ruleOk ? "pass" : "fail")}; confidence={conf.Score:0.00}; nli={(nliOk ? "pass" : "fail")}";

            if (!nliOk)  return Task.FromResult(Escalate("ai check failed: nli check", data));
            if (!ruleOk) return Task.FromResult(Escalate("ai check failed: rule recheck", data));
            if (!confOk) return Task.FromResult(Escalate("ai check failed: confidence score", data));
        }

        ctx.AnswerText = "Based on the approved content: " + Tidy(ctx.AnswerText!, ctx.Passages[0].Page);
        return Task.FromResult(new StageResult(StageOutcome.Pass, null, $"section={section}; {data}"));
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
        // The hand-off message depends on why the run was passed to a person
        // (wording: Services/ShowTheWork/Wording.cs).
        var kind = IsaChatApp.Services.ShowTheWork.Wording.KindOf(ctx.EscalatedBy, ctx.EscalationReason);
        ctx.AnswerText = IsaChatApp.Services.ShowTheWork.Wording.Handoff(kind, ticket, ctx.SafetyTopic);

        return Task.FromResult(new StageResult(StageOutcome.Pass, null, $"ticket={ticket}; from={ctx.EscalatedBy}"));
    }
}
