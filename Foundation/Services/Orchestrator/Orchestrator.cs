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

    // Stage order from Part B section 4
    // ticketing runs only on Escalate (handled inside the loop)
    public Orchestrator(
        IEnumerable<IStage> stages,
        EventLogService eventLog,
        ILogger<Orchestrator> log)
    {
        _stages = stages.ToList();
        _eventLog = eventLog;
        _log = log;
    }

    public async Task<AskResponse> RunAsync(QueryContext ctx)
    {
        // front_end event as soon as the question arrives
        await _eventLog.WriteAsync(ctx.TraceId, "front_end",
            $"user={ctx.UserId ?? "unknown"}; time={DateTime.UtcNow:O}");

        string finalStatus = "answered";
        string? message = null;

        foreach (var stage in _stages)
        {
            // Skip ticketing unless we are escalating
            if (stage.Name == "ticketing" && finalStatus != "escalating")
                continue;

            StageResult result;
            try
            {
                result = await stage.RunAsync(ctx);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Stage {Stage} threw", stage.Name);
                result = new StageResult(StageOutcome.Stop, $"Stage error: {ex.Message}", "error");
            }

            var dataText = result.Skipped
                ? $"skipped; {result.DataText}"
                : $"{result.Outcome}; {result.Reason ?? ""}; {result.DataText}";

            await _eventLog.WriteAsync(ctx.TraceId, stage.Name, dataText.Trim());

            if (result.Outcome == StageOutcome.Stop)
            {
                finalStatus = "stopped";
                ctx.StopReason = result.Reason;
                message = result.Reason ?? "Request could not be completed.";
                break;
            }

            if (result.Outcome == StageOutcome.Escalate)
            {
                finalStatus = "escalating";
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

        return new AskResponse(
            Status: finalStatus,
            Message: message ?? "",
            Sources: ctx.Sources,
            TicketReference: ctx.TicketReference,
            TraceId: ctx.TraceId
        );
    }
}

public record AskResponse(
    string Status,
    string Message,
    IReadOnlyList<SourceRef> Sources,
    string? TicketReference,
    string TraceId
);
