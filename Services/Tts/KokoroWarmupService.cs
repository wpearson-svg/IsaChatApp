namespace IsaChatApp.Services.Tts;

/// <summary>
/// Makes one short, throw-away sentence when Isa starts, so Kokoro has loaded its model before anyone asks
/// for speech. The first request after Kokoro starts is slow (the measured first run was about 10 times
/// slower than the next), so without this the first Listen or spoken answer waits for the model to load.
/// Waits for the speech service to come up, uses no cache and leaves no file behind, and never stops Isa
/// from starting. Turn off with Tts:WarmOnStart = false.
/// </summary>
public sealed class KokoroWarmupService : BackgroundService
{
    private readonly KokoroTtsService _tts;
    private readonly IConfiguration _config;
    private readonly ILogger<KokoroWarmupService> _log;

    public KokoroWarmupService(KokoroTtsService tts, IConfiguration config, ILogger<KokoroWarmupService> log)
    {
        _tts = tts; _config = config; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_tts.Enabled || !_config.GetValue("Tts:WarmOnStart", true)) return;
        try
        {
            // The speech service can take a minute to start (docker compose up): try for five minutes.
            var up = false;
            for (var i = 0; i < 60 && !up && !stoppingToken.IsCancellationRequested; i++)
            {
                up = await _tts.IsUpAsync(stoppingToken);
                if (!up) await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            if (!up) { _log.LogInformation("Kokoro warm-up: the speech service is not running."); return; }

            var started = DateTime.UtcNow;
            var path = await _tts.GetOrCreateAsync("Isa is ready to read to you.", stoppingToken, noCache: true);
            if (path is null) { _log.LogInformation("Kokoro warm-up: the speech service did not make the sentence."); return; }
            try { File.Delete(path); } catch { }
            _log.LogInformation("Kokoro warm-up done in {Seconds:0.0} s.", (DateTime.UtcNow - started).TotalSeconds);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _log.LogInformation(ex, "Kokoro warm-up stopped"); }
    }
}
