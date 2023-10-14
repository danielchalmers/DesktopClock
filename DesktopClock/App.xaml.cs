using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using DesktopClock.Properties;
using Microsoft.Win32;

namespace DesktopClock;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static FileInfo MainFileInfo = new(Process.GetCurrentProcess().MainModule.FileName);

    // https://www.materialui.co/colors - A100, A700.
    public static IReadOnlyList<Theme> Themes { get; } = new Theme[]
    {
        new("Light Text", "#F5F5F5", "#212121"),
        new("Dark Text", "#212121", "#F5F5F5"),
        new("Red", "#D50000", "#FF8A80"),
        new("Pink", "#C51162", "#FF80AB"),
        new("Purple", "#AA00FF", "#EA80FC"),
        new("Blue", "#2962FF", "#82B1FF"),
        new("Cyan", "#00B8D4", "#84FFFF"),
        new("Green", "#00C853", "#B9F6CA"),
        new("Orange", "#FF6D00", "#FFD180"),
    };

    /// <summary>
    /// Gets the time zone selected in settings, or local by default.
    /// </summary>
    public static TimeZoneInfo GetTimeZone() =>
        DateTimeUtil.TryGetTimeZoneById(Settings.Default.TimeZone, out var timeZoneInfo) ? timeZoneInfo : TimeZoneInfo.Local;

    /// <summary>
    /// Sets the time zone to be used.
    /// </summary>
    public static void SetTimeZone(TimeZoneInfo timeZone) =>
        Settings.Default.TimeZone = timeZone.Id;

    /// <summary>
    /// Sets or deletes a value in the registry which enables the current executable to run on system startup.
    /// </summary>
    public static void SetRunOnStartup(bool runOnStartup)
    {
        var keyName = GetSha256Hash(MainFileInfo.FullName);
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        if (runOnStartup)
            key?.SetValue(keyName, MainFileInfo.FullName); // Use the path as the name so we can handle multiple exes, but hash it or Windows won't like it.
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