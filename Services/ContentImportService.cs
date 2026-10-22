using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using UglyToad.PdfPig;

namespace IsaChatApp.Services;

/// <summary>One file's outcome from an import.</summary>
public sealed record ImportResult(string File, string Title, int Passages, string? Problem);

/// <summary>
/// Dev dashboard > Content import. Reads .docx, .pptx and .pdf files, splits them into passages
/// (about 800 characters, on paragraph / sentence boundaries, same rule as content/import_content.py),
/// adds them to the LIVE data/content.db (no restart needed), and also appends them to
/// content/content.json so a later rebuild (build_content_db.py) keeps them.
/// New documents are listed in data/content_categories.csv with the category chosen on the page.
/// </summary>
public class ContentImportService
{
    private const int Max = 800;
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly Regex DotLeader = new(@"\.{5,}\s*\d+\s*$", RegexOptions.Multiline);
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly string _dbPath;
    private readonly string _jsonPath;
    private readonly string _catPath;
    private readonly ILogger<ContentImportService> _log;

    public ContentImportService(IConfiguration config, IWebHostEnvironment env, ILogger<ContentImportService> log)
    {
        _log = log;
        var root = env.ContentRootPath;
        var db = config["ContentDb:Path"] ?? Path.Combine("data", "content.db");
        _dbPath = Path.IsPathRooted(db) ? db : Path.Combine(root, db);
        _jsonPath = Path.Combine(root, "content", "content.json");
        _catPath = Path.Combine(root, "data", "content_categories.csv");
    }

    public static readonly string[] Categories = { "Governance", "Operations", "Programs", "Products", "Uncategorized" };

    public async Task<List<ImportResult>> ImportAsync(IEnumerable<(string Name, Stream Data)> files, string category, bool replace)
    {
        var results = new List<ImportResult>();
        await Gate.WaitAsync();
        try
        {
            if (!File.Exists(_dbPath))
                return files.Select(f => new ImportResult(f.Name, "", 0, "The content database does not exist yet. Build it first (content\\build_content_db.py).")).ToList();

            await using var conn = new SqliteConnection($"Data Source={_dbPath};Mode=ReadWrite");
            await conn.OpenAsync();
            var json = File.Exists(_jsonPath) ? JsonNode.Parse(await File.ReadAllTextAsync(_jsonPath)) : null;
            var changedJson = false;
            var newTitles = new List<string>();

            foreach (var (name, data) in files)
            {
                try
                {
                    var ext = Path.GetExtension(name).ToLowerInvariant();
                    if (ext is not (".docx" or ".pptx" or ".pdf")) { results.Add(new(name, "", 0, "Only .docx, .pptx and .pdf files are supported.")); continue; }

                    var ms = new MemoryStream();
                    await data.CopyToAsync(ms);
                    ms.Position = 0;
                    var sections = ext switch { ".docx" => ReadDocx(ms), ".pptx" => ReadPptx(ms), _ => ReadPdf(ms) };

                    var passages = new List<(string Section, string Page, string Text)>();
                    foreach (var (sec, page, lines) in sections)
                        foreach (var text in Chunk(lines))
                            if (!IsToc(text) && text.Trim().Length >= 25) passages.Add((sec, page, text.Trim()));
                    if (passages.Count == 0) { results.Add(new(name, "", 0, "No readable text found (a scanned PDF has no text layer).")); continue; }

                    var source = "imported/" + name;
                    var title = Path.GetFileNameWithoutExtension(name);

                    var existing = await ScalarAsync(conn, "SELECT title FROM documents WHERE source = $s", ("$s", source));
                    if (existing is not null)
                    {
                        if (!replace) { results.Add(new(name, existing, 0, "Already imported. Tick \"Replace\" to load it again.")); continue; }
                        await RemoveDocumentAsync(conn, existing);
                        RemoveFromJson(json, existing, source);
                        title = existing; changedJson = true;
                    }
                    else if (await ScalarAsync(conn, "SELECT title FROM documents WHERE title = $t", ("$t", title)) is not null)
                        title += " (imported)";

                    var nextId = Convert.ToInt32(await ScalarAsync(conn, "SELECT COALESCE(MAX(CAST(id AS INTEGER)),0) FROM passages") ?? "0") + 1;
                    await using var tx = await conn.BeginTransactionAsync();
                    var docId = Convert.ToInt32(await ScalarAsync(conn, "SELECT COALESCE(MAX(id),0) FROM documents") ?? "0") + 1;
                    var secId = Convert.ToInt32(await ScalarAsync(conn, "SELECT COALESCE(MAX(id),0) FROM sections") ?? "0");
                    await ExecAsync(conn, "INSERT INTO documents(id,title,source,folder) VALUES($i,$t,$s,'Imported')", ("$i", docId), ("$t", title), ("$s", source));
                    var seen = new Dictionary<string, int>();
                    var jsonPassages = json?["passages"]?.AsArray();
                    foreach (var p in passages)
                    {
                        if (!seen.ContainsKey(p.Section))
                        {
                            seen[p.Section] = ++secId;
                            await ExecAsync(conn, "INSERT INTO sections(id,document_id,heading) VALUES($i,$d,$h)", ("$i", secId), ("$d", docId), ("$h", p.Section));
                        }
                        await ExecAsync(conn, "INSERT INTO passages(id,document,section,page,text) VALUES($i,$d,$s,$p,$x)",
                            ("$i", nextId), ("$d", title), ("$s", p.Section), ("$p", p.Page), ("$x", p.Text));
                        jsonPassages?.Add(new JsonObject { ["document"] = title, ["section"] = p.Section, ["page"] = p.Page, ["text"] = p.Text, ["id"] = nextId });
                        nextId++;
                    }
                    await tx.CommitAsync();
                    json?["documents"]?.AsArray().Add(new JsonObject { ["document"] = title, ["source"] = source, ["folder"] = "Imported" });
                    changedJson = true;
                    newTitles.Add(title);
                    results.Add(new(name, title, passages.Count, null));
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Content import failed for {File}", name);
                    results.Add(new(name, "", 0, "Could not read this file (" + ex.GetType().Name + ")."));
                }
            }

            if (changedJson && json is not null)
                await File.WriteAllTextAsync(_jsonPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), new UTF8Encoding(false));
            if (newTitles.Count > 0) AppendCategories(newTitles, category);
        }
        finally { Gate.Release(); }
        return results;
    }

    // ---------- database helpers ----------
    private static async Task<string?> ScalarAsync(SqliteConnection c, string sql, params (string, object)[] ps)
    {
        await using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        var r = await cmd.ExecuteScalarAsync();
        return r is null or DBNull ? null : Convert.ToString(r);
    }
    private static async Task ExecAsync(SqliteConnection c, string sql, params (string, object)[] ps)
    {
        await using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        foreach (var (n, v) in ps) cmd.Parameters.AddWithValue(n, v);
        await cmd.ExecuteNonQueryAsync();
    }
    private static async Task RemoveDocumentAsync(SqliteConnection c, string title)
    {
        await ExecAsync(c, "DELETE FROM passages WHERE document = $t", ("$t", title));
        await ExecAsync(c, "DELETE FROM sections WHERE document_id IN (SELECT id FROM documents WHERE title = $t)", ("$t", title));
        await ExecAsync(c, "DELETE FROM documents WHERE title = $t", ("$t", title));
    }
    private static void RemoveFromJson(JsonNode? json, string title, string source)
    {
        if (json is null) return;
        var ps = json["passages"]?.AsArray(); var ds = json["documents"]?.AsArray();
        if (ps is not null) for (var i = ps.Count - 1; i >= 0; i--) if ((string?)ps[i]?["document"] == title) ps.RemoveAt(i);
        if (ds is not null) for (var i = ds.Count - 1; i >= 0; i--) if ((string?)ds[i]?["source"] == source) ds.RemoveAt(i);
    }
    private void AppendCategories(List<string> titles, string category)
    {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        if (File.Exists(_catPath))
        {
            var text = File.ReadAllText(_catPath);
            foreach (var l in text.Split('\n').Skip(1)) { var d = l.Trim().Split(',')[0].Trim('"'); if (d.Length > 0) known.Add(d); }
            if (text.Length > 0 && !text.EndsWith('\n')) sb.Append('\n');
        }
        else { Directory.CreateDirectory(Path.GetDirectoryName(_catPath)!); sb.Append("document,category\n"); }
        foreach (var t in titles.Where(t => !known.Contains(t)))
            sb.Append('"').Append(t.Replace("\"", "\"\"")).Append("\",").Append(category).Append('\n');
        File.AppendAllText(_catPath, sb.ToString());
    }

    // ---------- chunking ----------
    private static bool IsToc(string t) => t.Contains("table of contents", StringComparison.OrdinalIgnoreCase) || DotLeader.Matches(t).Count >= 2;

    private static IEnumerable<string> SplitLong(string unit)
    {
        if (unit.Length <= Max) { yield return unit; yield break; }
        var cur = "";
        foreach (var s in Regex.Split(unit, @"(?<=[.!?])\s+"))
        {
            if (cur.Length > 0 && cur.Length + 1 + s.Length > Max) { yield return cur; cur = s; }
            else cur = (cur + " " + s).Trim();
        }
        if (cur.Length > 0) yield return cur;
    }

    private static List<string> Chunk(IEnumerable<string> lines)
    {
        var outp = new List<string>(); var cur = "";
        foreach (var line in lines)
            foreach (var part in SplitLong(line))
            {
                if (cur.Length > 0 && cur.Length + 1 + part.Length > Max) { outp.Add(cur); cur = part; }
                else cur = cur.Length > 0 ? cur + "\n" + part : part;
            }
        if (cur.Length > 0) outp.Add(cur);
        return outp;
    }

    // ---------- readers: (section, page, lines) ----------
    private static List<(string, string, List<string>)> ReadDocx(Stream s)
    {
        using var zip = new ZipArchive(s, ZipArchiveMode.Read);
        var doc = XDocument.Load(zip.GetEntry("word/document.xml")!.Open());
        var body = doc.Root!.Element(W + "body")!;
        var result = new List<(string, string, List<string>)>();
        var heading = "Introduction"; var lines = new List<string>();
        void Flush() { if (lines.Count > 0) result.Add((heading, "n/a", new List<string>(lines))); lines.Clear(); }
        static string ParaText(XElement p) => string.Concat(p.Descendants(W + "t").Select(t => t.Value)).Trim();

        foreach (var el in body.Elements())
        {
            if (el.Name == W + "p")
            {
                var txt = ParaText(el); if (txt.Length == 0) continue;
                var style = el.Element(W + "pPr")?.Element(W + "pStyle")?.Attribute(W + "val")?.Value ?? "";
                if (Regex.IsMatch(style, "^(heading\\d|title)", RegexOptions.IgnoreCase)) { Flush(); heading = txt; }
                else lines.Add(el.Element(W + "pPr")?.Element(W + "numPr") is not null ? "• " + txt : txt);
            }
            else if (el.Name == W + "tbl")
                foreach (var tr in el.Descendants(W + "tr"))
                {
                    var cells = tr.Elements(W + "tc").Select(tc => string.Join(" ", tc.Descendants(W + "p").Select(ParaText)).Trim()).Where(c => c.Length > 0);
                    var row = string.Join(" | ", cells); if (row.Length > 0) lines.Add(row);
                }
        }
        Flush();
        return result;
    }

    private static List<(string, string, List<string>)> ReadPptx(Stream s)
    {
        using var zip = new ZipArchive(s, ZipArchiveMode.Read);
        var result = new List<(string, string, List<string>)>();
        var slides = zip.Entries.Select(e => (e, m: Regex.Match(e.FullName, @"^ppt/slides/slide(\d+)\.xml$"))).Where(x => x.m.Success)
            .OrderBy(x => int.Parse(x.m.Groups[1].Value));
        foreach (var (e, m) in slides)
        {
            var n = int.Parse(m.Groups[1].Value).ToString();
            var lines = XDocument.Load(e.Open()).Descendants(A + "p")
                .Select(p => string.Concat(p.Descendants(A + "t").Select(t => t.Value)).Trim()).Where(t => t.Length > 0).ToList();
            if (lines.Count > 0) result.Add(("Slide " + n, n, lines));
        }
        return result;
    }

    private static List<(string, string, List<string>)> ReadPdf(Stream s)
    {
        var result = new List<(string, string, List<string>)>();
        using var pdf = PdfDocument.Open(s);
        foreach (var page in pdf.GetPages())
        {
            var raw = page.Text.Replace("", "\n•").Replace("", "\n•")
                .Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            // PdfPig often returns a page as one long line; rebuild lines from the words' vertical positions instead.
            var words = page.GetWords().ToList();
            if (words.Count > 0)
            {
                raw = words.GroupBy(w => Math.Round(w.BoundingBox.Bottom / 3.0))
                    .OrderByDescending(g => g.Key)
                    .Select(g => string.Join(" ", g.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text))).ToList();
            }
            var lines = new List<string>();
            foreach (var l in raw)
            {
                if (Regex.IsMatch(l, @"^\d{1,3}$")) continue;                       // stray page numbers
                if (lines.Count > 0 && !l.StartsWith("•") && !l.StartsWith("- ") && !Regex.IsMatch(lines[^1], @"[.!?:]$") && lines[^1].Length >= 55)
                    lines[^1] += " " + l;                                           // join hard-wrapped lines
                else lines.Add(l);
            }
            if (lines.Count > 0) result.Add(("Page " + page.Number, page.Number.ToString(), lines));
        }
        return result;
    }
}
