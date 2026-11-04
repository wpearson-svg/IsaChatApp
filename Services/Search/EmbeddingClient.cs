using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IsaChatApp.Services.Search;

/// <summary>
/// Calls the embedding service (Eliyas's E3 service, ai-services/embeddings: multilingual-e5-base,
/// 768-dimension vectors with the "query: " / "passage: " prefixes added by the service).
/// A service that is down, slow or has no model loaded gives null, never an exception: the
/// caller treats that as a clean miss. Settings: Embeddings:BaseUrl (default http://localhost:8092),
/// Embeddings:TimeoutSeconds (default 30).
/// </summary>
public sealed class EmbeddingClient
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<EmbeddingClient> _log;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout;

    public EmbeddingClient(IHttpClientFactory factory, IConfiguration config, ILogger<EmbeddingClient> log)
    {
        _factory = factory;
        _log = log;
        _baseUrl = (config["Embeddings:BaseUrl"] ?? "http://localhost:8092").TrimEnd('/');
        _timeout = TimeSpan.FromSeconds(config.GetValue("Embeddings:TimeoutSeconds", 30));
    }

    public sealed record EmbedResult(float[][] Vectors, string ModelId);

    /// <summary>mode is "query" for a question and "passage" for approved content being indexed.</summary>
    public async Task<EmbedResult?> EmbedAsync(IReadOnlyList<string> texts, string mode, CancellationToken ct = default)
    {
        if (texts.Count == 0) return null;
        try
        {
            var client = _factory.CreateClient();
            client.Timeout = _timeout;
            using var resp = await client.PostAsJsonAsync($"{_baseUrl}/embed", new EmbedRequest(texts, mode), ct);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct);
            // The service answers with an empty list while its model is still loading.
            if (body?.Vectors is not { Count: > 0 }) return null;
            return new EmbedResult(body.Vectors.Select(v => v.ToArray()).ToArray(), body.ModelId ?? "");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _log.LogWarning("Embedding service did not answer ({Reason})", ex.GetType().Name);
            return null;
        }
    }

    private sealed record EmbedRequest(
        [property: JsonPropertyName("texts")] IReadOnlyList<string> Texts,
        [property: JsonPropertyName("mode")] string Mode);

    private sealed record EmbedResponse(
        [property: JsonPropertyName("vectors")] List<List<float>>? Vectors,
        [property: JsonPropertyName("dimensions")] int Dimensions,
        [property: JsonPropertyName("model_id")] string? ModelId);
}
