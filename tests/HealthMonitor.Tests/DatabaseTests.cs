using Dapper;
using Microsoft.Data.Sqlite;

// Proves HIST-01..03: results are saved, still there after a "restart", and can be read
// back for a time window. Each test gets its own temporary SQLite file, never the real one.
public class DatabaseTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"healthmonitor-test-{Guid.NewGuid()}.db");
    private string ConnectionString => $"Data Source={_file}";

    // Seed URL 1 exists after InitializeAsync, so every test can save checks against it.
    private const long UrlId = 1;

    // HIST-01 + HIST-02: a saved result is read back by a brand-new Database object on the
    // same file, which is what happens after an app restart. Running InitializeAsync a second
    // time must not add the seeds again.
    [Fact]
    public async Task Saved_check_is_still_there_after_a_restart()
    {
        var before = new Database(ConnectionString);
        await before.InitializeAsync();
        await before.InsertCheckResultAsync(UrlId, new CheckResult(503, 120, false, null));

        var after = new Database(ConnectionString);
        await after.InitializeAsync();
        var urls = await after.GetUrlStatusesAsync();

        Assert.Equal(4, urls.Count);
        var last = urls.Single(u => u.Id == UrlId).LastCheck;
        Assert.NotNull(last);
        Assert.Equal(503, last.StatusCode);
        Assert.Equal(120, last.ResponseTimeMs);
        Assert.False(last.Healthy);
    }

    // HIST-03: only checks inside the window come back, oldest first. One check is saved now,
    // and one is written two hours in the past, so a 1h window must skip it and a 3h window must not.
    [Fact]
    public async Task History_window_returns_only_checks_inside_it()
    {
        var db = new Database(ConnectionString);
        await db.InitializeAsync();
        await InsertCheckAt(DateTime.UtcNow.AddHours(-2), statusCode: 500, healthy: false);
        await db.InsertCheckResultAsync(UrlId, new CheckResult(200, 80, true, null));

        var lastHour = await db.GetChecksSinceAsync(UrlId, DateTime.UtcNow.AddHours(-1));
        var lastThreeHours = await db.GetChecksSinceAsync(UrlId, DateTime.UtcNow.AddHours(-3));

        Assert.Equal(200, Assert.Single(lastHour).StatusCode);
        Assert.Equal([500L, 200L], lastThreeHours.Select(c => c.StatusCode!.Value));
    }

    // DASH-03: uptime % and average response time are calculated from the saved checks.
    // 3 healthy + 1 unhealthy = 75%. The average skips the check with no response (D-019).
    [Fact]
    public async Task Window_summary_counts_uptime_and_average()
    {
        var db = new Database(ConnectionString);
        await db.InitializeAsync();
        await db.InsertCheckResultAsync(UrlId, new CheckResult(200, 100, true, null));
        await db.InsertCheckResultAsync(UrlId, new CheckResult(200, 200, true, null));
        await db.InsertCheckResultAsync(UrlId, new CheckResult(204, 300, true, null));
        await db.InsertCheckResultAsync(UrlId, new CheckResult(null, 9999, false, "Timed out after 10s"));

        var summary = await db.GetWindowSummaryAsync(UrlId, DateTime.UtcNow.AddHours(-1));

        Assert.Equal(4, summary.CheckCount);
        Assert.Equal(75, summary.UptimePercent);
        Assert.Equal(200, summary.AvgResponseTimeMs);
    }

    // InsertCheckResultAsync always stamps "now", so an old row is written directly.
    private async Task InsertCheckAt(DateTime checkedAt, int statusCode, bool healthy)
    {
        using var connection = new SqliteConnection(ConnectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO check_results (url_id, checked_at, status_code, response_time_ms, healthy, error)
            VALUES (@UrlId, @CheckedAt, @StatusCode, 50, @Healthy, NULL)
            """,
            new { UrlId, CheckedAt = checkedAt.ToString("o"), StatusCode = statusCode, Healthy = healthy ? 1 : 0 });
    }

    public void Dispose()
    {
        // SQLite keeps pooled connections open, which would lock the file on some systems.
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }
}
