using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Orchestrator.Stages;

/// <summary>
/// Picks the sentence that best answers the question out of the passages the search found.
/// STUB for the real model: no AI, just word overlap, plus a bonus for numbers on "how many" questions.
/// Puts the passage the sentence came from first in Passages and Sources.
/// </summary>
public static class AnswerExtract
{
    private static readonly HashSet<string> Skip = new(StringComparer.OrdinalIgnoreCase)
    {
        "how","many","what","does","must","each","have","our","and","who","are","for","when","can",
        "with","that","this","you","is","do","we","to","a","of","in","the","hold","should","which","where"
    };

    private static readonly Regex Numberish = new(
        @"\b(\d+|one|two|three|four|five|six|seven|eight|nine|ten|twelve|monthly|quarterly|annually|at least|minimum)\b",
        RegexOptions.IgnoreCase);

    private static string Stem(string w)
    {
        w = w.ToLowerInvariant();
        foreach (var suf in new[] { "ing", "ed", "es", "s" })
            if (w.Length > suf.Length + 3 && w.EndsWith(suf)) return w[..^suf.Length];
        return w;
    }

    private static HashSet<string> Terms(string text) =>
        Regex.Matches(text ?? "", "[A-Za-z]+").Select(m => m.Value)
            .Where(w => !Skip.Contains(w)).Select(Stem).ToHashSet();

    public static void Apply(QueryContext ctx)
    {
        if (ctx.Passages.Count == 0) return;
        var qTerms = Terms(ctx.Question);
        var howMany = Regex.IsMatch(ctx.Question ?? "", @"\bhow (many|much|often|long)\b", RegexOptions.IgnoreCase);

        int bestPassage = 0; string bestSentence = ""; double bestScore = -1;
        for (int i = 0; i < ctx.Passages.Count; i++)
        {
            foreach (var line in ctx.Passages[i].Text.Replace("\r", "").Split('\n'))
            {
                foreach (var raw in Regex.Split(line.Trim(), @"(?<=[.!?])\s+"))
                {
                    var s = Regex.Replace(raw.Trim(), @"^(•|o |- |•)\s*", "");
                    if (s.Length < 25) continue;
                    var overlap = Terms(s).Count(t => qTerms.Contains(t));
                    double score = overlap;
                    if (howMany && Numberish.IsMatch(s)) score += 3;
                    score -= i * 0.01;                       // on a tie, keep the search ranking
                    if (score > bestScore) { bestScore = score; bestSentence = s; bestPassage = i; }
                }
            }
        }
        if (bestScore <= 0 || bestSentence.Length == 0) bestSentence = ctx.Passages[0].Text;

        // chosen passage first, so its source is the first one listed
        var ordered = new List<Passage> { ctx.Passages[bestPassage] };
        ordered.AddRange(ctx.Passages.Where((_, i) => i != bestPassage));
        ctx.Passages = ordered;
        ctx.Sources = ordered.Select(p => new SourceRef(p.Document, p.Section, p.Page)).Distinct().ToList();
        ctx.AnswerText = bestSentence;
    }
}
