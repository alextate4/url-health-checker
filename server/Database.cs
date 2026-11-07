using Dapper;
using Microsoft.Data.Sqlite;

// One check read back from check_results (the newest one, a recent one, or one from history).
public record SavedCheck(string CheckedAt, long? StatusCode, long ResponseTimeMs, bool Healthy, string? Error);

// One row of the dashboard: a URL plus its newest check (null before the first check).
public record UrlStatus(long Id, string Url, long IntervalSeconds, SavedCheck? LastCheck);

// Numbers for one URL over the chosen window. Percent and average are null when there is nothing to count.
public record WindowSummary(long CheckCount, double? UptimePercent, double? AvgResponseTimeMs);

// What /api/urls returns for each URL: the Phase 1 fields plus the window summary.
public record DashboardRow(long Id, string Url, long IntervalSeconds, SavedCheck? LastCheck, WindowSummary Summary);

public class Database
{
    // No migrations (D-005): changing a column means editing this script and deleting the .db file.
    private const string SchemaScript = """
        CREATE TABLE IF NOT EXISTS urls (
            id INTEGER PRIMARY KEY,
            url TEXT NOT NULL,
            interval_seconds INTEGER NOT NULL,
            created_at TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS check_results (
            id INTEGER PRIMARY KEY,
            url_id INTEGER NOT NULL REFERENCES urls(id),
            checked_at TEXT NOT NULL,
            status_code INTEGER NULL,
            response_time_ms INTEGER NOT NULL,
            healthy INTEGER NOT NULL,
            error TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS ix_check_results_url_id_checked_at
            ON check_results (url_id, checked_at);
        """;

    // Seeds are chosen so the first run shows every state: healthy, 404 with a code,
    // and a DNS failure with an error reason. ".invalid" can never resolve (RFC 2606).
    private static readonly (string Url, int IntervalSeconds)[] Seeds =
    [
        ("https://example.com", 30),
        ("https://github.com", 60),
        ("https://github.com/this-page-should-not-exist-rosnet", 30),
        ("https://unreachable.invalid", 30),
    ];

    private readonly string _connectionString;

    public Database(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(SchemaScript);

        var urlCount = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM urls");
        if (urlCount > 0)
        {
            return; // Seed only an empty table, so a restart does not add duplicates.
        }

        foreach (var seed in Seeds)
        {
            await connection.ExecuteAsync(
                "INSERT INTO urls (url, interval_seconds, created_at) VALUES (@Url, @IntervalSeconds, @CreatedAt)",
                new { seed.Url, seed.IntervalSeconds, CreatedAt = DateTime.UtcNow.ToString("o") });
        }
    }

    public async Task<List<UrlStatus>> GetUrlStatusesAsync()
    {
        // "Last checked" is not stored on urls (one source of truth): the newest check_results row is it.
        const string sql = """
            SELECT u.id AS Id,
                   u.url AS Url,
                   u.interval_seconds AS IntervalSeconds,
                   r.checked_at AS CheckedAt,
                   r.status_code AS StatusCode,
                   r.response_time_ms AS ResponseTimeMs,
                   r.healthy AS Healthy,
                   r.error AS Error
            FROM urls u
            LEFT JOIN check_results r ON r.id = (
                SELECT id FROM check_results
                WHERE url_id = u.id
                ORDER BY checked_at DESC
                LIMIT 1)
            ORDER BY u.id
            """;

        using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<StatusRow>(sql);

        return rows.Select(row => new UrlStatus(
            row.Id,
            row.Url,
            row.IntervalSeconds,
            row.CheckedAt is null
                ? null
                : new SavedCheck(row.CheckedAt, row.StatusCode, row.ResponseTimeMs ?? 0, row.Healthy == 1, row.Error)))
            .ToList();
    }

    // One request feeds the whole dashboard. The window cutoff comes in as a parameter, so a test can call this directly.
    public async Task<List<DashboardRow>> GetDashboardAsync(DateTime since)
    {
        var statuses = await GetUrlStatusesAsync();
        var rows = new List<DashboardRow>();

        // One small query per URL is fine here: SQLite runs inside our process, so there is no network trip (D-018).
        foreach (var status in statuses)
        {
            var summary = await GetWindowSummaryAsync(status.Id, since);
            rows.Add(new DashboardRow(status.Id, status.Url, status.IntervalSeconds, status.LastCheck, summary));
        }

        return rows;
    }

    public async Task<WindowSummary> GetWindowSummaryAsync(long urlId, DateTime since)
    {
        // healthy is 1 or 0, so its average is the share of healthy checks.
        // AVG skips NULL, so only checks that got an HTTP response count toward the average time (D-019).
        const string sql = """
            SELECT COUNT(*) AS CheckCount,
                   ROUND(AVG(healthy) * 100, 1) AS UptimePercent,
                   ROUND(AVG(CASE WHEN status_code IS NOT NULL THEN response_time_ms END)) AS AvgResponseTimeMs
            FROM check_results
            WHERE url_id = @UrlId AND checked_at >= @Since
            """;

        using var connection = new SqliteConnection(_connectionString);
        var stats = await connection.QuerySingleAsync<StatsRow>(sql, new { UrlId = urlId, Since = SinceText(since) });

        return new WindowSummary(stats.CheckCount, stats.UptimePercent, stats.AvgResponseTimeMs);
    }

    // Stored times are "o" text ending in Z. A DateTime parameter would be written in another format
    // (space, no Z), and text comparison would then let old rows into the window (D1-08).
    private static string SinceText(DateTime since) => since.ToString("o");

    // Always INSERT, never UPDATE: every check is its own row, which is what builds the history.
    public async Task InsertCheckResultAsync(long urlId, CheckResult result)
    {
        const string sql = """
            INSERT INTO check_results (url_id, checked_at, status_code, response_time_ms, healthy, error)
            VALUES (@UrlId, @CheckedAt, @StatusCode, @ResponseTimeMs, @Healthy, @Error)
            """;

        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            UrlId = urlId,
            CheckedAt = DateTime.UtcNow.ToString("o"),
            result.StatusCode,
            result.ResponseTimeMs,
            Healthy = result.Healthy ? 1 : 0,
            result.Error,
        });
    }

    private class StatsRow
    {
        public long CheckCount { get; set; }
        public double? UptimePercent { get; set; }
        public double? AvgResponseTimeMs { get; set; }
    }

    // SQLite hands back every INTEGER as long, and Dapper cannot fill the positional records above
    // from that. So Dapper fills this plain class and we build the records by hand.
    private class StatusRow
    {
        public long Id { get; set; }
        public string Url { get; set; } = "";
        public long IntervalSeconds { get; set; }
        public string? CheckedAt { get; set; }
        public long? StatusCode { get; set; }
        public long? ResponseTimeMs { get; set; }
        public long? Healthy { get; set; }
        public string? Error { get; set; }
    }
}
