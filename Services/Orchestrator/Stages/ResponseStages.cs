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
        // The model writes the label several ways: "PASSAGE 1", "PASSAGE: 1", "PASSAGE #1", "PASSAGE [1]", "Passage number 1", "[1]".
        // (A colon after PASSAGE was not accepted before, so "PASSAGE: 1" escalated as "source not retrieved".)
        var n = Regex.Match(sourceLine, @"(?:PASSAGES?\W{0,3}(?:NO\.?|NUMBER)?\W{0,3}|\[)(\d+)", RegexOptions.IgnoreCase);
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

    private readonly string _forceFail;

    private readonly IConfiguration _config;
    private readonly HhemScorer _hhem;
    private readonly IsaChatApp.Services.OutputComplianceSettings _settings;

    private readonly IsaChatApp.Services.Rules.RuleEngine _rules;

    public OutputComplianceStage(IConfiguration config, HhemScorer hhem, IsaChatApp.Services.OutputComplianceSettings settings, IsaChatApp.Services.Rules.RuleEngine rules)
    {
        _rules = rules;
        _config = config;
        _hhem = hhem;
        _settings = settings;   // thresholds are read per answer so the Dev Dashboard can change them live
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

            // The rehearsed demo questions keep the old fixed checks (AnswerWording:ExemptQuestions), so they behave exactly as rehearsed.
            // OutputCompliance:FixedChecks false switches the new checks off for everything.
            var exempt = AnswerExtract.IsExempt(_config, ctx.Question);
            var fixedOn = !exempt && _config.GetValue("OutputCompliance:FixedChecks", true);
            var citedPassage = ctx.Passages.FirstOrDefault(p => OutputChecks.Same(source.Cited!, p));
            var premise = citedPassage?.Text ?? "";

            // A source that still says "TBD" is never reworded: nobody should guess what it will say.
            if (fixedOn && citedPassage is not null && Regex.IsMatch(premise, @"\bTBD\b"))
                return Task.FromResult(Escalate("ai check failed: source incomplete", data + "; check=tbd"));

            // Rule recheck (REAL): the same rules files as the input check, applied to the written answer
            // (each applicable rule's answer_must_not phrases). A rule failure follows the rule's own outcome (hand off or stop), never a quote.
            ctx.Profile.TryGetValue("chapter_id", out var ruleChapter);
            ctx.Profile.TryGetValue("chapter_role", out var ruleRole);
            var ruleHit = _config.GetValue("Rules:Engine", true) ? _rules.CheckAnswer(ctx.AnswerText!, ruleChapter, ruleRole) : null;
            var ruleOk = ruleHit is null && _forceFail != "rule";
            var ruleTag = ruleHit is null ? "" : $"; rule={ruleHit.RuleId}; file={ruleHit.File}; version={ruleHit.Version}; checkpoint=output";
            if (!ruleOk)
            {
                if (ruleHit?.Outcome == "stop")
                    return Task.FromResult(new StageResult(StageOutcome.Stop, IsaChatApp.Services.ShowTheWork.Wording.OutsideScope, $"section={section}; {data}; rule_recheck=fail{ruleTag}"));
                return Task.FromResult(Escalate("ai check failed: rule recheck", data + "; rule_recheck=fail" + ruleTag));
            }

            // Answer shape (fixed): a "how many" question needs a number, "when" a date or period, "who" a role or team.
            // A failure goes to a person, never to a quote: the passage is what is wrong.
            if (fixedOn && AnswerChecks.ShapeMissing(ctx.Question, ctx.AnswerText!) is string missing)
                return Task.FromResult(Escalate("no approved content found", data + $"; check=answer_shape; missing={missing}"));

            // NLI check (REAL, HHEM-2.1-Open): each sentence of the answer is scored against the cited passage.
            // If the model files are not there the check is skipped and the event data says "skipped", never "pass".
            var nli = exempt || citedPassage is null
                ? OutputChecks.Nli(ctx.AnswerText!, source.Cited!)
                : OutputChecks.NliHhem(_hhem, ctx.AnswerText!, premise, _settings.NliThreshold);
            var nliSkipped = nli.Verdict == "unclear";
            var nliFailed = nli.Verdict == "not_supported";
            var answer = ctx.AnswerText!;
            var notes = "";

            // Fallback step 2: drop the unsupported sentences, when the first claim sentence and at least half of them are supported.
            if (nliFailed && fixedOn && _forceFail != "nli" && nli.Scored is not null
                && OutputChecks.TrimUnsupported(answer, nli.Scored, _settings.NliThreshold) is { } trimmed)
            {
                answer = trimmed.Text;
                nliFailed = false;
                notes += $"; step_down=trimmed; removed_sentences={trimmed.Removed}";
            }

            // The fixed accuracy checks (numbers, "must" stays "must", contact details) on the answer as it now stands.
            var fails = new List<string>();
            if (nliFailed) fails.Add("nli");
            var fixedText = "";
            if (fixedOn && citedPassage is not null)
            {
                var numbers = AnswerChecks.NumbersNotInSource(answer, premise);
                var strength = AnswerChecks.StrengthChanges(answer, premise);
                var contacts = AnswerChecks.ContactsNotInSource(answer, premise);
                if (numbers > 0) fails.Add("numbers");
                if (strength.Count > 0) fails.Add("strength");
                if (contacts > 0) fails.Add("contact");
                fixedText = $"; numbers={(numbers > 0 ? "fail:" + numbers : "pass")}; strength={(strength.Count > 0 ? "fail:" + string.Join("+", strength) : "pass")}; contact={(contacts > 0 ? "fail:" + contacts : "pass")}; shape=pass";
            }

            // Confidence: for a checked answer, built from parts that can be checked (search strength and the share of supported
            // sentences), each logged; never the model's opinion of itself. Rehearsed questions keep the fixed stub.
            ConfidenceResult conf;
            var confParts = "";
            var support = nli.Scored is { Count: > 0 } sc ? (double?)sc.Count(x => x.Score >= _settings.NliThreshold) / sc.Count : null;
            if (fixedOn)
            {
                var search = ctx.SearchScore ?? 1.0;   // a meaning-map match is an exact match on the question
                var parts = new List<double> { search };
                confParts = $"; confidence_parts=search:{search:0.00}";
                if (support is double sv) { parts.Add(sv); confParts += $",support:{sv:0.00}"; }
                var score = Math.Round(parts.Average(), 2);
                conf = new ConfidenceResult(score, _settings.ConfidenceThreshold, score >= _settings.ConfidenceThreshold);
            }
            else conf = OutputChecks.Confidence(answer, _settings.ConfidenceThreshold);
            if (!conf.Passed) fails.Add("confidence");

            data += $"; rule_recheck=pass; confidence={conf.Score:0.00}{confParts}; nli={(nliSkipped ? "skipped" : nliFailed ? "fail" : "pass")}; nli_detail={nli.Reason.Replace(';', ',')}{fixedText}{notes}";
            if (nli.MinScore is double nliScore)
                data += $"; nli_score={nliScore:0.00}; nli_mark={_settings.NliThreshold:0.00}";   // lowest sentence score and the pass mark, shown in Show the work

            // Forced failures (a demo or test of a bad answer being caught) escalate exactly as before.
            if (_forceFail == "nli") return Task.FromResult(Escalate("ai check failed: nli check", data));
            if (_forceFail == "confidence") return Task.FromResult(Escalate("ai check failed: confidence score", data));

            if (fails.Count > 0)
            {
                // Fallback step 3: ISACA's exact words with the source, when the search found a strong match. Otherwise a person.
                var strong = ctx.SearchScore is null || ctx.SearchScore >= _config.GetValue("OutputCompliance:StrongMatch", 0.0);
                if (fixedOn && strong && ctx.Passages.Count > 0)
                {
                    AnswerExtract.Apply(ctx, _config);
                    if (ctx.Sources.Count > 0) ctx.CitedSource = ctx.Sources[0];
                    answer = ctx.AnswerText!;
                    data += $"; step_down=isaca_words; step_reason={string.Join("+", fails)}";
                }
                else
                {
                    var reason = fails[0] switch
                    {
                        "nli" => "ai check failed: nli check",
                        "confidence" => "ai check failed: confidence score",
                        "numbers" => "ai check failed: numbers",
                        "strength" => "ai check failed: must-stays-must",
                        _ => "ai check failed: contact details",
                    };
                    return Task.FromResult(Escalate(reason, data + $"; failed={string.Join("+", fails)}"));
                }
            }
            ctx.AnswerText = answer;
        }

        // An answer from a best-practice guide says so, instead of "ISACA's guidance says".
        var opening = IsaChatApp.Services.ShowTheWork.Wording.IsBestPractice(ctx.Passages[0].Document)
            ? IsaChatApp.Services.ShowTheWork.Wording.BestPracticeOpening
            : IsaChatApp.Services.ShowTheWork.Wording.AnswerOpening;
        ctx.AnswerText = opening + Tidy(ctx.AnswerText!, ctx.Passages[0].Page);
        // "Answer, then point": the input rule's line follows the answer, so the leader knows a person is the right next step.
        if (!string.IsNullOrWhiteSpace(ctx.PointLine))
            ctx.AnswerText += "\n\n" + ctx.PointLine;
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
    // Starts at a random number in the middle of the 4-digit range each time Isa starts, so refs do not repeat across restarts and do not look like a default start.
    private static int _counter = Random.Shared.Next(4000, 6000);
    public string Name => "ticketing";

    private readonly IsaChatApp.Services.SalesforceTicketService _salesforce;
    public TicketingStage(IsaChatApp.Services.SalesforceTicketService salesforce) => _salesforce = salesforce;

    public async Task<StageResult> RunAsync(QueryContext ctx)
    {
        if (ctx.EscalatedBy is null)
            return new StageResult(StageOutcome.Pass, null, "not escalated", Skipped: true);

        var ticket = $"TKT-{Interlocked.Increment(ref _counter):0000}";
        ctx.TicketReference = ticket;

        // An escalated run shows no answer and no sources, whatever an earlier stage had built.
        ctx.Sources = new();
        // The hand-off message depends on why the run was passed to a person
        // (wording: Services/ShowTheWork/Wording.cs).
        var kind = IsaChatApp.Services.ShowTheWork.Wording.KindOf(ctx.EscalatedBy, ctx.EscalationReason);
        ctx.AnswerText = IsaChatApp.Services.ShowTheWork.Wording.Handoff(kind, ticket, ctx.SafetyTopic);

        // Send it to Salesforce when that is switched on (Ticketing:Salesforce:Enabled). A failure
        // never stops the hand-off: the leader still has the reference, and the run records it.
        var sf = "";
        if (_salesforce.Enabled)
        {
            ctx.Profile.TryGetValue("chapter_id", out var chapterId);
            ctx.Profile.TryGetValue("chapter_role", out var chapterRole);
            ctx.Profile.TryGetValue("person_id", out var personId);
            ctx.Profile.TryGetValue("display_name", out var contactName);
            ctx.Profile.TryGetValue("email", out var contactEmail);
            var requestedBy = string.Join(" ", new[] { contactName, string.IsNullOrWhiteSpace(contactEmail) ? null : $"<{contactEmail}>" }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(requestedBy)) requestedBy = ctx.UserId ?? "";
            var r = await _salesforce.CreateCaseAsync(new IsaChatApp.Services.SalesforceTicketService.CaseInput(
                ticket, ctx.TraceId, ctx.Language, ctx.EscalatedBy, ctx.EscalationReason,
                chapterId, chapterRole, ctx.SafetyTopic, requestedBy, personId));
            sf = r.Ok ? $"; salesforce=created; case_id={r.CaseId}" : $"; salesforce=failed; detail={r.Error}";
        }
        else sf = "; salesforce=off";

        return new StageResult(StageOutcome.Pass, null, $"ticket={ticket}; from={ctx.EscalatedBy}{sf}");
    }
}
