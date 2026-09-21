using System;

namespace DesktopClock.Tests;

public class RelativeTimeFormatterTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 12, 34, 56, 500);

    [Theory]
    [InlineData("00:00:00.499", "now")]
    [InlineData("00:00:00.500", "now")]
    [InlineData("00:00:01", "one second from now")]
    [InlineData("00:00:59.999", "59 seconds from now")]
    [InlineData("00:01:00", "a minute from now")]
    [InlineData("00:01:59", "a minute from now")]
    [InlineData("00:02:00", "2 minutes from now")]
    [InlineData("00:59:59", "59 minutes from now")]
    [InlineData("01:00:00", "an hour from now")]
    [InlineData("01:29:59", "an hour from now")]
    [InlineData("01:30:00", "an hour from now")]
    [InlineData("02:00:00", "2 hours from now")]
    [InlineData("23:59:59", "23 hours from now")]
    [InlineData("1.00:00:00", "tomorrow")]
    [InlineData("1.11:25:03", "tomorrow")]
    [InlineData("1.11:25:04", "2 days from now")]
    [InlineData("2.00:00:00", "2 days from now")]
    [InlineData("27.23:59:59", "27 days from now")]
    [InlineData("28.00:00:00", "28 days from now")]
    [InlineData("30.00:00:00", "one month from now")]
    [InlineData("58.23:59:59", "one month from now")]
    [InlineData("59.00:00:00", "2 months from now")]
    [InlineData("344.23:59:59", "11 months from now")]
    [InlineData("345.00:00:00", "one year from now")]
    [InlineData("729.23:59:59", "one year from now")]
    [InlineData("730.00:00:00", "2 years from now")]
    public void Format_PicksLargestUnitForFutureTarget(string offset, string expected)
    {
        var target = Now + TimeSpan.Parse(offset);

        Assert.Equal(expected, RelativeTimeFormatter.Format(target, Now));
    }

    [Theory]
    [InlineData("00:00:01", "one second ago")]
    [InlineData("00:00:30", "30 seconds ago")]
    [InlineData("00:01:00", "a minute ago")]
    [InlineData("00:05:00", "5 minutes ago")]
    [InlineData("01:00:00", "an hour ago")]
    [InlineData("03:00:00", "3 hours ago")]
    [InlineData("1.00:00:00", "yesterday")]
    [InlineData("3.00:00:00", "3 days ago")]
    [InlineData("30.00:00:00", "one month ago")]
    [InlineData("90.00:00:00", "3 months ago")]
    [InlineData("365.00:00:00", "one year ago")]
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
        Assert.Equal("one month from now", RelativeTimeFormatter.Format(febNow.AddDays(28), febNow));

        // In March the same 28 days fall short of the date next month, so it stays in days, but do reach back to February 1.
        var marNow = new DateTime(2026, 3, 1, 9, 0, 0);
        Assert.Equal("28 days from now", RelativeTimeFormatter.Format(marNow.AddDays(28), marNow));
        Assert.Equal("one month ago", RelativeTimeFormatter.Format(marNow.AddDays(-28), marNow));
    }

    [Fact]
    public void Format_CountsCalendarDaysWithinTwoDays()
    {
        // 25 hours later is only the next day if it doesn't cross a second midnight.
        var lateNow = new DateTime(2026, 3, 15, 23, 30, 0);
        Assert.Equal("2 days from now", RelativeTimeFormatter.Format(lateNow.AddHours(25), lateNow));

        var earlyNow = new DateTime(2026, 3, 15, 1, 0, 0);
        Assert.Equal("tomorrow", RelativeTimeFormatter.Format(earlyNow.AddHours(25), earlyNow));
    }

    [Fact]
    public void Format_IgnoresDateTimeKind()
    {
        var target = DateTime.SpecifyKind(Now.AddHours(3), DateTimeKind.Utc);
        var now = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified);

        Assert.Equal("3 hours from now", RelativeTimeFormatter.Format(target, now));
    }
}
