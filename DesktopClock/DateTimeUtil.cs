using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopClock
{
    public static class DateTimeUtil
    {
        /// <summary>
        /// List of standard date and time format strings.
        /// </summary>
        /// <remarks>
        /// Not including `U` specifier as it's not compatible with <see cref="DateTimeOffset"/>.
        /// </remarks>
        public static IReadOnlyList<string> StandardDateTimeFormats { get; } = new[] { "d", "D", "f", "F", "g", "G", "M", "O", "R", "s", "t", "T", "u", "Y" };

        /// <summary>
        /// Dictionary of common date time formats and their example string.
        /// </summary>
        public static IReadOnlyDictionary<string, string> StandardDateTimeFormatsAndExamples { get; } =
            StandardDateTimeFormats.
            ToDictionary(f => f, f => DateTime.Now.ToString(f));

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