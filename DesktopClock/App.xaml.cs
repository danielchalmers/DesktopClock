using System;
using System.Collections.Generic;
using System.Windows;
using DesktopClock.Properties;
using Microsoft.Win32;

namespace DesktopClock;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // https://www.materialui.co/colors - A100, A700.
    public static IReadOnlyList<Theme> Themes { get; } = new[]
    {
        new Theme("Light Text", "#F5F5F5", "#212121"),
        new Theme("Dark Text", "#212121", "#F5F5F5"),
        new Theme("Red", "#D50000", "#FF8A80"),
        new Theme("Pink", "#C51162", "#FF80AB"),
        new Theme("Purple", "#AA00FF", "#EA80FC"),
        new Theme("Blue", "#2962FF", "#82B1FF"),
        new Theme("Cyan", "#00B8D4", "#84FFFF"),
        new Theme("Green", "#00C853", "#B9F6CA"),
        new Theme("Orange", "#FF6D00", "#FFD180"),
    };

    /// <summary>
    /// Gets the time zone selected in settings, or local by default.
    /// </summary>
    public static TimeZoneInfo GetTimeZone() =>
        DateTimeUtil.TryGetTimeZoneById(Settings.Default.TimeZone, out var timeZoneInfo) ? timeZoneInfo : TimeZoneInfo.Local;

    /// <summary>
    /// Selects a time zone to use.
    /// </summary>
    public static void SetTimeZone(TimeZoneInfo timeZone) =>
        Settings.Default.TimeZone = timeZone.Id;

    /// <summary>
    /// Sets a value in the registry determining whether the current executable should run on system startup.
    /// </summary>
    /// <param name="runOnStartup"></param>
    public static void SetRunOnStartup(bool runOnStartup)
    {
        var exePath = ResourceAssembly.Location;
        var keyName = GetSha256Hash(exePath);
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        if (runOnStartup)
            key?.SetValue(keyName, exePath); // Use the path as the name so we can handle multiple exes, but hash it or Windows won't like it.
        else
            key?.DeleteValue(keyName, false);
    }

    internal static string GetSha256Hash(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        using var sha = new System.Security.Cryptography.SHA256Managed();
        var textData = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(textData);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}