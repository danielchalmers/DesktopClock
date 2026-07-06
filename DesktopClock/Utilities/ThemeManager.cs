using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace DesktopClock.Utilities;

/// <summary>
/// Applies the light or dark UI palette and keeps it in sync with the Windows theme and accent color.
/// </summary>
public static class ThemeManager
{
    private const string PaletteFolder = "Themes/";
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

    private static readonly Uri LightPaletteUri = new(PaletteFolder + "LightPalette.xaml", UriKind.Relative);
    private static readonly Uri DarkPaletteUri = new(PaletteFolder + "DarkPalette.xaml", UriKind.Relative);

    /// <summary>
    /// Whether the dark palette is currently applied.
    /// </summary>
    public static bool IsDarkTheme { get; private set; }

    /// <summary>
    /// Raised after the palette has been swapped for a new system theme.
    /// </summary>
    public static event EventHandler ThemeChanged;

    /// <summary>
    /// Applies the palette matching the current system theme and follows future changes.
    /// </summary>
    public static void Initialize()
    {
        Apply();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    /// <summary>
    /// Makes the window's title bar match the current theme (dark or light).
    /// </summary>
    public static void ApplyTitleBarTheme(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var useDark = IsDarkTheme ? 1 : 0;

        try
        {
            // Try the documented attribute first, then the pre-20H1 value for older Windows 10 builds.
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDark, sizeof(int));
        }
        catch
        {
            // DWM isn't available; keep the default title bar.
        }
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General)
            return;

        Application.Current?.Dispatcher.BeginInvoke(new Action(Apply));
    }

    private static void Apply()
    {
        var app = Application.Current;
        if (app == null)
            return;

        IsDarkTheme = !SystemThemeService.IsLightTheme();

        var paletteUri = IsDarkTheme ? DarkPaletteUri : LightPaletteUri;
        var palette = new ResourceDictionary { Source = paletteUri };

        var dictionaries = app.Resources.MergedDictionaries;
        var existingPalette = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("Palette.xaml") == true);

        if (existingPalette != null)
            dictionaries[dictionaries.IndexOf(existingPalette)] = palette;
        else
            dictionaries.Add(palette);

        app.Resources["AccentBrush"] = CreateAccentBrush();

        foreach (Window window in app.Windows)
            ApplyTitleBarTheme(window);

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Builds the accent brush from the system accent color, lightened in dark mode for contrast.
    /// </summary>
    private static SolidColorBrush CreateAccentBrush()
    {
        var accent = SystemThemeService.GetSystemAccentColor();

        if (IsDarkTheme)
            accent = Lighten(accent, 0.45);

        var brush = new SolidColorBrush(accent);
        brush.Freeze();
        return brush;
    }

    private static Color Lighten(Color color, double amount)
    {
        return Color.FromRgb(
            (byte)(color.R + (255 - color.R) * amount),
            (byte)(color.G + (255 - color.G) * amount),
            (byte)(color.B + (255 - color.B) * amount));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
