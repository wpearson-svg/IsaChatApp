using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// Hover definitions (November 9, 2026, Matthew): finds the "Terms explained" glossary terms
/// (onboarding/glossary.json, ISACA's words only) inside a piece of text, so the screen can
/// mark each one and show ISACA's definition when the leader hovers over it, taps it or tabs
/// to it. Used for Isa's chat answers, journey steps and the Today note.
///
/// Display only: the text itself never changes, and nothing here touches Isa's pipeline.
/// Fixed rules, no AI:
///   - "Chapter 360 (C360)" is found as "Chapter 360" and as "C360"; the same for a short
///     form in brackets, like "(SAC)".
///   - A plural with "s" counts too ("Focus Areas").
///   - Single words ("Engage", "Charter", "AGM", "CPE") must match their capital letters, so
///     "charter a plan" is not marked; phrases ("chapter president") match in any case.
///   - "Engage" and "Charter" are also ordinary words, so they aren't marked at the start of a
///     sentence, where the capital letter can't tell "Engage your board" from the platform.
///   - The longest term wins ("Chapter 360 self-assessment" before "Chapter 360").
///   - Only the first time a term appears in a piece of text is marked (one answer, one step
///     title, one step's "What to do"), so an answer that says "Engage" six times marks one
///     (Matthew, November 9, 2026). "Chapter 360" and "C360" count as the same term.
///   - Straight and curly apostrophes count as the same.
/// The glossary file is read again when it changes, like the Library.
/// </summary>
public sealed class GlossaryMatcher
{
    private readonly LibraryService _library;
    private readonly object _lock = new();
    private DateTime _loadedStamp = DateTime.MinValue;
    private Regex? _regex;
    private Dictionary<string, GlossaryTerm> _byKey = new(StringComparer.Ordinal);

    public GlossaryMatcher(LibraryService library) => _library = library;

    /// <summary>The text cut into pieces: plain text, and terms with their glossary entry.
    /// Joining every piece's Text gives back the original text exactly.</summary>
    public IReadOnlyList<GlossarySegment> Split(string? text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        var (regex, byKey) = Load();
        if (regex is null) return [new GlossarySegment(text, null)];

        var pieces = new List<GlossarySegment>();
        var seen = new HashSet<string>(StringComparer.Ordinal);   // first time only (Matthew, November 9)
        var at = 0;
        foreach (Match m in regex.Matches(text))
        {
            var term = Find(m, byKey);
            if (term is null) continue;
            if (OrdinaryWords.Contains(m.Value) && AtSentenceStart(text, m.Index)) continue;
            if (!seen.Add(term.Term)) continue;
            if (m.Index > at) pieces.Add(new GlossarySegment(text[at..m.Index], null));
            pieces.Add(new GlossarySegment(m.Value, term));
            at = m.Index + m.Length;
        }
        if (at < text.Length) pieces.Add(new GlossarySegment(text[at..], null));
        return pieces;
    }

    /// <summary>The glossary entry with exactly this name ("Chapter Secretary"), or null.
    /// Used where the screen already knows which term a word stands for, such as a board
    /// role in "Meet your board" ("Secretary" is ISACA's "Chapter Secretary").</summary>
    public GlossaryTerm? FindTerm(string name) =>
        _library.GetGlossary().FirstOrDefault(t => string.Equals(t.Term, name, StringComparison.OrdinalIgnoreCase));

    // Terms that are also ordinary English words. At the start of a sentence the capital letter
    // doesn't tell them apart ("Engage your board" is a verb, not ISACA's platform), so there
    // they are never marked (Matthew, November 9, 2026). A wrong definition misleads; a missed
    // one costs little, and a later mention in the same text is still marked.
    private static readonly HashSet<string> OrdinaryWords = new(StringComparer.Ordinal)
    {
        "Engage", "Engages", "Charter", "Charters",
    };

    // True when only spaces stand between this place and the start of the text, a line break,
    // or the end of a sentence (. ! ? :) or a list mark (• - –).
    private static bool AtSentenceStart(string text, int index)
    {
        var i = index - 1;
        while (i >= 0 && text[i] is ' ' or '\t' or '“' or '"' or '(') i--;
        return i < 0 || text[i] is '.' or '!' or '?' or ':' or '\n' or '\r' or '•' or '-' or '–';
    }

    // Which term a match belongs to: the group name says which key matched.
    private static GlossaryTerm? Find(Match m, Dictionary<string, GlossaryTerm> byKey)
    {
        foreach (Group g in m.Groups)
            if (g.Success && g.Name.StartsWith("k", StringComparison.Ordinal) && byKey.TryGetValue(g.Name, out var t))
                return t;
        return null;
    }

    private (Regex?, Dictionary<string, GlossaryTerm>) Load()
    {
        var stamp = _library.GlossaryStamp();
        lock (_lock)
        {
            if (stamp == _loadedStamp && _regex is not null) return (_regex, _byKey);

            var keys = new List<(string Key, GlossaryTerm Term)>();
            foreach (var t in _library.GetGlossary())
                foreach (var form in Forms(t.Term))
                    keys.Add((form, t));

            // Longest first, so the regex tries "Chapter 360 self-assessment" before "Chapter 360".
            var byKey = new Dictionary<string, GlossaryTerm>(StringComparer.Ordinal);
            var parts = new List<string>();
            var i = 0;
            foreach (var (key, term) in keys.DistinctBy(k => k.Key, StringComparer.OrdinalIgnoreCase)
                                            .OrderByDescending(k => k.Key.Length))
            {
                var name = "k" + i++;
                byKey[name] = term;
                var body = Pattern(key);
                // One word: its capital letters must match. A phrase: any case.
                var oneWord = !key.Contains(' ');
                parts.Add(oneWord ? $"(?<{name}>{body})" : $"(?<{name}>(?i:{body}))");
            }
            _regex = parts.Count == 0 ? null
                : new Regex(@"(?<![\p{L}\p{N}-])(?:" + string.Join("|", parts) + @")(?![\p{L}\p{N}-])",
                            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));
            _byKey = byKey;
            _loadedStamp = stamp;
            return (_regex, _byKey);
        }
    }

    // "Chapter 360 (C360)" -> "Chapter 360 (C360)", "Chapter 360", "C360".
    private static IEnumerable<string> Forms(string term)
    {
        var clean = term.Trim().Trim('“', '”', '"');
        yield return clean;
        var bracket = Regex.Match(clean, @"^(?<long>.+?)\s*\((?<short>[^)]+)\)$");
        if (bracket.Success)
        {
            yield return bracket.Groups["long"].Value.Trim();
            yield return bracket.Groups["short"].Value.Trim();
        }
    }

    // The key as a pattern: spaces match any run of spaces, either apostrophe matches,
    // and a plural "s" is allowed at the end of a term that ends in a letter.
    private static string Pattern(string key)
    {
        var words = Regex.Split(key, @"\s+").Select(w =>
            string.Concat(w.Select(c => c is '\'' or '’' ? "['’]" : Regex.Escape(c.ToString()))));
        var body = string.Join(@"\s+", words);
        var last = key[^1];
        return char.IsLetter(last) && last != 's' ? body + "s?" : body;
    }
}

/// <summary>A piece of text: plain (Term is null) or a glossary term.</summary>
public sealed record GlossarySegment(string Text, GlossaryTerm? Term);
