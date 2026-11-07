using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

// Starts the real app (Program.cs) in memory and calls its HTTP endpoints. Three things are swapped
// so the tests are fast and never touch real data or the internet: the database is a temp file, the
// two background services are removed (nothing runs on a timer), and "check now" gets a fake network.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public TempDatabase Temp { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<Database>();
            services.AddSingleton(Temp.Db);

            // Remove only our two services. The web server itself is also an IHostedService.
            var ours = services.Where(d => d.ServiceType == typeof(IHostedService)
                && (d.ImplementationType == typeof(CheckScheduler) || d.ImplementationType == typeof(RetentionSweeper)))
                .ToList();
            foreach (var descriptor in ours)
            {
                services.Remove(descriptor);
            }

            services.AddHttpClient("checker").ConfigurePrimaryHttpMessageHandler(() =>
                new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Temp.Dispose();
    }
}

// Proves the API contract: status codes, the 400 rules, and that each endpoint changes the right data.
public class ApiTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static HttpContent Body(object value) => JsonContent.Create(value);

    // DASH-01 + D-024: the dashboard returns the seeds as one page with a total.
    [Fact]
    public async Task Dashboard_returns_seeded_urls_as_a_page()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();

        var page = await Json(await client.GetAsync("/api/urls?window=24h&sort=url&dir=desc&pageSize=10"));

        Assert.Equal(4, page.GetProperty("total").GetInt32());
        var urls = page.GetProperty("rows").EnumerateArray().Select(r => r.GetProperty("url").GetString()).ToList();
        Assert.Equal(urls.OrderByDescending(u => u).ToList(), urls); // sorted by url, descending
    }

    // D-018 + D-024: anything outside the fixed lists is a 400, before any SQL runs.
    [Theory]
    [InlineData("/api/urls?window=2h")]
    [InlineData("/api/urls?sort=drop%20table")]
    [InlineData("/api/urls?dir=up")]
    [InlineData("/api/urls?pageSize=7")]
    [InlineData("/api/urls?page=0")]
    [InlineData("/api/checks?sort=nope")]
    [InlineData("/api/urls/1/checks?window=1y")]
    public async Task Unknown_query_values_return_400(string path)
    {
        using var app = new ApiFactory();
        var response = await app.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // URL-01 + D-026 + EC-004: add returns 201; a duplicate is 409; bad input is 400 with a reason.
    [Fact]
    public async Task Adding_a_url_validates_input_and_refuses_duplicates()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();

        var created = await client.PostAsync("/api/urls", Body(new { url = "https://example.org", intervalSeconds = 60 }));
        var duplicate = await client.PostAsync("/api/urls", Body(new { url = "HTTPS://EXAMPLE.ORG/", intervalSeconds = 60 }));
        var internalUrl = await client.PostAsync("/api/urls", Body(new { url = "http://127.0.0.1", intervalSeconds = 60 }));
        var tooFast = await client.PostAsync("/api/urls", Body(new { url = "https://example.net", intervalSeconds = 5 }));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, internalUrl.StatusCode);
        Assert.Contains("local network", (await Json(internalUrl)).GetProperty("error").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, tooFast.StatusCode);
        Assert.Equal(5, app.Temp.CountRows("urls"));
    }

    // URL-03 + EXTRA-01: PATCH changes the interval and the paused flag; empty or unknown is refused.
    [Fact]
    public async Task Patch_changes_interval_and_pause()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PatchAsync("/api/urls/2", Body(new { intervalSeconds = 120, paused = true }))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsync("/api/urls/2", Body(new { }))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.PatchAsync("/api/urls/999", Body(new { paused = true }))).StatusCode);

        var row = (await Json(await client.GetAsync("/api/urls"))).GetProperty("rows").EnumerateArray()
            .Single(r => r.GetProperty("id").GetInt32() == 2);
        Assert.Equal(120, row.GetProperty("intervalSeconds").GetInt32());
        Assert.True(row.GetProperty("paused").GetBoolean());
    }

    // URL-02 + D-023: DELETE hides the URL from the dashboard but its history stays in /api/checks.
    [Fact]
    public async Task Delete_is_a_soft_delete()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();
        await client.GetAsync("/api/urls"); // first request starts the app, which creates and seeds the DB
        await app.Temp.Db.InsertCheckResultAsync(1, new CheckResult(200, 50, true, null));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/urls/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/urls/1")).StatusCode);

        Assert.Equal(3, (await Json(await client.GetAsync("/api/urls"))).GetProperty("total").GetInt32());
        var history = await Json(await client.GetAsync("/api/checks?urlId=1"));
        Assert.Equal(1, history.GetProperty("total").GetInt32());
        Assert.True(history.GetProperty("rows")[0].GetProperty("urlRemoved").GetBoolean());
    }

    // EXTRA-02: check now runs one check (on the fake network), saves it, and returns it.
    [Fact]
    public async Task Check_now_saves_and_returns_one_result()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/urls/1/check", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await Json(response)).GetProperty("healthy").GetBoolean());
        Assert.Equal(1, app.Temp.CountRows("check_results", urlId: 1));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/urls/999/check", null)).StatusCode);
    }

    // CONF-01: the retention setting can be read and changed.
    [Fact]
    public async Task Retention_setting_round_trips()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();

        Assert.Equal(30, (await Json(await client.GetAsync("/api/settings"))).GetProperty("retentionDays").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsync("/api/settings", Body(new { retentionDays = 7 }))).StatusCode);
        Assert.Equal(7, (await Json(await client.GetAsync("/api/settings"))).GetProperty("retentionDays").GetInt32());
    }

    // D-032: one row can be read on its own (a playing row refreshes itself with this).
    [Fact]
    public async Task Single_row_endpoint_returns_the_row_or_404()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();
        await client.GetAsync("/api/urls");
        await app.Temp.Db.InsertCheckResultAsync(2, new CheckResult(200, 70, true, null));

        var row = await Json(await client.GetAsync("/api/urls/2?window=24h"));
        Assert.Equal(200, row.GetProperty("lastCheck").GetProperty("statusCode").GetInt32());
        Assert.Equal(1, row.GetProperty("summary").GetProperty("checkCount").GetInt32());

        await client.DeleteAsync("/api/urls/2");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/urls/2")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/urls/3?window=2h")).StatusCode);
    }

    // HIST-03: one URL's checks inside a window come back as a list.
    [Fact]
    public async Task Url_history_endpoint_returns_checks_in_the_window()
    {
        using var app = new ApiFactory();
        var client = app.CreateClient();
        await client.GetAsync("/api/urls");
        await app.Temp.Db.InsertCheckResultAsync(3, new CheckResult(404, 80, false, null));

        var checks = await Json(await client.GetAsync("/api/urls/3/checks?window=1h"));

        Assert.Equal(404, checks[0].GetProperty("statusCode").GetInt32());
    }
}
