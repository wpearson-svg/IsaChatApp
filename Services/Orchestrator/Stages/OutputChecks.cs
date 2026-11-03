namespace IsaChatApp.Services.Orchestrator.Stages;

// The checks behind output_compliance's AI side (stage contract section 8).
// The result shapes are fixed now, so the real checks can replace the stubs later
// without changing the contract, the event log or the panel.

/// <summary>Source check (REAL): the passage the model named must be one of the retrieved passages.</summary>
public record SourceCheckResult(SourceRef? Cited, bool Retrieved);

/// <summary>NLI check (STUB): does the cited passage support the answer?</summary>
/// <param name="Verdict">supported, not_supported or unclear.</param>
/// <summary>One claim sentence of an answer and its NLI score against the passage (0 to 1).</summary>
public record SentenceScore(string Text, double Score);

public record NliResult(string Verdict, string Reason, double? MinScore = null, IReadOnlyList<SentenceScore>? Scored = null);

/// <summary>Rule recheck (STUB): the answer is checked again against the rules.</summary>
public record RuleRecheckResult(bool Passed, string Rule, string Reason);

/// <summary>Confidence score (STUB): Score 0.00 to 1.00 against the Threshold.</summary>
public record ConfidenceResult(double Score, double Threshold, bool Passed);

public static class OutputChecks
{
    /// <summary>The one real AI-side check.</summary>
    public static SourceCheckResult CheckSource(SourceRef? cited, IEnumerable<Passage> retrieved)
    {
        if (cited is null) return new(null, false);   // the model named nothing
        return new(cited, retrieved.Any(p => Same(cited, p)));
    }

    /// <summary>Is this source one of these passages? Document, section and page must all match.</summary>
    public static bool Same(SourceRef s, Passage p) =>
        string.Equals(s.Document, p.Document, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(s.Section, p.Section, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(s.Page, p.Page, StringComparison.OrdinalIgnoreCase);

    // ---- stubs: the right shape every time, not smart ----

    public static NliResult Nli(string answer, SourceRef cited) =>
        new("supported", "stub: fixed pass");

    /// <summary>
    /// NLI check (REAL, HHEM-2.1-Open): every sentence of the answer that makes a claim is scored against the cited passage.
    /// "supported" = all scored at or above the threshold; "not_supported" = at least one below; "unclear" = the model is not
    /// available, so nothing was checked (the stage logs that as skipped, never as a pass).
    /// </summary>
    public static NliResult NliHhem(HhemScorer scorer, string answer, string premise, double threshold)
    {
        var sentences = ClaimSentences(answer);
        if (sentences.Count == 0) return new("supported", "hhem; no claims to check");
        var below = 0; var min = 1.0;
        var scored = new List<SentenceScore>();
        foreach (var sentence in sentences)
        {
            var score = scorer.Score(premise, sentence);
            if (score is null) return new("unclear", "hhem unavailable");
            scored.Add(new SentenceScore(sentence, score.Value));
            min = Math.Min(min, score.Value);
            if (score.Value < threshold) below++;
        }
        return below == 0
            ? new("supported", $"hhem; sentences={sentences.Count}; min_score={min:0.00}", min, scored)
            : new("not_supported", $"hhem; unsupported={below}/{sentences.Count}; min_score={min:0.00}", min, scored);
    }

    private static readonly string[] NoClaimOpeners = { "good question", "sure", "certainly", "of course", "here's", "here is", "great question" };

    /// <summary>The sentences of an answer that say something. Short openers ("Good question.") and lead-ins ending in a colon are skipped.</summary>
    public static List<string> ClaimSentences(string answer) => SplitSentences(answer).Where(IsClaim).ToList();

    /// <summary>Every sentence or line of an answer, in order, claims and non-claims alike.</summary>
    public static List<string> SplitSentences(string answer) =>
        System.Text.RegularExpressions.Regex.Split(answer ?? "", @"(?<=[.!?])\s+|\n+")
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

    /// <summary>A sentence that says something (not a short opener like "Good question.", not a lead-in ending in a colon).</summary>
    public static bool IsClaim(string x) =>
        !x.EndsWith(":") &&
        !NoClaimOpeners.Any(o => x.StartsWith(o, StringComparison.OrdinalIgnoreCase) && x.Split(' ').Length <= 6);

    /// <summary>
    /// Fallback step 2: the answer with its unsupported sentences removed. Only allowed when the first claim sentence survives and at least
    /// half of the claim sentences do; otherwise returns null (the caller goes to ISACA's exact words).
    /// </summary>
    public static (string Text, int Removed)? TrimUnsupported(string answer, IReadOnlyList<SentenceScore> scored, double threshold)
    {
        var kept = new List<string>(); var claimIndex = 0; var removed = 0; var firstClaimKept = false; var firstSeen = false;
        foreach (var s in SplitSentences(answer))
        {
            if (!IsClaim(s)) { kept.Add(s); continue; }
            var ok = claimIndex < scored.Count && scored[claimIndex].Score >= threshold;
            claimIndex++;
            if (!firstSeen) { firstSeen = true; firstClaimKept = ok; }
            if (ok) kept.Add(s); else removed++;
        }
        var total = claimIndex;
        if (removed == 0 || total == 0 || !firstClaimKept || (total - removed) * 2 < total) return null;
        return (string.Join(" ", kept), removed);
    }

    public static RuleRecheckResult RuleRecheck(string answer) =>
        new(true, "", "stub: fixed pass");

    public static ConfidenceResult Confidence(string answer, double threshold) =>
        new(0.90, threshold, 0.90 >= threshold);
}
