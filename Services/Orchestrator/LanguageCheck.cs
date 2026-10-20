namespace IsaChatApp.Services.Orchestrator;

/// <summary>
/// The quick language check (task T4W, stage contract sections 1 and 2).
/// The front end runs it on the question before the query enters the
/// pipeline, and puts the result on the query context (ctx.Language).
/// English skips the translation stage.
///
/// It is deliberately small and fast: no AI model, no web call, no package.
/// It looks at the writing system first (Cyrillic, Arabic, Chinese ...), then,
/// for Latin letters, counts common words of English, Spanish, French,
/// German, Portuguese and Italian. It returns a two-letter code, or "und"
/// when it cannot tell (too short, or no clear winner). "und" counts as NOT
/// English, so translation runs and the real translator makes the call.
/// It never logs or keeps the question text.
/// </summary>
public static class LanguageCheck
{
    private static readonly Dictionary<string, HashSet<string>> Words = new()
    {
        ["en"] = Set("the is are was were what how who where when why which do does did can could must should would my our we you your to of and in for with this that it be have has chapter board meeting meetings member members dues officer officers bylaws event events each year"),
        ["es"] = Set("el los las y un una cuántas cuántos cómo qué dónde quién cuándo debe deben por para es son del se año junta reuniones capítulo política puedo tengo"),
        ["fr"] = Set("le les des est qui quoi comment combien où pour dans une sont doit chaque réunion réunions c'est je nous vous du au ne pas chapitre quelle quel"),
        ["de"] = Set("der die das und ist wie wer wo was nicht ein eine für mit muss jedes jahr sind ich wir sitzung kapitel"),
        ["pt"] = Set("uma é são como quem onde quantas quantos deve não ano reuniões você os capítulo"),
        ["it"] = Set("il gli è sono come chi dove quante quanti per non anno ogni riunioni devo dobbiamo capitolo"),
    };

    // Accented letters and marks that point to one language.
    private static readonly (char Ch, string Lang, int Weight)[] Hints =
    {
        ('¿', "es", 2), ('¡', "es", 2), ('ñ', "es", 2),
        ('ß', "de", 2), ('ä', "de", 1), ('ö', "de", 1), ('ü', "de", 1),
        ('ã', "pt", 2), ('õ', "pt", 2),
        ('è', "fr", 1), ('ê', "fr", 1), ('à', "fr", 1), ('œ', "fr", 2),
    };

    /// <summary>Two-letter language code of the text, or "und" when it can't tell.</summary>
    public static string Detect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "und";

        // 1. Writing system. If a large share of the letters are not Latin, say so.
        int letters = 0, han = 0, kana = 0, hangul = 0, cyrillic = 0, arabic = 0, devanagari = 0, hebrew = 0, greek = 0, thai = 0;
        foreach (var c in text)
        {
            if (!char.IsLetter(c)) continue;
            letters++;
            if (c >= '一' && c <= '鿿') han++;
            else if (c >= '぀' && c <= 'ヿ') kana++;
            else if ((c >= '가' && c <= '힯') || (c >= 'ᄀ' && c <= 'ᇿ')) hangul++;
            else if (c >= 'Ѐ' && c <= 'ӿ') cyrillic++;
            else if (c >= '؀' && c <= 'ۿ') arabic++;
            else if (c >= 'ऀ' && c <= 'ॿ') devanagari++;
            else if (c >= '֐' && c <= '׿') hebrew++;
            else if (c >= 'Ͱ' && c <= 'Ͽ') greek++;
            else if (c >= '฀' && c <= '๿') thai++;
        }
        if (letters == 0) return "und";

        bool Mostly(int n) => n * 10 >= letters * 3;
        if (kana > 0 && Mostly(han + kana)) return "ja";
        if (Mostly(han)) return "zh";
        if (Mostly(hangul)) return "ko";
        if (Mostly(cyrillic)) return "ru";
        if (Mostly(arabic)) return "ar";
        if (Mostly(devanagari)) return "hi";
        if (Mostly(hebrew)) return "he";
        if (Mostly(greek)) return "el";
        if (Mostly(thai)) return "th";

        // 2. Latin letters: count the common words of each language.
        var score = Words.Keys.ToDictionary(k => k, _ => 0);
        var lower = text.ToLowerInvariant();

        foreach (var raw in lower.Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', ':', '!', '?', '¿', '¡', '(', ')', '"', '/' },
                                        StringSplitOptions.RemoveEmptyEntries))
        {
            var w = raw.Trim('\'', '’', '-');
            if (w.EndsWith("'s") || w.EndsWith("’s")) w = w[..^2];   // where's -> where
            foreach (var (lang, set) in Words)
                if (set.Contains(w)) score[lang]++;
        }
        foreach (var (ch, lang, weight) in Hints)
            if (lower.Contains(ch)) score[lang] += weight;

        var ranked = score.OrderByDescending(kv => kv.Value).ToList();
        if (ranked[0].Value == 0) return "und";
        if (ranked[0].Value == ranked[1].Value) return "und";   // a tie is not an answer
        return ranked[0].Key;
    }

    private static HashSet<string> Set(string words) =>
        new(words.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
