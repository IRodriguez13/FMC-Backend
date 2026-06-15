namespace Fmc.Application.Services;

/// <summary>Semana calendario Argentina: lunes 00:00 → domingo 23:59:59.</summary>
public static class CouponWeek
{
    private static readonly TimeZoneInfo ArgentinaTz =
        TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");

    public static (DateTimeOffset Start, DateTimeOffset End) CurrentBounds(DateTimeOffset? now = null)
    {
        var instant = now ?? DateTimeOffset.UtcNow;
        var local = TimeZoneInfo.ConvertTime(instant, ArgentinaTz);
        var daysFromMonday = ((int)local.DayOfWeek + 6) % 7;
        var monday = local.Date.AddDays(-daysFromMonday);
        var startLocal = DateTime.SpecifyKind(monday, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddDays(7).AddTicks(-1);
        var start = new DateTimeOffset(startLocal, ArgentinaTz.GetUtcOffset(startLocal));
        var end = new DateTimeOffset(endLocal, ArgentinaTz.GetUtcOffset(endLocal));
        return (start, end);
    }

    public static int WeekNumber(DateTimeOffset? now = null)
    {
        var (start, _) = CurrentBounds(now);
        var local = TimeZoneInfo.ConvertTime(start, ArgentinaTz);
        return System.Globalization.ISOWeek.GetWeekOfYear(local.DateTime);
    }

    public static bool Contains(DateTimeOffset validFrom, DateTimeOffset validUntil, DateTimeOffset? now = null)
    {
        var (weekStart, weekEnd) = CurrentBounds(now);
        return validFrom <= weekEnd && validUntil >= weekStart;
    }
}
