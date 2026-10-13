# IsaChatApp

A Blazor Server prototype of the chapter-support chat widget, built on
the locked-in tech stack for this project: **Blazor (ASP.NET Core), C#,
HTML, CSS — no hand-written JavaScript.** The only script tag a Blazor
app ships is the framework's own bootstrap file, which every Blazor
project has regardless of what you build; that's Microsoft's runtime
plumbing, not application code.

## Requirements

Windows 10 or 11. Nothing else is assumed to be installed.
`start-isa.bat` checks for the items below before it starts the app and
tells you what is missing.

1. **.NET 10 SDK** (required) -- the SDK, not just the runtime. Check with
   `dotnet --list-sdks`; you need a line starting with `10.` or higher.
   Download: https://dotnet.microsoft.com/download/dotnet/10.0
2. **Ollama with at least one model** (needed for the chat to answer) --
   install from https://ollama.com/download, then pull a model, for example
   `ollama pull qwen2.5-coder:1.5b-instruct`. The app still starts without
   Ollama, but the chat can't reply. See **Configuring Ollama hosts** below.
3. **Internet access on the first run** -- the first build downloads
   packages. Later runs don't need it.
4. **Free port 5067** -- if the app is already running, run `stop-isa.bat`
   first.

**Optional:** the code also lists a shared Ollama host that is only
reachable over a private network (Tailscale). You do not need it -- the
app uses your local Ollama if the shared one can't be reached. Ask Willis
for an invite if you want to use it.

## Running it

```
dotnet run
```

Open the URL the console prints (e.g. `https://localhost:5001`). The chat
widget appears as a floating button in the bottom-right corner on every
page — it's placed once in the root layout, not per-page.

## Project layout

- **`Program.cs`** — app startup. Note `builder.Services.AddHttpClient();`
  — required for `ChatWidget`'s calls to Ollama; the Blazor Web App
  template doesn't register `HttpClient` for Server render mode by
  default (only for WebAssembly), so this line has to be there by hand.
- **`Components/Layout/ChatWidget.razor`** / **`.razor.css`** — the
  floating chat widget itself: collapsed bubble, expanded panel, model
  dropdown, send/receive logic.
- **`Components/Shared/Throbber.razor`** / **`.razor.css`** — the ring +
  "ISA" wordmark icon, as a reusable component. `ChatWidget` places the
  exact same `<Throbber />` in two spots: the collapsed bubble (spins on
  mouse-over) and the expanded panel's input-row icon (spins while a
  request is in flight).
- **`Components/Pages/ThrobberTest.razor`** / **`.razor.css`** — a
  dev-only page at **`/throbber-test`** with a live settings panel
  covering every option the original throbber supported (placement, word
  mode, size, timing, direction, colours). No Ollama or `ChatWidget`
  dependency — useful for checking a throbber change in isolation.
- **`Components/Layout/MainLayout.razor`** — where `<ChatWidget />` is
  placed and its `Hosts` are configured.
- **`Throbber/`** — the original JS-based throbber/demo files this
  project's throbber was ported from, kept for reference.

## Configuring Ollama hosts

Edit the `Hosts` dictionary passed to `<ChatWidget />` in
`MainLayout.razor`:

```razor
<ChatWidget @rendermode="InteractiveServer"
            Hosts="@(new Dictionary<string,string> {
                ["remote"] = "http://100.89.6.69:11434",
                ["local"]  = "http://localhost:11434"
            })" />
```

Every host listed is queried in parallel for `/api/tags` on load (and
whenever **Refresh** is clicked in the panel) to build the model
dropdown, labeled `modelname (hostname)`. An unreachable or slow host
(past `HostTimeoutMs`, default 4000ms) is skipped and shown as
"unreachable" in the status line rather than blocking the others from
loading.

`@rendermode="InteractiveServer"` is required — without an interactive
render mode the widget won't respond to clicks. Because this is Blazor
**Server**, the `HttpClient` calls to Ollama run on the server process
itself, not in the visitor's browser — no CORS / `OLLAMA_ORIGINS`
configuration is needed, since the browser never talks to Ollama
directly.

## Status messages

While a request is in flight, the (disabled) input box shows a status
line instead of the draft text — e.g. "AI processing your request..." —
rather than a separate message in the chat log. This is
`ChatWidget.razor`'s `StatusMessages` dictionary and `SetStatus(key)`
method:

```csharp
private static readonly Dictionary<string, string> StatusMessages = new()
{
    ["searching"] = "Searching the database...",
    ["escalating"] = "Escalating to AI...",
    ["aiProcessing"] = "AI processing your request...",
    ["verifying"] = "Verifying response..."
};
```

**To add a message**, add a key. **To show one**, call
`SetStatus("thatKey")` from wherever that stage of the process actually
happens. Only `aiProcessing` is wired to something real right now (in
`Send()`, right before the call to Ollama); `searching`, `escalating`,
and `verifying` are defined and ready for when those become actual
pipeline steps — wiring one in is a single `SetStatus(...)` call at the
point it happens, nothing else changes. An unknown key is silently
ignored rather than throwing, so a typo can't take down a request
already in flight.

There's no live elapsed-time timer next to the message yet — planned,
not yet built.

## Before this goes further than a prototype

Right now `ChatWidget` calls Ollama directly. Once the ASP.NET Core
Minimal API backend exists, point one of the `Hosts` entries at that
instead — the API is where the RFP's governance requirements actually
belong:

- AI Use Register logging of each exchange
- Auth / access control for chapter leaders
- Escalation / human handoff when the model can't help
- Content grounding against approved ISACA material

`ChatWidget` doesn't need to change for this — it's a one-line edit to a
`Hosts` entry, since both Ollama and a future API would speak the same
`/api/tags` / `/api/generate` HTTP/JSON shape to it (or a small adapter
in the fetch logic if the API's shape ends up different).
