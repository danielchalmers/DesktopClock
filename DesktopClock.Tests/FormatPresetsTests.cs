using System;
using System.Globalization;
using System.Linq;

namespace DesktopClock.Tests;

[UseUICulture("en-US")]
public class FormatPresetsTests
{
    private static DateTimeFormatInfo Region(string culture) => CultureInfo.GetCultureInfo(culture).DateTimeFormat;

    [Fact]
    public void ForClock_KeepsTheUsFormats()
    {
        var presets = FormatPresets.ForClock(Region("en-US")).ToDictionary(p => p.Name, p => p.Format);

        Assert.Equal("{h:mm tt}", presets["Time"]);
        Assert.Equal("{HH:mm}", presets["Time, 24-hour"]);
        Assert.Equal("{h:mm:ss tt}", presets["Time with seconds"]);
        Assert.Equal("{ddd}, {MMM d}, {h:mm tt}", presets["Date and time"]);
        Assert.Equal("{dddd}, {MMMM d}", presets["Date only"]);
    }

    [Fact]
    public void ForClock_FollowsRegionalTimeAndDateOrder()
    {
        var german = FormatPresets.ForClock(Region("de-DE")).ToDictionary(p => p.Name, p => p.Format);
        Assert.Equal("{HH:mm}", german["Time"]);
        Assert.Equal("{dddd}, {d. MMMM}", german["Date only"]);

        var chinese = FormatPresets.ForClock(Region("zh-CN")).ToDictionary(p => p.Name, p => p.Format);
        Assert.Equal("{dddd}, {M月d日}", chinese["Date only"]);
    }

    [Theory]
    [InlineData("en-US", true)]
    [InlineData("ko-KR", true)]
    [InlineData("de-DE", false)]
    [InlineData("ja-JP", false)]
    [InlineData("fr-CA", false)]
    public void ForClock_OnlyOffersA24HourPresetWhereTheRegionUses12Hours(string culture, bool expected)
    {
        Assert.Equal(expected, FormatPresets.Uses12HourClock(Region(culture)));
        Assert.Equal(expected, FormatPresets.ForClock(Region(culture)).Any(p => p.Name == "Time, 24-hour"));
    }

    [Fact]
    public void DefaultClockFormat_FollowsTheRegion()
    {
        Assert.Equal("{ddd}, {MMM d}, {h:mm:ss tt}", FormatPresets.DefaultClockFormat(Region("en-US")));
        Assert.Equal("{ddd}, {d. MMM}, {HH:mm:ss}", FormatPresets.DefaultClockFormat(Region("de-DE")));
    }

    [Fact]
    public void AllPresets_RenderInEveryRegion()
    {
        var now = new DateTimeOffset(2026, 9, 22, 13, 1, 22, TimeSpan.Zero);

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            var formats = FormatPresets.ForClock(culture.DateTimeFormat).Select(p => p.Format).Append(FormatPresets.DefaultClockFormat(culture.DateTimeFormat));

            foreach (var format in formats)
                Assert.True(Tokenizer.FormatWithTokenizerOrFallBack(now, format, culture) != Tokenizer.FormatErrorMessage, $"{culture.Name}: {format}");
        }
    }
}
