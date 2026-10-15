using IsaChatApp.Components;
using IsaChatApp.Services;
using IsaChatApp.Services.Orchestrator;
using IsaChatApp.Services.Orchestrator.Stages;
using OrchestratorService = IsaChatApp.Services.Orchestrator.Orchestrator;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();
// Shared, in-memory, app-wide -- one WidgetSettings for every visitor,
// not persisted to disk. Configured at /isa-config (deliberately not
// linked in the nav -- see Components/Pages/IsaConfig.razor).
builder.Services.AddSingleton<WidgetSettings>();
// Shared, in-memory log of every completed AI reply (model, tokens,
// elapsed time) -- written to by ChatWidget.Send(), read by
// Components/Pages/ContentHealthDashboard.razor. Same lifetime/
// persistence story as WidgetSettings: one for the whole app, gone on
// restart.
builder.Services.AddSingleton<UsageLogService>();

// --- Working Foundation: event log, access control, pipeline stages ---
builder.Services.AddHttpClient("keycloak");
builder.Services.AddSingleton<EventLogService>();
builder.Services.AddSingleton<IAccessControlAdapter, KeycloakAccessControlAdapter>();
// Stage order matters: it is the order they are registered (matches docs/stage-contract.md).
// front_end is logged by the orchestrator itself; ticketing runs only on Escalate.
builder.Services.AddSingleton<IStage, AccessControlStage>();
builder.Services.AddSingleton<IStage, TranslationStage>();
builder.Services.AddSingleton<IStage, InputComplianceStage>();
builder.Services.AddSingleton<IStage, DatabaseSearchStage>();
builder.Services.AddSingleton<IStage, KnowledgeGraphStage>();
builder.Services.AddSingleton<IStage, RagStage>();
builder.Services.AddSingleton<IStage, ComplexAiStage>();
builder.Services.AddSingleton<IStage, OutputComplianceStage>();
builder.Services.AddSingleton<IStage, TicketingStage>();
builder.Services.AddSingleton<OrchestratorService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// POST /api/ask -- the one entry point to the pipeline.
// 401 when the token is missing/invalid; otherwise 200 with status answered / stopped / escalated.
app.MapPost("/api/ask", async (AskRequest req, HttpRequest http, OrchestratorService orchestrator) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.Question))
        return Results.BadRequest(new { error = "question is required" });

    var ctx = new QueryContext
    {
        TraceId = string.IsNullOrWhiteSpace(req.TraceId) ? Guid.NewGuid().ToString() : req.TraceId!,
        Question = req.Question,
        Language = req.Language ?? "en",
        IncomingToken = http.Headers.Authorization.FirstOrDefault(),
    };

    var result = await orchestrator.RunAsync(ctx);
    return ctx.AuthFailed ? Results.Unauthorized() : Results.Ok(result);
});

// Read the event log for one trace ID (used by the acceptance tests).
app.MapGet("/api/events/{traceId}", async (string traceId, EventLogService eventLog) =>
    Results.Ok(await eventLog.GetByTraceIdAsync(traceId)));

app.Run();

public record AskRequest(string? Question, string? TraceId, string? Language);
