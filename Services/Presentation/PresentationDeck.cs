using System.Text.Json;

namespace IsaChatApp.Services.Presentation;

/// <summary>
/// The slides for the /present page (Components/Pages/Present.razor), read from
/// presentation/slides.json. All slide wording lives in that file, so it can be edited
/// without touching code. The file is read each time the page opens, so a change shows
/// after a browser refresh, with no restart of Isa.
/// </summary>
public class PresentationDeck
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _path;

    public PresentationDeck(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "presentation", "slides.json");
    }

    /// <summary>
    /// Loads the slides. A missing or broken file returns no slides and the reason,
    /// so the page can say what is wrong instead of crashing.
    /// </summary>
    public (IReadOnlyList<Slide> Slides, string? Error) Load()
    {
        try
        {
            var deck = JsonSerializer.Deserialize<DeckFile>(File.ReadAllText(_path), Options);
            return (deck?.Slides ?? [], null);
        }
        catch (Exception ex)
        {
            return ([], $"{Path.GetFileName(_path)}: {ex.Message}");
        }
    }

    private sealed class DeckFile
    {
        public List<Slide> Slides { get; set; } = [];
    }
}

/// <summary>One slide. Which fields are used depends on Kind (see the top of slides.json).</summary>
public class Slide
{
    public string Kind { get; set; } = "statement";
    /// <summary>True on the slide where the model warm-up starts in the background (once per visit).</summary>
    public bool Warmup { get; set; }
    public string? Section { get; set; }
    public string? Presenter { get; set; }
    public string Title { get; set; } = "";
    public string? Lead { get; set; }
    public string? Subtitle { get; set; }
    public string? Note { get; set; }
    /// <summary>The big number on a "problem" slide, and the words under it.</summary>
    public string? Stat { get; set; }
    public string? StatLabel { get; set; }
    public List<string> Bullets { get; set; } = [];
    public List<string> Steps { get; set; } = [];
    public List<SlideItem> Items { get; set; } = [];
    public List<SlideItem> Rows { get; set; } = [];
    public List<SlideStage> Stages { get; set; } = [];
    public List<SlideGroup> Groups { get; set; } = [];
    public List<SlidePath> Paths { get; set; } = [];
}

public class SlideItem
{
    public string? Name { get; set; }
    public string Title { get; set; } = "";
    public string? Text { get; set; }
    public string? Status { get; set; }
}

public class SlideStage
{
    public string Name { get; set; } = "";
    /// <summary>Empty for stages every question passes; "ai" for the AI path only; "handoff" when a person takes over.</summary>
    public string? Path { get; set; }
}

/// <summary>A path through the pipeline, drawn as a bar under the stages (numbered from 1) it uses.</summary>
public class SlidePath
{
    public string Title { get; set; } = "";
    public string? Text { get; set; }
    public string Color { get; set; } = "blue";
    public List<int> Stages { get; set; } = [];
}

public class SlideGroup
{
    public string Label { get; set; } = "";
    public List<SlideItem> Items { get; set; } = [];
}
