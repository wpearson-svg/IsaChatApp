using System.Text;
using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Orchestrator.Stages;

// T11c -- the three stages Willis owns: complex_ai, output_compliance, ticketing.
// Stub rules (contract section 8): right shape every time, does not have to be smart,
// and stages never write to the event log (the orchestrator does that from what they return).

/// <summary>
/// Stage 8. The real model (phi4-mini through Ollama). Runs only after a miss, and is always sent the
/// question WITH the passages rag handed over, never the bare question (contract section 1).
/// Ported from Eliyas's Pipeline Built package (22 Oct 2026) and changed to fit the contract:
///   - passages are numbered and the model names the NUMBER it used (a small model is far more reliable
///     at that than at copying document, section and page word for word);
///   - a failed, timed-out or empty model call is Escalate with reason "model call failed" and
///     error=unavailable|timeout|empty (section 9), so the run gets a ticket and never hangs;
///   - "the passages don't answer it" escalates as "no approved content found";
///   - the named passage goes to ctx.CitedSource, and output_compliance's AI side does the real
///     source check (it must be one of the retrieved passages);
///   - the event data carries model_called, model, model_ms, passages and source; Ollama's token
///     counts go to ctx.PromptTokens / ctx.CompletionTokens for the dashboard.
/// </summary>
public class ComplexAiStage : IStage
{
    private readonly OllamaChatService _ollama;
    public string Name => "complex_ai";

    public ComplexAiStage(OllamaChatService ollama) => _ollama = ollama;

    public async Task<StageResult> RunAsync(QueryContext ctx)
    {
        // Direct hit: the answer is already the matching passage, no model. Logged so a demo can show it.
        if (ctx.DirectHit)
            return new StageResult(StageOutcome.Pass, null, "model_called=no; direct hit", Skipped: true);

        // Never send the bare question. No passages means no model call and an escalation.
        if (ctx.Passages.Count == 0)
            return new StageResult(StageOutcome.Escalate, "no approved content found", "model_called=no; passages=0");

        // ctx.TestModelOff is test-only: aim the call at a dead address to test the failure path without quitting Ollama.
        var result = await _ollama.GenerateAsync(BuildPrompt(ctx.Question, ctx.Passages), ctx.TestModelOff ? "http://127.0.0.1:1" : null);
        ctx.PromptTokens = result.PromptTokens;
        ctx.CompletionTokens = result.CompletionTokens;
        var ran = $"model_called=yes; model={result.Model}; model_ms={result.ElapsedMs}; passages={ctx.Passages.Count}";

        var error = result.Error ?? (string.IsNullOrWhiteSpace(result.Text) ? "empty" : null);
        if (error is not null)
            return new StageResult(StageOutcome.Escalate, "model call failed",
                $"error={error}; {ran}" + (result.Detail is null ? "" : $"; detail={result.Detail}"));

        if (result.Text.Contains("NOT_ANSWERED_BY_PASSAGES", StringComparison.OrdinalIgnoreCase))
            return new StageResult(StageOutcome.Escalate, "no approved content found", $"{ran}; answer_found=no");

        var (body, sourceLine) = Split(result.Text);
        if (sourceLine is null)
            return new StageResult(StageOutcome.Escalate, "model did not name a source", $"{ran}; source=none");
        if (string.IsNullOrWhiteSpace(body))
            return new StageResult(StageOutcome.Escalate, "model call failed", $"error=empty; {ran}");

        var matched = Resolve(sourceLine, ctx.Passages);
        ctx.AnswerText = body;
        if (matched is null)
        {
            // Named something that was not handed over. output_compliance's source check escalates it.
            ctx.CitedSource = new SourceRef(Clean(sourceLine), "", "");
            return new StageResult(StageOutcome.Pass, null, $"{ran}; source={Clean(sourceLine)}");
        }

        ctx.CitedSource = new SourceRef(matched.Document, matched.Section, matched.Page);
        ctx.Sources = new List<SourceRef> { ctx.CitedSource };
        var named = $"{matched.Document} {matched.Section}".Replace(';', ',');
        return new StageResult(StageOutcome.Pass, null, $"{ran}; source={named}");
    }

    internal static string BuildPrompt(string question, IReadOnlyList<Passage> passages)
    {
        var block = string.Join("\n\n", passages.Select((p, i) =>
            $"[{i + 1}] (document: {p.Document}; section: {p.Section}; page: {p.Page})\n{p.Text}"));
        return $"""
            You are Isa, a chapter-support assistant. Answer the QUESTION using only the numbered PASSAGES below.
            Rules:
            - Use no outside knowledge. Do not invent policies, names, dates or requirements.
            - If the passages do not answer the question, reply with exactly: NOT_ANSWERED_BY_PASSAGES
            - Otherwise answer in one to three plain sentences. Then, on a new last line, write SOURCE: PASSAGE <number> naming the one passage you used.

            PASSAGES:
            {block}

            QUESTION:
            {question}
            """;
    }

    // Splits the reply into the answer and the (last) SOURCE line. The SOURCE label may sit on its own
    // line or at the end of the answer, with or without bold marks.
    private static (string Body, string? Source) Split(string text)
    {
        string? source = null;
        var kept = new List<string>();
        foreach (var line in text.Replace("\r", "").Split('\n'))
        {
            var m = Regex.Match(line, @"\*{0,2}\bSOURCES?\s*:\s*\*{0,2}\s*(.*)$", RegexOptions.IgnoreCase);
            if (!m.Success) { kept.Add(line); continue; }
            var found = m.Groups[1].Value.Trim().Trim('*').Trim();
            if (found.Length > 0) source = found;
            var before = line[..m.Index].TrimEnd();
            if (before.Length > 0) kept.Add(before);
        }
        return (string.Join("\n", kept).Trim(), source);
    }

    // Which handed-over passage did the source line name? By number first, then by document and page.
    private static Passage? Resolve(string sourceLine, IReadOnlyList<Passage> passages)
    {
        var n = Regex.Match(sourceLine, @"(?:PASSAGE\s*#?\s*\[?|\[)(\d+)", RegexOptions.IgnoreCase);
        if (!n.Success && Regex.IsMatch(sourceLine.Trim(), @"^\d+\W*$")) n = Regex.Match(sourceLine, @"\d+");
        if (n.Success)
        {
            var idx = int.Parse(n.Groups[n.Groups.Count > 1 ? 1 : 0].Value);
            return idx >= 1 && idx <= passages.Count ? passages[idx - 1] : null;
        }
        var byDoc = passages.Where(p => sourceLine.Contains(p.Document, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byDoc.Count > 1)
            byDoc = byDoc.Where(p => Regex.IsMatch(sourceLine, $@"\b{Regex.Escape(p.Page)}\b") ||
                                     sourceLine.Contains(p.Section, StringComparison.OrdinalIgnoreCase)).ToList();
        return byDoc.Count == 1 ? byDoc[0] : null;
    }

    private static string Clean(string s)
    {
        s = s.Replace(';', ',').Trim();
        return s.Length > 80 ? s[..80] : s;
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

        // An answer from a best-practice guide says so, instead of "ISACA's guidance says".
        var opening = IsaChatApp.Services.ShowTheWork.Wording.IsBestPractice(ctx.Passages[0].Document)
            ? IsaChatApp.Services.ShowTheWork.Wording.BestPracticeOpening
            : IsaChatApp.Services.ShowTheWork.Wording.AnswerOpening;
        ctx.AnswerText = opening + Tidy(ctx.AnswerText!, ctx.Passages[0].Page);
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
