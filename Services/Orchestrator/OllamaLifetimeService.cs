namespace IsaChatApp.Services.Orchestrator;

/// <summary>
/// In presentation mode, loads the model when Isa starts (kept for Ollama:KeepAlive, 60 minutes). Always unloads it when Isa
/// stops normally (Ctrl+C, a closed console, a service stop), so it does not hold memory afterwards.
/// stop-isa.bat also sends the unload, because it ends Isa by force and this does not get to run then.
/// Neither step can stop Isa starting or stopping: both never throw.
/// </summary>
public sealed class OllamaLifetimeService : IHostedService
{
    private readonly OllamaChatService _ollama;

    public OllamaLifetimeService(OllamaChatService ollama) => _ollama = ollama;

    // Presentation mode on: warm in the background so Isa is not held up while the model loads.
    // Off (the default): load nothing now; the model loads when the first question needs it.
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_ollama.PresentationMode) _ = Task.Run(() => _ollama.WarmAsync(CancellationToken.None));
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken) => await _ollama.UnloadAsync(cancellationToken);
}
