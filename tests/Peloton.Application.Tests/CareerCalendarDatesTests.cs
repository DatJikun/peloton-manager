using System.Linq;
using Peloton.Application;
using Xunit;

namespace Peloton.Application.Tests;

public sealed class CareerCalendarDatesTests
{
    [Fact]
    public void DayZeroFormatsAsFirstJanuary2026()
    {
        Assert.Equal("1 stycznia 2026", CareerCalendarDates.FormatLong(0));
        Assert.Equal("1 STY", CareerCalendarDates.FormatSlab(0));
        Assert.DoesNotContain("dzień", CareerCalendarDates.FormatLong(0));
    }

    [Fact]
    public void Day247FormatsAsSeptember2026WithoutDayWord()
    {
        string formatted = CareerCalendarDates.FormatLong(247);
        Assert.Contains("września 2026", formatted);
        Assert.Contains("5", formatted);
        Assert.DoesNotContain("dzień", formatted);
    }

    [Fact]
    public void FormatRangeHandlesSingleDayAndSpan()
    {
        Assert.Equal("1 stycznia 2026", CareerCalendarDates.FormatRange(0, 0));
        Assert.Equal("20–25 stycznia 2026", CareerCalendarDates.FormatRange(19, 24));
    }
}
