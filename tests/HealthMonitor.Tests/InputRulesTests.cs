// Proves D-026: the server refuses bad URLs, internal addresses (SSRF), and out-of-range intervals,
// and still accepts normal public URLs. Pure functions, so no database or network is needed.
public class InputRulesTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/health?x=1")]
    [InlineData("https://93.184.215.14")]
    public void Public_http_urls_are_accepted(string url)
    {
        Assert.Null(InputRules.UrlProblem(url));
    }

    [Theory]
    [InlineData("example.com")]          // no scheme
    [InlineData("ftp://example.com")]    // not http(s)
    [InlineData("not a url")]
    [InlineData("")]
    public void Malformed_urls_are_refused(string url)
    {
        Assert.NotNull(InputRules.UrlProblem(url));
    }

    // Each of these would let a user make the server probe its own network (SSRF).
    [Theory]
    [InlineData("http://localhost:5432")]
    [InlineData("http://127.0.0.1")]
    [InlineData("http://10.0.0.5")]
    [InlineData("http://172.16.0.1")]
    [InlineData("http://192.168.1.1")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://[::1]")]
    [InlineData("http://printer.local")]
    public void Internal_addresses_are_refused(string url)
    {
        Assert.Equal("url must not point to this machine or the local network", InputRules.UrlProblem(url));
    }

    // 172.32.x is just outside the private 172.16/12 block, so it must not be caught by mistake.
    [Fact]
    public void Address_just_outside_a_private_range_is_allowed()
    {
        Assert.Null(InputRules.UrlProblem("http://172.32.0.1"));
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(86_400, true)]
    [InlineData(86_401, false)]
    public void Interval_must_be_between_10_seconds_and_one_day(int seconds, bool allowed)
    {
        Assert.Equal(allowed, InputRules.IntervalProblem(seconds) is null);
    }
}
