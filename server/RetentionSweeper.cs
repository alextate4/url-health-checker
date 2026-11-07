// Retention (CONF-02): on a timer, results older than the retention period move to the archive table.
// Nothing is deleted (D-023); the live table stays small, so the dashboard queries stay fast.
public class RetentionSweeper : BackgroundService
{
    private readonly Database _database;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RetentionSweeper> _logger;

    public RetentionSweeper(Database database, IConfiguration configuration, ILogger<RetentionSweeper> logger)
    {
        _database = database;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Sweep once at start, then on the timer. The period is read each time, so a change in the UI
        // applies on the next sweep without a restart.
        await SweepAsync();

        var every = TimeSpan.FromMinutes(_configuration.GetValue<int>("Monitor:RetentionSweepMinutes"));
        using var timer = new PeriodicTimer(every);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SweepAsync();
        }
    }

    private async Task SweepAsync()
    {
        // Same rule as the check loop: one failed sweep is logged, and the next one tries again.
        try
        {
            var days = await _database.GetRetentionDaysAsync();
            var moved = await _database.ArchiveOlderThanAsync(DateTime.UtcNow.AddDays(-days));
            if (moved > 0)
            {
                _logger.LogInformation("Archived {Count} results older than {Days} days", moved, days);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Retention sweep failed; trying again on the next timer tick");
        }
    }
}
