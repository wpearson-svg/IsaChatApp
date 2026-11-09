using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IsaChatApp.Services.Rules;

/// <summary>
/// What the rules decided for one question (or one answer). Outcome is "pass" (no rule fired), "answer_then_point",
/// "hand_off" or "stop". Only rule IDs, file names and versions are ever logged, never the question text.
/// </summary>
public sealed record RuleDecision(
    string Outcome,
    string? RuleId,
    string? File,
    int Version,
    string Layer,
    IReadOnlyList<string> Fired,
    string? Message,
    string? ExtraLine,
    string? PointLine,
    bool OfferHandoff,
    string? Route,
    IReadOnlyList<string> Flags)
{
    public static RuleDecision Pass { get; } = new("pass", null, null, 0, "", Array.Empty<string>(), null, null, null, false, null, Array.Empty<string>());
}

/// <summary>One rule's row in the rule tester: whether it fired and what it would do.</summary>
public sealed record RuleTrace(string RuleId, string Title, string File, int Version, string Layer, string Outcome, bool Fired, bool Winner);

/// <summary>One loaded rule, for the list on the Dev Dashboard.</summary>
public sealed record RuleSummary(string RuleId, string Title, string Group, string File, string Layer, string Outcome);

/// <summary>
/// Isa's policy adapter: reads ISACA's rules files from rules/ (global, then region, country, chapter), combines the layers
/// that apply to a leader's chapter, and decides what happens to a question (and, after the answer is written, to the answer).
/// Design: docs/Isa_Rules_Design.md. The same engine serves the live Input Compliance stage, the output rule recheck and the rule tester.
///
/// Matching: text clean-up (lower case, "t@xes" and "t a x" become "taxes"), then a rule fires when one topic signal AND one kind signal
/// both match at the start of a word (no kind signals = the topic alone is enough) and no "unless" phrase matches.
/// A lower-layer rule that "tightens" a higher rule inherits its signals and exceptions.
/// Conflict order: safety hand-off, stop, hand off, answer then point, answer. The most careful rule wins.
/// Files are re-read when they change on disk. A file with no approval record, not active, or that loosens a higher rule is refused and logged.
/// </summary>
public sealed class RuleEngine
{
    private static readonly Dictionary<string, int> Severity = new()
    {
        ["pass"] = 0, ["answer"] = 0, ["answer_then_point"] = 1, ["hand_off"] = 2, ["stop"] = 3,
    };
    private const int SafetySeverity = 4;

    private sealed class Rule
    {
        public string Id = "", Title = "", Group = "", Outcome = "", Route = "", Message = "", ExtraLine = "", PointLine = "", Tightens = "";
        public bool OfferHandoff;
        public List<string> Flags = new(), Topic = new(), Kind = new(), Unless = new(), AnswerMustNot = new();
        public List<string>? Roles;   // null = all
        public JsonElement Tests;
        public RuleFile File = null!;
    }

    private sealed class RuleFile
    {
        public string Name = "", Layer = "", Scope = "";
        public int Version;
        public List<Rule> Rules = new();
    }

    private sealed class State
    {
        public List<RuleFile> Files = new();
        public Dictionary<string, (string Country, string Region)> Chapters = new(StringComparer.OrdinalIgnoreCase);
        public DateTime Stamp;
    }

    private readonly string _rulesDir;
    private readonly string _chaptersCsv;
    private readonly ILogger<RuleEngine> _log;
    private readonly object _lock = new();
    private State _state = new();
    private DateTime _lastCheck = DateTime.MinValue;
    private static readonly ConcurrentDictionary<string, Regex> SignalCache = new();

    public RuleEngine(IHostEnvironment env, IConfiguration config, ILogger<RuleEngine> log)
    {
        _log = log;
        _rulesDir = Path.Combine(env.ContentRootPath, config["Rules:Folder"] ?? "rules");
        _chaptersCsv = Path.Combine(env.ContentRootPath, "directory", "chapters.csv");
        Reload();
        if (config.GetValue("Rules:SelfTest", true)) _ = Task.Run(() => LogSelfCheck());
    }

    public int FileCount => Current().Files.Count;
    public int RuleCount => Current().Files.Sum(f => f.Rules.Count);

    // ---------- loading ----------

    public void Reload()
    {
        lock (_lock)
        {
            var st = new State { Stamp = Stamp() };
            try
            {
                if (File.Exists(_chaptersCsv)) st.Chapters = ReadChapters();
                if (Directory.Exists(_rulesDir))
                {
                    var paths = Directory.GetFiles(_rulesDir, "*.json", SearchOption.AllDirectories)
                        .Where(p => !string.Equals(Path.GetFileName(Path.GetDirectoryName(p)), "tests", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => Path.GetRelativePath(_rulesDir, p).Replace('\\', '/'), StringComparer.Ordinal);
                    foreach (var p in paths)
                    {
                        var name = Path.GetRelativePath(_rulesDir, p).Replace('\\', '/');
                        try { if (ParseFile(name, p) is RuleFile f) st.Files.Add(f); }
                        catch (Exception ex) { _log.LogWarning("Rules file {File} refused: {Reason}", name, ex.Message); }
                    }
                    RefuseLoosening(st);
                }
            }
            catch (Exception ex) { _log.LogWarning("Rules could not be loaded: {Reason}", ex.Message); }
            _state = st;
            _lastCheck = DateTime.UtcNow;
            _log.LogInformation("Rules loaded: {Files} files, {Rules} rules", st.Files.Count, st.Files.Sum(f => f.Rules.Count));
        }
    }

    private DateTime Stamp()
    {
        var latest = DateTime.MinValue;
        if (File.Exists(_chaptersCsv)) latest = File.GetLastWriteTimeUtc(_chaptersCsv);
        if (Directory.Exists(_rulesDir))
            foreach (var p in Directory.GetFiles(_rulesDir, "*.json", SearchOption.AllDirectories))
            { var t = File.GetLastWriteTimeUtc(p); if (t > latest) latest = t; }
        return latest;
    }

    private State Current()
    {
        if ((DateTime.UtcNow - _lastCheck).TotalSeconds > 3)
        {
            _lastCheck = DateTime.UtcNow;
            if (Stamp() != _state.Stamp) Reload();
        }
        return _state;
    }

    private Dictionary<string, (string, string)> ReadChapters()
    {
        var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        var lines = File.ReadAllLines(_chaptersCsv);
        if (lines.Length == 0) return map;
        var head = SplitCsv(lines[0].TrimStart('﻿'));
        int id = head.IndexOf("Chapter ID"), country = head.IndexOf("Country"), region = head.IndexOf("Region");
        if (id < 0 || country < 0 || region < 0) return map;
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = SplitCsv(line);
            if (c.Count > Math.Max(id, Math.Max(country, region))) map[c[id]] = (c[country], c[region]);
        }
        return map;
    }

    private static List<string> SplitCsv(string line)
    {
        var cells = new List<string>(); var cur = new System.Text.StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quoted) { if (ch == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; } else quoted = false; } else cur.Append(ch); }
            else if (ch == '"') quoted = true;
            else if (ch == ',') { cells.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(ch);
        }
        cells.Add(cur.ToString());
        return cells;
    }

    private static List<string> Strings(JsonElement parent, string name)
    {
        var list = new List<string>();
        if (parent.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array)
            foreach (var e in arr.EnumerateArray()) if (e.ValueKind == JsonValueKind.String) list.Add(e.GetString() ?? "");
        return list;
    }

    private static string Str(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private RuleFile? ParseFile(string name, string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement.Clone();
        if (Str(root, "status") != "active") return null;   // retired files stay on disk but are not applied
        if (string.IsNullOrWhiteSpace(Str(root, "approved_by")) || string.IsNullOrWhiteSpace(Str(root, "approval_reference")))
            throw new InvalidDataException("no approval record");
        var layer = Str(root, "layer");
        if (layer is not ("global" or "region" or "country" or "chapter")) throw new InvalidDataException("unknown layer");
        var file = new RuleFile
        {
            Name = name, Layer = layer, Scope = Str(root, "scope"),
            Version = root.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0,
        };
        if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array) throw new InvalidDataException("no rules list");
        foreach (var r in rules.EnumerateArray())
        {
            var rule = new Rule
            {
                Id = Str(r, "id"), Title = Str(r, "title"), Group = Str(r, "group"), Outcome = Str(r, "outcome"), Route = Str(r, "route"),
                Message = Str(r, "message"), ExtraLine = Str(r, "extra_line"), PointLine = Str(r, "point_line"), Tightens = Str(r, "tightens"),
                OfferHandoff = r.TryGetProperty("offer_handoff", out var oh) && oh.ValueKind == JsonValueKind.True,
                Flags = Strings(r, "flags"), Topic = Strings(r, "topic_signals"), Kind = Strings(r, "kind_signals"),
                Unless = Strings(r, "unless"), AnswerMustNot = Strings(r, "answer_must_not"), File = file,
            };
            if (r.TryGetProperty("applies_to_roles", out var roles) && roles.ValueKind == JsonValueKind.Array) rule.Roles = Strings(r, "applies_to_roles");
            if (r.TryGetProperty("tests", out var tests)) rule.Tests = tests.Clone();
            if (rule.Id.Length == 0 || !Severity.ContainsKey(rule.Outcome)) throw new InvalidDataException($"rule {rule.Id}: bad id or outcome");
            file.Rules.Add(rule);
        }
        return file;
    }

    /// <summary>A lower-layer rule may only make a higher rule stricter. A file that loosens one is refused.</summary>
    private void RefuseLoosening(State st)
    {
        var byId = st.Files.SelectMany(f => f.Rules).GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.First());
        foreach (var f in st.Files.ToList())
            foreach (var r in f.Rules)
            {
                if (r.Tightens.Length == 0) continue;
                if (!byId.TryGetValue(r.Tightens, out var parent)) { _log.LogWarning("Rules file {File} refused: {Rule} tightens unknown rule {Parent}", f.Name, r.Id, r.Tightens); st.Files.Remove(f); break; }
                if (SeverityOf(r) < SeverityOf(parent)) { _log.LogWarning("Rules file {File} refused: {Rule} loosens {Parent}", f.Name, r.Id, parent.Id); st.Files.Remove(f); break; }
            }
    }

    // ---------- matching ----------

    /// <summary>Lower case, tidy quotes, undo simple disguises ("t@xes", "t a x e s").</summary>
    public static string Clean(string text)
    {
        var t = (text ?? "").ToLowerInvariant().Replace('’', '\'').Replace('‘', '\'').Replace('@', 'a').Replace('$', 's');
        return Regex.Replace(t, @"\b(?:[a-z] ){2,}[a-z]\b", m => m.Value.Replace(" ", ""));
    }

    private static bool Has(string signal, string cleanedText)
    {
        var rx = SignalCache.GetOrAdd(signal, s => new Regex("(?<![a-z0-9])" + Regex.Escape(Clean(s)), RegexOptions.CultureInvariant));
        return rx.IsMatch(cleanedText);
    }

    private static int SeverityOf(Rule r) =>
        r.Outcome == "hand_off" && r.Flags.Contains("urgent") && r.Group == "A" ? SafetySeverity : Severity[r.Outcome];

    private static bool RoleApplies(Rule r, string? role) => r.Roles is null || (role is not null && r.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));

    private List<Rule> RulesFor(State st, string? chapterId)
    {
        st.Chapters.TryGetValue(chapterId ?? "", out var ch);
        var known = chapterId is not null && st.Chapters.ContainsKey(chapterId);
        var files = st.Files.Where(f =>
            f.Layer == "global" ||
            (known && f.Layer == "region" && string.Equals(f.Scope, ch.Region, StringComparison.OrdinalIgnoreCase)) ||
            (known && f.Layer == "country" && string.Equals(f.Scope, ch.Country, StringComparison.OrdinalIgnoreCase)) ||
            (f.Layer == "chapter" && string.Equals(f.Scope, chapterId, StringComparison.OrdinalIgnoreCase)));
        return files.SelectMany(f => f.Rules).ToList();
    }

    private static bool Fires(Rule r, string text, string? role, Dictionary<string, Rule> byId)
    {
        if (!RoleApplies(r, role)) return false;
        var topic = new List<string>(r.Topic); var kind = new List<string>(r.Kind); var unless = new List<string>(r.Unless);
        if (r.Tightens.Length > 0 && byId.TryGetValue(r.Tightens, out var parent))
        { topic.AddRange(parent.Topic); kind.AddRange(parent.Kind); unless.AddRange(parent.Unless); }
        if (!topic.Any(s => Has(s, text))) return false;
        if (kind.Count > 0 && !kind.Any(s => Has(s, text))) return false;
        return !unless.Any(s => Has(s, text));
    }

    private static Dictionary<string, Rule> ById(List<Rule> rules)
    {
        var d = new Dictionary<string, Rule>();
        foreach (var r in rules) d.TryAdd(r.Id, r);
        return d;
    }

    private static RuleDecision ToDecision(Rule winner, IReadOnlyList<string> fired) =>
        new(winner.Outcome, winner.Id, winner.File.Name, winner.File.Version, winner.File.Layer, fired,
            winner.Message, winner.ExtraLine, winner.PointLine, winner.OfferHandoff, winner.Route, winner.Flags);

    private static Rule? Winner(IEnumerable<Rule> fired)
    {
        Rule? best = null;
        foreach (var r in fired) if (best is null || SeverityOf(r) > SeverityOf(best)) best = r;   // first of equals wins
        return best;
    }

    // ---------- the two checkpoints ----------

    /// <summary>Input checkpoint: what should happen to this question, for a leader of this chapter and role.</summary>
    public RuleDecision Decide(string question, string? chapterId, string? role)
    {
        var st = Current();
        var rules = RulesFor(st, chapterId);
        var byId = ById(rules);
        var text = Clean(question);
        var fired = rules.Where(r => Fires(r, text, role, byId)).ToList();
        var winner = Winner(fired);
        return winner is null ? RuleDecision.Pass : ToDecision(winner, fired.Select(r => r.Id).ToList());
    }

    /// <summary>
    /// Output checkpoint: does a written answer say something an applicable rule forbids (its answer_must_not phrases)?
    /// Returns null when it is fine. It can only tighten: the result is a hand-off or a stop, never a looser outcome.
    /// </summary>
    public RuleDecision? CheckAnswer(string answer, string? chapterId, string? role)
    {
        var st = Current();
        var rules = RulesFor(st, chapterId);
        var byId = ById(rules);
        var text = Clean(answer);
        var hits = new List<Rule>();
        foreach (var r in rules)
        {
            if (!RoleApplies(r, role)) continue;
            var source = r.Tightens.Length > 0 && byId.TryGetValue(r.Tightens, out var p) ? p : r;
            var phrases = r.AnswerMustNot.Concat(source.AnswerMustNot);
            if (phrases.Any(ph => Has(ph, text))) hits.Add(r);
        }
        var winner = Winner(hits);
        if (winner is null) return null;
        var d = ToDecision(winner, hits.Select(r => r.Id).ToList());
        return Severity[d.Outcome] < Severity["hand_off"] ? d with { Outcome = "hand_off" } : d;
    }

    /// <summary>For the rule tester: every applicable rule, whether it fired for this question, and which one won.</summary>
    public IReadOnlyList<RuleTrace> Trace(string question, string? chapterId, string? role)
    {
        var st = Current();
        var rules = RulesFor(st, chapterId);
        var byId = ById(rules);
        var text = Clean(question);
        var fired = rules.Where(r => Fires(r, text, role, byId)).ToList();
        var winner = Winner(fired);
        return rules.Select(r => new RuleTrace(r.Id, r.Title, r.File.Name, r.File.Version, r.File.Layer, r.Outcome, fired.Contains(r), ReferenceEquals(r, winner)))
            .OrderByDescending(t => t.Winner).ThenByDescending(t => t.Fired).ToList();
    }

    /// <summary>The chapter's country and region from directory/chapters.csv (never from what the person typed).</summary>
    public (string Country, string Region)? ChapterInfo(string chapterId) =>
        Current().Chapters.TryGetValue(chapterId, out var c) ? c : null;

    /// <summary>Every rule that is loaded, from all layers (read-only; for the Dev Dashboard).</summary>
    public IReadOnlyList<RuleSummary> AllRules() =>
        Current().Files.SelectMany(f => f.Rules.Select(r => new RuleSummary(r.Id, r.Title, r.Group, f.Name, f.Layer, r.Outcome))).ToList();

    public IReadOnlyList<string> ChapterIds => Current().Chapters.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    // ---------- self-check (every rule's own tests, and the rehearsed demo questions) ----------

    private static bool Meets(string expect, string outcome, IReadOnlyList<string> fired)
    {
        if (expect.StartsWith("not ")) return !fired.Contains(expect[4..]);
        if (expect is "answer" or "pass") return outcome == "pass";
        return outcome == expect;
    }

    public (int Checked, List<string> Problems) SelfCheck()
    {
        var st = Current();
        var problems = new List<string>(); var checkedCount = 0;
        var all = st.Files.SelectMany(f => f.Rules).ToList();
        var allById = ById(all);
        foreach (var rule in all)
        {
            if (rule.Tests.ValueKind != JsonValueKind.Object) continue;
            if (rule.Tests.TryGetProperty("questions", out var qs) && qs.ValueKind == JsonValueKind.Array)
                foreach (var t in qs.EnumerateArray())
                {
                    checkedCount++;
                    var d = Decide(Str(t, "text"), Str(t, "as_chapter"), Str(t, "as_role"));
                    if (!Meets(Str(t, "expect"), d.Outcome, d.Fired))
                        problems.Add($"{rule.Id}: a test question as {Str(t, "as_chapter")} expected {Str(t, "expect")}, got {d.Outcome} ({d.RuleId})");
                }
            if (rule.Tests.TryGetProperty("answers_must_be_stopped", out var ans) && ans.ValueKind == JsonValueKind.Array)
                foreach (var a in ans.EnumerateArray())
                {
                    checkedCount++;
                    var source = rule.Tightens.Length > 0 && allById.TryGetValue(rule.Tightens, out var p) ? p : rule;
                    var text = Clean(a.GetString() ?? "");
                    if (!source.AnswerMustNot.Any(ph => Has(ph, text)))
                        problems.Add($"{rule.Id}: a listed answer would not be stopped by the output checkpoint");
                }
        }
        var demoPath = Path.Combine(_rulesDir, "tests", "rehearsed_demo.json");
        if (File.Exists(demoPath))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(demoPath));
            var root = doc.RootElement;
            foreach (var q in root.GetProperty("questions").EnumerateArray())
            {
                checkedCount++;
                var d = Decide(Str(q, "text"), Str(root, "as_chapter"), Str(root, "as_role"));
                var wantRule = Str(q, "rule");
                var ok = Meets(Str(q, "expect"), d.Outcome, d.Fired) && (wantRule.Length == 0 ? d.RuleId is null : d.RuleId == wantRule);
                if (!ok) problems.Add($"Rehearsed demo question: today {Str(q, "today")}, would now be {d.Outcome} ({d.RuleId})");
            }
        }
        return (checkedCount, problems);
    }

    private void LogSelfCheck()
    {
        try
        {
            var (n, problems) = SelfCheck();
            _log.LogInformation("Rules self-check: {Checks} checks, {Problems} problems", n, problems.Count);
            foreach (var p in problems) _log.LogWarning("Rules self-check: {Problem}", p);
        }
        catch (Exception ex) { _log.LogWarning("Rules self-check could not run: {Reason}", ex.Message); }
    }
}
