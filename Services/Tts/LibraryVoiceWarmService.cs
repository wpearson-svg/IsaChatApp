using IsaChatApp.Services.Onboarding;

namespace IsaChatApp.Services.Tts;

/// <summary>
/// Makes the audio for every Library sentence in the background after Isa starts (when Tts:PreGenerate
/// is true), so Listen starts at once during the demo. Waits for the speech service to come up, skips
/// sentences already in the cache, and stops quietly if the service never answers.
/// </summary>
public sealed class LibraryVoiceWarmService : BackgroundService
{
    private readonly KokoroTtsService _tts;
    private readonly LibraryService _library;
    private readonly IConfiguration _config;
    private readonly ILogger<LibraryVoiceWarmService> _log;

    public LibraryVoiceWarmService(KokoroTtsService tts, LibraryService library, IConfiguration config, ILogger<LibraryVoiceWarmService> log)
    {
        _tts = tts; _library = library; _config = config; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_tts.Enabled || !_config.GetValue("Tts:PreGenerate", true)) return;
        try
        {
            // The speech service can take a minute to start (docker compose up): try for five minutes.
            var up = false;
            for (var i = 0; i < 60 && !up && !stoppingToken.IsCancellationRequested; i++)
            {
                up = await _tts.IsUpAsync(stoppingToken);
                if (!up) await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            if (!up) { _log.LogInformation("Library voice: the speech service is not running, so no audio was pre-made."); return; }

            var made = 0;
            foreach (var doc in _library.GetDocuments())
            {
                var text = _library.GetText(doc.Document);
                if (text is null) continue;
                foreach (var sentence in Sentences(text))
                {
                    if (stoppingToken.IsCancellationRequested) return;
                    if (await _tts.GetOrCreateAsync(sentence, stoppingToken, background: true) is null) { _log.LogInformation("Library voice: pre-making stopped early."); return; }
                    made++;
                }
            }
            _log.LogInformation("Library voice: all sentences are ready ({Count}).", made);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _log.LogInformation(ex, "Library voice pre-making stopped"); }
    }

    /// <summary>The text of the sentence (or heading) the player calls number <paramref name="index"/>, or null.</summary>
    public static string? Find(LibraryText text, int index)
    {
        foreach (var section in text.Sections)
        {
            if (section.HeadingIndex == index && !string.IsNullOrWhiteSpace(section.Heading)) return section.Heading;
            foreach (var para in section.Paragraphs)
                foreach (var s in para.Sentences)
                    if (s.Index == index) return s.Text;
        }
        return null;
    }

    /// <summary>Every sentence of a document in reading order, headings included (what the player reads).</summary>
    public static IEnumerable<string> Sentences(LibraryText text)
    {
        foreach (var section in text.Sections)
        {
            if (!string.IsNullOrWhiteSpace(section.Heading)) yield return section.Heading!;
            foreach (var para in section.Paragraphs)
                foreach (var s in para.Sentences) yield return s.Text;
        }
    }
}
