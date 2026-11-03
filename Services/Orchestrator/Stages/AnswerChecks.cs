using System.Text.Json;
using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Orchestrator.Stages;

/// <summary>
/// The fixed checks on an AI-written answer (no AI involved), from Matthew's Output Compliance proposal (November 2, 2026):
///   numbers  - every date, number, amount and percentage in the answer must be in the passage it comes from ("November 1", "1 Nov" and "1 November" count as the same date; "4" and "four" as the same number);
///   strength - "must" stays "must": the answer may not use a strength (obligation, advice, permission, prohibition) its passage does not have;
///   contact  - any email address or phone number in the answer must appear in the passage;
///   shape    - a "how many" question needs a number in the answer, a "when" question a date or period, a "who" question a role or team.
/// Each returns what failed (for the log: counts and group names, never the text of an email address or number).
/// Accuracy failures (numbers, strength, contact) step down to ISACA's exact words; a shape failure goes to a person, because the passage is the problem.
/// </summary>
public static class AnswerChecks
{
    // ---------- numbers ----------

    private static readonly string[] Months =
        { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };

    private static readonly Dictionary<string, int> NumberWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5, ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
        ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14, ["fifteen"] = 15, ["twenty"] = 20, ["thirty"] = 30,
        ["forty"] = 40, ["fifty"] = 50, ["sixty"] = 60, ["ninety"] = 90, ["hundred"] = 100,   // "one" is left out: it is mostly the pronoun
    };

    private const string MonthPattern = @"(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\.?";
    private static readonly Regex DayMonth = new(@"\b(\d{1,2})(?:st|nd|rd|th)?\s+" + MonthPattern + @"(?![a-z])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex MonthDay = new(@"\b" + MonthPattern + @"\s+(\d{1,2})(?:st|nd|rd|th)?\b(?!\s*%)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex MonthOnly = new(@"\b(january|february|march|april|june|july|august|september|october|november|december)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Percent = new(@"(\d+(?:\.\d+)?)\s?(?:%|percent\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Amount = new(@"(?:[$€£]\s?|\b(?:usd|eur|gbp)\s?)(\d[\d,]*(?:\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Digits = new(@"(?<![\w.])(\d[\d,]*(?:\.\d+)?)(?:st|nd|rd|th)?(?![\w])", RegexOptions.CultureInvariant);
    private static readonly Regex WordNumber = new(@"\b(" + string.Join("|", NumberWords.Keys) + @")\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex ListMarker = new(@"(?m)^\s*\d+[.)]\s");

    private static int MonthNumber(string m)
    {
        var s = m.ToLowerInvariant().TrimEnd('.');
        for (var i = 0; i < Months.Length; i++) if (Months[i].StartsWith(s.Length >= 3 ? s[..3] : s)) return i + 1;
        return 0;
    }

    /// <summary>The dates, amounts, percentages and numbers in a text, each in one standard form.</summary>
    public static HashSet<string> Facts(string text)
    {
        var facts = new HashSet<string>();
        var t = ListMarker.Replace(text ?? "", " ");
        t = Regex.Replace(t, @"\b(\d{4})-(\d{2})-(\d{2})\b", m =>
        {   // an ISO date
            facts.Add($"d{int.Parse(m.Groups[2].Value)}-{int.Parse(m.Groups[3].Value)}"); facts.Add($"m{int.Parse(m.Groups[2].Value)}"); facts.Add("n" + m.Groups[1].Value);
            return " ";
        });
        t = DayMonth.Replace(t, m => { AddDate(facts, MonthNumber(m.Groups[2].Value), int.Parse(m.Groups[1].Value)); return " "; });
        t = MonthDay.Replace(t, m => { AddDate(facts, MonthNumber(m.Groups[1].Value), int.Parse(m.Groups[2].Value)); return " "; });
        t = MonthOnly.Replace(t, m => { facts.Add("m" + MonthNumber(m.Value)); return " "; });
        t = Percent.Replace(t, m => { facts.Add("p" + Norm(m.Groups[1].Value)); return " "; });
        t = Amount.Replace(t, m => { facts.Add("a" + Norm(m.Groups[1].Value)); return " "; });
        foreach (Match m in Digits.Matches(t)) facts.Add("n" + Norm(m.Groups[1].Value));
        foreach (Match m in WordNumber.Matches(t)) facts.Add("n" + NumberWords[m.Value]);
        return facts;
    }

    private static void AddDate(HashSet<string> facts, int month, int day)
    {
        if (month == 0) return;
        facts.Add($"d{month}-{day}"); facts.Add($"m{month}");
    }

    private static string Norm(string n) => n.Replace(",", "").TrimEnd('.').TrimStart('0') is { Length: > 0 } s ? (s.StartsWith('.') ? "0" + s : s) : "0";

    /// <summary>How many dates or numbers in the answer are not in the passage. A month on its own is satisfied by any date in that month.</summary>
    public static int NumbersNotInSource(string answer, string source)
    {
        var inSource = Facts(source);
        var missing = 0;
        foreach (var f in Facts(answer))
        {
            if (inSource.Contains(f)) continue;
            // a bare month in the answer is fine when the passage has a date in that month
            if (f[0] == 'm' && inSource.Any(s => s == f)) continue;
            missing++;
        }
        return missing;
    }

    // ---------- "must" stays "must" ----------

    private sealed class Words
    {
        public List<(string Group, string Phrase)> Phrases = new();
    }

    private static Words? _words;
    private static readonly object WordsLock = new();

    private static Words LoadWords()
    {
        if (_words is not null) return _words;
        lock (WordsLock)
        {
            if (_words is not null) return _words;
            var w = new Words();
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "content", "answer_strength.json");
                if (!File.Exists(path)) path = Path.Combine("content", "answer_strength.json");
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                foreach (var group in new[] { "prohibition", "obligation", "advice", "permission", "negated" })
                    if (doc.RootElement.TryGetProperty(group, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        foreach (var p in arr.EnumerateArray())
                            if (p.GetString() is { Length: > 0 } phrase) w.Phrases.Add((group, phrase.ToLowerInvariant()));
            }
            catch
            {
                // No word file: use a short built-in list so the check still works.
                foreach (var p in new[] { "must not", "may not", "shall not", "never" }) w.Phrases.Add(("prohibition", p));
                foreach (var p in new[] { "must", "required", "shall", "need to" }) w.Phrases.Add(("obligation", p));
                foreach (var p in new[] { "should", "recommended", "encouraged" }) w.Phrases.Add(("advice", p));
                foreach (var p in new[] { "may", "can" }) w.Phrases.Add(("permission", p));
                foreach (var p in new[] { "not required", "need not" }) w.Phrases.Add(("negated", p));
            }
            w.Phrases = w.Phrases.OrderByDescending(p => p.Phrase.Length).ToList();   // longest first, so "must not" is not counted as "must"
            _words = w;
            return w;
        }
    }

    /// <summary>The strengths a text uses: obligation, advice, permission, prohibition, negated.</summary>
    public static HashSet<string> Strengths(string text)
    {
        var found = new HashSet<string>();
        var t = (text ?? "").ToLowerInvariant().Replace('’', '\'');
        foreach (var (group, phrase) in LoadWords().Phrases)
        {
            var rx = new Regex(@"(?<![a-z])" + Regex.Escape(phrase) + @"(?![a-z])", RegexOptions.CultureInvariant);
            if (rx.IsMatch(t)) { found.Add(group); t = rx.Replace(t, " "); }   // used up, so a shorter phrase inside it does not count again
        }
        return found;
    }

    /// <summary>The strengths the answer uses that its passage does not (a dropped or added "not" counts as a mismatch). Empty = fine.</summary>
    public static List<string> StrengthChanges(string answer, string source)
    {
        var allowed = Strengths(source);
        var bad = new HashSet<string>();
        foreach (var sentence in OutputChecks.ClaimSentences(answer))
        {
            var used = Strengths(sentence);
            foreach (var g in used) if (!allowed.Contains(g)) bad.Add(g);
            // a "not" the passage lacks, or one it has that the sentence dropped while still using a strength
            if (used.Contains("negated") != allowed.Contains("negated") && used.Count > 0) bad.Add("negated");
        }
        return bad.OrderBy(g => g, StringComparer.Ordinal).ToList();
    }

    // ---------- contact details ----------

    private static readonly Regex Email = new(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.CultureInvariant);
    private static readonly Regex Phone = new(@"(?<![\d])\+?\(?\d[\d ().\-]{6,}\d(?![\d])", RegexOptions.CultureInvariant);
    private static readonly Regex IsoDate = new(@"\b\d{4}-\d{2}-\d{2}\b");

    /// <summary>How many email addresses or phone numbers in the answer do not appear in the passage. The log gets the count only.</summary>
    public static int ContactsNotInSource(string answer, string source)
    {
        var missing = 0;
        var src = (source ?? "").ToLowerInvariant();
        foreach (Match m in Email.Matches(answer ?? ""))
            if (!src.Contains(m.Value.ToLowerInvariant())) missing++;
        var srcDigits = new string((source ?? "").Where(char.IsDigit).ToArray());
        foreach (Match m in Phone.Matches(IsoDate.Replace(answer ?? "", " ")))
        {
            var digits = new string(m.Value.Where(char.IsDigit).ToArray());
            if (digits.Length >= 7 && !srcDigits.Contains(digits)) missing++;
        }
        return missing;
    }

    // ---------- answer shape ----------

    private static readonly string[] CountWords = { "once", "twice", "monthly", "annually", "weekly", "quarterly", "yearly", "daily", "every", "each", "per" };
    private static readonly string[] WhenWords = { "annually", "quarterly", "monthly", "weekly", "yearly", "every", "each", "before", "after", "within", "by", "between", "deadline", "window", "period", "end of", "start of" };
    private static readonly string[] WhoWords =
        { "president", "vice president", "treasurer", "secretary", "director", "board", "committee", "team", "officer", "chair", "leader", "member", "staff", "isaca", "chapter", "volunteer", "experience" };

    /// <summary>
    /// Does the answer have the shape the question asks for? "How many/much/often" needs a number, "when" a date or period, "who" a role or team.
    /// Returns null when fine (or the question is not one of these kinds), otherwise the kind that is missing: count, when or who.
    /// </summary>
    public static string? ShapeMissing(string question, string answer)
    {
        var q = (question ?? "").ToLowerInvariant();
        var a = (answer ?? "").ToLowerInvariant();
        var hasNumber = Regex.IsMatch(a, @"\d") || WordNumber.IsMatch(a) || Regex.IsMatch(a, @"\bone\b");
        if (Regex.IsMatch(q, @"\bhow (many|much|often|long)\b"))
            return hasNumber || CountWords.Any(w => Regex.IsMatch(a, @"\b" + w + @"\b")) ? null : "count";
        if (Regex.IsMatch(q, @"\b(when|by when|what date|what time|deadline)\b"))
            return hasNumber || MonthOnly.IsMatch(a) || WhenWords.Any(w => Regex.IsMatch(a, @"\b" + Regex.Escape(w) + @"\b")) ? null : "when";
        if (Regex.IsMatch(q, @"^\s*who\b|\bwho (is|are|does|do|should|can|writes|keeps|handles|approves|signs)\b"))
            return WhoWords.Any(w => Regex.IsMatch(a, @"\b" + Regex.Escape(w))) ? null : "who";
        return null;
    }
}
