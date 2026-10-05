using System;
using System.Globalization;
using System.Linq;

namespace DesktopClock.Tests;

[UseUICulture("en-US")]
public class FormatPresetsTests
{
    private static readonly DateTime Sample = new(2026, 9, 22, 13, 1, 22);

    private static CultureInfo Region(string culture) => CultureInfo.GetCultureInfo(culture);

    [Fact]
    public void ForClock_KeepsTheUsFormats()
    {
        var presets = FormatPresets.ForClock(Region("en-US")).ToDictionary(p => p.Name, p => p.Format);

        Assert.Equal("{h:mm tt}", presets["Time"]);
        Assert.Equal("{HH:mm}", presets["Time (24-hour)"]);
        Assert.Equal("{h:mm:ss tt}", presets["Time with seconds"]);
        Assert.Equal("{ddd}, {h:mm tt}", presets["Weekday and time"]);
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
        Assert.Equal("{M月d日} {dddd}", chinese["Date only"]);
    }

    [Theory]
    [InlineData("en-US", "Tue, Sep 22, 1:01:22 PM")]
    [InlineData("ja-JP", "9月22日(火) 13:01:22")]
    [InlineData("zh-CN", "9月22日 周二 13:01:22")]
    [InlineData("ko-KR", "9월 22일 (화) 오후 1:01:22")]
    public void DefaultClockFormat_ReadsNaturallyInTheRegion(string culture, string expected)
    {
        var format = FormatPresets.DefaultClockFormat(Region(culture));

        Assert.Equal(expected, Tokenizer.FormatWithTokenizerOrFallBack(Sample, format, Region(culture)));
    }

    [Theory]
    [InlineData("en-US", "Tue, Sep 22 1:01 PM")]
    [InlineData("ja-JP", "9月22日(火) 13:01")]
    public void ShortDateTimePattern_ReadsNaturallyInTheRegion(string culture, string expected)
    {
        Assert.Equal(expected, Sample.ToString(FormatPresets.ShortDateTimePattern(Region(culture)), Region(culture)));
    }

    [Theory]
    [InlineData("en-US", true)]
    [InlineData("ko-KR", true)]
    [InlineData("de-DE", false)]
    [InlineData("ja-JP", false)]
    [InlineData("fr-CA", false)]
    public void ForClock_OnlyOffersA24HourPresetWhereTheRegionUses12Hours(string culture, bool expected)
    {
        Assert.Equal(expected, FormatPresets.Uses12HourClock(Region(culture).DateTimeFormat));
        Assert.Equal(expected, FormatPresets.ForClock(Region(culture)).Any(p => p.Name == "Time (24-hour)"));
    }

    [Fact]
    public void AllPresets_RenderInEveryRegion()
    {
        var now = new DateTimeOffset(Sample, TimeSpan.Zero);

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            var formats = FormatPresets.ForClock(culture).Select(p => p.Format)
                .Append(FormatPresets.DefaultClockFormat(culture));

            foreach (var format in formats)
                Assert.True(Tokenizer.FormatWithTokenizerOrFallBack(now, format, culture) != Tokenizer.FormatErrorMessage, $"{culture.Name}: {format}");

            Assert.NotEmpty(now.ToString(FormatPresets.ShortDateTimePattern(culture), culture));
        }
    }
}
