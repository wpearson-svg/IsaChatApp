using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services;

/// <summary>
/// The key for the Content Health API (GET /api/content-health), 16 November 2026, Willis.
///
/// One key for the prototype. It is shown on the Content Health Dashboard, so anyone who can open the dashboard can copy
/// it, and a program sends it (header X-Isa-Key) as proof of that access. "Replace key" makes a new one and the old one
/// stops working on the next call. Kept in data/apikeys.db (git-ignored, local to each computer); made the first time
/// the dashboard or the API asks for it.
///
/// For a real deployment: one key per person or program, so calls can be traced to whoever made them.
/// </summary>
public class ApiKeyService
{
    public const string HeaderName = "X-Isa-Key";

    public sealed record KeyInfo(string Key, DateTime CreatedUtc, DateTime? LastUsedUtc);

    private readonly string _dbPath;
    private readonly object _lock = new();

    public ApiKeyService(IConfiguration config)
    {
        _dbPath = config["ApiKeys:Path"] ?? Path.Combine("data", "apikeys.db");
        var folder = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS api_key(
              id            INTEGER PRIMARY KEY CHECK (id = 1),   -- only ever one row
              key           TEXT NOT NULL,
              created_utc   TEXT NOT NULL,
              last_used_utc TEXT);
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>The current key, made now if there isn't one yet.</summary>
    public KeyInfo Get()
    {
        lock (_lock)
        {
            using var conn = Open();
            var info = Read(conn);
            if (info is not null) return info;
            Write(conn, NewKey());
            return Read(conn)!;
        }
    }

    /// <summary>Makes a new key. The old one stops working at once.</summary>
    public KeyInfo Replace()
    {
        lock (_lock)
        {
            using var conn = Open();
            Write(conn, NewKey());
            return Read(conn)!;
        }
    }

    /// <summary>True if this is the current key (and notes that it was used).</summary>
    public bool IsValid(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var current = Get().Key;
        var ok = CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(key.Trim()), System.Text.Encoding.UTF8.GetBytes(current));
        if (!ok) return false;
        lock (_lock)
        {
            using var conn = Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE api_key SET last_used_utc = $at WHERE id = 1";
            cmd.Parameters.AddWithValue("$at", Now());
            cmd.ExecuteNonQuery();
        }
        return true;
    }

    private static void Write(SqliteConnection conn, string key)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO api_key(id, key, created_utc, last_used_utc) VALUES (1, $k, $at, NULL)";
        cmd.Parameters.AddWithValue("$k", key);
        cmd.Parameters.AddWithValue("$at", Now());
        cmd.ExecuteNonQuery();
    }

    private static KeyInfo? Read(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT key, created_utc, last_used_utc FROM api_key WHERE id = 1";
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new KeyInfo(r.GetString(0), Parse(r.GetString(1)) ?? DateTime.UtcNow, r.IsDBNull(2) ? null : Parse(r.GetString(2)));
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    private static string NewKey() =>
        "isa_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    private static DateTime? Parse(string s) =>
        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d) ? d : null;
}
