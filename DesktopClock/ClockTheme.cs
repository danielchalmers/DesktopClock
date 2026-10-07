using System;
using System.Collections.Generic;
using System.Windows.Media;
using DesktopClock.Properties;
using DesktopClock.Utilities;

namespace DesktopClock;

/// <summary>
/// A coherent visual identity for the clock — font, colors, opacity, and shape — applied in one click.
/// </summary>
public sealed class ClockTheme
{
    public ClockTheme(string name, string fontFamily, string fontWeight, Color textColor, Color outerColor,
        bool backgroundEnabled, double backgroundOpacity, double backgroundCornerRadius, double outlineThickness,
        string fontStyle = "Normal")
    {
        Name = name;
        FontFamily = fontFamily;
        FontWeight = fontWeight;
        FontStyle = fontStyle;
        TextColor = textColor;
        OuterColor = outerColor;
        BackgroundEnabled = backgroundEnabled;
        BackgroundOpacity = backgroundOpacity;
        BackgroundCornerRadius = backgroundCornerRadius;
        OutlineThickness = outlineThickness;
    }

    public string Name { get; }
    public string FontFamily { get; }
    public string FontWeight { get; }
    public string FontStyle { get; }
    public Color TextColor { get; }
    public Color OuterColor { get; }
    public bool BackgroundEnabled { get; }
    public double BackgroundOpacity { get; }
    public double BackgroundCornerRadius { get; }
    public double OutlineThickness { get; }

    /// <summary>
    /// Built-in looks, starting with the system-seeded default.
    /// </summary>
    public static IReadOnlyList<ClockTheme> GetBuiltInThemes()
    {
        var accent = SystemThemeService.GetSystemAccentColor();

        return new[]
        {
            CreateSystemTheme(),
            new ClockTheme(Loc.Get("ThemeAccent"), "Segoe UI", "SemiBold",
                ReadableTextOn(accent), accent,
                backgroundEnabled: true, backgroundOpacity: 1, backgroundCornerRadius: 1, outlineThickness: 0.2),
            new ClockTheme(Loc.Get("ThemeSmoke"), "Segoe UI", "Normal",
                Color.FromRgb(0xF2, 0xF2, 0xF2), Color.FromRgb(0x0A, 0x0A, 0x10),
                backgroundEnabled: true, backgroundOpacity: 0.55, backgroundCornerRadius: 1, outlineThickness: 0.2),
            new ClockTheme(Loc.Get("ThemeTerminal"), "Consolas", "Bold",
                Color.FromRgb(0x00, 0xE5, 0xFF), Color.FromRgb(0x0C, 0x0C, 0x0C),
                backgroundEnabled: true, backgroundOpacity: 0.85, backgroundCornerRadius: 1, outlineThickness: 0.2),
            new ClockTheme(Loc.Get("ThemeMidnight"), "Segoe UI", "SemiBold",
                Color.FromRgb(0x4C, 0xC2, 0xFF), Color.FromRgb(0x1B, 0x1B, 0x1B),
                backgroundEnabled: true, backgroundOpacity: 0.95, backgroundCornerRadius: 1, outlineThickness: 0.2),
            new ClockTheme(Loc.Get("ThemePaper"), "Georgia", "Normal",
                Color.FromRgb(0x1A, 0x1A, 0x1A), Color.FromRgb(0xFA, 0xF9, 0xF6),
                backgroundEnabled: true, backgroundOpacity: 0.97, backgroundCornerRadius: 1, outlineThickness: 0.2),
            new ClockTheme(Loc.Get("ThemeMinimal"), "Segoe UI", "Light",
                Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0x00, 0x00, 0x00),
                backgroundEnabled: false, backgroundOpacity: 1, backgroundCornerRadius: 1, outlineThickness: 0),
            new ClockTheme(Loc.Get("ThemeChalk"), "Segoe UI", "SemiBold",
                Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0x00, 0x00, 0x00),
                backgroundEnabled: false, backgroundOpacity: 1, backgroundCornerRadius: 1, outlineThickness: 1.5),
        };
    }

    /// <summary>
    /// White or near-black text, whichever stands out more on the given background, so a light accent like yellow still gets readable text.
    /// </summary>
    public static Color ReadableTextOn(Color background)
    {
        var white = Color.FromRgb(0xFF, 0xFF, 0xFF);
        var black = Color.FromRgb(0x1A, 0x1A, 0x1A);
        return Contrast(white, background) >= Contrast(black, background) ? white : black;
    }

    /// <summary>
    /// The WCAG contrast ratio between two colors, from 1 for the same color to 21 for black and white.
    /// </summary>
    public static double Contrast(Color a, Color b)
    {
        static double Channel(byte value)
        {
            var c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        static double Luminance(Color c) => (0.2126 * Channel(c.R)) + (0.7152 * Channel(c.G)) + (0.0722 * Channel(c.B));

        var (lighter, darker) = (Math.Max(Luminance(a), Luminance(b)), Math.Min(Luminance(a), Luminance(b)));
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Applies this look to the given settings; the clock updates immediately.
    /// </summary>
    public void Apply(Settings settings)
    {
        settings.FontFamily = FontFamily;
        settings.FontWeight = FontWeight;
        settings.FontStyle = FontStyle;
        settings.TextColor = TextColor;
        settings.TextOpacity = 1;
        settings.OuterColor = OuterColor;
        settings.BackgroundEnabled = BackgroundEnabled;
        settings.BackgroundOpacity = BackgroundOpacity;
        settings.BackgroundCornerRadius = BackgroundCornerRadius;
        settings.OutlineThickness = OutlineThickness;

        // A leftover background image would keep covering the theme's colors.
        settings.BackgroundImagePath = string.Empty;
    }

    /// <summary>
    /// The factory look: neutral text matching the Windows light or dark theme, like a fresh install.
    /// </summary>
    private static ClockTheme CreateSystemTheme()
    {
        if (!SystemThemeService.TryGetThemeDefaults(out var textColor, out var outerColor))
        {
            // Match the hardcoded defaults in Settings when the system theme can't be read.
            textColor = Color.FromRgb(33, 33, 33);
            outerColor = Color.FromRgb(247, 247, 247);
        }

        return new ClockTheme(Loc.Get("ThemeSystem"), "Consolas", "Normal", textColor, outerColor,
            backgroundEnabled: true, backgroundOpacity: 0.9, backgroundCornerRadius: 1, outlineThickness: 0.2);
    }
}
