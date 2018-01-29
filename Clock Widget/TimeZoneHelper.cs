using System;
using System.Collections.ObjectModel;
using Clock_Widget.Properties;

namespace Clock_Widget
{
    public static class TimeZoneHelper
    {
        public static ReadOnlyCollection<TimeZoneInfo> TimeZones { get; } = TimeZoneInfo.GetSystemTimeZones();

        public static DateTime GetCurrentTimeInSelectedTimeZone() => TimeZoneInfo.ConvertTime(DateTime.UtcNow, GetSelectedTimeZone());

        public static void SetSelectedTimeZone(TimeZoneInfo timeZone)
        {
            Settings.Default.TimeZone = timeZone.Id;
        }

        public static TimeZoneInfo GetSelectedTimeZone()
        {
            if (string.IsNullOrEmpty(Settings.Default.TimeZone))
                return TimeZoneInfo.Local;

            // Try to find a time zone that matches the ID in settings.
            // If a time zone couldn't be found, reset the ID in settings.
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(Settings.Default.TimeZone);
            }
            catch
            {
                Settings.Default.TimeZone = null;
                return TimeZoneInfo.Local;
            }
        }
    }
}