using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// A relative file name would follow whatever folder the app was started from, so pin it to the
// server folder. That way the history is always in the same place. (An absolute path is kept as is.)
var connection = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("HealthMonitor"));
connection.DataSource = Path.Combine(builder.Environment.ContentRootPath, connection.DataSource);

// One Database object for the whole app; it opens a short-lived connection per call.
builder.Services.AddSingleton(new Database(connection.ToString()));

// One shared, named HttpClient. The timeout comes from appsettings.json, not from the UI.
builder.Services.AddHttpClient("checker", client =>
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("Monitor:RequestTimeoutSeconds")));

builder.Services.AddHostedService<CheckScheduler>();
builder.Services.AddHostedService<RetentionSweeper>();

var app = builder.Build();

await app.Services.GetRequiredService<Database>().InitializeAsync();

// Serves the built React app from server/wwwroot, so one process gives both the UI and the API.
app.UseDefaultFiles();
app.UseStaticFiles();

// The UI offers only these three windows, so any other value is a client mistake (400).
var historyWindows = new Dictionary<string, TimeSpan>
{
    ["1h"] = TimeSpan.FromHours(1),
    ["24h"] = TimeSpan.FromHours(24),
    ["7d"] = TimeSpan.FromDays(7),
};

// Sorting the dashboard happens in C# after the rows are built: uptime and average are computed
// per URL, and the list is small (one row per URL). The All-checks page sorts in SQL instead.
var dashboardSorts = new Dictionary<string, Func<DashboardRow, object?>>
{
    ["id"] = r => r.Id,
    ["url"] = r => r.Url,
    ["interval"] = r => r.IntervalSeconds,
    ["paused"] = r => r.Paused,
    ["status"] = r => r.LastCheck?.Healthy,
    ["statusCode"] = r => r.LastCheck?.StatusCode,
    ["responseTimeMs"] = r => r.LastCheck?.ResponseTimeMs,
    ["lastChecked"] = r => r.LastCheck?.CheckedAt,
    ["uptime"] = r => r.Summary.UptimePercent,
    ["avg"] = r => r.Summary.AvgResponseTimeMs,
};

// string? makes ?window= optional. A plain string would be required, and the framework would answer 400.
app.MapGet("/api/urls", async (string? window, string? sort, string? dir, int? page, int? pageSize, Database db) =>
{
    if (!historyWindows.TryGetValue(window ?? "1h", out var span))
    {
        return Results.BadRequest(new { error = "window must be 1h, 24h, or 7d" });
    }
    var paging = ReadPaging(sort ?? "id", dir, page, pageSize, dashboardSorts.Keys);
    if (paging is null)
    {
        return Results.BadRequest(new { error = PagingError });
    }

    var rows = await db.GetDashboardAsync(DateTime.UtcNow - span);
    var key = dashboardSorts[paging.Sort];
    var sorted = paging.Descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
    var pageRows = sorted.Skip((paging.Page - 1) * paging.PageSize).Take(paging.PageSize).ToList();
    return Results.Ok(new Page<DashboardRow>(rows.Count, pageRows));
});

// Every saved result, live and archived, including removed URLs: nothing is ever hidden for good.
// ?urlId= narrows it to one URL for the history modal.
app.MapGet("/api/checks", async (string? sort, string? dir, int? page, int? pageSize, long? urlId, Database db) =>
{
    var paging = ReadPaging(sort ?? "checkedAt", dir ?? "desc", page, pageSize, Database.HistorySortColumns.Keys);
    if (paging is null)
    {
        return Results.BadRequest(new { error = PagingError });
    }

    return Results.Ok(await db.GetAllChecksAsync(paging.Sort, paging.Descending, paging.Page, paging.PageSize, urlId));
});

app.MapGet("/api/settings", async (Database db) => Results.Ok(new { retentionDays = await db.GetRetentionDaysAsync() }));

app.MapPut("/api/settings", async (SettingsChange body, Database db) =>
{
    await db.SetRetentionDaysAsync(body.RetentionDays);
    return Results.NoContent();
});

// Full history of one URL inside the window (HIST-03). Not polled by the UI, so it is not paged.
app.MapGet("/api/urls/{id}/checks", async (long id, string? window, Database db) =>
{
    if (!historyWindows.TryGetValue(window ?? "1h", out var span))
    {
        return Results.BadRequest(new { error = "window must be 1h, 24h, or 7d" });
    }

    return Results.Ok(await db.GetChecksSinceAsync(id, DateTime.UtcNow - span));
});

// URL management (URL-01..03). Input is not validated yet: that is a logged edge case, built only if time is left.
app.MapPost("/api/urls", async (NewUrl body, Database db) =>
{
    var id = await db.AddUrlAsync(body.Url, body.IntervalSeconds);
    return Results.Created($"/api/urls/{id}", new { id });
});

// One PATCH changes either the interval or the paused state (or both); fields left out stay as they are.
app.MapPatch("/api/urls/{id}", async (long id, UrlChange body, Database db) =>
{
    if (body.IntervalSeconds is null && body.Paused is null)
    {
        return Results.BadRequest(new { error = "send intervalSeconds, paused, or both" });
    }
    if (body.IntervalSeconds is int seconds && !await db.UpdateIntervalAsync(id, seconds))
    {
        return Results.NotFound();
    }
    if (body.Paused is bool paused && !await db.SetPausedAsync(id, paused))
    {
        return Results.NotFound();
    }
    return Results.NoContent();
});

// Soft delete: the URL leaves the dashboard and stops being checked; its history stays (D-023).
app.MapDelete("/api/urls/{id}", async (long id, Database db) =>
    await db.SoftDeleteUrlAsync(id) ? Results.NoContent() : Results.NotFound());

app.Run();

// Shared by both tables. The UI offers only these page sizes, so anything else is a client mistake (400).
static Paging? ReadPaging(string sort, string? dir, int? page, int? pageSize, IEnumerable<string> allowedSorts)
{
    var size = pageSize ?? 10;
    var number = page ?? 1;
    var direction = dir ?? "asc";
    if (!allowedSorts.Contains(sort) || direction is not ("asc" or "desc") || size is not (10 or 50 or 100) || number < 1)
    {
        return null;
    }
    return new Paging(sort, direction == "desc", number, size);
}

// The JSON bodies the UI sends. ASP.NET Core fills these from camelCase JSON automatically.
record NewUrl(string Url, int IntervalSeconds);
record UrlChange(int? IntervalSeconds, bool? Paused);
record SettingsChange(int RetentionDays);
record Paging(string Sort, bool Descending, int Page, int PageSize);

// Kept next to ReadPaging so the message matches the rules.
partial class Program
{
    const string PagingError = "sort must be a known column, dir asc or desc, pageSize 10, 50, or 100, page 1 or more";
}
