// One background loop checks every URL whose interval has passed.
// Per-URL intervals fall out of this: a short tick plus an "is it due?" test for each URL.
public class CheckScheduler : BackgroundService
{
    private readonly Database _database;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CheckScheduler> _logger;

    public CheckScheduler(
        Database database,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CheckScheduler> logger)
    {
        _database = database;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check right away so the dashboard is not "Pending" until the first tick.
        await RunPassAsync(stoppingToken);

        var tick = TimeSpan.FromSeconds(_configuration.GetValue<int>("Monitor:TickSeconds"));
        using var timer = new PeriodicTimer(tick);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunPassAsync(stoppingToken);
        }
    }

    private async Task RunPassAsync(CancellationToken stoppingToken)
    {
        // Without this catch, one unhandled error would stop the whole app (.NET 8+ default).
        try
        {
            var statuses = await _database.GetUrlStatusesAsync();
            foreach (var status in statuses.Where(IsDue))
            {
                // One after another for now; checking several at once (with a cap) is Phase 2.
                var checker = new UrlChecker(_httpClientFactory.CreateClient("checker"));
                var result = await checker.CheckAsync(status.Url, stoppingToken);
                await _database.InsertCheckResultAsync(status.Id, result);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app is shutting down. That is not an error.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check pass failed; trying again on the next tick");
        }
    }

    // Due = never checked, or its interval has passed since the newest check.
    private static bool IsDue(UrlStatus status)
    {
        if (status.LastCheck is null)
        {
            return true;
        }
        var lastChecked = DateTimeOffset.Parse(status.LastCheck.CheckedAt);
        return lastChecked.AddSeconds(status.IntervalSeconds) <= DateTimeOffset.UtcNow;
    }
}
