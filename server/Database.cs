using Dapper;
using Microsoft.Data.Sqlite;

// One check read back from check_results (the newest one, a recent one, or one from history).
public record SavedCheck(string CheckedAt, long? StatusCode, long ResponseTimeMs, bool Healthy, string? Error);

// One row of the dashboard: a URL plus its newest check (null before the first check).
public record UrlStatus(long Id, string Url, long IntervalSeconds, SavedCheck? LastCheck);

// Numbers for one URL over the chosen window. Percent and average are null when there is nothing to count.
public record WindowSummary(long CheckCount, double? UptimePercent, double? AvgResponseTimeMs, List<SavedCheck> Recent);

// What /api/urls returns for each URL: the Phase 1 fields plus the window summary.
public record DashboardRow(long Id, string Url, long IntervalSeconds, SavedCheck? LastCheck, WindowSummary Summary);

// One row of the All-checks page: live results and archived ones together, so no history is hidden.
public record HistoryRow(long UrlId, string Url, bool UrlRemoved, string CheckedAt, long? StatusCode,
    long ResponseTimeMs, bool Healthy, string? Error, bool Archived);

// One page of a sorted table plus the total, so the UI can show "page 2 of 7".
public record Page<T>(long Total, List<T> Rows);

public class Database
{
    // No migrations (D-005): changing a column means editing this script and deleting the .db file.
    private const string SchemaScript = """
        CREATE TABLE IF NOT EXISTS urls (
            id INTEGER PRIMARY KEY,
            url TEXT NOT NULL,
            interval_seconds INTEGER NOT NULL,
            created_at TEXT NOT NULL,
            deleted_at TEXT NULL
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

        -- Results older than the retention period move here instead of being deleted (D-023).
        CREATE TABLE IF NOT EXISTS check_results_archive (
            id INTEGER PRIMARY KEY,
            url_id INTEGER NOT NULL,
            checked_at TEXT NOT NULL,
            status_code INTEGER NULL,
            response_time_ms INTEGER NOT NULL,
            healthy INTEGER NOT NULL,
            error TEXT NULL
        );

        -- One row per setting. Only retention for now; the default is 30 days.
        CREATE TABLE IF NOT EXISTS settings (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );
        INSERT OR IGNORE INTO settings (key, value) VALUES ('retention_days', '30');
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

        // A database made before soft delete has no deleted_at column. CREATE TABLE IF NOT EXISTS
        // does not change an existing table, so add the column once here (no migrations, D-005).
        var hasDeletedAt = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM pragma_table_info('urls') WHERE name = 'deleted_at'");
        if (hasDeletedAt == 0)
        {
            await connection.ExecuteAsync("ALTER TABLE urls ADD COLUMN deleted_at TEXT NULL");
        }

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
            WHERE u.deleted_at IS NULL
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

    // About 15 minutes of a 30s URL: it fits in one table cell, and the payload does not grow with the window.
    private const int RecentCount = 30;

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

        // Newest 30 only: reading the whole window (up to ~20k rows for 7d) on every 5s poll would be wasteful.
        const string recentSql = """
            SELECT checked_at AS CheckedAt, status_code AS StatusCode, response_time_ms AS ResponseTimeMs,
                   healthy AS Healthy, error AS Error
            FROM check_results
            WHERE url_id = @UrlId AND checked_at >= @Since
            ORDER BY checked_at DESC
            LIMIT @Count
            """;

        var recentRows = await connection.QueryAsync<CheckRow>(recentSql,
            new { UrlId = urlId, Since = SinceText(since), Count = RecentCount });

        // Newest came first for LIMIT; reverse so the strip reads oldest to newest, left to right.
        var recent = recentRows.Select(ToSavedCheck).Reverse().ToList();

        return new WindowSummary(stats.CheckCount, stats.UptimePercent, stats.AvgResponseTimeMs, recent);
    }

    // Every check of one URL inside the window, oldest first (HIST-03). The window filter lives only here.
    public async Task<List<SavedCheck>> GetChecksSinceAsync(long urlId, DateTime since)
    {
        const string sql = """
            SELECT checked_at AS CheckedAt, status_code AS StatusCode, response_time_ms AS ResponseTimeMs,
                   healthy AS Healthy, error AS Error
            FROM check_results
            WHERE url_id = @UrlId AND checked_at >= @Since
            ORDER BY checked_at
            """;

        using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<CheckRow>(sql, new { UrlId = urlId, Since = SinceText(since) });

        return rows.Select(ToSavedCheck).ToList();
    }

    private static SavedCheck ToSavedCheck(CheckRow row) =>
        new(row.CheckedAt, row.StatusCode, row.ResponseTimeMs, row.Healthy == 1, row.Error);

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

    // Returns the new id. The scheduler sees a URL with no checks as due, so it is checked on the next tick.
    public async Task<long> AddUrlAsync(string url, int intervalSeconds)
    {
        const string sql = """
            INSERT INTO urls (url, interval_seconds, created_at) VALUES (@Url, @IntervalSeconds, @CreatedAt);
            SELECT last_insert_rowid();
            """;

        using var connection = new SqliteConnection(_connectionString);
        return await connection.ExecuteScalarAsync<long>(
            sql, new { Url = url, IntervalSeconds = intervalSeconds, CreatedAt = DateTime.UtcNow.ToString("o") });
    }

    // The scheduler reads interval_seconds on every pass, so the new value applies on the next tick (URL-03).
    // Returns false when no URL has this id.
    public async Task<bool> UpdateIntervalAsync(long id, int intervalSeconds)
    {
        using var connection = new SqliteConnection(_connectionString);
        var changed = await connection.ExecuteAsync(
            "UPDATE urls SET interval_seconds = @IntervalSeconds WHERE id = @Id AND deleted_at IS NULL",
            new { Id = id, IntervalSeconds = intervalSeconds });
        return changed > 0;
    }

    // Soft delete: the URL is only marked as removed. The scheduler and the dashboard skip it,
    // but every result it ever had stays in the database and on the All-checks page (D-023).
    // Returns false when no live URL has this id.
    public async Task<bool> SoftDeleteUrlAsync(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var changed = await connection.ExecuteAsync(
            "UPDATE urls SET deleted_at = @Now WHERE id = @Id AND deleted_at IS NULL",
            new { Id = id, Now = DateTime.UtcNow.ToString("o") });
        return changed > 0;
    }

    public async Task<int> GetRetentionDaysAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var value = await connection.ExecuteScalarAsync<string>("SELECT value FROM settings WHERE key = 'retention_days'");
        return int.Parse(value!);
    }

    public async Task SetRetentionDaysAsync(int days)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(
            "UPDATE settings SET value = @Value WHERE key = 'retention_days'", new { Value = days.ToString() });
    }

    // Retention (CONF-02): results older than the cutoff move to check_results_archive. Copy and delete
    // run in one transaction, so a row is never in both tables or in neither. Returns how many moved.
    public async Task<int> ArchiveOlderThanAsync(DateTime cutoff)
    {
        const string copy = """
            INSERT INTO check_results_archive (id, url_id, checked_at, status_code, response_time_ms, healthy, error)
            SELECT id, url_id, checked_at, status_code, response_time_ms, healthy, error
            FROM check_results WHERE checked_at < @Cutoff
            """;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();
        var moved = await connection.ExecuteAsync(copy, new { Cutoff = SinceText(cutoff) }, transaction);
        await connection.ExecuteAsync(
            "DELETE FROM check_results WHERE checked_at < @Cutoff", new { Cutoff = SinceText(cutoff) }, transaction);
        transaction.Commit();
        return moved;
    }

    // Column names cannot be SQL parameters, so the sort column comes only from this fixed list.
    // Anything the client sends is looked up here; an unknown key never reaches the SQL text.
    public static readonly Dictionary<string, string> HistorySortColumns = new()
    {
        ["checkedAt"] = "a.checked_at",
        ["url"] = "u.url",
        ["statusCode"] = "a.status_code",
        ["responseTimeMs"] = "a.response_time_ms",
        ["healthy"] = "a.healthy",
        ["archived"] = "a.archived",
    };

    // Every result ever saved, live and archived, for every URL including removed ones. Sorted and paged
    // in SQL, because this table can hold many thousands of rows.
    public async Task<Page<HistoryRow>> GetAllChecksAsync(string sortKey, bool descending, int page, int pageSize)
    {
        var orderBy = HistorySortColumns[sortKey];
        var direction = descending ? "DESC" : "ASC";

        const string allChecks = """
            WITH a AS (
                SELECT url_id, checked_at, status_code, response_time_ms, healthy, error, 0 AS archived FROM check_results
                UNION ALL
                SELECT url_id, checked_at, status_code, response_time_ms, healthy, error, 1 AS archived FROM check_results_archive
            )
            """;

        var sql = allChecks + $"""
            SELECT a.url_id AS UrlId, u.url AS Url, (u.deleted_at IS NOT NULL) AS UrlRemoved,
                   a.checked_at AS CheckedAt, a.status_code AS StatusCode, a.response_time_ms AS ResponseTimeMs,
                   a.healthy AS Healthy, a.error AS Error, a.archived AS Archived
            FROM a JOIN urls u ON u.id = a.url_id
            ORDER BY {orderBy} {direction}, a.checked_at DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        using var connection = new SqliteConnection(_connectionString);
        var total = await connection.ExecuteScalarAsync<long>(allChecks + "SELECT COUNT(*) FROM a");
        var rows = await connection.QueryAsync<HistoryDbRow>(sql, new { PageSize = pageSize, Offset = (page - 1) * pageSize });

        return new Page<HistoryRow>(total, rows.Select(r => new HistoryRow(
            r.UrlId, r.Url, r.UrlRemoved == 1, r.CheckedAt, r.StatusCode, r.ResponseTimeMs, r.Healthy == 1, r.Error,
            r.Archived == 1)).ToList());
    }

    // SQLite gives back 0/1 as Int64, so Dapper fills this flat class first (same pattern as StatusRow).
    private class HistoryDbRow
    {
        public long UrlId { get; set; }
        public string Url { get; set; } = "";
        public long UrlRemoved { get; set; }
        public string CheckedAt { get; set; } = "";
        public long? StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public long Healthy { get; set; }
        public string? Error { get; set; }
        public long Archived { get; set; }
    }

    private class CheckRow
    {
        public string CheckedAt { get; set; } = "";
        public long? StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public long Healthy { get; set; }
        public string? Error { get; set; }
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
