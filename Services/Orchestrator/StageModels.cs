namespace IsaChatApp.Services.Orchestrator;

public enum StageOutcome { Pass, Stop, Escalate }

public record StageResult(
    StageOutcome Outcome,
    string? Reason = null,
    string DataText = "",
    bool Skipped = false);

public class QueryContext
{
    public string TraceId { get; set; } = "";
    public string? UserId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public string Question { get; set; } = "";
    public string Language { get; set; } = "en";

    public List<Passage> Passages { get; set; } = new();
    public bool DirectHit { get; set; }
    public string? AnswerText { get; set; }
    public List<SourceRef> Sources { get; set; } = new();
    public string? TicketReference { get; set; }
    public string? StopReason { get; set; }
}

public record Passage(string Document, string Section, string Page, string Text);
public record SourceRef(string Document, string Section, string Page);

public interface IStage
{
    string Name { get; }
    Task<StageResult> RunAsync(QueryContext ctx);
}
