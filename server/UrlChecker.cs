using System.Diagnostics;

// The outcome of one check. StatusCode is null when no response came back at all.
public record CheckResult(int? StatusCode, long ResponseTimeMs, bool Healthy, string? Error);

public class UrlChecker
{
    private readonly HttpClient _client;

    // The HttpClient comes in from outside so tests can give it a fake handler later.
    public UrlChecker(HttpClient client)
    {
        _client = client;
    }

    public async Task<CheckResult> CheckAsync(string url, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // GET, not HEAD: some servers answer HEAD with 405 and would look unhealthy by mistake.
            // ResponseHeadersRead stops after the headers, so the page body is never downloaded.
            using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            var code = (int)response.StatusCode;
            return new CheckResult(code, stopwatch.ElapsedMilliseconds, code >= 200 && code <= 399, null);
        }
        catch (TaskCanceledException) when (!token.IsCancellationRequested)
        {
            // HttpClient reports its own timeout this way. If the token was cancelled instead,
            // the app is shutting down, so we let that propagate and do not record a failure.
            return Failed(stopwatch, $"Timed out after {_client.Timeout.TotalSeconds}s");
        }
        catch (HttpRequestException ex)
        {
            // Covers DNS failure and connection refused.
            return Failed(stopwatch, ex.Message);
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            // A check must never crash the scheduler loop, so anything else is also "unhealthy".
            return Failed(stopwatch, ex.Message);
        }
    }

    private static CheckResult Failed(Stopwatch stopwatch, string error) =>
        new(null, stopwatch.ElapsedMilliseconds, false, error);
}
