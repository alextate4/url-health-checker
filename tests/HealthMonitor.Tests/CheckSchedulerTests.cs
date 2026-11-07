using System.Net;
using Microsoft.Extensions.Logging.Abstractions;

// Proves CHK-01, CHK-05 and EXTRA-01 by running the real background loop against a temp database
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

    // CHK-01 + EXTRA-01: every due URL is checked and saved; a paused URL is skipped.
    [Fact]
    public async Task First_pass_checks_every_due_url_except_paused_ones()
    {
        using var temp = new TempDatabase();
        await temp.Db.InitializeAsync(); // 4 seeds, none checked yet, so all are due
        await temp.Db.SetPausedAsync(4, true);
        var scheduler = Scheduler(temp, AlwaysOk());

        await scheduler.StartAsync(CancellationToken.None);
        await Wait.UntilAsync(() => temp.CountRows("check_results") == 3);
        await scheduler.StopAsync(CancellationToken.None);

        Assert.Equal(0, temp.CountRows("check_results", urlId: 4));
    }

    // CHK-01: a URL checked less than its interval ago is not due, so it is not checked again.
    [Fact]
    public async Task Url_checked_recently_is_not_checked_again()
    {
        using var temp = new TempDatabase();
        await temp.Db.InitializeAsync();
        await temp.Db.InsertCheckResultAsync(1, new CheckResult(200, 50, true, null)); // seed 1: 30 s interval
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
