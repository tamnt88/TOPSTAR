using System.Security.Cryptography;
using System.Text;
using Topstar.Application;

namespace Topstar.Web.Pages.Public;

public abstract class AuthPageBase : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    protected DeviceContext BuildDeviceContext()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var fingerprint = BuildFingerprint(ip, userAgent);

        return new DeviceContext(
            DeviceFingerprint: fingerprint,
            DeviceName: Request.Headers["sec-ch-ua-platform"].ToString(),
            DeviceType: IsMobile(userAgent) ? "Mobile" : "Web",
            OsName: Request.Headers["sec-ch-ua-platform"].ToString(),
            OsVersion: null,
            BrowserName: Request.Headers["sec-ch-ua"].ToString(),
            BrowserVersion: null,
            UserAgent: userAgent,
            IpAddress: ip,
            CountryCode: null,
            City: null
        );
    }

    private static string BuildFingerprint(string? ip, string? userAgent)
    {
        var source = $"{ip}|{userAgent}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool IsMobile(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return false;
        }

        return userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            || userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase);
    }
}
