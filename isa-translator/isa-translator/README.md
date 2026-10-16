# ISA Translator Feature

Complete translator page for IsaChatApp. Supports translation between 70+ languages using your local Ollama model.

## Files included

```
Services/
  └── TranslationService.cs          ← Translation logic (calls Ollama)

Components/Pages/
  ├── Translator.razor               ← The page UI
  └── Translator.razor.css           ← Brand-styled CSS
```

## Installation steps

### 1. Copy the files into your project

Copy the folders into your IsaChatApp root so the structure becomes:

```
IsaChatApp/
├── Services/
│   └── TranslationService.cs
├── Components/
│   └── Pages/
│       ├── Translator.razor
│       └── Translator.razor.css
```

### 2. Register the service in Program.cs

Add this line near your other service registrations:

```csharp
builder.Services.AddHttpClient<TranslationService>();
```

### 3. Add a navigation link

In your NavMenu (usually `Components/Layout/NavMenu.razor`) add:

```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="translator">
        ISA Translator
    </NavLink>
</div>
```

### 4. (Optional) Configure the model

In `appsettings.json` or `appsettings.Development.json` you can override:

```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "TranslationModel": "qwen2.5-coder:1.5b-instruct"
}
```

### 5. Run the app

```bash
dotnet run
```

Then open: **http://localhost:5067/translator**

## Requirements

- Ollama must be running
- At least one model pulled (the default is `qwen2.5-coder:1.5b-instruct`)

## Features

- Auto-detect source language
- 70+ languages in the dropdown
- Swap languages button
- Copy translation to clipboard
- Clean loading & error states
- Fully responsive + ISACA brand colors
