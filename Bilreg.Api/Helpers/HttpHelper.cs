using System.Net;

namespace Bilreg.Api.Helpers;

public class HttpHelper
{
    public static string GetUserAgent(HttpRequest request) => request.Headers.UserAgent.ToString();

    public static string GetIpAddress(HttpRequest request, HttpContext context)
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress;

        request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedHeader);
        if (string.IsNullOrWhiteSpace(xForwardedHeader)) return remoteIpAddress?.ToString() ?? "0.0.0.0";

        var forwardedFor = xForwardedHeader.ToString();
        var firstIp = forwardedFor.Split(',').FirstOrDefault()?.Trim();
        if (IPAddress.TryParse(firstIp, out var parsedIp))
            remoteIpAddress = parsedIp;

        return remoteIpAddress?.ToString() ?? "0.0.0.0";
    }
}
