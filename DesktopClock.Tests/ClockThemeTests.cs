using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DesktopClock.Properties;

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

    [Fact]
    public void SystemTheme_IsReadableWithAnyAccentColor()
    {
        // The first preset is the look a fresh install starts with; it follows light or dark mode but not the accent, which can be any shade.
        var system = ClockTheme.GetBuiltInThemes().First();

        Assert.True(Contrast(system.TextColor, system.OuterColor) >= 7, $"{system.TextColor} on {system.OuterColor}");
    }

    /// <summary>
    /// The WCAG contrast ratio between two colors, from 1 (same) to 21 (black and white).
    /// </summary>
    private static double Contrast(Color a, Color b)
    {
        static double Channel(byte value)
        {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        static double Luminance(Color c) => (0.2126 * Channel(c.R)) + (0.7152 * Channel(c.G)) + (0.0722 * Channel(c.B));

        var (light, dark) = (Math.Max(Luminance(a), Luminance(b)), Math.Min(Luminance(a), Luminance(b)));
        return (light + 0.05) / (dark + 0.05);
    }
}
