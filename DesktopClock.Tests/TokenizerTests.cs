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
}
