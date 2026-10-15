namespace IsaChatApp.Services.Chat;

/// <summary>
/// Chat helpers for ISACA Chat page.
/// T7A: functions + systematic error handling.
/// </summary>
public class ChatService
{
    private readonly ILogger<ChatService> _log;
    private const int MaxQuestionLength = 2000;

    public ChatService(ILogger<ChatService> log) => _log = log;

    /// <summary>Validate and trim a user question. Throws ArgumentException if empty/too long.</summary>
    public string ValidateQuestion(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question cannot be empty.");

        var text = question.Trim();
        if (text.Length > MaxQuestionLength)
            throw new ArgumentException($"Question must be {MaxQuestionLength} characters or fewer.");

        return text;
    }

    /// <summary>Create a new trace ID (GUID) for one ask request.</summary>
    public string CreateTraceId() => Guid.NewGuid().ToString();

    /// <summary>Safe error message for the UI when pipeline fails.</summary>
    public string GetFriendlyError(Exception ex)
    {
        _log.LogError(ex, "[{Time}] ERROR Chat pipeline failed", DateTime.UtcNow.ToString("O"));
        return "Something went wrong answering your question. Please try again.";
    }
}
