using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace IsaChatApp.Services.Orchestrator.Stages;

/// <summary>
/// The NLI check's model: Vectara HHEM-2.1-Open (Apache-2.0), ONNX export by Familiar Tools, run in-process with
/// ONNX Runtime. No Python, no service. It reads a passage (the premise) and one sentence (the hypothesis) and returns
/// a score from 0 (the passage does not support the sentence) to 1 (it does).
///
/// Files (download once, see docs/HHEM_Setup.md; both are kept out of Git by data/models/ in .gitignore):
///   data/models/hhem/model.onnx    from huggingface.co/FamiliarTools/HHEM-2.1-Open-onnx (about 419 MB)
///   data/models/hhem/spiece.model  from huggingface.co/google/flan-t5-base (the flan-t5 SentencePiece tokenizer)
/// If either file is missing the scorer reports "unavailable" (Score returns null) and the NLI check is skipped and
/// logged as skipped. It never claims a pass it did not check.
/// Diagnostics go to the console log only, and never include question or answer text.
/// </summary>
public sealed class HhemScorer : IDisposable
{
    private readonly string _modelPath;
    private readonly string _tokenizerPath;
    private readonly int _maxTokens;
    private readonly ILogger<HhemScorer> _log;
    private readonly object _lock = new();
    private InferenceSession? _session;
    private UnigramTokenizer? _tokenizer;
    private bool _loaded;

    public string? LoadError { get; private set; }

    public HhemScorer(IConfiguration config, ILogger<HhemScorer> log)
    {
        _log = log;
        _modelPath = config["Hhem:ModelPath"] ?? Path.Combine("data", "models", "hhem", "model.onnx");
        _tokenizerPath = config["Hhem:TokenizerPath"] ?? Path.Combine("data", "models", "hhem", "spiece.model");
        _maxTokens = config.GetValue("Hhem:MaxTokens", 1024);
        // Dev check: scores two made-up pairs at start-up so the console shows the model and tokenizer work.
        if (config.GetValue("Hhem:SelfTest", false)) _ = Task.Run(SelfTest);
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        lock (_lock)
        {
            if (_loaded) return;
            try
            {
                if (!File.Exists(_modelPath)) throw new FileNotFoundException("HHEM model file not found: " + _modelPath);
                if (!File.Exists(_tokenizerPath)) throw new FileNotFoundException("HHEM tokenizer file not found: " + _tokenizerPath);
                _tokenizer = new UnigramTokenizer(_tokenizerPath);   // no automatic start or end marks: Encode adds the T5 ones
                _session = new InferenceSession(_modelPath);
                _log.LogInformation("HHEM loaded from {Path}", _modelPath);
            }
            catch (Exception ex)
            {
                LoadError = ex.Message;
                _log.LogWarning(ex, "HHEM unavailable, the NLI check will be skipped: {Reason}", ex.Message);
            }
            _loaded = true;
        }
    }

    /// <summary>Consistency score 0..1, or null when the model is not available or the run failed.</summary>
    public double? Score(string premise, string hypothesis)
    {
        EnsureLoaded();
        if (_session is null || _tokenizer is null) return null;
        try
        {
            var ids = Encode(premise, hypothesis);
            var inputIds = new DenseTensor<long>(new[] { 1, ids.Length });
            var mask = new DenseTensor<long>(new[] { 1, ids.Length });
            for (var i = 0; i < ids.Length; i++) { inputIds[0, i] = ids[i]; mask[0, i] = 1; }

            using var results = _session.Run(new[]
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", mask)
            });
            var logits = results.First(r => r.Name == "logits").AsTensor<float>();   // [1, seq, 2]
            double a = logits[0, 0, 0], b = logits[0, 0, 1];                           // consistency = softmax(position 0)[1]
            var m = Math.Max(a, b);
            double ea = Math.Exp(a - m), eb = Math.Exp(b - m);
            return eb / (ea + eb);
        }
        catch (Exception ex)
        {
            _log.LogWarning("HHEM run failed: {Reason}", ex.Message);
            return null;
        }
    }

    // HHEM prompt template: "<pad> Determine if the hypothesis is true given the premise?\n\nPremise: {p}\n\nHypothesis: {h}".
    // "<pad>" is token 0; the T5 tokenizer adds the end mark (token 1). A premise that is too long is cut in half until it fits.
    private long[] Encode(string premise, string hypothesis)
    {
        IReadOnlyList<int> ids;
        while (true)
        {
            ids = _tokenizer!.EncodeToIds($"Determine if the hypothesis is true given the premise?\n\nPremise: {premise}\n\nHypothesis: {hypothesis}");
            if (ids.Count + 2 <= _maxTokens || premise.Length < 200) break;
            premise = premise[..(premise.Length / 2)];
        }
        var all = new long[ids.Count + 2];
        all[0] = 0;
        for (var i = 0; i < ids.Count; i++) all[i + 1] = ids[i];
        all[^1] = 1;
        return all;
    }

    private void SelfTest()
    {
        const string premise = "Chapters must hold at least four board meetings each year.";
        var s1 = Score(premise, "A chapter needs a minimum of four board meetings annually.");
        var s2 = Score(premise, "Chapters only have to meet twice a year.");
        if (s1 is null) { _log.LogWarning("HHEM self-test could not run: {Reason}", LoadError ?? "run failed"); return; }
        _log.LogInformation("HHEM self-test: supported sentence scored {S1:0.00} (expect high), unsupported sentence scored {S2:0.00} (expect low)", s1, s2);
    }

    public void Dispose() => _session?.Dispose();
}
