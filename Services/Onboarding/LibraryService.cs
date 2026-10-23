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

    private readonly string _listPath;
    private readonly string _contentDbPath;

    public LibraryService(IWebHostEnvironment env, IConfiguration config)
    {
        _listPath = Path.Combine(env.ContentRootPath, "onboarding", "library.json");
        _contentDbPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
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
        return entries
            .Where(e => words.ContainsKey(e.Document))
            .Select(e => new LibraryDocument(e.Document, e.Title ?? e.Document, e.Group ?? "", e.Description ?? "", e.Date,
                Math.Max(1, (int)Math.Round(words[e.Document] / WordsPerMinute))))
            .ToList();
    }

    /// <summary>One document's text, split into sections, paragraphs and numbered sentences,
    /// or null if it isn't one of the Library's documents.</summary>
    public LibraryText? GetText(string document)
    {
        var doc = GetDocuments().FirstOrDefault(d => d.Document == document);
        if (doc is null || !File.Exists(_contentDbPath)) return null;

        using var conn = new SqliteConnection($"Data Source={_contentDbPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT section, page, text FROM passages WHERE document = $d ORDER BY rowid";
        cmd.Parameters.AddWithValue("$d", document);

        var sections = new List<LibrarySection>();
        LibrarySection? current = null;
        var next = 0;   // sentence number across the whole document
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var section = reader.IsDBNull(0) ? "" : reader.GetString(0);
            var text = reader.IsDBNull(2) ? "" : reader.GetString(2);

            if (current is null || current.Key != section)
            {
                // The last part of a "Parent > Child" section name is the heading.
                var heading = section.Split(" > ").Last().Trim();
                var page = PageSection.Match(heading);
                current = new LibrarySection(section, page.Success ? null : heading,
                    page.Success ? $"Page {page.Groups[1].Value}" : null, []);
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
                var body = bullet ? line.TrimStart('•', '-', ' ') : line;
                var sentences = SentenceBreak.Split(body)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .Select(s => new LibrarySentence(next++, s))
                    .ToList();
                if (sentences.Count > 0) current.Paragraphs.Add(new LibraryParagraph(bullet, sentences));
            }
        }

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

        return new LibraryText(doc, numbered);
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
    }
}

/// <summary>A document in the Library: its title in content.db, the name shown, its group,
/// its description, an optional date, and about how many minutes it takes to listen to.</summary>
public record LibraryDocument(string Document, string Title, string Group, string Description, string? Date, int Minutes);

/// <summary>A document's text, ready to show and read aloud.</summary>
public record LibraryText(LibraryDocument Document, IReadOnlyList<LibrarySection> Sections);

/// <summary>A section: its heading (read aloud, numbered HeadingIndex) or, for a page the PDF
/// import named after its number, a quiet page marker instead.</summary>
public record LibrarySection(string Key, string? Heading, string? PageMarker, List<LibraryParagraph> Paragraphs)
{
    public int? HeadingIndex { get; init; }
}

public record LibraryParagraph(bool Bullet, List<LibrarySentence> Sentences);

/// <summary>One sentence, numbered across the whole document, so the panel can highlight it.</summary>
public record LibrarySentence(int Index, string Text);
