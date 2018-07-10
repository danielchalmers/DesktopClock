using System;
using Clock_Widget.Properties;

namespace Clock_Widget
{
    public static class SettingsHelper
    {
        /// <summary>
        /// The time zone selected in settings, or local by default.
        /// </summary>
        public static TimeZoneInfo GetTimeZone() =>
            TimeZoneUtil.TryGetTimeZoneById(Settings.Default.TimeZone, out var timeZoneInfo) ? timeZoneInfo : TimeZoneInfo.Local;

        /// <summary>
        /// Select a time zone to use.
        /// </summary>
        public static void SetTimeZone(TimeZoneInfo timeZone) =>
            Settings.Default.TimeZone = timeZone.Id;
    }
}