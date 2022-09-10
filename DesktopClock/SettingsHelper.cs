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
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        if (runOnStartup)
            key?.SetValue("DesktopClock", App.ResourceAssembly.Location);
        else
            key?.DeleteValue("DesktopClock", false);
    }
}