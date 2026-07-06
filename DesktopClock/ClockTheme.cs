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
        return new[]
        {
            CreateSystemTheme(),
            new ClockTheme("Accent", "Segoe UI", "SemiBold",
                Color.FromRgb(0xFF, 0xFF, 0xFF), SystemThemeService.GetSystemAccentColor(),
                backgroundEnabled: true, backgroundOpacity: 1, backgroundCornerRadius: 8, outlineThickness: 0.2),
            new ClockTheme("Smoke", "Segoe UI", "Normal",
                Color.FromRgb(0xF2, 0xF2, 0xF2), Color.FromRgb(0x0A, 0x0A, 0x10),
                backgroundEnabled: true, backgroundOpacity: 0.55, backgroundCornerRadius: 10, outlineThickness: 0.2),
            new ClockTheme("Terminal", "Consolas", "Bold",
                Color.FromRgb(0x00, 0xE5, 0xFF), Color.FromRgb(0x0C, 0x0C, 0x0C),
                backgroundEnabled: true, backgroundOpacity: 0.85, backgroundCornerRadius: 6, outlineThickness: 0.2),
            new ClockTheme("Midnight", "Segoe UI", "SemiBold",
                Color.FromRgb(0x4C, 0xC2, 0xFF), Color.FromRgb(0x1B, 0x1B, 0x1B),
                backgroundEnabled: true, backgroundOpacity: 0.95, backgroundCornerRadius: 8, outlineThickness: 0.2),
            new ClockTheme("Paper", "Georgia", "Normal",
                Color.FromRgb(0x1A, 0x1A, 0x1A), Color.FromRgb(0xFA, 0xF9, 0xF6),
                backgroundEnabled: true, backgroundOpacity: 0.97, backgroundCornerRadius: 8, outlineThickness: 0.2),
            new ClockTheme("Minimal", "Segoe UI", "Light",
                Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0x00, 0x00, 0x00),
                backgroundEnabled: false, backgroundOpacity: 0, backgroundCornerRadius: 1, outlineThickness: 0),
            new ClockTheme("Chalk", "Segoe UI", "SemiBold",
                Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0x00, 0x00, 0x00),
                backgroundEnabled: false, backgroundOpacity: 1, backgroundCornerRadius: 1, outlineThickness: 1.5),
        };
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
    }

    /// <summary>
    /// The factory look: accent-colored text seeded from the Windows theme, like a fresh install.
    /// </summary>
    private static ClockTheme CreateSystemTheme()
    {
        if (!SystemThemeService.TryGetThemeDefaults(out var textColor, out var outerColor))
        {
            // Match the hardcoded defaults in Settings when the system theme can't be read.
            textColor = Color.FromRgb(33, 33, 33);
            outerColor = Color.FromRgb(247, 247, 247);
        }

        return new ClockTheme("System", "Consolas", "Normal", textColor, outerColor,
            backgroundEnabled: true, backgroundOpacity: 0.9, backgroundCornerRadius: 1, outlineThickness: 0.2);
    }
}
