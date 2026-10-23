using System.Text.Json;

namespace IsaChatApp.Services.Onboarding;

/// <summary>
/// The words of "How to use Isa" (Feature 3 in ONBOARDING-FEATURE.md): the help section
/// (Components/Shared/HelpPanel.razor) and the day-1 tour in the "Get to know Isa" step
/// (Components/Shared/TodayPanel.razor). Read from onboarding/help.json each time, so the
/// guide can be kept true to what Isa does without touching code. Nothing here comes from
/// the AI model.
/// </summary>
public class HelpGuideService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _path;

    public HelpGuideService(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "onboarding", "help.json");
    }

    /// <summary>The guide, or an empty one if the file is missing or broken (Help then says so, rather than breaking the chat).</summary>
    public HelpGuide Load()
    {
        try
        {
            return JsonSerializer.Deserialize<HelpGuide>(File.ReadAllText(_path), Options) ?? new HelpGuide();
        }
        catch
        {
            return new HelpGuide();
        }
    }
}

public class HelpGuide
{
    public List<HelpSection> Sections { get; set; } = [];
    public List<HelpCard> Tour { get; set; } = [];
}

public class HelpSection
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    /// <summary>A shorter name for the links at the top.</summary>
    public string? Short { get; set; }
    public List<string> Points { get; set; } = [];
    /// <summary>Example questions, each with an "Ask Isa" button that puts it in the question box.</summary>
    public List<string> Questions { get; set; } = [];
    public string? Text { get; set; }
}

public class HelpCard
{
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}
