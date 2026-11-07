using System.Net;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

// Proves CHK-01, CHK-05 and the global interval (D-033) by running the real background loop against a temp database
// and a fake network. The tick is 60 s, so only the first pass (which runs at start) happens.
public class CheckSchedulerTests
{
    private static CheckScheduler Scheduler(TempDatabase temp, HttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(temp.Db,
            new FakeHttpClientFactory(handler, timeout ?? TimeSpan.FromSeconds(5)),
            Wait.Config(("Monitor:TickSeconds", "60"), ("Monitor:MaxConcurrentChecks", "4")),
            NullLogger<CheckScheduler>.Instance);

    private static FakeHandler AlwaysOk() =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

    // CHK-01: every URL that was never checked is due, so the first pass checks and saves all of them.
    [Fact]
    public async Task First_pass_checks_every_never_checked_url()
    {
        using var temp = new TempDatabase();
        await temp.Db.InitializeAsync(); // 4 seeds, none checked yet
        var scheduler = Scheduler(temp, AlwaysOk());

        await scheduler.StartAsync(CancellationToken.None);
        await Wait.UntilAsync(() => temp.CountRows("check_results") == 4);
        await scheduler.StopAsync(CancellationToken.None);

        Assert.Equal(1, temp.CountRows("check_results", urlId: 4));
    }

    // D-033: the one global interval decides what is due. A URL checked 20 s ago is due with a 10 s
    // interval and not due with the default 30 s.
    [Theory]
    [InlineData(10, 2)]
    [InlineData(30, 1)]
    public async Task Global_check_interval_decides_what_is_due(int intervalSeconds, int expectedRows)
    {
        using var temp = new TempDatabase();
        await temp.Db.InitializeAsync();
        await temp.Db.SetCheckIntervalSecondsAsync(intervalSeconds);
        using (var connection = new SqliteConnection(temp.ConnectionString))
        {
            await connection.ExecuteAsync(
                "INSERT INTO check_results (url_id, checked_at, status_code, response_time_ms, healthy, error) VALUES (1, @At, 200, 50, 1, NULL)",
                new { At = DateTime.UtcNow.AddSeconds(-20).ToString("o") });
        }
        var scheduler = Scheduler(temp, AlwaysOk());

        await scheduler.StartAsync(CancellationToken.None);
        await Wait.UntilAsync(() => temp.CountRows("check_results", urlId: 4) == 1); // the pass has run
        await Wait.UntilAsync(() => temp.CountRows("check_results") == 3 + expectedRows);
        await scheduler.StopAsync(CancellationToken.None);

        Assert.Equal(expectedRows, temp.CountRows("check_results", urlId: 1));
    }

    // CHK-01: a URL checked less than its interval ago is not due, so it is not checked again.
    [Fact]
    public async Task Url_checked_recently_is_not_checked_again()
    {
        using var temp = new TempDatabase();
        await temp.Db.InitializeAsync();
        await temp.Db.InsertCheckResultAsync(1, new CheckResult(200, 50, true, null)); // default interval: 30 s
        var scheduler = Scheduler(temp, AlwaysOk());

        await scheduler.StartAsync(CancellationToken.None);
        await Wait.UntilAsync(() => temp.CountRows("check_results") == 4); // 1 old + seeds 2, 3, 4
        await scheduler.StopAsync(CancellationToken.None);

        Assert.Equal(1, temp.CountRows("check_results", urlId: 1));
    }

    // CHK-05: a URL that never answers must not hold up the others. Its request hangs until the
    // test ends, yet the other three URLs are saved. The hanging one is seed 1, the FIRST in the
    // pass: a one-at-a-time loop would wait on it forever and save nothing.
    [Fact]
    public async Task Hanging_url_does_not_delay_the_others()
    {
        using var temp = new TempDatabase();
        await temp.Db.InitializeAsync();
        var hanging = new FakeHandler(async (request, token) =>
        {
            if (request.RequestUri!.Host == "example.com")
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var scheduler = Scheduler(temp, hanging, timeout: TimeSpan.FromMinutes(1));

        await scheduler.StartAsync(CancellationToken.None);
        await Wait.UntilAsync(() => temp.CountRows("check_results") == 3, timeoutMs: 3000);
        Assert.Equal(0, temp.CountRows("check_results", urlId: 1)); // still hanging, not saved
        await scheduler.StopAsync(CancellationToken.None);
    }
}
