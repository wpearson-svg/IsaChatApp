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
    public string Language { get; set; } = "en";

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
}

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
