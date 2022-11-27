using System;
using DesktopClock.Properties;
using Microsoft.Win32;

namespace DesktopClock;

public static class SettingsHelper
{
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

    public static void SetRunOnStartup(bool runOnStartup)
    {
        var exePath = App.ResourceAssembly.Location;
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