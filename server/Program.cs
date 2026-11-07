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

app.MapGet("/api/urls", (Database db) => db.GetUrlStatusesAsync());

app.Run();
