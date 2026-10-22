namespace IsaChatApp.Services.Orchestrator;

/// <summary>
/// T10 — The pipeline spine. Runs stages in order, writes one event per stage.
/// Only this class writes to the event log.
/// </summary>
public class Orchestrator
{
    private readonly IReadOnlyList<IStage> _stages;
    private readonly EventLogService _eventLog;
    private readonly ILogger<Orchestrator> _log;
    private readonly UsageSummaryService _usage;

    // Stage order from Part B section 4
    // ticketing runs only on Escalate (handled inside the loop)
    public Orchestrator(
        IEnumerable<IStage> stages,
        EventLogService eventLog,
        ILogger<Orchestrator> log,
        UsageSummaryService usage)
    {
        _usage = usage;
        _stages = stages.ToList();
        _eventLog = eventLog;
        _log = log;
    }

    /// <param name="onStep">
    /// Optional. Called after each stage finishes, with the step just added to
    /// ctx.WorkSteps, so the chat can show "show the work" live while the run
    /// is still going. Leaving it out changes nothing about the run.
    /// </param>
    public async Task<AskResponse> RunAsync(QueryContext ctx, Func<WorkStep, Task>? onStep = null)
    {
        // Records one stage for "show the work" and tells the chat, if it asked.
        async Task AddStepAsync(WorkStep step)
        {
            ctx.WorkSteps.Add(step);
            if (onStep is not null) await onStep(step);
        }

        // front_end event as soon as the question arrives
        // The question text is never logged. user_id does not exist yet (access_control sets it).
        // The quick language check (T4W) runs here, before any stage, so the
        // whole pipeline sees the language. English skips translation.
        var runClock = System.Diagnostics.Stopwatch.StartNew();
        var originalQuestion = ctx.Question;   // translation may change ctx.Question; the usage summary hashes the original
        ctx.Language = LanguageCheck.Detect(ctx.Question);
        var frontEndData = $"question_chars={ctx.Question.Length}; language={ctx.Language}";
        await _eventLog.WriteAsync(ctx.TraceId, "front_end",
            $"result=pass; reason=none; {frontEndData}");
        await AddStepAsync(new WorkStep("front_end", "pass", false, null, frontEndData, 0));

        string finalStatus = "answered";
        string? message = null;

        foreach (var stage in _stages)
        {
            // Skip ticketing unless we are escalating
            if (stage.Name == "ticketing" && finalStatus != "escalating")
                continue;

            // After an Escalate, jump straight to ticketing; log the skipped stages
            if (finalStatus == "escalating" && stage.Name != "ticketing")
            {
                await _eventLog.WriteAsync(ctx.TraceId, stage.Name, "result=pass; reason=none; note=skipped, escalated");
                await AddStepAsync(new WorkStep(stage.Name, "pass", true, null, "escalated", 0));
                continue;
            }

            StageResult result;
            var stageClock = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Test only (test-failures.ps1): make this stage crash on purpose.
                if (ctx.TestFailStage == stage.Name) throw new InvalidOperationException("forced test failure");
                result = await stage.RunAsync(ctx);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Stage {Stage} threw", stage.Name);
                result = new StageResult(StageOutcome.Stop, IsaChatApp.Services.ShowTheWork.Wording.WentWrong, $"error={ex.GetType().Name}");
            }
            stageClock.Stop();   // time the stage only, not the event-log write below

            // Contract format: result=pass; reason=none; <stage data>
            // A stage that does not apply returns Pass with the note "skipped" (contract section 3)
            var dataText = result.Skipped
                ? $"result=pass; reason=none; note=skipped, {result.DataText}"
                : $"result={result.Outcome.Tag()}; reason={result.Reason ?? "none"}; {result.DataText}";

            await _eventLog.WriteAsync(ctx.TraceId, stage.Name, dataText.Trim());
            await AddStepAsync(new WorkStep(stage.Name, result.Outcome.Tag(),
                result.Skipped, result.Reason, result.DataText, stageClock.Elapsed.TotalMilliseconds));

            if (result.Outcome == StageOutcome.Stop)
            {
                finalStatus = "stopped";
                ctx.StopReason = result.Reason;
                ctx.Sources = new();   // a stopped run shows no sources
                message = result.Reason ?? IsaChatApp.Services.ShowTheWork.Wording.WentWrong;
                break;
            }

            if (result.Outcome == StageOutcome.Escalate)
            {
                finalStatus = "escalating";
                ctx.EscalatedBy = stage.Name;
                ctx.EscalationReason = result.Reason;
                // Continue so ticketing stage can run
                continue;
            }
        }

        if (finalStatus == "escalating")
        {
            finalStatus = "escalated";
            message = ctx.AnswerText ?? "Your request has been escalated to a person.";
        }
        else if (finalStatus == "answered")
        {
            message = ctx.AnswerText ?? "No answer produced.";
        }

        // Content Health Dashboard: one summary row per run (no question text, person hashed).
        await _usage.RecordRunAsync(ctx, finalStatus, runClock.Elapsed.TotalMilliseconds, originalQuestion);

        return new AskResponse(
            Status: finalStatus,
            Message: message ?? "",
            Sources: ctx.Sources,
            TicketReference: ctx.TicketReference,
            TraceId: ctx.TraceId
        );
    }

    // The stages that check text a leader types outside the chat, in order.
    private static readonly string[] TextCheckStages = { "access_control", "input_compliance" };

    /// <summary>
    /// Checks a piece of text a leader typed outside the chat (for example a
    /// succession notebook note) with ONLY the sign-in check (access_control)
    /// and the Input Compliance gate, so typed text can't become a side door
    /// around Isa's rules. Nothing is searched or answered. Writes one event
    /// per stage like RunAsync, with front_end recording what was checked
    /// (purpose=...) and the text length, never the text. Added 2026-10-20
    /// for the succession notebook (Matt).
    /// </summary>
    /// <returns>The first stage result that isn't Pass, or Pass if both passed.</returns>
    public async Task<StageResult> CheckTextAsync(QueryContext ctx, string purpose)
    {
        await _eventLog.WriteAsync(ctx.TraceId, "front_end",
            $"result=pass; reason=none; purpose={purpose}; text_chars={ctx.Question.Length}");

        foreach (var stage in _stages.Where(s => TextCheckStages.Contains(s.Name)))
        {
            StageResult result;
            try
            {
                result = await stage.RunAsync(ctx);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Stage {Stage} threw during a text check", stage.Name);
                result = new StageResult(StageOutcome.Stop, IsaChatApp.Services.ShowTheWork.Wording.WentWrong, $"error={ex.GetType().Name}");
            }

            await _eventLog.WriteAsync(ctx.TraceId, stage.Name,
                $"result={result.Outcome.Tag()}; reason={result.Reason ?? "none"}; purpose={purpose}; {result.DataText}".Trim());

            if (result.Outcome != StageOutcome.Pass) return result;
        }
        return new StageResult(StageOutcome.Pass);
    }
}

public record AskResponse(
    string Status,
    string Message,
    IReadOnlyList<SourceRef> Sources,
    string? TicketReference,
    string TraceId
);
