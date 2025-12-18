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
    public void FormatWithTokenizer_NullOrWhitespace_ShouldReturnDefaultToString(string format)
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(dateTime.ToString(), result);
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
    public void FormatWithTokenizer_InvalidFormat_ShouldNotThrow()
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "{invalid-format-string-xyz}";

        // Act - should not throw, may produce unexpected output but should handle gracefully
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert - just verify it returns something and doesn't throw
        Assert.NotNull(result);
    }

    [Fact]
    public void FormatWithTokenizer_EmptyTokens_ShouldHandleGracefully()
    {
        // Arrange
        var dateTime = new DateTime(2023, 09, 24, 12, 13, 14);
        var format = "{}";

        // Act
        var result = Tokenizer.FormatWithTokenizerOrFallBack(dateTime, format, CultureInfo.InvariantCulture);

        // Assert
        // Empty braces should be handled
        Assert.NotNull(result);
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
