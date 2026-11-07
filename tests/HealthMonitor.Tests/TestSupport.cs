using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

// Small shared helpers for the background-service tests. No mocking library: each fake is a few lines.

// HttpClient hands every request to SendAsync, so this decides the "network" answer per request.
public class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> answer)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
        answer(request, token);
}

// What CheckScheduler asks for. Always returns a client on the fake handler.
public class FakeHttpClientFactory(HttpMessageHandler handler, TimeSpan timeout) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { Timeout = timeout };
}

// A fresh SQLite file per test, deleted afterwards.
public sealed class TempDatabase : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"healthmonitor-test-{Guid.NewGuid()}.db");
    public string ConnectionString => $"Data Source={_file}";
    public Database Db { get; }

    public TempDatabase()
    {
        Db = new Database(ConnectionString);
    }

    public long CountRows(string table, long? urlId = null)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = urlId is null
            ? $"SELECT COUNT(*) FROM {table}"
            : $"SELECT COUNT(*) FROM {table} WHERE url_id = {urlId}";
        return (long)command.ExecuteScalar()!;
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }
}

public static class Wait
{
    // Background services work on their own schedule, so tests wait for an outcome (with a limit)
    // instead of sleeping a fixed time and hoping.
    public static async Task UntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("condition was not met in time");
            }
            await Task.Delay(25);
        }
    }

    public static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();
}
