namespace IsaChatApp.Services.Orchestrator.Stages;

// The checks behind output_compliance's AI side (stage contract section 8).
// The result shapes are fixed now, so the real checks can replace the stubs later
// without changing the contract, the event log or the panel.

/// <summary>Source check (REAL): the passage the model named must be one of the retrieved passages.</summary>
public record SourceCheckResult(SourceRef? Cited, bool Retrieved);

/// <summary>NLI check (STUB): does the cited passage support the answer?</summary>
/// <param name="Verdict">supported, not_supported or unclear.</param>
public record NliResult(string Verdict, string Reason);

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

    public static RuleRecheckResult RuleRecheck(string answer) =>
        new(true, "", "stub: fixed pass");

    public static ConfidenceResult Confidence(string answer, double threshold) =>
        new(0.90, threshold, 0.90 >= threshold);
}
