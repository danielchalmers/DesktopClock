using System;
using System.Globalization;

namespace DesktopClock.Tests;

public class DateTimeTests
{
    [Theory]
    [InlineData("2024-07-18T12:30:45.123Z", "2024-07-18T12:30:45.456Z", true)] // Different millisecond
    [InlineData("2024-07-18T12:30:45.123Z", "2024-07-18T12:30:46.123Z", false)] // Different second
    [InlineData("2024-07-18T12:30:45.123Z", "2024-07-18T12:31:45.123Z", false)] // Different minute
    [InlineData("2024-07-18T12:30:45.123Z", "2024-07-18T13:30:45.123Z", false)] // Different hour
    [InlineData("2024-07-18T12:30:45.123Z", "2024-07-19T12:30:45.123Z", false)] // Different day
    [InlineData("2024-07-18T12:30:45.123Z", "2024-08-18T12:30:45.123Z", false)] // Different month
    [InlineData("2024-07-18T12:30:45.123Z", "2025-07-18T12:30:45.123Z", false)] // Different year
    public void EqualExcludingMilliseconds(string dt1String, string dt2String, bool expected)
    {
        // Arrange
        var dt1 = DateTime.Parse(dt1String);
        var dt2 = DateTime.Parse(dt2String);

        // Act
        var result = dt1.EqualExcludingMillisecond(dt2);

        // Assert
        Assert.Equal(expected, result);
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
        var result = DateTimeUtil.IsNowOrCountdownOnInterval(dateTime, countdownTo, interval);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("dddd, MMMM dd", "Monday, January 01")]
    [InlineData("yyyy-MM-dd", "2024-01-01")]
    [InlineData("HH:mm:ss", "00:00:00")]
    [InlineData("MMMM dd, yyyy", "January 01, 2024")]
    public void FromFormat_CreatesCorrectExample(string format, string expected)
    {
        // Arrange
        var dateTimeOffset = new DateTime(2024, 01, 01);

        // Act
        var dateFormatExample = DateFormatExample.FromFormat(format, dateTimeOffset, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(format, dateFormatExample.Format);
        Assert.Equal(expected, dateFormatExample.Example);
    }
}
