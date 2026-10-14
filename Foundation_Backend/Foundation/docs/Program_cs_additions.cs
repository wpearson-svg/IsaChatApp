// ============================================================
// PASTE / MERGE into Program.cs
// Working Foundation — T5, T9, T10 registration + POST /api/ask
// ============================================================

// --- using (add at top if missing) ---
// using IsaChatApp.Services;
// using IsaChatApp.Services.Orchestrator;
// using IsaChatApp.Services.Orchestrator.Stages;
// using Microsoft.Data.Sqlite;

// --- After builder is created ---

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("keycloak");

// T9 — Event log
builder.Services.AddSingleton<EventLogService>();

// T5 — Access control
builder.Services.AddSingleton<IAccessControlAdapter, KeycloakAccessControlAdapter>();
builder.Services.AddSingleton<AccessControlStage>();

// T11b + placeholder stages (order matches Part B section 4)
builder.Services.AddSingleton<IStage, TranslationStage>();
builder.Services.AddSingleton<IStage>(sp => sp.GetRequiredService<AccessControlStage>());
// NOTE: access_control is registered above; register remaining in order via a factory:

builder.Services.AddSingleton<IEnumerable<IStage>>(sp =>
{
    // Order is critical — matches contract
    return new IStage[]
    {
        sp.GetRequiredService<AccessControlStage>(), // access_control
        new TranslationStage(),                       // translation   (A'keyrah T11a)
        new InputComplianceStage(),                   // input_compliance (A'keyrah T11a)
        new DatabaseSearchStage(sp.GetRequiredService<IConfiguration>()), // T11b
        new KnowledgeGraphStage(),                    // T11b
        new RagStage(),                               // T11b
        new ComplexAiStage(),                         // Willis T11c
        new OutputComplianceStage(),                  // Willis T11c
        new TicketingStage(),                         // Willis T11c (only on escalate)
    };
});

builder.Services.AddSingleton<Orchestrator>();

// --- After var app = builder.Build(); ---

// T10 — POST /api/ask
app.MapPost("/api/ask", async (
    AskRequest req,
    HttpRequest http,
    Orchestrator orchestrator,
    AccessControlStage accessStage,
    EventLogService eventLog) =>
{
    var traceId = string.IsNullOrWhiteSpace(req.TraceId)
        ? Guid.NewGuid().ToString()
        : req.TraceId!;

    // Token from Authorization header
    var token = http.Headers.Authorization.FirstOrDefault();

    // Inject token into access_control stage for this request
    accessStage.IncomingToken = token;

    var ctx = new QueryContext
    {
        TraceId = traceId,
        Question = req.Question ?? "",
        Language = req.Language ?? "en",
    };

    var result = await orchestrator.RunAsync(ctx);
    return Results.Ok(result);
});

// Helper for A'keyrah T13 — read events by trace ID
app.MapGet("/api/events/{traceId}", async (string traceId, EventLogService eventLog) =>
{
    var rows = await eventLog.GetByTraceIdAsync(traceId);
    return Results.Ok(rows);
});

// --- Request DTO ---
public record AskRequest(string? Question, string? TraceId, string? Language);

// ============================================================
// appsettings.json additions:
// ============================================================
// {
//   "EventLog": { "Path": "data/events.db" },
//   "ContentDb": { "Path": "data/content.db" },
//   "Keycloak": {
//     "Authority": "http://localhost:8080/realms/isa"
//   },
//   "AccessControl": {
//     "DevBypass": true
//   },
//   "Ollama": {
//     "Address": "http://localhost:11434"
//   }
// }
//
// IMPORTANT: AccessControl:DevBypass must be FALSE for Friday demo.
