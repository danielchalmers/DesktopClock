using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DesktopClock.Properties;
using DesktopClock.Utilities;

namespace DesktopClock.Tests;

public class ClockThemeTests
{
    [Fact]
    public void BuiltInThemes_HaveDistinctNamesAndSaneValues()
    {
        var themes = ClockTheme.GetBuiltInThemes();

        Assert.NotEmpty(themes);
        Assert.Equal(themes.Count, themes.Select(t => t.Name).Distinct().Count());

        foreach (var theme in themes)
        {
            Assert.False(string.IsNullOrWhiteSpace(theme.Name));
            Assert.False(string.IsNullOrWhiteSpace(theme.FontFamily));
            Assert.InRange(theme.BackgroundOpacity, 0, 1);
            Assert.True(theme.BackgroundCornerRadius >= 0);
            Assert.True(theme.OutlineThickness >= 0);
        }
    }

    [Fact]
    public void BuiltInThemes_FontWeightsAndStylesParse()
    {
        // ThemePresetPicker converts these strings directly when rendering the chips,
        // so an invalid value in a preset would crash the settings window at load.
        var weightConverter = new FontWeightConverter();
        var styleConverter = new FontStyleConverter();

        foreach (var theme in ClockTheme.GetBuiltInThemes())
        {
            Assert.IsType<FontWeight>(weightConverter.ConvertFromString(theme.FontWeight));
            Assert.IsType<FontStyle>(styleConverter.ConvertFromString(theme.FontStyle));
        }
    }

    [Fact]
    public void Apply_ClearsLeftoverBackgroundImage()
    {
        // Arrange - a user previously picked a background image.
        var settings = (Settings)Activator.CreateInstance(typeof(Settings), nonPublic: true)!;
        settings.BackgroundImagePath = @"C:\wallpaper.png";

        var theme = new ClockTheme("Test", "Segoe UI", "Normal",
            Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0x00, 0x00, 0x00),
            backgroundEnabled: true, backgroundOpacity: 1, backgroundCornerRadius: 8, outlineThickness: 0.2);

        // Act
        theme.Apply(settings);

        // Assert - the image would otherwise keep covering the theme's background color.
        Assert.Equal(string.Empty, settings.BackgroundImagePath);
    }

    [Fact]
    public void Apply_SetsAppearanceFromTheme()
    {
        var settings = (Settings)Activator.CreateInstance(typeof(Settings), nonPublic: true)!;

        var theme = new ClockTheme("Test", "Georgia", "SemiBold",
            Color.FromRgb(0x11, 0x22, 0x33), Color.FromRgb(0x44, 0x55, 0x66),
            backgroundEnabled: false, backgroundOpacity: 0.5, backgroundCornerRadius: 6, outlineThickness: 1.5,
            fontStyle: "Italic");

        theme.Apply(settings);

        Assert.Equal("Georgia", settings.FontFamily);
        Assert.Equal("SemiBold", settings.FontWeight);
        Assert.Equal("Italic", settings.FontStyle);
        Assert.Equal(Color.FromRgb(0x11, 0x22, 0x33), settings.TextColor);
        Assert.Equal(Color.FromRgb(0x44, 0x55, 0x66), settings.OuterColor);
        Assert.False(settings.BackgroundEnabled);
        Assert.Equal(0.5, settings.BackgroundOpacity);
        Assert.Equal(6, settings.BackgroundCornerRadius);
        Assert.Equal(1.5, settings.OutlineThickness);
        Assert.Equal(1, settings.TextOpacity);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThemeDefaults_AreReadableInLightAndDarkMode(bool isLightTheme)
    {
        // A fresh install and the System preset follow light or dark mode but not the accent, which can be any shade.
        var (text, outer) = SystemThemeService.GetThemeDefaults(isLightTheme);

        Assert.True(ClockTheme.Contrast(text, outer) >= 7, $"{text} on {outer}");
    }

    [Theory]
    [InlineData("#3B003B", "#FFFFFF")] // Plum
    [InlineData("#0078D4", "#FFFFFF")] // Default Windows blue keeps white text.
    [InlineData("#D50056", "#FFFFFF")] // Dark pink
    [InlineData("#FFB900", "#1A1A1A")] // Gold
    [InlineData("#00CC6A", "#1A1A1A")] // Light green
    [InlineData("#FFFFFF", "#1A1A1A")]
    [InlineData("#000000", "#FFFFFF")]
    public void ReadableTextOn_PicksWhiteOrDarkByContrast(string background, string expected)
    {
        var text = ClockTheme.ReadableTextOn((Color)ColorConverter.ConvertFromString(background));

        Assert.Equal((Color)ColorConverter.ConvertFromString(expected), text);
    }

    [Theory]
    [InlineData("#3B003B", "#FFFFFF")] // Plum keeps white text.
    [InlineData("#FFB900", "#1A1A1A")] // Gold gets dark text.
    public void AccentTheme_PicksReadableTextForTheAccent(string accent, string expectedText)
    {
        var theme = ClockTheme.CreateAccentTheme((Color)ColorConverter.ConvertFromString(accent));

        Assert.Equal((Color)ColorConverter.ConvertFromString(accent), theme.OuterColor);
        Assert.Equal((Color)ColorConverter.ConvertFromString(expectedText), theme.TextColor);
    }

    [Fact]
    public void ReadableTextOn_IsAtLeastFourToOneOnEveryGray()
    {
        for (var v = 0; v <= 255; v++)
        {
            var background = Color.FromRgb((byte)v, (byte)v, (byte)v);
            Assert.True(ClockTheme.Contrast(ClockTheme.ReadableTextOn(background), background) >= 4, $"gray {v}");
        }
    }
}
