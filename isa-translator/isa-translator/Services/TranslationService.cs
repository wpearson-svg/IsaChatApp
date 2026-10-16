using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace IsaChatApp.Services;

public class TranslationService
{
    private readonly HttpClient _http;
    private readonly ILogger<TranslationService> _logger;
    private readonly string _ollamaBaseUrl;
    private readonly string _model;

    public TranslationService(HttpClient http, IConfiguration config, ILogger<TranslationService> logger)
    {
        _http = http;
        _logger = logger;
        // Prefer local Ollama, fallback to the shared one used by the project
        _ollamaBaseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model = config["Ollama:TranslationModel"] ?? "qwen2.5-coder:1.5b-instruct";
    }

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult { Success = false, Error = "Please enter some text to translate." };

        if (string.IsNullOrWhiteSpace(targetLanguage))
            return new TranslationResult { Success = false, Error = "Please choose a target language." };

        var prompt = BuildPrompt(text, targetLanguage, sourceLanguage);

        try
        {
            var request = new
            {
                model = _model,
                prompt = prompt,
                stream = false,
                options = new { temperature = 0.1 } // low temperature = more accurate translation
            };

            var response = await _http.PostAsJsonAsync($"{_ollamaBaseUrl}/api/generate", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Ollama translation failed: {Status} {Body}", response.StatusCode, errorBody);
                return new TranslationResult
                {
                    Success = false,
                    Error = "Translation service is currently unavailable. Make sure Ollama is running."
                };
            }

            var json = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
            var raw = json?.Response?.Trim() ?? "";

            // Clean up possible extra explanation the model sometimes adds
            var translated = CleanTranslation(raw);

            return new TranslationResult
            {
                Success = true,
                TranslatedText = translated,
                DetectedSourceLanguage = sourceLanguage ?? "auto"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed");
            return new TranslationResult
            {
                Success = false,
                Error = "Could not reach the translation service. Is Ollama running?"
            };
        }
    }

    private static string BuildPrompt(string text, string targetLanguage, string? sourceLanguage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional translator. Translate the text accurately and naturally.");
        sb.AppendLine("Return ONLY the translated text. Do not add explanations, notes, or quotation marks.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(sourceLanguage) && sourceLanguage != "auto")
            sb.AppendLine($"Source language: {sourceLanguage}");
        else
            sb.AppendLine("Detect the source language automatically.");

        sb.AppendLine($"Target language: {targetLanguage}");
        sb.AppendLine();
        sb.AppendLine("Text to translate:");
        sb.AppendLine(text);

        return sb.ToString();
    }

    private static string CleanTranslation(string raw)
    {
        // Remove common prefixes some models add
        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return raw;

        // If the model returned only one clean line, use it
        if (lines.Length == 1) return lines[0].Trim().Trim('"');

        // Otherwise take the longest line (usually the actual translation)
        return lines.OrderByDescending(l => l.Length).First().Trim().Trim('"');
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}

public class TranslationResult
{
    public bool Success { get; set; }
    public string? TranslatedText { get; set; }
    public string? DetectedSourceLanguage { get; set; }
    public string? Error { get; set; }
}
