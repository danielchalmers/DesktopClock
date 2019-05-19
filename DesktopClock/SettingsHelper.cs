using System;
using DesktopClock.Properties;

namespace DesktopClock
{
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
    }
}