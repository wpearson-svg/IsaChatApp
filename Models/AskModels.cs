namespace IsaChatApp.Models;

/// <summary>Request body for POST /api/ask</summary>
public record AskRequest(string? Question, string? TraceId, string? Language);

/// <summary>Citation shown under an answer</summary>
public record CitationDto(string Document, string Section, string Page);

/// <summary>Response from POST /api/ask</summary>
public record AskResponseDto(
    string Status,
    string Message,
    IReadOnlyList<CitationDto> Sources,
    string? TicketReference,
    string TraceId);
