using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Topstar.Application.Billing;

public static class VnPayProtocol
{
    public static string Canonical(IEnumerable<KeyValuePair<string, string>> values) =>
        string.Join("&", values.Where(x => x.Key.StartsWith("vnp_", StringComparison.Ordinal)
            && x.Key is not ("vnp_SecureHash" or "vnp_SecureHashType") && !string.IsNullOrEmpty(x.Value))
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));

    public static string Sign(IEnumerable<KeyValuePair<string, string>> values, string secret) =>
        Convert.ToHexString(HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(Canonical(values)))).ToLowerInvariant();

    public static bool Verify(IReadOnlyDictionary<string, string> values, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || !values.TryGetValue("vnp_SecureHash", out var supplied) || supplied.Length != 128) return false;
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(Sign(values, secret)), Convert.FromHexString(supplied)); }
        catch (FormatException) { return false; }
    }

    public static bool AmountMatches(string raw, decimal expected) =>
        expected > 0 && expected == decimal.Truncate(expected) &&
        decimal.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) && amount == expected * 100;
}
