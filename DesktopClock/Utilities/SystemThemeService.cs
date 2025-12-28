using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace DesktopClock.Utilities;

public static class SystemThemeService
{
    private static readonly Color DefaultAccentColor = Color.FromRgb(0, 120, 215);
    private static readonly Color LightThemeOuterColor = Color.FromRgb(247, 247, 247);
    private static readonly Color DarkThemeOuterColor = Color.FromRgb(32, 32, 32);
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string DwmKeyPath = @"Software\Microsoft\Windows\DWM";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";
    private const string SystemUsesLightThemeValueName = "SystemUsesLightTheme";
    private const string ColorizationColorValueName = "ColorizationColor";

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
        if (TryGetRegistryDword(PersonalizeKeyPath, AppsUseLightThemeValueName, out var appsUseLightTheme))
        {
            isLightTheme = appsUseLightTheme > 0;
            return true;
        }

        if (TryGetRegistryDword(PersonalizeKeyPath, SystemUsesLightThemeValueName, out var systemUsesLightTheme))
        {
            isLightTheme = systemUsesLightTheme > 0;
            return true;
        }

        isLightTheme = true;
        return false;
    }

    private static Color GetSystemAccentColor()
    {
        if (TryGetAccentColorFromDwm(out var accent) || TryGetAccentColorFromRegistry(out accent))
            return accent;

        accent = SystemParameters.WindowGlassColor;
        if (accent.A != 0)
            return Color.FromArgb(255, accent.R, accent.G, accent.B);

        return DefaultAccentColor;
    }

    private static bool TryGetRegistryDword(string keyPath, string valueName, out int value)
    {
        value = default;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            if (key?.GetValue(valueName) is int intValue)
            {
                value = intValue;
                return true;
            }

            if (key?.GetValue(valueName) is byte byteValue)
            {
                value = byteValue;
                return true;
            }

            if (key?.GetValue(valueName) is uint uintValue)
            {
                value = unchecked((int)uintValue);
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool TryGetAccentColorFromDwm(out Color color)
    {
        color = default;

        try
        {
            var result = DwmGetColorizationColor(out var colorizationColor, out _);
            if (result != 0)
                return false;

            color = ColorFromArgbUint(colorizationColor);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetAccentColorFromRegistry(out Color color)
    {
        color = default;

        if (!TryGetRegistryDword(DwmKeyPath, ColorizationColorValueName, out var colorization))
            return false;

        color = ColorFromArgbUint(unchecked((uint)colorization));
        return true;
    }

    private static Color ColorFromArgbUint(uint color)
    {
        var r = (byte)((color >> 16) & 0xFF);
        var g = (byte)((color >> 8) & 0xFF);
        var b = (byte)(color & 0xFF);
        return Color.FromArgb(255, r, g, b);
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetColorizationColor(out uint colorizationColor, out bool opaqueBlend);
}
