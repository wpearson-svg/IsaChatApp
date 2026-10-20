namespace IsaChatApp.Services.Orchestrator;

/// <summary>What every stage returns.</summary>
public enum StageOutcome
{
    Pass,
    Stop,
    Escalate
}

public record StageResult(
    StageOutcome Outcome,
    string? Reason = null,
    string DataText = "",
    bool Skipped = false
);

/// <summary>
/// Shared context that travels through every stage.
/// Fields match the stage contract (T1 / Part B section 4).
/// </summary>
public class QueryContext
{
    public string TraceId { get; set; } = "";
    public string? IncomingToken { get; set; }   // raw Authorization header; never logged
    public bool AuthFailed { get; set; }
    /// <summary>Signed in, but the account has no role that allows chat.</summary>
    public bool AccessDenied { get; set; }
    public string? UserId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    /// <summary>Who the person is, read from the sign-in token (chapter_role, permission, term_start, term_end, years_as_leader, language, formality, form_of_address ...). Empty for accounts without them.</summary>
    public IReadOnlyDictionary<string, string> Profile { get; set; } = new Dictionary<string, string>();
    public string Question { get; set; } = "";
    /// <summary>Two-letter code from the quick language check, set by the front end (LanguageCheck). "und" = could not tell, treated as not English.</summary>
    public string Language { get; set; } = "und";

    // Filled by earlier stages
    public List<Passage> Passages { get; set; } = new();
    public bool DirectHit { get; set; }
    public string? AnswerText { get; set; }
    public List<SourceRef> Sources { get; set; } = new();
    public string? TicketReference { get; set; }
    public string? StopReason { get; set; }
    /// <summary>Set by the orchestrator when a stage returns Escalate: which stage, and why.</summary>
    public string? EscalatedBy { get; set; }
    public string? EscalationReason { get; set; }

    /// <summary>
    /// "Show the work": one entry per stage, added by the orchestrator in the
    /// order the stages ran. The chat turns these into plain sentences
    /// (Services/ShowTheWork/WorkStepText.cs). Filled from what each stage
    /// returned, the same facts the event log gets; never the question text
    /// or the model's own reasoning.
    /// </summary>
    public List<WorkStep> WorkSteps { get; } = new();
}

/// <summary>One stage of one run, for "show the work".</summary>
/// <param name="Stage">Stage name from the stage contract, e.g. input_compliance.</param>
/// <param name="Outcome">pass, stop or escalate.</param>
/// <param name="Skipped">True when the stage did not apply (or was passed over after an escalation).</param>
/// <param name="Reason">The stage's reason for a stop or escalation, if any.</param>
/// <param name="DataText">The stage's short data text, exactly as written to the event log.</param>
/// <param name="ElapsedMs">How long the stage took.</param>
public record WorkStep(
    string Stage,
    string Outcome,
    bool Skipped,
    string? Reason,
    string DataText,
    double ElapsedMs
);

/// <summary>The three kinds of live update (stage contract, section 6).</summary>
public enum StagePhase { Started, Finished, Done }

/// <summary>
/// One live update from a run, so the chat can show "Show the work" as it
/// happens (stage contract section 6, decided in task T2W).
/// Started: a stage that really runs has begun (Stage is set).
/// Finished: a stage ended; Step is the same entry added to work_steps.
///   A skipped stage sends only Finished, with Step.Skipped = true.
/// Done: the run is over; Status is answered, stopped or escalated.
/// Never carries the question text or the model's own reasoning.
/// </summary>
public record StageProgress(
    string TraceId,
    int Seq,
    StagePhase Phase,
    string Stage = "",
    WorkStep? Step = null,
    string? Status = null
);

public record Passage(
    string Document,
    string Section,
    string Page,
    string Text
);

public record SourceRef(
    string Document,
    string Section,
    string Page
);

/// <summary>Contract every stage implements.</summary>
public interface IStage
{
    string Name { get; }
    Task<StageResult> RunAsync(QueryContext ctx);
}
