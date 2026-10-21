using System.Threading.Channels;
using IsaChatApp.Services.Orchestrator;

namespace IsaChatApp.Services.ShowTheWork;

/// <summary>What the live panel shows at one moment. Immutable, so the page can read it while the feed moves on.</summary>
/// <param name="Steps">The stages already finished (they sit in the panel).</param>
/// <param name="Current">The stage running now, or null when unknown (then the panel works it out from Steps).</param>
/// <param name="Status">answered, stopped or escalated once the run is done; null while it runs.</param>
public record FeedView(IReadOnlyList<WorkStep> Steps, string? Current, string? Status);

/// <summary>
/// The chat's side of live stage updates (task T12W, stage contract section 6).
/// The pipeline posts StageProgress messages; this class applies them in order
/// (through StageFeedState), holds each stage on screen for at least the
/// minimum display time (task T9W, Settings.MinStageMs), and tells the page
/// to redraw. The pipeline itself is never slowed: Post only queues.
///
/// Pacing rule: a stage that really runs stays on screen as the status message
/// for at least the minimum time, counted from when its "started" message was
/// shown (or, if the sender never sends "started", from when the previous step
/// appeared). A skipped stage and the final "done" are shown at once.
///
/// While the pipeline only reports finished stages (today), no "started" message
/// arrives, Current stays null, and the panel infers the running stage from the
/// finished ones, as before. When the pipeline sends "started", nothing here changes.
/// </summary>
public sealed class PacedStageFeed
{
    private readonly StageFeedState _state = new();
    private readonly int _minMs;
    private readonly Func<Task> _changed;
    private readonly Channel<StageProgress> _queue = Channel.CreateUnbounded<StageProgress>();
    private readonly Task _loop;

    private volatile FeedView _view = new(Array.Empty<WorkStep>(), null, null);

    /// <param name="minMs">Minimum display time per stage in milliseconds (0 = show at once).</param>
    /// <param name="changed">Called after each update so the page can redraw.</param>
    public PacedStageFeed(int minMs, Func<Task> changed)
    {
        _minMs = Math.Max(0, minMs);
        _changed = changed;
        _loop = Task.Run(RunAsync);
    }

    /// <summary>What to draw right now.</summary>
    public FeedView View => _view;

    /// <summary>Hand over one update. Never waits, so the pipeline is never slowed.</summary>
    public Task Post(StageProgress update)
    {
        _queue.Writer.TryWrite(update);
        return Task.CompletedTask;
    }

    /// <summary>The run is over: wait until every queued update has been shown.</summary>
    public async Task CompleteAsync()
    {
        _queue.Writer.TryComplete();
        await _loop;
    }

    private async Task RunAsync()
    {
        DateTime? shownAt = null;   // when the current stage's status message appeared
        await foreach (var p in _queue.Reader.ReadAllAsync())
        {
            // A finished stage that really ran stays at least _minMs on screen.
            if (_minMs > 0 && p.Phase == StagePhase.Finished && p.Step is { Skipped: false })
            {
                var already = shownAt is null ? TimeSpan.Zero : DateTime.UtcNow - shownAt.Value;
                var wait = TimeSpan.FromMilliseconds(_minMs) - already;
                if (wait > TimeSpan.Zero) await Task.Delay(wait);
            }

            _state.Apply(p);
            _view = new FeedView(_state.Finished.ToList(), _state.Current, _state.Status);

            // Time starts when a stage's status message appears; after a finished
            // stage, the next one (or the first step of a run that sends only
            // "finished") starts from now.
            shownAt = p.Phase == StagePhase.Done ? null : DateTime.UtcNow;

            try { await _changed(); } catch { /* the page may be gone */ }
        }
    }
}
