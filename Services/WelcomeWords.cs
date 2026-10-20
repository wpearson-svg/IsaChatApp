namespace IsaChatApp.Services;

/// <summary>
/// The word "Welcome" in the signed-in person's own language, for the
/// greeting at the top of the landing page ("ようこそ, Watanabe-san").
/// Matthew asked for this on 20 October 2026, to show ISACA that Isa greets
/// each person in their language of preference. Only this one word is
/// translated; the name is the person's form of address exactly as it is
/// in the directory, and the rest of Isa stays in English.
///
/// The language comes from the person's sign-in (the "locale" claim, e.g.
/// "ja-JP"), which comes from the Language column in directory/people.csv.
///
/// Every word is fixed here, never written by an AI model. Where the usual
/// word for "welcome" changes with the person's gender (Spanish,
/// Portuguese, Icelandic), a form that doesn't is used, because the
/// directory doesn't record gender. These were not checked by native
/// speakers; have them checked before showing them to ISACA.
/// </summary>
public static class WelcomeWords
{
    private static readonly Dictionary<string, Word> ByLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new("en", "Welcome"),
        ["ja"] = new("ja", "ようこそ"),
        ["ar"] = new("ar", "أهلاً وسهلاً"),        // "ahlan wa sahlan": welcome, for anyone
        ["id"] = new("id", "Selamat datang"),
        ["es"] = new("es", "Le damos la bienvenida"), // "we welcome you", formal; "Bienvenido/a" would need gender
        ["is"] = new("is", "Góðan daginn"),          // "good day"; "Velkomin/Velkominn" would need gender
        ["de"] = new("de", "Willkommen"),
        ["fr"] = new("fr", "Bienvenue"),
        ["pt"] = new("pt", "Boas-vindas"),           // "Bem-vindo/a" would need gender
    };

    /// <summary>A welcome word and the language it is in (for the page's lang attribute).</summary>
    public record Word(string Lang, string Text);

    /// <summary>
    /// The welcome word for a language code such as "ja-JP" or "pt-BR".
    /// Uses the language part ("ja", "pt"); anything unknown or missing
    /// falls back to English.
    /// </summary>
    public static Word For(string? locale)
    {
        var lang = (locale ?? "").Split('-', '_')[0];
        return ByLanguage.TryGetValue(lang, out var word) ? word : ByLanguage["en"];
    }
}
