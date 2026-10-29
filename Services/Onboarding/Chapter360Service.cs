using System.Text.Json;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// ISACA's Chapter 360 documents, for the sample Chapter 360 page
/// (Components/Pages/SampleChapter360.razor) and the Library's Chapter 360 group.
///
/// The text comes from onboarding/chapter360.json, which onboarding/build_chapter360.py builds
/// from ISACA's Word documents in content/Chapter 360. ISACA's words are kept as they are; the
/// script leaves out only ISACA's internal "[Node]" codes, the leftover text of a diagram, and the
/// asterisk after template names. Nothing comes from the AI model, and Isa does not answer
/// questions from these documents (they aren't in her approved content yet).
/// </summary>
public class Chapter360Service
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly string _path;
    private Chapter360Data? _data;
    private DateTime _loadedAt;

    public Chapter360Service(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "onboarding", "chapter360.json");
    }

    /// <summary>The documents and Focus Areas, or empty lists if the file hasn't been built.
    /// Read again when the file changes, so a rebuild needs no restart.</summary>
    public Chapter360Data Data
    {
        get
        {
            try
            {
                var stamp = File.GetLastWriteTimeUtc(_path);
                if (_data is null || stamp != _loadedAt)
                {
                    _data = JsonSerializer.Deserialize<Chapter360Data>(File.ReadAllText(_path), Options) ?? Chapter360Data.Empty;
                    _loadedAt = stamp;
                }
            }
            catch
            {
                _data ??= Chapter360Data.Empty;   // a missing or broken file shows an empty page rather than an error
            }
            return _data;
        }
    }

    public C360FocusArea? GetArea(string? code) =>
        Data.FocusAreas.FirstOrDefault(a => string.Equals(a.Code, code, StringComparison.OrdinalIgnoreCase));

    public C360Document? GetDocument(string? id) =>
        Data.Documents.FirstOrDefault(d => d.Id == id);
}

public record Chapter360Data(List<C360Document> Documents, List<C360FocusArea> FocusAreas)
{
    public static readonly Chapter360Data Empty = new([], []);
}

/// <summary>One Word document, as the Library reads it: its sections and paragraphs, in order.</summary>
public record C360Document(string Id, string Title, string? Code, string File, int Words, List<C360Section> Sections);
public record C360Section(string? Heading, List<C360Paragraph> Paragraphs);
public record C360Paragraph(bool Bullet, string Text);

/// <summary>A Focus Area (OP, MX, PD, GV, AD, CI) with its Practice Areas and practices.</summary>
public record C360FocusArea(string Code, string Name, List<string> Intro, List<C360Point> Why,
    List<string> PracticeNames, List<C360PracticeArea> PracticeAreas);

public record C360Point(string Title, string Text);

public record C360PracticeArea(int Number, string Name, string WhatItIs, List<C360Point> Why, List<C360Practice> Practices);

/// <summary>One numbered practice ("1.1 Evaluate &amp; Share Member Needs"), its benefit and its tools.</summary>
public record C360Practice(string Number, string Title, string Text, string Benefit, List<C360Tool> Tools);

/// <summary>A tool or resource: its kind ("Template") and name. Kind is empty when the document gives none.</summary>
public record C360Tool(string Type, string Name)
{
    /// <summary>"TBD" entries stay in the Library's word-for-word text but aren't shown as tools on the page.</summary>
    public bool IsTbd => Name.Contains("TBD", StringComparison.Ordinal);
}
