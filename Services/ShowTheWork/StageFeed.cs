using IsaChatApp.Services.Orchestrator;

namespace IsaChatApp.Services.ShowTheWork;

/// <summary>
/// What the chat keeps while a run is going: turns the live updates
/// (StageProgress, stage contract section 6) into three things the panel
/// shows: the stage running now (Current), the stages already finished
/// (Finished, which move into the panel) and, at the end, the Status.
/// Used by the made-up feed (task T3W) and, later, by the real connection (T12W).
/// </summary>
public class StageFeedState
{
    public List<WorkStep> Finished { get; } = new();
    public string? Current { get; private set; }
    public string? Status { get; private set; }
    public bool IsDone => Status is not null;
    private int _lastSeq;

    public void Apply(StageProgress p)
    {
        if (p.Seq <= _lastSeq) return;      // late or repeated message: ignore
        _lastSeq = p.Seq;
        switch (p.Phase)
        {
            case StagePhase.Started:
                Current = p.Stage;
                break;
            case StagePhase.Finished:
                if (p.Step is not null) Finished.Add(p.Step);
                if (Current == p.Stage || Current is null) Current = null;
                break;
            case StagePhase.Done:
                Current = null;
                Status = p.Status ?? "answered";
                break;
        }
    }
}

/// <summary>
/// A made-up feed of stage updates for the five test questions of the stage
/// contract (section 11) plus a non-English one. Built from the contract rules,
/// not from the real stages, so the panel can be built and tested before the
/// real stages report anything (task T3W). Nothing here calls a stage or a model.
/// </summary>
public static class MockStageFeed
{
    public record Case(int Number, string Question, string Expect);

    public static readonly IReadOnlyList<Case> Cases = new[]
    {
        new Case(1, "How many board meetings must a chapter hold each year?", "Direct hit: answered; the AI steps are skipped"),
        new Case(2, "Who writes down what the board decided?", "Not found, then related topics, passages, the model: answered"),
        new Case(3, "Where's a good pizza place near the conference?", "Input compliance stops it: nothing after"),
        new Case(4, "What is the cryptocurrency policy?", "Nothing approved found: escalated with a ticket"),
        new Case(5, "A board member keeps harassing me, what should I do?", "Sensitive: escalated at input compliance, with a ticket"),
        new Case(6, "\u00bfCu\u00e1ntas reuniones de la junta debe celebrar un cap\u00edtulo cada a\u00f1o?", "Not English: the language step runs; then a direct hit"),
    };

    private static readonly string[] AfterInput = { "database_search", "knowledge_graph", "rag", "complex_ai", "output_compliance" };

    /// <summary>The updates for one test question, in the order the contract says they arrive.</summary>
    public static IReadOnlyList<StageProgress> Script(int number)
    {
        var trace = $"mock-{number}";
        var ev = new List<StageProgress>();
        var seq = 0;

        void Ran(string stage, string outcome, string data, double ms, string? reason = null)
        {
            ev.Add(new(trace, ++seq, StagePhase.Started, stage));
            ev.Add(new(trace, ++seq, StagePhase.Finished, stage, new WorkStep(stage, outcome, false, reason, data, ms)));
        }
        void Skip(string stage, string why) =>
            ev.Add(new(trace, ++seq, StagePhase.Finished, stage, new WorkStep(stage, "pass", true, null, why, 0)));
        void Done(string status) => ev.Add(new(trace, ++seq, StagePhase.Done, Status: status));

        Ran("front_end", "pass", "received", 1);
        Ran("access_control", "pass", "access=granted", 4);
        if (number == 6) Ran("translation", "pass", "language=es; translated=yes", 310);
        else Skip("translation", "skipped, language=en");

        switch (number)
        {
            case 1:
            case 6:
                Ran("input_compliance", "pass", "in_scope=yes", 2);
                Ran("database_search", "pass", "direct_hit; count=1", 6);
                Skip("knowledge_graph", "skipped, direct hit");
                Skip("rag", "skipped, direct hit");
                Skip("complex_ai", "skipped, direct hit");
                Ran("output_compliance", "pass", "section=database; sources=1", 3);
                Done("answered");
                break;
            case 2:
                Ran("input_compliance", "pass", "in_scope=yes", 2);
                Ran("database_search", "not_found", "no_match", 5);
                Ran("knowledge_graph", "pass", "related=2", 3);
                Ran("rag", "pass", "passages=3", 4);
                Ran("complex_ai", "pass", "model_called=yes; model=phi4-mini; model_ms=4210; passages=3", 4215);
                Ran("output_compliance", "pass", "section=ai; sources=1", 8);
                Done("answered");
                break;
            case 3:
                Ran("input_compliance", "stop", "in_scope=no", 2, "outside chapter support");
                Done("stopped");
                break;
            case 4:
                Ran("input_compliance", "pass", "in_scope=yes", 2);
                Ran("database_search", "not_found", "no_match", 5);
                Ran("knowledge_graph", "pass", "related=0", 3);
                Ran("rag", "escalate", "no_passages", 2, "no approved content found");
                Skip("complex_ai", "escalated");
                Skip("output_compliance", "escalated");
                Ran("ticketing", "pass", "ticket=TKT-0001", 6);
                Done("escalated");
                break;
            case 5:
                Ran("input_compliance", "escalate", "sensitive=harassment", 2, "sensitive topic");
                foreach (var s in AfterInput) Skip(s, "escalated");
                Ran("ticketing", "pass", "ticket=TKT-0002", 6);
                Done("escalated");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(number));
        }
        return ev;
    }

    /// <summary>
    /// Plays a script as if the stages were running: waits msPerStage after each
    /// "started", then sends the rest. (The real minimum display time is task T9W.)
    /// </summary>
    public static async Task PlayAsync(int number, Func<StageProgress, Task> onUpdate, int msPerStage, CancellationToken ct = default)
    {
        foreach (var p in Script(number))
        {
            ct.ThrowIfCancellationRequested();
            await onUpdate(p);
            if (p.Phase == StagePhase.Started && msPerStage > 0) await Task.Delay(msPerStage, ct);
        }
    }
}
