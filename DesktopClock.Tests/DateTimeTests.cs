using System;
using System.Globalization;
using System.Linq;

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

    [Fact]
    public void EqualExcludingMilliseconds_SameDateTime_ShouldBeTrue()
    {
        // Arrange
        var dt1 = new DateTime(2024, 7, 18, 12, 30, 45, 123);
        var dt2 = new DateTime(2024, 7, 18, 12, 30, 45, 123);

        // Act
        var result = dt1.EqualExcludingMillisecond(dt2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EqualExcludingMilliseconds_OnlyMillisecondsDiffer_ShouldBeTrue()
    {
        // Arrange
        var dt1 = new DateTime(2024, 7, 18, 12, 30, 45, 0);
        var dt2 = new DateTime(2024, 7, 18, 12, 30, 45, 999);

        // Act
        var result = dt1.EqualExcludingMillisecond(dt2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void EqualExcludingMilliseconds_Reflexive_ShouldBeTrue()
    {
        // Arrange
        var dt = DateTime.Now;

        // Act & Assert
        Assert.True(dt.EqualExcludingMillisecond(dt));
    }

    [Fact]
    public void EqualExcludingMilliseconds_Symmetric_ShouldWork()
    {
        // Arrange
        var dt1 = new DateTime(2024, 7, 18, 12, 30, 45, 100);
        var dt2 = new DateTime(2024, 7, 18, 12, 30, 45, 200);

        // Act & Assert
        Assert.Equal(dt1.EqualExcludingMillisecond(dt2), dt2.EqualExcludingMillisecond(dt1));
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

    [Fact]
    public void IsOnInterval_DefaultCountdown_ShouldUseNowTimeOfDay()
    {
        // Arrange - when countdown is default, it uses now.TimeOfDay
        var now = new DateTime(2024, 7, 18, 12, 30, 0); // 12:30:00 = 45000 seconds
        var countdownTo = default(DateTime);
        var interval = TimeSpan.FromMinutes(30); // 1800 seconds

        // Act
        var result = DateTimeUtil.IsNowOrCountdownOnInterval(now, countdownTo, interval);

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
        var result = DateTimeUtil.IsNowOrCountdownOnInterval(now, countdownTo, interval);

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
        var result = DateTimeUtil.IsNowOrCountdownOnInterval(now, countdownTo, interval);

        // Assert
        Assert.True(result);
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

    [Fact]
    public void FromFormat_WithTokenizedFormat_ShouldWork()
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2024, 3, 15, 14, 30, 0, TimeSpan.Zero);
        var format = "{ddd}, {MMM dd}, {HH:mm}";

        // Act
        var dateFormatExample = DateFormatExample.FromFormat(format, dateTimeOffset, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(format, dateFormatExample.Format);
        Assert.Equal("Fri, Mar 15, 14:30", dateFormatExample.Example);
    }

    [Fact]
    public void DefaultExamples_ShouldNotBeEmpty()
    {
        // Assert
        Assert.NotEmpty(DateFormatExample.DefaultExamples);
    }

    [Fact]
    public void DefaultExamples_AllShouldHaveFormatAndExample()
    {
        // Assert
        foreach (var example in DateFormatExample.DefaultExamples)
        {
            Assert.NotNull(example.Format);
            Assert.NotEmpty(example.Format);
            Assert.NotNull(example.Example);
            Assert.NotEmpty(example.Example);
        }
    }

    [Fact]
    public void DefaultExamples_ShouldContainCustomFormats()
    {
        // Assert - check for some expected custom formats
        var formats = DateFormatExample.DefaultExamples.Select(e => e.Format).ToList();

        Assert.Contains(formats, f => f.Contains("{ddd}"));
        Assert.Contains(formats, f => f.Contains("{HH:mm}") || f.Contains("{h:mm tt}"));
    }

    [Fact]
    public void DefaultExamples_ShouldContainStandardFormats()
    {
        // Assert - check for some expected standard formats
        var formats = DateFormatExample.DefaultExamples.Select(e => e.Format).ToList();

        Assert.Contains("D", formats);  // Long date pattern
        Assert.Contains("T", formats);  // Long time pattern
        Assert.Contains("t", formats);  // Short time pattern
    }
}
