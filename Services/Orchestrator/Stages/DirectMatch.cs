using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Orchestrator.Stages;

/// <summary>
/// Does a full-text hit really answer the question? No AI: fixed word lists and counting.
///
/// The full-text search finds passages that contain the question's words, but a passage is a whole section, so the words
/// can come from different requirements ("How many members must attend each educational event?" matched the Annual Review
/// section through "attendance records", "educational events" and "members", three different requirements). So the
/// search also scores the sentence Isa would actually quote:
///
///   1. The question's meaningful words: every word except linking words (the, should, an, on...), question words
///      (how many, what, who...) and general-purpose verbs (hold, make, take, use...). "chapter's" counts as "chapter".
///   2. Each word is weighted by how rare it is in Isa's approved content: a word in almost every passage ("chapter",
///      "leader") counts for little, a rare one ("compilation", "appendix") for a lot.
///   3. Score = the weight of the meaningful words found in the quote / the weight of all of them (0 to 1).
///
/// Below the direct-answer match mark (Dev Dashboard slider), the hit is not a direct answer: the question goes on to
/// search by meaning, where the search pass mark and the AI's checks apply.
///
/// Words are compared on their first five letters, so meeting/meetings, review/reviews, accept/accepted count as one.
/// </summary>
public static class DirectMatch
{
    // Linking words (articles, pronouns, prepositions, conjunctions, helping verbs): a standard English stop-word list.
    // Then question words, and general-purpose verbs and adjectives that say nothing about the topic.
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "a","about","above","after","again","against","all","am","an","and","any","are","as","at","be","because","been",
        "before","being","below","between","both","but","by","can","could","did","do","does","doing","down","during","each",
        "few","for","from","further","had","has","have","having","he","her","here","hers","herself","him","himself","his",
        "i","if","in","into","is","it","its","itself","just","me","might","more","most","must","my","myself","no","nor","not",
        "now","of","off","on","once","only","or","other","our","ours","ourselves","out","over","own","same","shall","she",
        "should","so","some","such","than","that","the","their","theirs","them","themselves","then","there","these","they",
        "this","those","through","to","too","under","until","up","very","was","we","were","while","will","with","would",
        "you","your","yours","yourself","yourselves","us","let","may","also",
        // question words
        "how","what","when","where","which","who","whom","whose","why",
        // general-purpose verbs and adjectives
        "get","got","need","needs","hold","holds","held","make","makes","made","take","takes","took","use","uses","used",
        "keep","keeps","give","gives","put","want","wants","go","goes","come","comes","know","usually","usual","new",
        "good","best","really"
    };

    // "how many / much / often / long" asks for a kind of answer (a number, a length of time), not about a topic.
    private static readonly Regex AnswerType = new(@"\bhow (many|much|often|long)\b", RegexOptions.IgnoreCase);
    private static readonly Regex Possessive = new(@"['’]s\b", RegexOptions.IgnoreCase);
    private static readonly Regex Word = new("[A-Za-z0-9]+");

    private static IEnumerable<string> Words(string? text) =>
        Word.Matches(Possessive.Replace(text ?? "", "")).Select(m => m.Value.ToLowerInvariant());

    private static string Key(string w) => w.Length > 5 ? w[..5] : w;

    /// <summary>The question's meaningful words, as full words, for the full-text search.</summary>
    public static List<string> SearchWords(string? question) =>
        Words(AnswerType.Replace(question ?? "", " ")).Where(w => w.Length > 1 && !Stop.Contains(w)).Distinct().ToList();

    private static List<string> Keys(string? question) => SearchWords(question).Select(Key).Distinct().ToList();

    // ---------- word weights (how rare each word is in the approved content) ----------

    private sealed record WeightTable(string Path, DateTime Stamp, int Passages, Dictionary<string, int> DocFreq);
    private static WeightTable? _weights;
    private static readonly object WeightLock = new();

    private static WeightTable Weights(string contentDbPath)
    {
        var stamp = File.Exists(contentDbPath) ? File.GetLastWriteTimeUtc(contentDbPath) : DateTime.MinValue;
        lock (WeightLock)
        {
            if (_weights is { } w && w.Path == contentDbPath && w.Stamp == stamp) return w;
            var df = new Dictionary<string, int>();
            var n = 0;
            using (var conn = new SqliteConnection($"Data Source={contentDbPath};Mode=ReadOnly"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT text FROM passages;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    n++;
                    foreach (var k in Words(reader.GetString(0)).Where(x => !Stop.Contains(x)).Select(Key).ToHashSet())
                        df[k] = df.TryGetValue(k, out var c) ? c + 1 : 1;
                }
            }
            return _weights = new WeightTable(contentDbPath, stamp, n, df);
        }
    }

    private static double Weight(WeightTable t, string key) =>
        Math.Log(1.0 + (double)t.Passages / ((t.DocFreq.TryGetValue(key, out var c) ? c : 0) + 1));

    /// <summary>
    /// How well the quote covers the question's meaningful words, weighted by rarity (0 to 1), and the words it is missing.
    /// A question with no meaningful words scores 0.
    /// </summary>
    public static (double Score, List<string> Missing) Score(string? question, string? quote, string contentDbPath)
    {
        var keys = Keys(question);
        if (keys.Count == 0) return (0, new List<string>());
        var table = Weights(contentDbPath);
        var present = Words(quote).Select(Key).ToHashSet();
        double total = 0, found = 0;
        var missing = new List<string>();
        foreach (var k in keys)
        {
            var w = Weight(table, k);
            total += w;
            if (present.Contains(k)) found += w; else missing.Add(k);
        }
        return (total > 0 ? Math.Round(found / total, 2) : 0, missing);
    }
}
