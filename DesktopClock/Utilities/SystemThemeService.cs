using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace DesktopClock.Utilities;

public static class SystemThemeService
{
    private static readonly Color DefaultAccentColor = Color.FromRgb(0, 120, 215);
    private static readonly Color LightThemeOuterColor = Color.FromRgb(247, 247, 247);
    private static readonly Color DarkThemeOuterColor = Color.FromRgb(32, 32, 32);
    private const string PersonalizeKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    public static bool TryGetThemeDefaults(out Color textColor, out Color outerColor)
    {
        textColor = default;
        outerColor = default;

        if (!TryGetSystemThemeIsLight(out var isLightTheme))
            return false;

        textColor = GetSystemAccentColor();
        outerColor = isLightTheme ? LightThemeOuterColor : DarkThemeOuterColor;
        return true;
    }

    private static bool TryGetSystemThemeIsLight(out bool isLightTheme)
    {
        var value = Registry.GetValue(PersonalizeKeyPath, AppsUseLightThemeValueName, null);
        if (value is null)
        {
            isLightTheme = true;
            return false;
        }

        isLightTheme = value switch
        {
            int intValue => intValue > 0,
            byte byteValue => byteValue > 0,
            _ => true,
        };

        return true;
    }

    private static Color GetSystemAccentColor()
    {
        var accent = SystemParameters.WindowGlassColor;
        if (accent.A == 0)
            return DefaultAccentColor;

        return Color.FromArgb(255, accent.R, accent.G, accent.B);
    }
}
