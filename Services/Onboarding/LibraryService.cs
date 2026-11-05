using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// The Library (Feature 5 in ONBOARDING-FEATURE.md): the ISACA documents Isa answers from,
/// to read or listen to word for word.
///
/// The list, groups and descriptions come from onboarding/library.json. The text comes from
/// data/content.db, the same approved passages Isa answers from, in their stored order, so
/// nothing is rewritten or summarised and nothing can be wrong that isn't in the approved
/// document. The text is split into sentences here so the panel can highlight the sentence
/// being read (Components/Shared/LibraryPanel.razor). Nothing comes from the AI model.
///
/// Documents marked "source": "chapter360" in library.json (Matthew, 28 October 2026) come
/// instead from ISACA's Chapter 360 documents (Chapter360Service, onboarding/chapter360.json).
/// They are read and played the same way, but Isa doesn't answer questions from them yet,
/// so the Library says so.
///
/// Documents marked "source": "text" (Matthew, 3 November 2026) come from a file in
/// onboarding/library_texts/, named after the document's id, built from ISACA's own file by a
/// script beside it (the first is the Chapter Affiliation Agreement,
/// onboarding/build_affiliation_agreement.py). Read and played the same way; Isa doesn't
/// answer questions from them either.
///
/// Listen on journey steps (3 November 2026): onboarding/step_listen.json says which part of
/// which Library document each step points to (a guidebook page, or a whole document).
/// GetStepListens gives a step's buttons; GetRange gives the sentences to read, so the Library
/// reads that part and stops.
///
/// "Terms explained" (3 November 2026, piece D in ONBOARDING-FEATURE.md): GetGlossary reads
/// onboarding/glossary.json, terms that ISACA's documents define, each quoted word for word
/// (onboarding/check_glossary.py checks every quote against the same text the Library shows).
/// GetQuoteRange finds a quote's sentences in its document, so "Read it" opens right there.
/// </summary>
public class LibraryService
{
    // A comfortable listening pace, used only for "about N minutes to listen".
    private const double WordsPerMinute = 150;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    // A sentence ends at . ! or ? followed by a space and a capital, digit, quote or bracket.
    private static readonly Regex SentenceBreak = new(@"(?<=[.!?][""”’)]?)\s+(?=[A-Z0-9""“‘(])", RegexOptions.Compiled);

    // Section names the PDF import made up from page numbers ("Guidebook page 3").
    // They are shown as a quiet page marker, not read aloud as a heading.
    private static readonly Regex PageSection = new(@"^Guidebook page (\d+)$", RegexOptions.Compiled);

    private const string Chapter360Source = "chapter360";
    private const string TextSource = "text";

    private static readonly JsonSerializerOptions TextOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string _listPath;
    private readonly string _textsFolder;
    private readonly string _stepListenPath;
    private readonly string _glossaryPath;
    private readonly string _contentDbPath;
    private readonly Chapter360Service _chapter360;

    public LibraryService(IWebHostEnvironment env, IConfiguration config, Chapter360Service chapter360)
    {
        _listPath = Path.Combine(env.ContentRootPath, "onboarding", "library.json");
        _textsFolder = Path.Combine(env.ContentRootPath, "onboarding", "library_texts");
        _stepListenPath = Path.Combine(env.ContentRootPath, "onboarding", "step_listen.json");
        _glossaryPath = Path.Combine(env.ContentRootPath, "onboarding", "glossary.json");
        _contentDbPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
        _chapter360 = chapter360;
    }

    /// <summary>The Library's documents, in order, with how long each takes to listen to.
    /// A document missing from content.db is left out, so the Library never offers text it can't play.</summary>
    public IReadOnlyList<LibraryDocument> GetDocuments()
    {
        List<LibraryEntry> entries;
        try
        {
            entries = JsonSerializer.Deserialize<LibraryFile>(File.ReadAllText(_listPath), Options)?.Documents ?? [];
        }
        catch
        {
            return [];   // a broken list file shows an empty Library rather than breaking the chat
        }

        var words = WordCounts();
        var c360 = _chapter360.Data.Documents.ToDictionary(d => d.Id, d => d.Words);
        var list = new List<LibraryDocument>();
        foreach (var e in entries)
        {
            var count = e.Source switch
            {
                Chapter360Source => c360.GetValueOrDefault(e.Document),
                TextSource => LoadText(e.Document)?.Words ?? 0,
                _ => words.GetValueOrDefault(e.Document),
            };
            if (count == 0) continue;   // not loaded: never offer text the Library can't play
            list.Add(new LibraryDocument(e.Document, e.Title ?? e.Document, e.Group ?? "", e.Description ?? "", e.Date,
                Math.Max(1, (int)Math.Round(count / WordsPerMinute)), AnswersFrom: e.Source is not (Chapter360Source or TextSource),
                Source: e.Source, Note: e.Note));
        }
        return list;
    }

    /// <summary>One document's text, split into sections, paragraphs and numbered sentences,
    /// or null if it isn't one of the Library's documents.</summary>
    public LibraryText? GetText(string document)
    {
        var doc = GetDocuments().FirstOrDefault(d => d.Document == document);
        if (doc is null) return null;
        var sections = doc.Source switch
        {
            Chapter360Source => Chapter360Sections(document),
            TextSource => TextSections(document),
            _ => ContentDbSections(document),
        };
        return sections is null ? null : new LibraryText(doc, Number(sections));
    }

    // ---- Listen on journey steps ----------------------------------------------

    /// <summary>A journey step's Listen buttons, in the order onboarding/step_listen.json lists
    /// them, each with about how long it takes. A document not in the Library, or a page it
    /// doesn't have, gets no button, so a step never offers something that can't play.</summary>
    public IReadOnlyList<StepListen> GetStepListens(string stepId)
    {
        List<StepListenEntry> entries;
        try
        {
            entries = JsonSerializer.Deserialize<StepListenFile>(File.ReadAllText(_stepListenPath), Options)?.Listen ?? [];
        }
        catch
        {
            return [];   // a missing or broken file shows no buttons rather than breaking the step
        }

        var list = new List<StepListen>();
        foreach (var e in entries.Where(e => e.Step == stepId))
        {
            var text = GetText(e.Document);
            if (text is null) continue;
            var range = GetRange(text, e.Pages);
            if (range is null) continue;
            var words = text.Sections
                .SelectMany(s => (s.Heading is null ? [] : new[] { new LibrarySentence(s.HeadingIndex ?? -1, s.Heading) })
                    .Concat(s.Paragraphs.SelectMany(p => p.Sentences)))
                .Where(x => x.Index >= range.Value.From && x.Index <= range.Value.To)
                .Sum(x => x.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length);
            list.Add(new StepListen(e.Document, e.Pages, e.Label ?? text.Document.Title,
                Math.Max(1, (int)Math.Round(words / WordsPerMinute))));
        }
        return list;
    }

    /// <summary>The first and last sentence numbers to read: the given guidebook page or pages
    /// ("13" or "13-14"), or the whole document when pages is empty. Null if the document has
    /// no such page.</summary>
    public static (int From, int To)? GetRange(LibraryText text, string? pages)
    {
        var numbers = text.Sections
            .Where(s => InPages(s.Page, pages))
            .SelectMany(s => (s.HeadingIndex is int h ? new[] { h } : [])
                .Concat(s.Paragraphs.SelectMany(p => p.Sentences.Select(x => x.Index))))
            .ToList();
        return numbers.Count == 0 ? null : (numbers.Min(), numbers.Max());
    }

    private static bool InPages(int? page, string? pages)
    {
        if (string.IsNullOrWhiteSpace(pages)) return true;
        if (page is null) return false;
        var parts = pages.Split('-', StringSplitOptions.TrimEntries);
        if (!int.TryParse(parts[0], out var from)) return false;
        var to = parts.Length > 1 && int.TryParse(parts[1], out var t) ? t : from;
        return page >= from && page <= to;
    }

    // ---- Terms explained (the glossary) ----------------------------------------

    /// <summary>The glossary, A to Z, each term with ISACA's quote and the Library document it
    /// comes from. A term whose document isn't in the Library is left out.</summary>
    public IReadOnlyList<GlossaryTerm> GetGlossary()
    {
        List<GlossaryEntry> entries;
        try
        {
            entries = JsonSerializer.Deserialize<GlossaryFile>(File.ReadAllText(_glossaryPath), Options)?.Terms ?? [];
        }
        catch
        {
            return [];   // a missing or broken file shows an empty glossary rather than breaking the Library
        }
        var docs = GetDocuments().ToDictionary(d => d.Document);
        return entries
            .Where(e => e.Quote.Count > 0 && docs.ContainsKey(e.Document))
            .Select(e => new GlossaryTerm(e.Term, e.Quote, e.Bullets, docs[e.Document], e.Pages, e.Where))
            .OrderBy(t => t.Term.TrimStart('“', '"'), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>The sentences of a glossary quote in its document (on the given guidebook page, if
    /// any): from the first sentence the quote starts in to the last one it covers. Null if the quote
    /// can't be found (then "Read it" opens the page, or the top of the document).</summary>
    public static (int From, int To)? GetQuoteRange(LibraryText text, IReadOnlyList<string> quotes, string? pages = null)
    {
        static string Norm(string s) => Regex.Replace(s, @"\s+", " ").Trim();
        var sentences = text.Sections.Where(s => InPages(s.Page, pages))
            .SelectMany(s => s.Paragraphs.SelectMany(p => p.Sentences)).ToList();
        int? from = null, to = null;
        foreach (var quote in quotes.Select(Norm))
        {
            // A long quote covers whole sentences; a short one (like "Annual General Meeting (AGM)")
            // sits inside one sentence, and only its first place counts.
            var covered = sentences.Where(s => Norm(s.Text).Length >= 12 && quote.Contains(Norm(s.Text))).ToList();
            if (covered.Count == 0 && sentences.FirstOrDefault(s => Norm(s.Text).Contains(quote)) is { } inside) covered.Add(inside);
            foreach (var s in covered)
            {
                from = from is null ? s.Index : Math.Min(from.Value, s.Index);
                to = to is null ? s.Index : Math.Max(to.Value, s.Index);
            }
        }
        return from is null ? null : (from.Value, to!.Value);
    }

    // A Chapter 360 document: its sections and paragraphs as onboarding/chapter360.json has them.
    private List<LibrarySection>? Chapter360Sections(string id)
    {
        var source = _chapter360.GetDocument(id);
        if (source is null) return null;
        var sections = new List<LibrarySection>();
        var key = 0;
        foreach (var s in source.Sections)
        {
            var section = new LibrarySection($"s{key++}", s.Heading, null, []);
            foreach (var p in s.Paragraphs) AddLine(section, p.Bullet, p.Text);
            sections.Add(section);
        }
        return sections;
    }

    // A document from onboarding/library_texts/ (for example the Chapter Affiliation Agreement).
    private List<LibrarySection>? TextSections(string id)
    {
        var source = LoadText(id);
        if (source is null) return null;
        var sections = new List<LibrarySection>();
        var key = 0;
        foreach (var s in source.Sections)
        {
            var section = new LibrarySection($"s{key++}", s.Heading, null, []);
            foreach (var p in s.Paragraphs) AddLine(section, p.Bullet, p.Text);
            sections.Add(section);
        }
        return sections;
    }

    // Reads onboarding/library_texts/<id>.json; null if it is missing or broken.
    private LibraryFileText? LoadText(string id)
    {
        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
        try
        {
            return JsonSerializer.Deserialize<LibraryFileText>(File.ReadAllText(Path.Combine(_textsFolder, id + ".json")), TextOptions);
        }
        catch
        {
            return null;
        }
    }

    // An approved document from data/content.db, in its stored order.
    private List<LibrarySection>? ContentDbSections(string document)
    {
        if (!File.Exists(_contentDbPath)) return null;

        using var conn = new SqliteConnection($"Data Source={_contentDbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT section, page, text FROM passages WHERE document = $d ORDER BY rowid";
        cmd.Parameters.AddWithValue("$d", document);

        var sections = new List<LibrarySection>();
        LibrarySection? current = null;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var section = reader.IsDBNull(0) ? "" : reader.GetString(0);
            var pageNumber = !reader.IsDBNull(1) && int.TryParse(reader.GetValue(1)?.ToString(), out var pg) ? pg : (int?)null;
            var text = reader.IsDBNull(2) ? "" : reader.GetString(2);

            if (current is null || current.Key != section)
            {
                // The last part of a "Parent > Child" section name is the heading.
                var heading = section.Split(" > ").Last().Trim();
                var page = PageSection.Match(heading);
                current = new LibrarySection(section, page.Success ? null : heading,
                    page.Success ? $"Page {page.Groups[1].Value}" : null, []) { Page = pageNumber };
                sections.Add(current);
            }

            var lines = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            // Many passages start with their own heading; it is shown (and read) once, as the heading.
            if (current.Paragraphs.Count == 0 && current.Heading is not null && lines.Count > 0
                && string.Equals(lines[0].TrimEnd(':'), current.Heading.TrimEnd(':'), StringComparison.OrdinalIgnoreCase))
            {
                lines.RemoveAt(0);
            }

            foreach (var line in lines)
            {
                var bullet = line.StartsWith('•') || line.StartsWith("- ");
                AddLine(current, bullet, bullet ? line.TrimStart('•', '-', ' ') : line);
            }
        }
        return sections;
    }

    // One line of text as a paragraph of sentences (numbered later, in reading order).
    private static void AddLine(LibrarySection section, bool bullet, string body)
    {
        var sentences = SentenceBreak.Split(body)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s => new LibrarySentence(0, s))
            .ToList();
        if (sentences.Count > 0) section.Paragraphs.Add(new LibraryParagraph(bullet, sentences));
    }

    private static List<LibrarySection> Number(List<LibrarySection> sections)
    {
        // A heading is read aloud too, as the first "sentence" of its section, so a listener
        // hears where they are; it gets its own number so it highlights like any sentence.
        var numbered = new List<LibrarySection>();
        var n = 0;
        foreach (var s in sections.Where(s => s.Paragraphs.Count > 0))
        {
            int? headingIndex = s.Heading is null ? null : n++;
            var paragraphs = s.Paragraphs
                .Select(p => p with { Sentences = p.Sentences.Select(x => x with { Index = n++ }).ToList() })
                .ToList();
            numbered.Add(s with { HeadingIndex = headingIndex, Paragraphs = paragraphs });
        }
        return numbered;
    }

    // Words per document, from the same passages that are read aloud.
    private Dictionary<string, int> WordCounts()
    {
        var counts = new Dictionary<string, int>();
        if (!File.Exists(_contentDbPath)) return counts;
        using var conn = new SqliteConnection($"Data Source={_contentDbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT document, text FROM passages";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var d = reader.GetString(0);
            var words = (reader.IsDBNull(1) ? "" : reader.GetString(1))
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            counts[d] = counts.GetValueOrDefault(d) + words;
        }
        return counts;
    }

    private sealed class GlossaryFile
    {
        public List<GlossaryEntry> Terms { get; set; } = [];
    }

    private sealed class GlossaryEntry
    {
        public string Term { get; set; } = "";
        public List<string> Quote { get; set; } = [];
        public bool Bullets { get; set; }
        public string Document { get; set; } = "";
        public string? Pages { get; set; }
        public string? Where { get; set; }
    }

    private sealed class StepListenFile
    {
        public List<StepListenEntry> Listen { get; set; } = [];
    }

    private sealed class StepListenEntry
    {
        public string Step { get; set; } = "";
        public string Document { get; set; } = "";
        public string? Pages { get; set; }
        public string? Label { get; set; }
    }

    private sealed class LibraryFile
    {
        public List<LibraryEntry> Documents { get; set; } = [];
    }

    private sealed class LibraryEntry
    {
        public string Document { get; set; } = "";
        public string? Title { get; set; }
        public string? Group { get; set; }
        public string? Description { get; set; }
        public string? Date { get; set; }
        /// <summary>"chapter360" for a Chapter 360 document, "text" for one in onboarding/library_texts/;
        /// empty for one in content.db.</summary>
        public string? Source { get; set; }
        /// <summary>Optional line shown with the document, for example "This is ISACA's template".</summary>
        public string? Note { get; set; }
    }
}

/// <summary>A document in the Library: its title in content.db (or its Chapter 360 or library_texts
/// id), the name shown, its group, its description, an optional date, about how many minutes it
/// takes to listen to, whether Isa answers questions from it, where its text comes from, and an
/// optional note shown with it.</summary>
public record LibraryDocument(string Document, string Title, string Group, string Description, string? Date, int Minutes,
    bool AnswersFrom = true, string? Source = null, string? Note = null);

/// <summary>A document in onboarding/library_texts/, as its build script writes it.</summary>
public record LibraryFileText(string Id, string Title, int Words, List<LibraryFileSection> Sections);
public record LibraryFileSection(string? Heading, List<LibraryFileParagraph> Paragraphs);
public record LibraryFileParagraph(bool Bullet, string Text);

/// <summary>A document's text, ready to show and read aloud.</summary>
public record LibraryText(LibraryDocument Document, IReadOnlyList<LibrarySection> Sections);

/// <summary>A section: its heading (read aloud, numbered HeadingIndex) or, for a page the PDF
/// import named after its number, a quiet page marker instead.</summary>
public record LibrarySection(string Key, string? Heading, string? PageMarker, List<LibraryParagraph> Paragraphs)
{
    public int? HeadingIndex { get; init; }
    /// <summary>The page the section is on, for a document from content.db that has pages.</summary>
    public int? Page { get; init; }
}

/// <summary>One term in "Terms explained": ISACA's quote (one or more passages, shown as a list
/// when Bullets), the Library document it comes from, its page, and the part of the document.</summary>
public record GlossaryTerm(string Term, IReadOnlyList<string> Quote, bool Bullets, LibraryDocument Document, string? Pages, string? Where);

/// <summary>One Listen button on a journey step: the Library document, the pages to read (empty
/// for the whole document), what the button says, and about how many minutes it takes.</summary>
public record StepListen(string Document, string? Pages, string Label, int Minutes);

/// <summary>A request from a journey step to the Library: open this document and read this part.
/// Number changes with every press, so pressing the same button again starts it again.</summary>
public record ListenRequest(string Document, string? Pages, string Label, int Number);

public record LibraryParagraph(bool Bullet, List<LibrarySentence> Sentences);

/// <summary>One sentence, numbered across the whole document, so the panel can highlight it.</summary>
public record LibrarySentence(int Index, string Text);
