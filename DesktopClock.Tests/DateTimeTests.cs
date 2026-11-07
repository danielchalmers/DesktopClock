using System;
using System.Globalization;

namespace DesktopClock.Tests;

public class DateTimeTests
{
    [Fact]
    public void TruncateToSecond_DropsTheFractionAndKeepsTheOffset()
    {
        var time = new DateTimeOffset(2024, 7, 18, 12, 30, 45, 999, TimeSpan.FromHours(-5)).AddTicks(9999);

        var result = time.TruncateToSecond();

        Assert.Equal(new DateTimeOffset(2024, 7, 18, 12, 30, 45, TimeSpan.FromHours(-5)), result);
        Assert.Equal(TimeSpan.FromHours(-5), result.Offset);
    }

    [Theory]
    [InlineData("2024-07-18 12:30:00", "2024-07-18 12:30:00", 30, true)] // Countdown reached, on interval
    [InlineData("2024-07-18 12:30:01", "2024-07-18 12:30:01", 30, true)] // Countdown reached, not on interval
    [InlineData("2024-07-18 12:29:00", "2024-07-18 12:30:00", 30, true)] // Not yet reached, on interval
    [InlineData("2024-07-18 12:29:01", "2024-07-18 12:30:01", 30, true)] // Not yet reached, on interval
    [InlineData("2024-07-18 12:29:02", "2024-07-18 12:30:01", 30, false)] // Not yet reached, not on interval
    [InlineData("2024-07-18 12:31:00", "2024-07-18 12:30:00", 30, true)] // Past countdown, on interval
    [InlineData("2024-07-18 12:31:02", "2024-07-18 12:30:01", 30, false)] // Past countdown, not on interval
    public void IsOnInterval(string nowString, string countdownString, int intervalSeconds, bool expected)
    {
        // Arrange
        var dateTime = DateTime.Parse(nowString);
        var countdownTo = DateTime.Parse(countdownString);
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        // Act
        var result = DateTimeUtil.IsNowOrCountdownOnInterval(new DateTimeOffset(dateTime, TimeSpan.Zero), new DateTimeOffset(dateTime.AddSeconds(-1), TimeSpan.Zero), countdownTo, interval, TimeZoneInfo.Utc);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsOnInterval_DefaultCountdown_ShouldUseNowTimeOfDay()
    {
        // Arrange - when countdown is default, it uses now.TimeOfDay
        var now = new DateTime(2024, 7, 18, 12, 30, 0); // 12:30:00 = 45000 seconds
        var countdownTo = default(DateTime);
        var interval = TimeSpan.FromMinutes(30); // 1800 seconds

        // Act
        var result = DateTimeUtil.IsNowOrCountdownOnInterval(new DateTimeOffset(now, TimeSpan.Zero), new DateTimeOffset(now.AddSeconds(-1), TimeSpan.Zero), countdownTo, interval, TimeZoneInfo.Utc);

        // Assert - 45000 % 1800 = 0, so should be on interval
        Assert.True(result);
    }

    [Fact]
    public void IsOnInterval_DefaultInterval_ShouldReturnFalseUnlessCountdownReached()
    {
        // Arrange
        var now = new DateTime(2024, 7, 18, 12, 30, 0);
        var countdownTo = new DateTime(2024, 7, 18, 12, 35, 0);
        var interval = TimeSpan.Zero;

        // Act
        var result = DateTimeUtil.IsNowOrCountdownOnInterval(new DateTimeOffset(now, TimeSpan.Zero), new DateTimeOffset(now.AddSeconds(-1), TimeSpan.Zero), countdownTo, interval, TimeZoneInfo.Utc);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOnInterval_CountdownReached_ShouldReturnTrue()
    {
        // Arrange
        var now = new DateTime(2024, 7, 18, 12, 30, 45);
        var countdownTo = new DateTime(2024, 7, 18, 12, 30, 45);
        var interval = TimeSpan.Zero; // No interval

        // Act
        var result = DateTimeUtil.IsNowOrCountdownOnInterval(new DateTimeOffset(now, TimeSpan.Zero), new DateTimeOffset(now.AddSeconds(-1), TimeSpan.Zero), countdownTo, interval, TimeZoneInfo.Utc);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("12:30:43", "12:30:46", true)] // Sleep or a drag skipped the target second
    [InlineData("12:30:45", "12:30:46", false)] // Already played on the target second
    [InlineData(null, "12:30:46", false)] // Ended before the app started
    public void IsOnInterval_CountdownPlaysOnceWhenATickPassesIt(string previousTime, string nowTime, bool expected)
    {
        var day = new DateTime(2024, 7, 18);
        var previous = previousTime == null ? default : new DateTimeOffset(day + TimeSpan.Parse(previousTime, CultureInfo.InvariantCulture), TimeSpan.Zero);
        var now = new DateTimeOffset(day + TimeSpan.Parse(nowTime, CultureInfo.InvariantCulture), TimeSpan.Zero);

        var result = DateTimeUtil.IsNowOrCountdownOnInterval(now, previous, day + new TimeSpan(12, 30, 45), TimeSpan.Zero, TimeZoneInfo.Utc);

        Assert.Equal(expected, result);
    }

    private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

    [Fact]
    public void TimeUntil_CountsTheRealTimeLeftAcrossADaylightSavingChange()
    {
        // October 24 at noon in summer time; the clocks go back an hour before Christmas.
        var now = new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.FromHours(1));

        var left = DateTimeUtil.TimeUntil(new DateTime(2026, 12, 25), now, London);

        Assert.Equal(new TimeSpan(61, 13, 0, 0), left);
    }

    [Fact]
    public void TimeUntil_KeepsCountingDownWhenTheClocksGoBack()
    {
        // 01:59:59 summer time is followed by 01:00:00 winter time.
        var before = DateTimeUtil.TimeUntil(new DateTime(2026, 12, 25), new DateTimeOffset(2026, 10, 25, 1, 59, 59, TimeSpan.FromHours(1)), London);
        var after = DateTimeUtil.TimeUntil(new DateTime(2026, 12, 25), new DateTimeOffset(2026, 10, 25, 1, 0, 0, TimeSpan.Zero), London);

        Assert.Equal(TimeSpan.FromSeconds(1), before - after);
    }

    [Theory]
    [InlineData("2024-01-01", 1, 2024)] // Monday starting the first ISO week of 2024.
    [InlineData("2024-12-29", 52, 2024)] // Sunday ending the last ISO week of 2024.
    [InlineData("2024-12-30", 1, 2025)] // Monday belonging to the first ISO week of 2025.
    [InlineData("2021-01-01", 53, 2020)] // Friday belonging to the last ISO week of 2020.
    [InlineData("2026-07-06", 28, 2026)] // Mid-year date where week year matches calendar year.
    public void GetIsoWeek_MatchesIso8601(string dateString, int expectedWeek, int expectedWeekYear)
    {
        // Arrange
        var date = DateTime.Parse(dateString, CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Equal(expectedWeek, date.GetIsoWeekOfYear());
        Assert.Equal(expectedWeekYear, date.GetIsoWeekYear());
    }
}
