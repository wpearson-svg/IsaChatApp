using Microsoft.Data.Sqlite;

namespace IsaChatApp.Services;

/// <summary>
/// T9 — Event log. Separate SQLite file, WAL mode.
/// Only the Orchestrator writes. Stages never write directly.
/// </summary>
public class EventLogService
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public EventLogService(IConfiguration config)
    {
        _dbPath = config["EventLog:Path"] ?? Path.Combine("data", "events.db");
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS events (
                id        INTEGER PRIMARY KEY AUTOINCREMENT,
                trace_id  TEXT    NOT NULL,
                stage     TEXT    NOT NULL,
                timestamp TEXT    NOT NULL,
                data      TEXT    NOT NULL DEFAULT ''
            );
            CREATE INDEX IF NOT EXISTS idx_events_trace ON events(trace_id);
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    public async Task WriteAsync(string traceId, string stage, string data)
    {
        await _writeLock.WaitAsync();
        try
        {
            await using var conn = Open();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO events (trace_id, stage, timestamp, data)
                VALUES ($trace, $stage, $ts, $data);
                """;
            cmd.Parameters.AddWithValue("$trace", traceId);
            cmd.Parameters.AddWithValue("$stage", stage);
            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("$data", data ?? "");
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IReadOnlyList<EventRow>> GetByTraceIdAsync(string traceId)
    {
        await using var conn = Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT trace_id, stage, timestamp, data
            FROM events
            WHERE trace_id = $trace
            ORDER BY id ASC;
            """;
        cmd.Parameters.AddWithValue("$trace", traceId);

        var list = new List<EventRow>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new EventRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)));
        }
        return list;
    }
}

public record EventRow(string TraceId, string Stage, string Timestamp, string Data);
