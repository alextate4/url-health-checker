using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

// Proves CONF-02 end to end: the timer service itself sweeps at start and archives old results,
// using the retention setting stored in the database.
public class RetentionSweeperTests
{
    [Fact]
    public async Task Sweep_at_start_archives_results_older_than_the_setting()
    {
        using var temp = new TempDatabase();
        await temp.Db.InitializeAsync();
        await temp.Db.SetRetentionDaysAsync(7);
        using (var connection = new SqliteConnection(temp.ConnectionString))
        {
            await connection.ExecuteAsync(
                "INSERT INTO check_results (url_id, checked_at, status_code, response_time_ms, healthy, error) VALUES (1, @Old, 200, 50, 1, NULL)",
                new { Old = DateTime.UtcNow.AddDays(-8).ToString("o") });
        }
        await temp.Db.InsertCheckResultAsync(1, new CheckResult(200, 60, true, null)); // recent: must stay

        var sweeper = new RetentionSweeper(temp.Db, Wait.Config(("Monitor:RetentionSweepMinutes", "60")),
            NullLogger<RetentionSweeper>.Instance);
        await sweeper.StartAsync(CancellationToken.None);
        await Wait.UntilAsync(() => temp.CountRows("check_results_archive") == 1);
        await sweeper.StopAsync(CancellationToken.None);

        Assert.Equal(1, temp.CountRows("check_results"));
    }
}
