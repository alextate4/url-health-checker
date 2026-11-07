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

// string? makes ?window= optional. A plain string would be required, and the framework would answer 400.
app.MapGet("/api/urls", async (string? window, Database db) =>
{
    if (!historyWindows.TryGetValue(window ?? "1h", out var span))
    {
        return Results.BadRequest(new { error = "window must be 1h, 24h, or 7d" });
    }

    return Results.Ok(await db.GetDashboardAsync(DateTime.UtcNow - span));
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

app.MapPatch("/api/urls/{id}", async (long id, IntervalChange body, Database db) =>
    await db.UpdateIntervalAsync(id, body.IntervalSeconds) ? Results.NoContent() : Results.NotFound());

app.MapDelete("/api/urls/{id}", async (long id, Database db) =>
    await db.DeleteUrlAsync(id) ? Results.NoContent() : Results.NotFound());

app.Run();

// The JSON bodies the UI sends. ASP.NET Core fills these from camelCase JSON automatically.
record NewUrl(string Url, int IntervalSeconds);
record IntervalChange(int IntervalSeconds);
