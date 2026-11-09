using System.Globalization;
namespace Topstar.Web.Features.Common;

public static class PortalFormat
{
    public static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    public static DateTime ToVietnamTime(this DateTime value) => TimeZoneInfo.ConvertTimeFromUtc(value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc), Zone);
    public static DateTimeOffset ToVietnamTime(this DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Zone);
    public static string Money(decimal value) => value.ToString("N0", Vietnamese) + " ₫";
    public static string Status(string value) => PortalStatusLabels.Of(value);
}
