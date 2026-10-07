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
    [InlineData("de-DE", "Di, 22. Sep, 13:01:22")]
    [InlineData("fr-FR", "mar. 22 sept. 13:01:22")]
    [InlineData("tr-TR", "22 Eyl Sal 13:01:22")]
    [InlineData("hu-HU", "szept. 22., K 13:01:22")]
    [InlineData("lt-LT", "rugs. 22 d., an 13:01:22")]
    [InlineData("eu-ES", "ira.ren 22(a), ar. 13:01:22")]
    public void DefaultClockFormat_ReadsNaturallyInTheRegion(string culture, string expected)
    {
        var format = FormatPresets.DefaultClockFormat(Region(culture));

        Assert.Equal(expected, Tokenizer.FormatWithTokenizerOrFallBack(Sample, format, Region(culture)));
    }

    [Theory]
    [InlineData("en-US", "Tue, Sep 22, 1:01 PM")]
    [InlineData("fr-FR", "mar. 22 sept. 13:01")]
    [InlineData("ja-JP", "9月22日(火) 13:01")]
    [InlineData("hu-HU", "szept. 22., K 13:01")]
    public void ShortDateTimePattern_ReadsNaturallyInTheRegion(string culture, string expected)
    {
        Assert.Equal(expected, Sample.ToString(FormatPresets.ShortDateTimePattern(Region(culture)), Region(culture)));
    }

    [Theory]
    [InlineData("en-US", "MMM d")]
    [InlineData("de-DE", "d. MMM")]
    [InlineData("uk-UA", "d MMMM")] // Short month names aren't in the case a date needs, so the full name is used.
    [InlineData("vi-VN", "dd/MM")] // Short month names carry the number (Thg9), so the date is written in numbers.
    [InlineData("ja-JP", "M月d日")]
    public void ShortMonthDayPattern_UsesNamesThatReadWell(string culture, string expected)
    {
        Assert.Equal(expected, FormatPresets.ShortMonthDayPattern(Region(culture).DateTimeFormat));
    }

    [Fact]
    public void ClockTokens_IncludeTheUnitInChineseJapaneseAndKorean()
    {
        var japanese = FormatPresets.ClockTokens(Region("ja-JP")).ToDictionary(t => t.Name, t => t.Token);
        Assert.Equal("{d日}", japanese["Day"]);
        Assert.Equal("{M月}", japanese["Month"]);
        Assert.Equal("{yyyy年}", japanese["Year"]);
        Assert.DoesNotContain("Month (full)", japanese.Keys);

        var english = FormatPresets.ClockTokens(Region("en-US")).ToDictionary(t => t.Name, t => t.Token);
        Assert.Equal("{dd}", english["Day"]);
        Assert.Equal("{MMMM}", english["Month (full)"]);
    }

    [Theory]
    [InlineData("en-US", true)]
    [InlineData("ko-KR", true)]
    [InlineData("de-DE", false)]
    [InlineData("ja-JP", false)]
    [InlineData("fr-CA", false)]
    public void TwentyFourHourOptions_OnlyShowWhereTheRegionUses12Hours(string culture, bool expected)
    {
        Assert.Equal(expected, FormatPresets.Uses12HourClock(Region(culture).DateTimeFormat));
        Assert.Equal(expected, FormatPresets.ForClock(Region(culture)).Any(p => p.Name == "Time (24-hour)"));
        Assert.Equal(expected, FormatPresets.ClockTokens(Region(culture)).Any(t => t.Name == "Time (24-hour)"));
    }

    [Fact]
    public void AllPresets_RenderInEveryRegion()
    {
        var now = new DateTimeOffset(Sample, TimeSpan.Zero);

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            var formats = FormatPresets.ForClock(culture).Select(p => p.Format)
                .Concat(FormatPresets.ClockTokens(culture).Select(t => t.Token))
                .Append(FormatPresets.DefaultClockFormat(culture));

            foreach (var format in formats)
                Assert.True(Tokenizer.FormatWithTokenizerOrFallBack(now, format, culture) != Tokenizer.FormatErrorMessage, $"{culture.Name}: {format}");

            Assert.NotEmpty(now.ToString(FormatPresets.ShortDateTimePattern(culture), culture));
        }
    }
}
