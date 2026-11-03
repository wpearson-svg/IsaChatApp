using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Orchestrator.Stages;

/// <summary>
/// Picks the answer for a direct hit out of the passages the search found. No AI: the words are always ISACA's own,
/// never reworded. Spacing and bullet marks are tidied, nothing else is changed.
///
/// W3 (whole thoughts): the old version picked one sentence by shared words, line by line, so it returned fragments
/// (a sentence cut at a hard line break, a bullet without its lead-in, a sentence that starts "This..." with no
/// sentence before it). This version joins lines broken mid-sentence, puts a bullet's lead-in (the section heading)
/// in front of it, adds the sentence before one that starts mid-thought, and checks the result is a whole thought.
/// A quote that fails the check falls back to the whole bullet, then to the whole passage.
///
/// The old behaviour is kept as ApplyLegacy and is used for the rehearsed demo questions
/// (AnswerWording:ExemptQuestions in appsettings.json) so their answers stay exactly as rehearsed, and for everything
/// when AnswerWording:Enabled is false.
///
/// Puts the passage the quote came from first in Passages and Sources.
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

    public static string Normalize(string? s) =>
        Regex.Replace(s ?? "", "[^a-z0-9]+", " ", RegexOptions.IgnoreCase).Trim().ToLowerInvariant();

    /// <summary>
    /// Entry point. Rehearsed questions (and everything, when AnswerWording:Enabled is false) keep the old behaviour.
    /// </summary>
    public static void Apply(QueryContext ctx, IConfiguration? config = null)
    {
        if (ctx.Passages.Count == 0) return;

        var enabled = (config?["AnswerWording:Enabled"] ?? "true").Trim().ToLowerInvariant() != "false";

        if (!enabled || IsExempt(config, ctx.Question))
        {
            ApplyLegacy(ctx);
            return;
        }
        ApplyWholeThought(ctx);
    }

    /// <summary>Is this one of the rehearsed demo questions (AnswerWording:ExemptQuestions)? Shared with output compliance's NLI check.</summary>
    public static bool IsExempt(IConfiguration? config, string? question)
    {
        if (config is null) return false;
        var wanted = Normalize(question);
        return config.GetSection("AnswerWording:ExemptQuestions").GetChildren().Any(c => Normalize(c.Value) == wanted);
    }

    // ---------- whole-thought version ----------

    private sealed class Unit
    {
        public string Text = "";
        public bool Bullet;
        public string LeadIn = "";
    }

    private static readonly Regex MidThoughtStart = new(
        @"^(This|These|That|Those|They|Such|Their|Its|However|Also|Additionally|Therefore)\b", RegexOptions.None);

    private static bool MidThought(string s) => s.Length > 0 && (char.IsLower(s[0]) || MidThoughtStart.IsMatch(s));

    /// <summary>A quote is a whole thought when it starts like a sentence, is more than a few words, and is not just a lead-in.</summary>
    private static bool WholeThought(string s)
    {
        s = s.Trim();
        if (s.Length < 25) return false;
        if (MidThought(s)) return false;
        if (s.EndsWith(":")) return false;
        if (s.Contains('|')) return false;           // a table row, not a sentence
        if (!Regex.IsMatch(s, "[.!?)\"\u201D]$")) return false;   // a heading or a cut-off line, not a sentence
        return s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 3;
    }

    // Words that start a bullet written to follow "should:", "must:", "will:" or "to:". A bullet that starts any other
    // way ("Effective meetings enable...") is a statement on its own, so the lead-in is not put in front of it.
    private static readonly HashSet<string> ActionVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "Access","Acknowledge","Approve","Assist","Attend","Chair","Collaborate","Communicate","Complete","Conduct",
        "Confirm","Coordinate","Develop","Encourage","Ensure","Establish","Exercise","Facilitate","Help","Identify",
        "Implement","Lead","Maintain","Manage","Monitor","Oversee","Participate","Plan","Prepare","Present","Promote",
        "Provide","Recommend","Recruit","Report","Represent","Review","Schedule","Serve","Share","Submit","Support",
        "Update","Verify","View","Work"
    };

    private static readonly Regex NeedsActionVerb = new(@"\b(should|must|will|can|could|may|need to|has to)( also)?:$|\bto:$", RegexOptions.IgnoreCase);

    private static bool LeadInFits(string leadIn, string bullet)
    {
        if (leadIn.Length == 0) return false;
        if (!NeedsActionVerb.IsMatch(leadIn)) return true;           // "include:", "if:" and so on read with any bullet
        var first = bullet.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        return ActionVerbs.Contains(first.Trim(',', ';', ':'));
    }

    /// <summary>
    /// Turns a passage into units: one per bullet, one per paragraph. Lines the PDF broke in the middle of a sentence
    /// are joined to the line before. A bullet carries its lead-in ("The Chapter President should:") when the bullet
    /// reads as a continuation of it: the lead-in line just above the bullets, or, for the bullets at the top of a
    /// passage, the passage's section heading when that ends with a colon.
    /// </summary>
    private static List<Unit> Units(Passage p)
    {
        var units = new List<Unit>();
        Unit? cur = null;
        var section = (p.Section ?? "").Trim();
        // The bullets at the top of a passage follow the section heading: "Chapter Vice President > Common Activities".
        var lead = section.EndsWith(":") ? section : section.Contains(" > ") ? section + ":" : "";

        foreach (var raw in (p.Text ?? "").Replace("\r", "").Split('\n'))
        {
            var t = raw.Trim();
            if (t.Length == 0) { cur = null; continue; }

            var bullet = Regex.Match(t, @"^(•|o\s|-\s)\s*(.*)$");
            if (bullet.Success && bullet.Groups[2].Value.Trim().Length > 0)
            {
                var body = bullet.Groups[2].Value.Trim();
                cur = new Unit { Text = body, Bullet = true, LeadIn = LeadInFits(lead, body) ? lead : "" };
                units.Add(cur);
                continue;
            }

            // A line that carries on from the one before (starts lower case, or the line before ends mid-phrase).
            if (cur != null && (char.IsLower(t[0]) ||
                Regex.IsMatch(cur.Text, @"(,|;|\(|\b(and|or|of|the|to|a|an|in|for|with))\s*$", RegexOptions.IgnoreCase)))
            {
                cur.Text += " " + t;
                continue;
            }

            // A line that ends with a colon introduces the bullets that follow. It is a lead-in, not an answer.
            if (t.EndsWith(":") && t.Length < 120) { lead = t; cur = null; continue; }

            lead = "";                                                // a heading or paragraph ends the list above it
            cur = new Unit { Text = t, Bullet = false };
            units.Add(cur);
        }
        return units;
    }

    private static string[] Sentences(string text) =>
        Regex.Split(text, @"(?<=[.!?])\s+").Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

    private static void ApplyWholeThought(QueryContext ctx)
    {
        var qTerms = Terms(ctx.Question);
        var howMany = Regex.IsMatch(ctx.Question ?? "", @"\bhow (many|much|often|long)\b", RegexOptions.IgnoreCase);

        Unit? bestUnit = null; string[] bestSentences = Array.Empty<string>();
        int bestIndex = 0, bestPassage = 0; double bestScore = -1;

        for (int i = 0; i < ctx.Passages.Count; i++)
        {
            foreach (var unit in Units(ctx.Passages[i]))
            {
                var sentences = Sentences(unit.Text);
                for (int k = 0; k < sentences.Length; k++)
                {
                    var s = sentences[k];
                    if (s.Length < 25) continue;
                    double score = Terms(s).Count(t => qTerms.Contains(t));
                    if (howMany && Numberish.IsMatch(s)) score += 3;
                    score -= i * 0.01;                       // on a tie, keep the search ranking
                    if (score > bestScore)
                    {
                        bestScore = score; bestUnit = unit; bestSentences = sentences;
                        bestIndex = k; bestPassage = i;
                    }
                }
            }
        }

        string answer;
        if (bestScore <= 0 || bestUnit == null)
        {
            bestPassage = 0;
            answer = ctx.Passages[0].Text;           // nothing matched: the whole first passage (tidied later)
        }
        else
        {
            // The best sentence, with the sentence before it when it starts mid-thought.
            var start = bestIndex;
            if (MidThought(bestSentences[start]) && start > 0) start--;
            var quote = string.Join(" ", bestSentences.Skip(start).Take(bestIndex - start + 1));
            // A bullet's lead-in goes in front when the quote starts at the bullet's first sentence.
            if (bestUnit.Bullet && start == 0 && bestUnit.LeadIn.Length > 0)
                quote = bestUnit.LeadIn + " " + quote;

            if (WholeThought(quote)) answer = quote;
            else
            {
                // Not a whole thought: quote the whole bullet or paragraph, then the whole passage.
                var unitText = bestUnit.Bullet && bestUnit.LeadIn.Length > 0
                    ? bestUnit.LeadIn + " " + bestUnit.Text : bestUnit.Text;
                answer = WholeThought(unitText) ? unitText : ctx.Passages[bestPassage].Text;
            }
        }

        // chosen passage first, so its source is the first one listed
        var ordered = new List<Passage> { ctx.Passages[bestPassage] };
        ordered.AddRange(ctx.Passages.Where((_, i) => i != bestPassage));
        ctx.Passages = ordered;
        ctx.Sources = ordered.Select(p => new SourceRef(p.Document, p.Section, p.Page)).Distinct().ToList();
        ctx.AnswerText = answer;
    }

    // ---------- the old version (rehearsed questions, and the off switch) ----------

    public static void ApplyLegacy(QueryContext ctx)
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
