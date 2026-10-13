using IsaChatApp.Components;
using IsaChatApp.Services;

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

app.Run();
