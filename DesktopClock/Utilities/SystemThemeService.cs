using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace DesktopClock.Utilities;

/// <summary>
/// Reads Windows theme and accent color values to seed default UI colors.
/// </summary>
public static class SystemThemeService
{
    // Fallbacks match the app's existing default colors.
    private static readonly Color DefaultAccentColor = Color.FromRgb(0, 120, 215);
    private static readonly Color LightThemeOuterColor = Color.FromRgb(247, 247, 247);
    private static readonly Color DarkThemeOuterColor = Color.FromRgb(32, 32, 32);

    // Theme and colorization values stored under HKCU.
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string DwmKeyPath = @"Software\Microsoft\Windows\DWM";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";
    private const string SystemUsesLightThemeValueName = "SystemUsesLightTheme";
    private const string ColorizationColorValueName = "ColorizationColor";

    /// <summary>
    /// Tries to build default text/outer colors from the system theme and accent color.
    /// </summary>
    /// <remarks>
    /// Returns <c>false</c> when the system theme cannot be read (older Windows or missing keys).
    /// </remarks>
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

    /// <summary>
    /// Reads the light/dark preference from the Windows personalize key.
    /// </summary>
    private static bool TryGetSystemThemeIsLight(out bool isLightTheme)
    {
        // AppsUseLightTheme is the primary indicator for Win10+ app theme.
        if (TryGetRegistryDword(PersonalizeKeyPath, AppsUseLightThemeValueName, out var appsUseLightTheme))
        {
            isLightTheme = appsUseLightTheme > 0;
            return true;
        }

        // SystemUsesLightTheme is a fallback when AppsUseLightTheme is absent.
        if (TryGetRegistryDword(PersonalizeKeyPath, SystemUsesLightThemeValueName, out var systemUsesLightTheme))
        {
            isLightTheme = systemUsesLightTheme > 0;
            return true;
        }

        // Unknown; caller treats this as "no system theme available".
        isLightTheme = true;
        return false;
    }

    /// <summary>
    /// Resolves the current accent color using DWM, registry, then system parameters.
    /// </summary>
    private static Color GetSystemAccentColor()
    {
        // Prefer DWM because it tracks the active colorization value.
        if (TryGetAccentColorFromDwm(out var accent) || TryGetAccentColorFromRegistry(out accent))
            return accent;

        // SystemParameters is a safe fallback when DWM/registry are unavailable.
        accent = SystemParameters.WindowGlassColor;
        if (accent.A != 0)
            return Color.FromArgb(255, accent.R, accent.G, accent.B);

        return DefaultAccentColor;
    }

    /// <summary>
    /// Reads a DWORD value from HKCU, if present.
    /// </summary>
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

    /// <summary>
    /// Uses DWM to fetch the current colorization color.
    /// </summary>
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

    /// <summary>
    /// Reads the colorization color from the DWM registry key.
    /// </summary>
    private static bool TryGetAccentColorFromRegistry(out Color color)
    {
        color = default;

        if (!TryGetRegistryDword(DwmKeyPath, ColorizationColorValueName, out var colorization))
            return false;

        color = ColorFromArgbUint(unchecked((uint)colorization));
        return true;
    }

    /// <summary>
    /// Converts a DWM colorization ARGB value into an opaque WPF color.
    /// </summary>
    private static Color ColorFromArgbUint(uint color)
    {
        var r = (byte)((color >> 16) & 0xFF);
        var g = (byte)((color >> 8) & 0xFF);
        var b = (byte)(color & 0xFF);
        return Color.FromArgb(255, r, g, b);
    }

    /// <summary>
    /// Win32 API for reading the current DWM colorization color.
    /// </summary>
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmGetColorizationColor(out uint colorizationColor, out bool opaqueBlend);
}
