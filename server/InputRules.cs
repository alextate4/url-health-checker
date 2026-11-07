using System.Net;

// Server-side checks for what a user can send (EC-002, EC-003). The browser form has the same limits,
// but the API must not trust the browser: anyone can call it directly with curl.
// Each method returns an error message, or null when the input is fine.
public static class InputRules
{
    // 10 s matches the form's min. Faster than that would hammer the target and flood the database.
    public const int MinIntervalSeconds = 10;
    // One day. Slower than that is not really "monitoring".
    public const int MaxIntervalSeconds = 86_400;

    public static string? IntervalProblem(int seconds) =>
        seconds is < MinIntervalSeconds or > MaxIntervalSeconds
            ? $"intervalSeconds must be between {MinIntervalSeconds} and {MaxIntervalSeconds}"
            : null;

    public static string? UrlProblem(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host))
        {
            return "url must be a full address, like https://example.com";
        }
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return "url must start with http:// or https://";
        }

        // The server makes the request, so a URL pointing inside our own network would let a user
        // probe machines they cannot reach themselves (SSRF). Block the obvious internal targets.
        var host = uri.Host;
        if (host == "localhost" || host.EndsWith(".localhost") || host.EndsWith(".local"))
        {
            return "url must not point to this machine or the local network";
        }
        if (IPAddress.TryParse(host.Trim('[', ']'), out var ip) && IsInternal(ip))
        {
            return "url must not point to this machine or the local network";
        }

        return null;
    }

    // Loopback, private (10/8, 172.16/12, 192.168/16), link-local (169.254/16, fe80::/10),
    // unique-local IPv6 (fc00::/7), and "any" (0.0.0.0, ::).
    private static bool IsInternal(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }
        if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        var b = ip.GetAddressBytes();
        if (b.Length == 4)
        {
            return b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254);
        }
        return ip.IsIPv6LinkLocal || (b[0] & 0xFE) == 0xFC;
    }
}
