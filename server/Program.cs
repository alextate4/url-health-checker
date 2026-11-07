var builder = WebApplication.CreateBuilder(args);

// One Database object for the whole app; it opens a short-lived connection per call.
builder.Services.AddSingleton(new Database(builder.Configuration.GetConnectionString("HealthMonitor")!));

var app = builder.Build();

await app.Services.GetRequiredService<Database>().InitializeAsync();

// Serves the built React app from server/wwwroot, so one process gives both the UI and the API.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/urls", (Database db) => db.GetUrlStatusesAsync());

app.Run();
