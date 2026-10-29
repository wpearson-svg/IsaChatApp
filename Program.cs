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
// The /present slides, read from presentation/slides.json (Components/Pages/Present.razor).
builder.Services.AddSingleton<IsaChatApp.Services.Presentation.PresentationDeck>();

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
// The Library (Feature 5): ISACA's documents to read or listen to, from onboarding/library.json
// and the approved passages in data/content.db (Components/Shared/LibraryPanel.razor).
builder.Services.AddSingleton<LibraryService>();
// "How to use Isa" (Feature 3): the guide's words, from onboarding/help.json
// (Components/Shared/HelpPanel.razor and the day-1 tour in TodayPanel.razor).
builder.Services.AddSingleton<HelpGuideService>();
// HandoffService: the outgoing president's handoff checklist (the President
// Guidebook's turnover items), for the last 30 days of a term and 30 days after.
builder.Services.AddSingleton<HandoffService>();

// --- Working Foundation: event log, access control, pipeline stages ---
builder.Services.AddHttpClient("keycloak");
// The local model for complex_ai (phi4-mini through Ollama). No client-level timeout: OllamaChatService enforces Ollama:TimeoutSeconds itself.
builder.Services.AddHttpClient("ollama", client => client.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.AddSingleton<IsaChatApp.Services.Orchestrator.OllamaChatService>();
builder.Services.AddHostedService<IsaChatApp.Services.Orchestrator.OllamaLifetimeService>();   // model loaded at start (60m), unloaded at stop
builder.Services.AddSingleton<EventLogService>();
builder.Services.AddSingleton<UsageSummaryService>();
builder.Services.AddSingleton<UsageReportService>();
builder.Services.AddSingleton<ContentImportService>();
builder.Services.AddSingleton<DevAccessService>();
builder.Services.AddHttpClient("salesforce");
// Isa's spoken voice for the Library's Listen button: Kokoro-82M (voice bf_lily) running on this machine.
builder.Services.AddHttpClient("tts", client => client.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.AddSingleton<IsaChatApp.Services.Tts.KokoroTtsService>();
builder.Services.AddHostedService<IsaChatApp.Services.Tts.KokoroWarmupService>();       // loads Kokoro's model at start, so the first spoken sentence is not slow
builder.Services.AddHostedService<IsaChatApp.Services.Tts.LibraryVoiceWarmService>();
builder.Services.AddSingleton<SalesforceTicketService>();
builder.Services.AddScoped<PresentPanels>();   // lets Backstage open the /present panels
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
app.MapPost("/api/ask", async (AskRequest req, HttpRequest http, OrchestratorService orchestrator, IConfiguration config) =>
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

    // Test only: "X-Isa-Test-Fail: <stage>" makes that stage crash, so test-failures.ps1 can check
    // what a leader sees. Ignored unless Testing:AllowForcedFailures is true (Development only).
    if (config.GetValue<bool>("Testing:AllowForcedFailures"))
    {
        ctx.TestFailStage = http.Headers["X-Isa-Test-Fail"].FirstOrDefault();
        ctx.TestModelOff = string.Equals(http.Headers["X-Isa-Test-Model"].FirstOrDefault(), "off", StringComparison.OrdinalIgnoreCase);
    }

    var result = await orchestrator.RunAsync(ctx);
    if (ctx.AuthFailed) return Results.Unauthorized();
    if (ctx.AccessDenied) return Results.StatusCode(StatusCodes.Status403Forbidden);
    return Results.Ok(result);
});

// POST /api/ask/stream -- same as /api/ask, but sends one small line of JSON per step while Isa works
// (newline-delimited JSON): {"t":"step","text":"Searching ISACA's approved content","n":5,"of":9} ...
// then one {"t":"done", ...the answer..., "summary":{...}, "steps":[...]} line. Plain sentences only:
// no developer data and never the question text. 401/403 are sent as the status code before any line.
app.MapPost("/api/ask/stream", async (AskRequest req, HttpRequest http, HttpResponse res, OrchestratorService orchestrator) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.Question))
    {
        res.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    var ctx = new QueryContext
    {
        TraceId = string.IsNullOrWhiteSpace(req.TraceId) ? Guid.NewGuid().ToString() : req.TraceId!,
        Question = req.Question,
        IncomingToken = http.Headers.Authorization.FirstOrDefault(),
    };
    var json = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
    var started = false;
    var finished = new List<IsaChatApp.Services.Orchestrator.WorkStep>();
    var startedAt = DateTime.UtcNow;

    async Task Write(object line)
    {
        await res.WriteAsync(System.Text.Json.JsonSerializer.Serialize(line, json) + "\n", http.HttpContext.RequestAborted);
        await res.Body.FlushAsync(http.HttpContext.RequestAborted);
    }
    bool Begin()
    {
        if (started) return true;
        if (ctx.AuthFailed) { res.StatusCode = StatusCodes.Status401Unauthorized; return false; }
        if (ctx.AccessDenied) { res.StatusCode = StatusCodes.Status403Forbidden; return false; }
        res.StatusCode = StatusCodes.Status200OK;
        res.ContentType = "application/x-ndjson";
        res.Headers.CacheControl = "no-store";
        started = true;
        return true;
    }

    IsaChatApp.Services.Orchestrator.AskResponse result;
    try
    {
        result = await orchestrator.RunAsync(ctx, async step =>
        {
            finished.Add(step);
            if (step.Stage == "front_end") return;          // always passes; the sign-in check is next
            if (!Begin()) return;
            await Write(new
            {
                t = "step",
                text = IsaChatApp.Services.ShowTheWork.WorkStepText.Running(finished),
                n = finished.Count + 1,
                of = IsaChatApp.Services.ShowTheWork.WorkStepText.TotalStages(finished),
            });
        });
    }
    catch (OperationCanceledException) { return; }

    if (!Begin()) return;
    var totalMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
    var steps = ctx.WorkSteps.ToList();
    var outcome = IsaChatApp.Services.ShowTheWork.WorkStepText.Summarize(steps, totalMs, developer: false);
    var lines = IsaChatApp.Services.ShowTheWork.WorkStepText.Lines(steps, developer: false);
    await Write(new
    {
        t = "done",
        status = result.Status,
        message = result.Message,
        sources = result.Sources,
        ticketReference = result.TicketReference,
        summary = new { state = outcome.State.ToString(), text = outcome.Text, meta = outcome.Meta },
        steps = lines.Select(l => new { state = l.State.ToString(), text = l.Text, detail = l.Detail }),
    });
});

// GET /api/library/tts?doc=...&i=N -- the spoken audio (mp3) for one sentence of a Library document.
// It takes a document and a sentence number, never free text, so it can only read the approved passages.
// 503 means no spoken voice is available right now: the player then uses the device's own voice.
app.MapGet("/api/library/tts", async (string doc, int i, LibraryService library, IsaChatApp.Services.Tts.KokoroTtsService tts, HttpContext http) =>
{
    var text = library.GetText(doc);
    if (text is null) return Results.NotFound();
    var sentence = IsaChatApp.Services.Tts.LibraryVoiceWarmService.Find(text, i);
    if (sentence is null) return Results.NotFound();
    var path = await tts.GetOrCreateAsync(sentence, http.RequestAborted);
    if (path is null) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    http.Response.Headers.CacheControl = "private, max-age=86400";
    return Results.File(path, "audio/mpeg");
}).RequireAuthorization();

// ---- The phone app's Library (bearer token, like /api/ask). Same approved text and same audio as the web Library. ----
// GET /api/app/library -- the documents, in the order and groups of onboarding/library.json.
app.MapGet("/api/app/library", async (HttpContext http, IAccessControlAdapter access, LibraryService library) =>
{
    if (await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault()) is null) return Results.Unauthorized();
    return Results.Ok(library.GetDocuments().Select(d => new
    {
        document = d.Document, title = d.Title, group = d.Group, description = d.Description, date = d.Date, minutes = d.Minutes
    }));
});

// GET /api/app/library/doc?doc=... -- one document as a flat list in reading order. kind: heading, para, bullet or page.
// "i" is the sentence number the player and /api/app/library/tts use (null for a page marker, which is not read aloud).
app.MapGet("/api/app/library/doc", async (string doc, HttpContext http, IAccessControlAdapter access, LibraryService library) =>
{
    if (await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault()) is null) return Results.Unauthorized();
    var text = library.GetText(doc);
    if (text is null) return Results.NotFound();
    var items = new List<object>();
    foreach (var section in text.Sections)
    {
        if (section.PageMarker is not null) items.Add(new { i = (int?)null, text = section.PageMarker, kind = "page", newPara = true });
        if (section.HeadingIndex is int h && !string.IsNullOrWhiteSpace(section.Heading))
            items.Add(new { i = (int?)h, text = section.Heading, kind = "heading", newPara = true });
        foreach (var para in section.Paragraphs)
            for (var k = 0; k < para.Sentences.Count; k++)
                items.Add(new { i = (int?)para.Sentences[k].Index, text = para.Sentences[k].Text, kind = para.Bullet ? "bullet" : "para", newPara = k == 0 });
    }
    return Results.Ok(new { document = text.Document.Document, title = text.Document.Title, items });
});

// GET /api/app/library/tts?doc=...&i=N -- the spoken mp3 for one sentence (a document and a number, never free text).
app.MapGet("/api/app/library/tts", async (string doc, int i, HttpContext http, IAccessControlAdapter access, LibraryService library,
    IsaChatApp.Services.Tts.KokoroTtsService tts) =>
{
    if (await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault()) is null) return Results.Unauthorized();
    var text = library.GetText(doc);
    if (text is null) return Results.NotFound();
    var sentence = IsaChatApp.Services.Tts.LibraryVoiceWarmService.Find(text, i);
    if (sentence is null) return Results.NotFound();
    var path = await tts.GetOrCreateAsync(sentence, http.RequestAborted);
    if (path is null) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    return Results.File(path, "audio/mpeg");
});

// ---- The phone app's Today (bearer token). Same journey, progress and key dates as the web Today panel; every date and count
// comes from the databases, never from the AI model. person_id comes from the signed-in person's token. ----
// GET /api/app/today -- where this leader is in their first 90 days. hasJourney is false outside the journey.
app.MapGet("/api/app/today", async (HttpContext http, IAccessControlAdapter access, JourneyContextService contexts,
    ProgressService progress, JourneyService journey) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    string? personId = null;
    identity.Profile?.TryGetValue("person_id", out personId);
    var ctx = contexts.GetFor(personId);
    if (ctx is null) return Results.Ok(new { hasJourney = false, message = "There is no first-90-days journey for you right now." });

    var done = progress.GetCompletedSteps(ctx.PersonId);
    var day = ctx.Term.DayOfTerm;
    object Step(JourneyStep st) => new
    {
        id = st.StepId, title = st.Title, details = st.Details,
        source = JourneyContext.IsFromIsacaContent(st) ? st.Source : null,
        dayFrom = st.DayFrom, dayTo = st.DayTo,
        done = done.ContainsKey(st.StepId),
        doneOn = done.TryGetValue(st.StepId, out var d) ? d.DoneOn?.ToString("yyyy-MM-dd") : null,
        needsContactChoice = st.StepId == JourneyContextService.ContactStepId,
    };
    string Day(DateOnly x) => x.ToString("d MMMM", System.Globalization.CultureInfo.InvariantCulture);
    string KeyLine(KeyDateOnDay k) => k.IsOpen
        ? (k.DaysLeft == 0 ? $"{k.Title} closes today ({Day(k.Ends)})."
            : $"{k.Title} is open until {Day(k.Ends)} ({k.DaysLeft} {(k.DaysLeft == 1 ? "day" : "days")} left).")
        : (k.DaysUntil == 1 ? $"{k.Title} opens tomorrow ({Day(k.Starts)})."
            : $"{k.Title} opens in {k.DaysUntil} days ({Day(k.Starts)}).");

    return Results.Ok(new
    {
        hasJourney = true,
        date = ctx.Today.ToString("dddd d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
        dayOfTerm = day, week = ctx.Week, percentThrough = ctx.PercentThrough, badge = ctx.BadgeText,
        todaySteps = ctx.TodaySteps.Select(Step),
        comingUp = ctx.AllSteps.Where(x => x.DayFrom > day && x.DayFrom <= day + 7 && !done.ContainsKey(x.StepId)).Select(Step),
        allSteps = ctx.AllSteps.Select(Step),
        doneCount = ctx.AllSteps.Count(x => done.ContainsKey(x.StepId)), totalCount = ctx.AllSteps.Count,
        keyDates = journey.GetKeyDates(ctx.Today).Where(k => k.IsOpen || k.DaysUntil <= 30).Select(KeyLine),
    });
});

// POST /api/app/today/step { "stepId": "J-02", "done": true } -- mark a step done, or not done again.
// The contact step is the one exception: it is completed by choosing one of its three options, which the app does not offer yet.
app.MapPost("/api/app/today/step", async (StepRequest req, HttpContext http, IAccessControlAdapter access,
    JourneyContextService contexts, ProgressService progress) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    string? personId = null;
    identity.Profile?.TryGetValue("person_id", out personId);
    var ctx = contexts.GetFor(personId);
    if (ctx is null || req is null || string.IsNullOrWhiteSpace(req.StepId)) return Results.BadRequest(new { error = "no journey or no step" });
    if (!ctx.AllSteps.Any(x => x.StepId == req.StepId)) return Results.NotFound();
    if (req.Done && req.StepId == JourneyContextService.ContactStepId)
        return Results.BadRequest(new { error = "This step is finished by choosing how to meet your contact, in the web version for now." });
    if (req.Done) progress.CompleteStep(ctx.PersonId, req.StepId, "done", ctx.Today);
    else progress.UndoStep(ctx.PersonId, req.StepId, ctx.Today);
    return Results.Ok(new { ok = true });
});

// ---- The phone app's succession notebook (bearer token). The same NotebookService and rules as the web notebook:
// presidents (and the outgoing president for 30 days) read and write; other board members read only what was shared.
// Typed text is checked by the sign-in check and Input Compliance before it is saved. ----
static string? PersonIdOf(AccessIdentity identity)
{
    string? personId = null;
    identity.Profile?.TryGetValue("person_id", out personId);
    return personId;
}

// GET /api/app/notebook -- what this person may do, and the entries they may see, newest first.
app.MapGet("/api/app/notebook", async (HttpContext http, IAccessControlAdapter access, NotebookService notebook, WidgetSettings settings) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    var personId = PersonIdOf(identity);
    if (personId is null) return Results.Ok(new { canRead = false });
    var who = notebook.GetAccess(personId, settings.Today);
    if (!who.CanRead) return Results.Ok(new { canRead = false });
    var all = notebook.GetEntries(personId, settings.Today);
    return Results.Ok(new
    {
        canRead = true, canWrite = who.CanWrite, seesAll = who.SeesAll,
        chapter = who.ChapterName, role = who.RoleLabel,
        handoffEnds = who.HandoffEnds?.ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture),
        categories = NotebookService.Categories,
        entries = all.Select(e => new
        {
            id = e.EntryId, parentId = e.ParentId, kind = e.Kind, kindLabel = e.KindLabel, topic = e.Topic, title = e.Title,
            question = e.Question, answer = e.Answer, note = e.Note,
            sources = e.Sources.Select(x => new { document = x.Document, section = x.Section, page = x.Page }),
            shared = e.Shared, mine = e.AuthorId == personId, authorName = e.AuthorName, authorRole = e.AuthorRole,
            recorded = NotebookService.RecordedLabel(e, who.TimeZone), number = NotebookService.RecordNumber(e),
        }),
    });
});

// POST /api/app/notebook/note { category, text, share, parentId? } -- add a typed note.
app.MapPost("/api/app/notebook/note", async (NotebookNoteRequest req, HttpContext http, IAccessControlAdapter access,
    NotebookService notebook, WidgetSettings settings) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    var personId = PersonIdOf(identity);
    if (personId is null || req is null) return Results.BadRequest(new { error = "no notebook for you" });
    var r = await notebook.AddNoteAsync(personId, settings.Today, req.Category ?? "", req.Text ?? "", req.Share,
        http.Request.Headers.Authorization.FirstOrDefault(), req.ParentId);
    return r.IsSaved ? Results.Ok(new { id = r.EntryId }) : Results.BadRequest(new { error = r.Problem });
});

// POST /api/app/notebook/save { question, answer, sources:[{document,section,page}], decided?, share } -- save one of Isa's answers.
app.MapPost("/api/app/notebook/save", async (NotebookSaveRequest req, HttpContext http, IAccessControlAdapter access,
    NotebookService notebook, WidgetSettings settings) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    var personId = PersonIdOf(identity);
    if (personId is null || req is null || string.IsNullOrWhiteSpace(req.Question) || string.IsNullOrWhiteSpace(req.Answer))
        return Results.BadRequest(new { error = "a question and an answer are needed" });
    var sources = (req.Sources ?? []).Select(x => new SourceRef(x.Document ?? "", x.Section ?? "", x.Page ?? "")).ToList();
    var r = await notebook.SaveAnswerAsync(personId, settings.Today, req.Question, req.Answer, sources, req.Decided, req.Share,
        http.Request.Headers.Authorization.FirstOrDefault());
    return r.IsSaved ? Results.Ok(new { id = r.EntryId }) : Results.BadRequest(new { error = r.Problem });
});

// POST /api/app/notebook/share { entryId, share } and /api/app/notebook/remove { entryId } -- the writer's own entries only.
app.MapPost("/api/app/notebook/share", async (NotebookEntryRequest req, HttpContext http, IAccessControlAdapter access,
    NotebookService notebook, WidgetSettings settings) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    var personId = PersonIdOf(identity);
    if (personId is null || req is null) return Results.BadRequest(new { error = "no notebook for you" });
    return notebook.SetShared(personId, req.EntryId, req.Share, settings.Today) ? Results.Ok(new { ok = true })
        : Results.BadRequest(new { error = "You can only change your own entries while you can still write to the notebook." });
});
app.MapPost("/api/app/notebook/remove", async (NotebookEntryRequest req, HttpContext http, IAccessControlAdapter access,
    NotebookService notebook, WidgetSettings settings) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    var personId = PersonIdOf(identity);
    if (personId is null || req is null) return Results.BadRequest(new { error = "no notebook for you" });
    return notebook.Remove(personId, req.EntryId, settings.Today) ? Results.Ok(new { ok = true })
        : Results.BadRequest(new { error = "You can only remove your own entries while you can still write to the notebook." });
});

// POST /api/tts/speak?nocache=true  { "text": "..." } -- the phone app's voice test (laptop voice).
// Signed-in people only (bearer token, checked like /api/ask), and the text is capped, so it cannot be used as a
// general speech service. Answers with the mp3 and the time the voice took, in the X-Isa-Gen-Ms header.
app.MapPost("/api/tts/speak", async (SpeakRequest req, bool? nocache, HttpContext http, IAccessControlAdapter access,
    IsaChatApp.Services.Tts.KokoroTtsService tts) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    var text = req?.Text?.Trim();
    if (string.IsNullOrEmpty(text)) return Results.BadRequest(new { error = "text is required" });
    if (text.Length > 400) return Results.BadRequest(new { error = "text is too long for the voice test" });

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var path = await tts.GetOrCreateAsync(text, http.RequestAborted, noCache: nocache == true);
    sw.Stop();
    if (path is null) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    var bytes = await File.ReadAllBytesAsync(path);
    if (nocache == true) { try { File.Delete(path); } catch { } }
    http.Response.Headers["X-Isa-Gen-Ms"] = sw.ElapsedMilliseconds.ToString();
    return Results.File(bytes, "audio/mpeg");
});

// POST /api/tts/stream  { "text": "..." } -- the phone app's streaming voice test. Same rules as /api/tts/speak
// (bearer token, text capped), but the audio is sent as Kokoro makes it: raw 24 kHz, 16-bit, mono.
app.MapPost("/api/tts/stream", async (SpeakRequest req, HttpContext http, IAccessControlAdapter access,
    IsaChatApp.Services.Tts.KokoroTtsService tts) =>
{
    var identity = await access.ValidateAsync(http.Request.Headers.Authorization.FirstOrDefault());
    if (identity is null) return Results.Unauthorized();
    var text = req?.Text?.Trim();
    if (string.IsNullOrEmpty(text)) return Results.BadRequest(new { error = "text is required" });
    if (text.Length > 600) return Results.BadRequest(new { error = "text is too long for the voice test" });

    using var upstream = await tts.StartStreamAsync(text, http.RequestAborted);
    if (upstream is null) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

    http.Response.ContentType = "audio/L16;rate=24000;channels=1";
    await using var source = await upstream.Content.ReadAsStreamAsync(http.RequestAborted);
    var buffer = new byte[8192];
    int read;
    while ((read = await source.ReadAsync(buffer, http.RequestAborted)) > 0)
    {
        await http.Response.Body.WriteAsync(buffer.AsMemory(0, read), http.RequestAborted);
        await http.Response.Body.FlushAsync(http.RequestAborted);   // send each piece at once, not when the buffer fills
    }
    return Results.Empty;
});

// Read the event log for one trace ID (used by the acceptance tests).
app.MapGet("/api/events/{traceId}", async (string traceId, EventLogService eventLog) =>
    Results.Ok(await eventLog.GetByTraceIdAsync(traceId)));

app.Run();

public record SpeakRequest(string? Text);
public record StepRequest(string? StepId, bool Done);
public record NotebookNoteRequest(string? Category, string? Text, bool Share, long? ParentId);
public record NotebookSourceDto(string? Document, string? Section, string? Page);
public record NotebookSaveRequest(string? Question, string? Answer, List<NotebookSourceDto>? Sources, string? Decided, bool Share);
public record NotebookEntryRequest(long EntryId, bool Share);
public record AskRequest(string? Question, string? TraceId, string? Language);
