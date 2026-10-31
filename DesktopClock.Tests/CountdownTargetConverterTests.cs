using System;
using System.Globalization;
using System.Windows;

namespace DesktopClock.Tests;

public class CountdownTargetConverterTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly DateTime Target = new(2026, 12, 24, 18, 30, 0);

    [Fact]
    public void Convert_ShowsNoTargetAsAnEmptyBox()
    {
        Assert.Equal("", new CountdownTargetConverter().Convert(default(DateTime), typeof(string), null, German));
    }

    [Fact]
    public void Convert_ShowsTheTargetInTheRegionalFormat()
    {
        Assert.Equal("24.12.2026 18:30:00", new CountdownTargetConverter().Convert(Target, typeof(string), null, German));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertBack_ClearingTheBoxTurnsTheCountdownOff(string text)
    {
        Assert.Equal(default(DateTime), new CountdownTargetConverter().ConvertBack(text, typeof(DateTime), null, German));
    }

    [Theory]
    [InlineData("24.12.2026 18:30")]
    [InlineData("2026-12-24 18:30")]
    public void ConvertBack_ParsesRegionalAndIsoDates(string text)
    {
        Assert.Equal(Target, new CountdownTargetConverter().ConvertBack(text, typeof(DateTime), null, German));
    }

    [Fact]
    public void ConvertBack_RejectsText()
    {
        Assert.Equal(DependencyProperty.UnsetValue, new CountdownTargetConverter().ConvertBack("tomorrow", typeof(DateTime), null, German));
    }
}
