using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services.Search;

/// <summary>
/// Search by meaning (E3, ported from Eliyas's Postgres/pgvector version to SQLite, November 3, 2026).
///
/// How it works: data/vectors.db holds one vector per approved passage (written by
/// content/build_vectors.py). A question is turned into a vector by the embedding service, compared
/// with every stored vector (cosine similarity, in memory: there are only a few hundred passages),
/// and the closest ones above a minimum score come back with their source, section and page.
/// The passage text itself is read from content.db, so only approved content can ever be returned.
///
/// A clean miss (Found = false, with a short reason) is what the caller gets whenever there is no good
/// match, the embedding service is down, or no vectors have been built yet. It never throws.
///
/// Settings: VectorSearch:Enabled (default true), VectorSearch:Path (default data/vectors.db), VectorSearch:MinScore
/// (the default for the Dev Dashboard's search pass mark slider, 0.80; Eliyas started at 0.25, which suits a different model: multilingual-e5 scores even unrelated text
/// around 0.70, so 0.25 would match everything. NOT yet calibrated: use Dev Dashboard > Vector search test),
/// VectorSearch:TopK (default 3, read by the rag stage), ContentDb:Path.
/// Called by the rag stage when the meaning map has nothing; a miss there still escalates to a person.
/// </summary>
public sealed class VectorSearchService
{
    private readonly string _vectorsPath;
    private readonly string _contentPath;
    private readonly IConfiguration _config;
    private readonly EmbeddingClient _embeddings;
    private readonly OutputComplianceSettings _settings;
    private readonly ILogger<VectorSearchService> _log;
    private readonly object _lock = new();
    private IndexSnapshot? _snapshot;
    private DateTime _snapshotStamp;

    public VectorSearchService(IConfiguration config, EmbeddingClient embeddings, OutputComplianceSettings settings, ILogger<VectorSearchService> log)
    {
        _embeddings = embeddings;
        _settings = settings;
        _log = log;
        _vectorsPath = config["VectorSearch:Path"] ?? Path.Combine("data", "vectors.db");
        _contentPath = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
        _config = config;
    }

    public bool Enabled => _config.GetValue("VectorSearch:Enabled", true);

    /// <summary>The search pass mark: a Dev Dashboard slider (OutputComplianceSettings.SearchPassMark, default VectorSearch:MinScore). Read each time, so a change applies to the next question.</summary>
    public double MinScore => _settings.SearchPassMark;

    public sealed record VectorHit(long PassageId, string Document, string Section, string Page, string Text, double Score);

    public sealed record VectorSearchResult(bool Found, IReadOnlyList<VectorHit> Hits, string Reason);

    public sealed record IndexStatus(bool Ready, int Passages, string ModelId);

    private sealed record IndexSnapshot(long[] Ids, float[][] Vectors, string ModelId);

    private static VectorSearchResult Miss(string reason) => new(false, Array.Empty<VectorHit>(), reason);

    /// <summary>How many passages have a vector, and which model made them.</summary>
    public IndexStatus Status()
    {
        try
        {
            var index = Load();
            return index is null ? new IndexStatus(false, 0, "") : new IndexStatus(index.Ids.Length > 0, index.Ids.Length, index.ModelId);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "vectors.db could not be read");
            return new IndexStatus(false, 0, "");
        }
    }

    /// <param name="minScore">Overrides VectorSearch:MinScore. Pass a very low number (for example -1) to get the closest passages whatever their score, for tuning.</param>
    public async Task<VectorSearchResult> SearchAsync(string query, int topK = 5, CancellationToken ct = default, double? minScore = null)
    {
        if (string.IsNullOrWhiteSpace(query)) return Miss("empty question");

        IndexSnapshot? index;
        try { index = Load(); }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "vectors.db could not be read");
            return Miss("vector index could not be read");
        }
        if (index is null || index.Ids.Length == 0) return Miss("no vectors built yet");

        var embedded = await _embeddings.EmbedAsync(new[] { query }, "query", ct);
        if (embedded is null) return Miss("embedding service unavailable");
        if (embedded.ModelId.Length > 0 && index.ModelId.Length > 0 && embedded.ModelId != index.ModelId)
            return Miss("vectors were built with a different model");

        var q = Normalize(embedded.Vectors[0]);
        if (q.Length != index.Vectors[0].Length) return Miss("vector size mismatch");

        var scored = new List<(long Id, double Score)>(index.Ids.Length);
        for (var i = 0; i < index.Ids.Length; i++)
        {
            double dot = 0;
            var v = index.Vectors[i];
            for (var j = 0; j < q.Length; j++) dot += q[j] * v[j];
            scored.Add((index.Ids[i], dot));
        }

        var top = scored
            .OrderByDescending(s => s.Score)
            .Take(Math.Clamp(topK, 1, 20))
            .Where(s => s.Score >= (minScore ?? MinScore))
            .ToList();
        if (top.Count == 0) return Miss("no passage close enough");

        try
        {
            var hits = FetchPassages(top);
            return hits.Count == 0 ? Miss("passages not found in content.db") : new VectorSearchResult(true, hits, "ok");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "content.db could not be read for vector hits");
            return Miss("content database could not be read");
        }
    }

    // The passage text comes from content.db (approved content only), best match first.
    private List<VectorHit> FetchPassages(List<(long Id, double Score)> top)
    {
        var byId = new Dictionary<long, (string Document, string Section, string Page, string Text)>();
        using var conn = new SqliteConnection($"Data Source={_contentPath};Mode=ReadOnly");
        conn.Open();
        using var cmd = conn.CreateCommand();
        // The ids are numbers from our own index, so listing them in the query text is safe.
        cmd.CommandText = $"SELECT id, document, section, page, text FROM passages WHERE id IN ({string.Join(",", top.Select(t => t.Id))});";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            byId[Convert.ToInt64(r.GetValue(0))] = (r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4));

        var hits = new List<VectorHit>();
        foreach (var (id, score) in top)
            if (byId.TryGetValue(id, out var p))
                hits.Add(new VectorHit(id, p.Document, p.Section, p.Page, p.Text, Math.Round(score, 4)));
        return hits;
    }

    // Reads data/vectors.db into memory; reads it again only when the file has changed.
    private IndexSnapshot? Load()
    {
        if (!File.Exists(_vectorsPath)) return null;
        var stamp = File.GetLastWriteTimeUtc(_vectorsPath);
        lock (_lock)
        {
            if (_snapshot is not null && stamp == _snapshotStamp) return _snapshot;

            var ids = new List<long>();
            var vectors = new List<float[]>();
            var model = "";
            using var conn = new SqliteConnection($"Data Source={_vectorsPath};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT passage_id, model_id, embedding FROM passage_vectors ORDER BY passage_id;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var bytes = r.GetFieldValue<byte[]>(2);
                var v = new float[bytes.Length / sizeof(float)];
                Buffer.BlockCopy(bytes, 0, v, 0, v.Length * sizeof(float));
                ids.Add(r.GetInt64(0));
                vectors.Add(Normalize(v));
                if (model.Length == 0 && !r.IsDBNull(1)) model = r.GetString(1);
            }

            _snapshot = new IndexSnapshot(ids.ToArray(), vectors.ToArray(), model);
            _snapshotStamp = stamp;
            return _snapshot;
        }
    }

    private static float[] Normalize(float[] v)
    {
        double sum = 0;
        foreach (var x in v) sum += (double)x * x;
        var norm = Math.Sqrt(sum);
        if (norm <= 0) return v;
        var result = new float[v.Length];
        for (var i = 0; i < v.Length; i++) result[i] = (float)(v[i] / norm);
        return result;
    }
}
