using System;
using DesktopClock.Properties;

namespace DesktopClock
{
    public static class SettingsHelper
    {
        /// <summary>
        /// The time zone selected in settings, or local by default.
        /// </summary>
        public static TimeZoneInfo GetTimeZone() =>
            DateTimeUtil.TryGetTimeZoneById(Settings.Default.TimeZone, out var timeZoneInfo) ? timeZoneInfo : TimeZoneInfo.Local;

        /// <summary>
        /// Select a time zone to use.
        /// </summary>
        public static void SetTimeZone(TimeZoneInfo timeZone) =>
            Settings.Default.TimeZone = timeZone.Id;
    }
}