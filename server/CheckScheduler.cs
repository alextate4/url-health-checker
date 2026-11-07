using System.Collections.Concurrent;

// One background loop checks every URL whose interval has passed.
// Per-URL intervals fall out of this: a short tick plus an "is it due?" test for each URL.
public class CheckScheduler : BackgroundService
{
    // Caps how many checks run at the same time (CHK-05), so 100 URLs do not open 100 connections at once.
    private readonly SemaphoreSlim _slots;

    // URLs whose check has started but not finished. A slow URL can still be running on the next tick;
    // without this set it would look due again and be checked twice.
    private readonly ConcurrentDictionary<long, bool> _inFlight = new();

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
        _slots = new SemaphoreSlim(configuration.GetValue<int>("Monitor:MaxConcurrentChecks"));
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
                if (_inFlight.TryAdd(status.Id, true))
                {
                    // Not awaited on purpose: the loop starts the check and moves on, so one URL that
                    // takes the full 10s timeout never delays the other URLs or the next tick.
                    _ = CheckOneAsync(status, stoppingToken);
                }
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

    private async Task CheckOneAsync(UrlStatus status, CancellationToken stoppingToken)
    {
        try
        {
            // Waits here when the cap is reached; the check starts as soon as another one finishes.
            await _slots.WaitAsync(stoppingToken);
            try
            {
                var checker = new UrlChecker(_httpClientFactory.CreateClient("checker"));
                var result = await checker.CheckAsync(status.Url, stoppingToken);
                await _database.InsertCheckResultAsync(status.Id, result);
            }
            finally
            {
                _slots.Release();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The app is shutting down. That is not an error.
        }
        catch (Exception ex)
        {
            // Nobody awaits this task, so an error must be logged here or it would be lost.
            _logger.LogError(ex, "Check failed for {Url}", status.Url);
        }
        finally
        {
            _inFlight.TryRemove(status.Id, out _);
        }
    }

    // Due = not paused, and never checked, or its interval has passed since the newest check.
    private static bool IsDue(UrlStatus status)
    {
        if (status.Paused)
        {
            return false;
        }
        if (status.LastCheck is null)
        {
            return true;
        }
        var lastChecked = DateTimeOffset.Parse(status.LastCheck.CheckedAt);
        return lastChecked.AddSeconds(status.IntervalSeconds) <= DateTimeOffset.UtcNow;
    }
}
