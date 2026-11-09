namespace IsaChatApp.Services;

/// <summary>
/// Keeps the Content Health "Time to resolution" tile real: every few minutes it asks Salesforce which
/// Isa Cases a person has closed and records each one's ClosedDate against Isa's reference number
/// (UsageSummaryService.CloseTicketAsync). Does nothing while Ticketing:Salesforce:Enabled is false.
/// Setting: Ticketing:Salesforce:ClosedSyncMinutes (default 10). Reads Salesforce only; writes only usage.db.
/// </summary>
public class TicketCloseSyncService : BackgroundService
{
    /// <summary>When the last check of Salesforce finished, and whether Salesforce answered (shown on the Dev Dashboard's Health alerts).</summary>
    public static DateTime? LastRunUtc { get; private set; }
    public static bool LastRunOk { get; private set; }

    private readonly SalesforceTicketService _sf;
    private readonly UsageSummaryService _usage;
    private readonly IConfiguration _config;
    private readonly ILogger<TicketCloseSyncService> _log;

    public TicketCloseSyncService(SalesforceTicketService sf, UsageSummaryService usage, IConfiguration config, ILogger<TicketCloseSyncService> log)
    {
        _sf = sf; _usage = usage; _config = config; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(45), stop); } catch (OperationCanceledException) { return; }
        while (!stop.IsCancellationRequested)
        {
            try
            {
                var closed = await _sf.ListClosedCasesAsync(200, stop);
                if (closed is not null)
                    foreach (var c in closed) await _usage.CloseTicketAsync(c.IsaReference, c.ClosedUtc);
                if (_sf.Enabled) { LastRunUtc = DateTime.UtcNow; LastRunOk = closed is not null; }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { _log.LogWarning("Ticket close sync failed: {Type}", ex.GetType().Name); }

            var minutes = Math.Clamp(_config.GetValue("Ticketing:Salesforce:ClosedSyncMinutes", 10), 1, 1440);
            try { await Task.Delay(TimeSpan.FromMinutes(minutes), stop); } catch (OperationCanceledException) { return; }
        }
    }
}
