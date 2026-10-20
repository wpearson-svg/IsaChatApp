using IsaChatApp.Components;
using IsaChatApp.Services;
using IsaChatApp.Services.Onboarding;
using IsaChatApp.Services.Orchestrator;
using IsaChatApp.Services.Orchestrator.Stages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
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

// --- Sign-in (Keycloak) ---
// The sign-in page (/sign-in) checks the email and password with
// Keycloak through KeycloakSignInService, then this cookie remembers
// that the person is signed in. Keycloak settings are in
// appsettings.json under "Keycloak"; what the Keycloak client needs is
// in docs/sign-in-setup.md.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Isa.SignIn";
        options.LoginPath = "/sign-in";
        options.AccessDeniedPath = "/sign-in";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<KeycloakSignInService>();

// --- Onboarding feature (see ONBOARDING-FEATURE.md) ---
// LeaderTermService: a leader's current role and day of term, from
// data/directory.db. JourneyService: the 90-day journey steps, from
// data/onboarding.db. Both read-only; paths are in appsettings.json.
builder.Services.AddSingleton<LeaderTermService>();
builder.Services.AddSingleton<JourneyService>();
// JourneyContextService: puts the two above together for one signed-in
// leader on "today" (used by the journey strip and the Today window).
// ContactService: the chapter's invented sample Chapter Experience
// contact and the fake calendar. ProgressService: what leaders have done
// (completed steps, bookings), kept in data/progress.db, created on first run.
builder.Services.AddSingleton<JourneyContextService>();
builder.Services.AddSingleton<ContactService>();
builder.Services.AddSingleton<ProgressService>();
// NotebookService: each chapter's succession notebook (saved answers and
// notes), kept in data/notebook.db, created on first run.
builder.Services.AddSingleton<NotebookService>();

// --- Working Foundation: event log, access control, pipeline stages ---
builder.Services.AddHttpClient("keycloak");
builder.Services.AddSingleton<EventLogService>();
// Reads/writes dev-keys.json (Dev Dashboard > Models, RAG, access & ticketing).
builder.Services.AddSingleton<DevKeysStore>();
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
// Pretty "not found" page for the website only. /api calls keep their real status code (401, 403 ...);
// re-executing a POST to a Blazor page would otherwise turn them into a 400.
app.UseWhen(c => !c.Request.Path.StartsWithSegments("/api"),
    b => b.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();

// Sign-in must come before antiforgery.
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// POST /account/sign-out -- used by the "Sign out" button on the landing
// page. Ends the Keycloak session, clears the sign-in cookie, and goes
// back to the landing page. [FromForm] makes ASP.NET check the form's
// antiforgery token, so other websites can't sign people out.
app.MapPost("/account/sign-out", async (HttpContext http, KeycloakSignInService signIn, [FromForm] string? returnUrl) =>
{
    var sessionId = http.User.FindFirst(KeycloakSignInService.SessionClaim)?.Value;
    await signIn.SignOutAsync(sessionId);
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.LocalRedirect("~/");
});

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
        // Language is not taken from the caller: the front end works it out (LanguageCheck).
        IncomingToken = http.Headers.Authorization.FirstOrDefault(),
    };

    var result = await orchestrator.RunAsync(ctx);
    if (ctx.AuthFailed) return Results.Unauthorized();
    if (ctx.AccessDenied) return Results.StatusCode(StatusCodes.Status403Forbidden);
    return Results.Ok(result);
});

// Read the event log for one trace ID (used by the acceptance tests).
app.MapGet("/api/events/{traceId}", async (string traceId, EventLogService eventLog) =>
    Results.Ok(await eventLog.GetByTraceIdAsync(traceId)));

app.Run();

public record AskRequest(string? Question, string? TraceId, string? Language);
