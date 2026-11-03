using System.Text;

namespace IsaChatApp.Services.Orchestrator.Stages;

/// <summary>
/// A small SentencePiece "unigram" tokenizer for the flan-t5 spiece.model (the tokenizer HHEM-2.1-Open uses).
/// It reads the model file directly (a protobuf list of pieces with scores) and picks the best-scoring split of the
/// text with a Viterbi search, like the SentencePiece library does. Written here because the Microsoft.ML.Tokenizers
/// package could not read this file. English text only needs normal pieces; a character with no piece becomes token 2 (unknown).
/// </summary>
public sealed class UnigramTokenizer
{
    private readonly Dictionary<string, (int Id, float Score)> _pieces = new();
    private readonly int _maxLen;
    private readonly float _unkScore;
    public int PieceCount { get; }

    public UnigramTokenizer(string path)
    {
        var b = File.ReadAllBytes(path);
        var id = 0; var pos = 0; var min = 0f; var maxLen = 1;
        while (pos < b.Length)
        {
            var tag = Varint(b, ref pos);
            var field = (int)(tag >> 3); var wire = (int)(tag & 7);
            if (field == 1 && wire == 2)
            {
                var len = (int)Varint(b, ref pos);
                var end = pos + len; var q = pos;
                string piece = ""; float score = 0; long type = 1;
                while (q < end)
                {
                    var t2 = Varint(b, ref q);
                    var f2 = (int)(t2 >> 3); var w2 = (int)(t2 & 7);
                    if (f2 == 1 && w2 == 2) { var l2 = (int)Varint(b, ref q); piece = Encoding.UTF8.GetString(b, q, l2); q += l2; }
                    else if (f2 == 2 && w2 == 5) { score = BitConverter.ToSingle(b, q); q += 4; }
                    else if (f2 == 3 && w2 == 0) { type = (long)Varint(b, ref q); }
                    else q = Skip(b, q, w2);
                }
                if (type == 1 || type == 4)   // normal or user-defined pieces only
                {
                    _pieces[piece] = (id, score);
                    min = Math.Min(min, score);
                    maxLen = Math.Max(maxLen, piece.Length);
                }
                id++;
                pos = end;
            }
            else pos = Skip(b, pos, wire);
        }
        PieceCount = id;
        _maxLen = maxLen;
        _unkScore = min - 10;
    }

    /// <summary>Token ids for the text (no start or end marks).</summary>
    public List<int> EncodeToIds(string text)
    {
        // Same clean-up as the T5 tokenizer: compatibility normalisation, any run of whitespace (new lines too) becomes one space.
        text = System.Text.RegularExpressions.Regex.Replace(text.Normalize(NormalizationForm.FormKC), @"\s+", " ").Trim();
        var s = "▁" + text.Replace(' ', '▁');
        var n = s.Length;
        var best = new float[n + 1]; var from = new int[n + 1]; var tok = new int[n + 1];
        Array.Fill(best, float.NegativeInfinity); best[0] = 0;
        for (var i = 0; i < n; i++)
        {
            if (float.IsNegativeInfinity(best[i])) continue;
            var hasSingle = false;
            for (var j = i + 1; j <= Math.Min(n, i + _maxLen); j++)
            {
                if (!_pieces.TryGetValue(s.Substring(i, j - i), out var p)) continue;
                if (j == i + 1) hasSingle = true;
                var v = best[i] + p.Score;
                if (v > best[j]) { best[j] = v; from[j] = i; tok[j] = p.Id; }
            }
            if (!hasSingle)
            {
                var v = best[i] + _unkScore;
                if (v > best[i + 1]) { best[i + 1] = v; from[i + 1] = i; tok[i + 1] = 2; }
            }
        }
        var ids = new List<int>();
        for (var j = n; j > 0; j = from[j]) ids.Add(tok[j]);
        ids.Reverse();
        return ids;
    }

    private static ulong Varint(byte[] b, ref int p)
    {
        ulong r = 0; var shift = 0;
        while (true)
        {
            var c = b[p++];
            r |= (ulong)(c & 0x7f) << shift;
            if (c < 0x80) return r;
            shift += 7;
        }
    }

    private static int Skip(byte[] b, int p, int wire)
    {
        switch (wire)
        {
            case 0: Varint(b, ref p); return p;
            case 1: return p + 8;
            case 2: { var l = (int)Varint(b, ref p); return p + l; }
            case 5: return p + 4;
            default: throw new InvalidDataException("unknown protobuf wire type " + wire);
        }
    }
}
