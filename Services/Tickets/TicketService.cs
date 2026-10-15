namespace IsaChatApp.Services.Tickets;

/// <summary>
/// Support ticket helpers (escalation from chat).
/// T7A: functions + validation errors.
/// </summary>
public class TicketService
{
    private readonly ILogger<TicketService> _log;

    public TicketService(ILogger<TicketService> log) => _log = log;

    /// <summary>Generate a display ticket reference (Foundation stub style).</summary>
    public string CreateTicketReference()
        => $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

    /// <summary>Validate subject/description before creating a ticket.</summary>
    public void ValidateTicketInput(string? subject, string? description)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.");
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.");
        if (subject.Length > 200)
            throw new ArgumentException("Subject must be 200 characters or fewer.");
    }

    /// <summary>Log ticket creation for audit trail.</summary>
    public void LogTicketCreated(string ticketRef, string? userId)
    {
        _log.LogInformation(
            "[{Time}] INFO Ticket created ref={Ref} user={User}",
            DateTime.UtcNow.ToString("O"), ticketRef, userId ?? "unknown");
    }
}
