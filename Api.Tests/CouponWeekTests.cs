using Fmc.Application.Services;
using Fmc.Domain.Entities;

namespace Fmc.Api.Tests;

public class CouponWeekTests
{
    [Fact]
    public void CurrentBounds_MondayStartsWeek()
    {
        // Wednesday 2026-06-10 15:00 Argentina
        var wed = new DateTimeOffset(2026, 6, 10, 18, 0, 0, TimeSpan.Zero);
        var (start, end) = CouponWeek.CurrentBounds(wed);

        var localStart = TimeZoneInfo.ConvertTime(start, TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"));
        Assert.Equal(DayOfWeek.Monday, localStart.DayOfWeek);
        Assert.Equal(0, localStart.Hour);
        Assert.Equal(DayOfWeek.Sunday, TimeZoneInfo.ConvertTime(end, TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires")).DayOfWeek);
    }

    [Fact]
    public void Contains_OverlappingRange_ReturnsTrue()
    {
        var (start, end) = CouponWeek.CurrentBounds();
        Assert.True(CouponWeek.Contains(start, end));
    }
}
