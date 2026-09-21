using System;

namespace DesktopClock.Tests;

public class RelativeTimeFormatterTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 12, 34, 56, 500);

    [Theory]
    [InlineData("00:00:00.499", "now")]
    [InlineData("00:00:00.500", "now")]
    [InlineData("00:00:01", "in a second")]
    [InlineData("00:00:59.999", "in 59 seconds")]
    [InlineData("00:01:00", "in a minute")]
    [InlineData("00:01:59", "in a minute")]
    [InlineData("00:02:00", "in 2 minutes")]
    [InlineData("00:59:59", "in 59 minutes")]
    [InlineData("01:00:00", "in an hour")]
    [InlineData("01:29:59", "in an hour")]
    [InlineData("01:30:00", "in an hour")]
    [InlineData("02:00:00", "in 2 hours")]
    [InlineData("23:59:59", "in 23 hours")]
    [InlineData("1.00:00:00", "tomorrow")]
    [InlineData("1.11:25:03", "tomorrow")]
    [InlineData("1.11:25:04", "in 2 days")]
    [InlineData("2.00:00:00", "in 2 days")]
    [InlineData("27.23:59:59", "in 27 days")]
    [InlineData("28.00:00:00", "in 28 days")]
    [InlineData("30.00:00:00", "in a month")]
    [InlineData("58.23:59:59", "in a month")]
    [InlineData("59.00:00:00", "in 2 months")]
    [InlineData("344.23:59:59", "in 11 months")]
    [InlineData("345.00:00:00", "in a year")]
    [InlineData("729.23:59:59", "in a year")]
    [InlineData("730.00:00:00", "in 2 years")]
    public void Format_PicksLargestUnitForFutureTarget(string offset, string expected)
    {
        var target = Now + TimeSpan.Parse(offset);

        Assert.Equal(expected, RelativeTimeFormatter.Format(target, Now));
    }

    [Theory]
    [InlineData("00:00:01", "a second ago")]
    [InlineData("00:00:30", "30 seconds ago")]
    [InlineData("00:01:00", "a minute ago")]
    [InlineData("00:05:00", "5 minutes ago")]
    [InlineData("01:00:00", "an hour ago")]
    [InlineData("03:00:00", "3 hours ago")]
    [InlineData("1.00:00:00", "yesterday")]
    [InlineData("3.00:00:00", "3 days ago")]
    [InlineData("30.00:00:00", "a month ago")]
    [InlineData("90.00:00:00", "3 months ago")]
    [InlineData("365.00:00:00", "a year ago")]
    [InlineData("1095.00:00:00", "3 years ago")]
    public void Format_PicksLargestUnitForPastTarget(string offset, string expected)
    {
        var target = Now - TimeSpan.Parse(offset);

        Assert.Equal(expected, RelativeTimeFormatter.Format(target, Now));
    }

    [Fact]
    public void Format_JustUnderAMonthIsOneMonthOnlyOnTheSameDate()
    {
        // February is short enough that 28 days lands on the same date next month.
        var febNow = new DateTime(2026, 2, 1, 9, 0, 0);
        Assert.Equal("in a month", RelativeTimeFormatter.Format(febNow.AddDays(28), febNow));

        // In March the same 28 days fall short of the date next month, so it stays in days, but do reach back to February 1.
        var marNow = new DateTime(2026, 3, 1, 9, 0, 0);
        Assert.Equal("in 28 days", RelativeTimeFormatter.Format(marNow.AddDays(28), marNow));
        Assert.Equal("a month ago", RelativeTimeFormatter.Format(marNow.AddDays(-28), marNow));
    }

    [Fact]
    public void Format_CountsCalendarDaysWithinTwoDays()
    {
        // 25 hours later is only the next day if it doesn't cross a second midnight.
        var lateNow = new DateTime(2026, 3, 15, 23, 30, 0);
        Assert.Equal("in 2 days", RelativeTimeFormatter.Format(lateNow.AddHours(25), lateNow));

        var earlyNow = new DateTime(2026, 3, 15, 1, 0, 0);
        Assert.Equal("tomorrow", RelativeTimeFormatter.Format(earlyNow.AddHours(25), earlyNow));
    }

    [Fact]
    public void Format_IgnoresDateTimeKind()
    {
        var target = DateTime.SpecifyKind(Now.AddHours(3), DateTimeKind.Utc);
        var now = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified);

        Assert.Equal("in 3 hours", RelativeTimeFormatter.Format(target, now));
    }
}
