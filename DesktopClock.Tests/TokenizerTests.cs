using System;
using System.Globalization;

namespace DesktopClock.Tests;

public class TokenizerTests
{
    [Fact]
    public void FormatWithTokenizer()
    {
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "{dddd}, {MMM dd}, {HH:mm:ss}";
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        Assert.Equal("Sunday, Sep 24, 12:13:14", result);
    }

    [Fact]
    public void FormatWithFallback()
    {
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "dddd, MMM dd, HH:mm:ss";
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        Assert.Equal("Sunday, Sep 24, 12:13:14", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FormatWithTokenizer_NullOrWhitespace_ShouldReturnDefaultFormatOfTheGivenCulture(string format)
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, new CultureInfo("de-DE"));

        // Assert
        Assert.Equal("24.09.2023 12:13:14", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{d}")]
    public void FormatWithTokenizer_DatePastTheEndOfTheRegionalCalendar_ShouldReturnErrorMessage(string format)
    {
        // The Saudi regional format's Um al-Qura calendar ends in November 2077, and the default format used to throw on later dates, which crashed the clock every second.
        var dateTime = new DateTime(2078, 1, 1, 12, 13, 14);

        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, new CultureInfo("ar-SA"));

        Assert.Equal(Tokenizer.FormatErrorMessage, result);
    }

    [Fact]
    public void FormatWithTokenizer_SingleToken_ShouldFormat()
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "{yyyy}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("2023", result);
    }

    [Fact]
    public void FormatWithTokenizer_MixedTextAndTokens_ShouldFormat()
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "Today is {dddd}!";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Today is Sunday!", result);
    }

    [Fact]
    public void FormatWithTokenizer_MultipleTokens_ShouldFormatAll()
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "{HH}:{mm}:{ss}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("12:13:14", result);
    }

    [Fact]
    public void FormatWithTokenizer_12HourFormat_ShouldWork()
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 14, 30, 45);
        var format = "{h:mm tt}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("2:30 PM", result);
    }

    [Fact]
    public void FormatWithTokenizer_DateTimeOffset_ShouldWork()
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2023, 09, 24, 12, 13, 14, TimeSpan.FromHours(2));
        var format = "{dddd}, {MMM dd}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTimeOffset, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Sunday, Sep 24", result);
    }

    [Theory]
    [InlineData("2024-01-01", "Week 01")]
    [InlineData("2024-12-29", "Week 52")]
    [InlineData("2024-12-30", "Week 01")]
    [InlineData("2021-01-01", "Week 53")]
    [InlineData("2026-02-02", "Week 06")]
    public void FormatWithTokenizer_WeekToken_ShouldUseIsoWeek(string dateString, string expected)
    {
        // Arrange
        var dateTime = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        var format = "Week {week}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2024-12-30", "2025-W01")]
    [InlineData("2021-01-01", "2020-W53")]
    [InlineData("2026-05-06", "2026-W19")]
    public void FormatWithTokenizer_WeekYearToken_ShouldUseIsoWeekYear(string dateString, string expected)
    {
        // Arrange
        var dateTime = DateTime.Parse(dateString, CultureInfo.InvariantCulture);
        var format = "{weekYear}-W{week}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatWithTokenizer_WeekToken_MixesWithStandardTokens()
    {
        // Arrange
        var dateTimeOffset = new DateTimeOffset(2024, 12, 30, 14, 30, 0, TimeSpan.Zero);
        var format = "{ddd}, {MMM dd} (W{week})";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTimeOffset, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Mon, Dec 30 (W01)", result);
    }

    [Fact]
    public void FormatWithTokenizer_WeekToken_OnTimeSpan_ShowsErrorMessage()
    {
        // Arrange - week tokens only apply to dates, so a countdown format using one is invalid.
        var timeSpan = new TimeSpan(1, 2, 3, 4);
        var format = "Week {week}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(timeSpan, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Tokenizer.FormatErrorMessage, result);
    }

    [Theory]
    [InlineData("2026-01-01", "001")]
    [InlineData("2026-07-24", "205")]
    [InlineData("2026-12-31", "365")]
    [InlineData("2028-02-29", "060")]
    [InlineData("2028-12-31", "366")]
    public void FormatWithTokenizer_DayOfYearToken_ShouldBeThreeDigits(string dateString, string expected)
    {
        // Arrange
        var dateTime = DateTime.Parse(dateString, CultureInfo.InvariantCulture);

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, "{dayOfYear}", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatWithTokenizer_DayOfYearToken_UsesTheClocksTimeZone()
    {
        // Arrange - just after midnight UTC on New Year's Day is still New Year's Eve in New York.
        var newYork = new DateTimeOffset(2026, 12, 31, 19, 30, 0, TimeSpan.FromHours(-5));

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(newYork, "{yyyy}-{dayOfYear}", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("2026-365", result);
    }

    [Fact]
    public void FormatWithTokenizer_DayOfYearToken_OnTimeSpan_ShowsErrorMessage()
    {
        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(new TimeSpan(3, 0, 0, 0), "{dayOfYear}", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Tokenizer.FormatErrorMessage, result);
    }

    [Fact]
    public void FormatWithTokenizer_StandardFormat_WithoutBraces_ShouldWork()
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "D"; // Long date pattern

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Sunday, 24 September 2023", result);
    }

    [Fact]
    public void FormatWithTokenizer_TimeSpan_ShouldWork()
    {
        // Arrange
        var timeSpan = new TimeSpan(1, 23, 45, 30);
        var format = @"{d\.hh\:mm\:ss}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(timeSpan, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("1.23:45:30", result);
    }

    [Fact]
    public void FormatWithTokenizer_InvalidFormat_ShouldReturnErrorMessage()
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "{Q}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Tokenizer.FormatErrorMessage, result);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{HH:mm")]
    [InlineData("HH:mm}")]
    [InlineData("{{HH:mm}}")]
    public void FormatWithTokenizer_BrokenTokenSyntax_ShouldReturnErrorMessage(string format)
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(Tokenizer.FormatErrorMessage, result);
    }

    [Theory]
    [InlineData("{yyyy}-{MM}-{dd}", "2023-09-24")]
    [InlineData("{yyyy}/{MM}/{dd}", "2023/09/24")]
    [InlineData("{dd}.{MM}.{yyyy}", "24.09.2023")]
    public void FormatWithTokenizer_VariousDateFormats_ShouldWork(string format, string expected)
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24);

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, result);
    }
}
