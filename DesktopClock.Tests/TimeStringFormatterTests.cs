using System;
using System.Globalization;
using Humanizer;

namespace DesktopClock.Tests;

public class TimeStringFormatterTests
{
    [Fact]
    public void Format_UsesFormatWhenNoCountdown()
    {
        var now = new DateTimeOffset(2024, 1, 1, 10, 15, 0, TimeSpan.Zero);
        var timeZone = TimeZoneInfo.CreateCustomTimeZone("UtcPlus2", TimeSpan.FromHours(2), "UtcPlus2", "UtcPlus2");

        var result = TimeStringFormatter.Format(
            now,
            now.DateTime,
            timeZone,
            default,
            "HH:mm",
            null,
            TextTransform.None,
            CultureInfo.InvariantCulture);

        Assert.Equal("12:15", result);
    }

    [Fact]
    public void Format_UsesFormatProviderForStandardFormats()
    {
        var now = new DateTimeOffset(2024, 1, 2, 10, 15, 0, TimeSpan.Zero);
        var formatProvider = new CultureInfo("en-GB");

        var result = TimeStringFormatter.Format(
            now,
            now.DateTime,
            TimeZoneInfo.Utc,
            default,
            "d",
            null,
            TextTransform.None,
            formatProvider);

        Assert.Equal(now.ToString("d", formatProvider), result);
    }

    [Fact]
    public void Format_UsesTokenizedCountdownFormat()
    {
        var now = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var nowDateTime = now.DateTime;
        var countdownTo = nowDateTime.AddHours(1).AddMinutes(2).AddSeconds(3);

        var result = TimeStringFormatter.Format(
            now,
            nowDateTime,
            TimeZoneInfo.Utc,
            countdownTo,
            "HH:mm",
            "{hh\\:mm}",
            TextTransform.None,
            CultureInfo.InvariantCulture);

        Assert.Equal("01:02", result);
    }

    [Fact]
    public void Format_UsesCountdownFormatWhenProvided()
    {
        var now = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var nowDateTime = now.DateTime;
        var countdownTo = nowDateTime.AddMinutes(90);
        var formatProvider = CultureInfo.InvariantCulture;

        var result = TimeStringFormatter.Format(
            now,
            nowDateTime,
            TimeZoneInfo.Utc,
            countdownTo,
            "HH:mm",
            "c",
            TextTransform.None,
            formatProvider);

        Assert.Equal((countdownTo - nowDateTime).ToString("c", formatProvider), result);
    }

    [Fact]
    public void Format_UsesHumanizerWhenCountdownFormatMissing()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var enUs = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = enUs;
            CultureInfo.CurrentUICulture = enUs;

            var now = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
            var nowDateTime = now.DateTime;
            var countdownTo = nowDateTime.AddDays(1);

            var result = TimeStringFormatter.Format(
                now,
                nowDateTime,
                TimeZoneInfo.Utc,
                countdownTo,
                "HH:mm",
                " ",
                TextTransform.None,
                CultureInfo.CurrentCulture);

            Assert.Equal(countdownTo.Humanize(utcDate: false, dateToCompareAgainst: nowDateTime), result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Format_ConvertsUsingSelectedTimeZone()
    {
        var now = new DateTimeOffset(2024, 1, 1, 23, 30, 0, TimeSpan.Zero);
        var timeZone = TimeZoneInfo.CreateCustomTimeZone("UtcMinus5", TimeSpan.FromHours(-5), "UtcMinus5", "UtcMinus5");

        var result = TimeStringFormatter.Format(
            now,
            now.DateTime,
            timeZone,
            default,
            "HH:mm",
            null,
            TextTransform.None,
            CultureInfo.InvariantCulture);

        Assert.Equal("18:30", result);
    }

    [Fact]
    public void Format_AppliesTextTransform()
    {
        var now = new DateTimeOffset(2024, 1, 1, 13, 5, 0, TimeSpan.Zero);
        var formatProvider = new CultureInfo("en-US");

        var result = TimeStringFormatter.Format(
            now,
            now.DateTime,
            TimeZoneInfo.Utc,
            default,
            "h:mm tt",
            null,
            TextTransform.Lowercase,
            formatProvider);

        Assert.Equal("1:05 pm", result);
    }
}
