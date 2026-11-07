using System.Net;

// Proves CHK-02..04: one check gives the right status code, response time, and healthy flag.
// A fake handler stands in for the network, so the tests never depend on a real website.
public class UrlCheckerTests
{
    // Healthy means 200-399 (CHK-03). The edges 399 and 400 are included because they are
    // exactly where the rule flips; a wrong "<" vs "<=" would fail one of them.
    [Theory]
    [InlineData(200, true)]
    [InlineData(301, true)]
    [InlineData(399, true)]
    [InlineData(400, false)]
    [InlineData(404, false)]
    [InlineData(500, false)]
    public async Task Status_code_decides_healthy(int code, bool expectedHealthy)
    {
        var checker = CheckerAnswering(_ => new HttpResponseMessage((HttpStatusCode)code));

        var result = await checker.CheckAsync("https://site.test", CancellationToken.None);

        Assert.Equal(code, result.StatusCode);
        Assert.Equal(expectedHealthy, result.Healthy);
        // A 4xx/5xx is explained by its code, so no error text is saved (D1-20).
        Assert.Null(result.Error);
    }

    // CHK-02: response time is measured, not made up. The fake server waits 150 ms,
    // so a stopwatch that was never started (or stopped too early) would fail this.
    [Fact]
    public async Task Response_time_covers_the_wait_for_the_server()
    {
        var checker = CheckerAnswering(_ =>
        {
            Thread.Sleep(150);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await checker.CheckAsync("https://site.test", CancellationToken.None);

        Assert.True(result.ResponseTimeMs >= 150, $"was {result.ResponseTimeMs} ms");
    }

    // CHK-04: no response at all (DNS failure, connection refused) is unhealthy,
    // with no status code and the reason saved.
    [Fact]
    public async Task Unreachable_url_is_unhealthy_with_the_reason()
    {
        var checker = CheckerAnswering(_ => throw new HttpRequestException("No such host is known."));

        var result = await checker.CheckAsync("https://site.test", CancellationToken.None);

        Assert.False(result.Healthy);
        Assert.Null(result.StatusCode);
        Assert.Equal("No such host is known.", result.Error);
    }

    // CHK-04: a timeout is unhealthy too, and the error says so. The fake server is slower
    // than the client's 100 ms timeout.
    [Fact]
    public async Task Timeout_is_unhealthy_with_a_timeout_reason()
    {
        var handler = new FakeHandler(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var checker = new UrlChecker(new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(100) });

        var result = await checker.CheckAsync("https://site.test", CancellationToken.None);

        Assert.False(result.Healthy);
        Assert.Null(result.StatusCode);
        Assert.StartsWith("Timed out", result.Error);
    }

    private static UrlChecker CheckerAnswering(Func<HttpRequestMessage, HttpResponseMessage> answer) =>
        new(new HttpClient(new FakeHandler((request, _) => Task.FromResult(answer(request)))));

    // The smallest stand-in for the network: HttpClient hands every request to this method.
    private class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> answer)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            answer(request, token);
    }
}
