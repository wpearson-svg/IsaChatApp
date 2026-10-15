namespace IsaChatApp.Services.Metrics;

/// <summary>
/// KPI helpers for Dashboard / Analytics.
/// T7A: reusable calculation functions.
/// </summary>
public class MetricsService
{
    private readonly ILogger<MetricsService> _log;

    public MetricsService(ILogger<MetricsService> log) => _log = log;

    /// <summary>Self-service rate: resolved by ISA ÷ total questions × 100.</summary>
    public double SelfServiceRate(int resolvedByIsa, int totalQuestions)
    {
        if (totalQuestions <= 0) return 0;
        return Math.Round(resolvedByIsa * 100.0 / totalQuestions, 1);
    }

    /// <summary>Answer accuracy: correct ÷ audited × 100.</summary>
    public double AnswerAccuracy(int correct, int audited)
    {
        if (audited <= 0) return 0;
        return Math.Round(correct * 100.0 / audited, 1);
    }

    /// <summary>Ticket reduction: (old − current) ÷ old × 100.</summary>
    public double TicketReduction(int baseline, int current)
    {
        if (baseline <= 0) return 0;
        return Math.Round((baseline - current) * 100.0 / baseline, 1);
    }

    /// <summary>Simple average of chapter health dimensions (0–100).</summary>
    public double ChapterHealthScore(params double[] dimensions)
    {
        if (dimensions is null || dimensions.Length == 0)
        {
            _log.LogWarning("[{Time}] WARN ChapterHealthScore called with no dimensions",
                DateTime.UtcNow.ToString("O"));
            return 0;
        }
        return Math.Round(dimensions.Average(), 0);
    }
}
