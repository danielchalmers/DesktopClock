using System;
using System.Collections.Generic;

namespace Clock_Widget
{
    public static class TimeZoneUtil
    {
        public static IReadOnlyCollection<TimeZoneInfo> TimeZones { get; } = TimeZoneInfo.GetSystemTimeZones();

        public static bool TryGetTimeZoneById(string timeZoneId, out TimeZoneInfo timeZoneInfo)
        {
            try
            {
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                timeZoneInfo = null;
                return false;
            }
        }
    }
}