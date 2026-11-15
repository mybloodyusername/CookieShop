using System.Globalization;
using System.Security.Cryptography;

namespace CookieShop.Infra.Services;

public static class OrderNumberGenerator
{
    private static readonly TimeZoneInfo Tehran = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");

    public static string Generate(DateTimeOffset createdAt)
    {
        var local = TimeZoneInfo.ConvertTime(createdAt, Tehran).DateTime;
        var calendar = new PersianCalendar();
        return string.Create(CultureInfo.InvariantCulture,
            $"{calendar.GetYear(local):D4}{calendar.GetMonth(local):D2}{calendar.GetDayOfMonth(local):D2}-{RandomNumberGenerator.GetInt32(1_000_000):D6}");
    }
}
