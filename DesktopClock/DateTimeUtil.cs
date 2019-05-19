using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopClock
{
    public static class DateTimeUtil
    {
        /// <summary>
        /// Standard date and time formatting strings that are compatible with both <see cref="DateTime"/> and <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings
        /// </remarks>
        public static IReadOnlyList<string> StandardDateTimeFormats { get; } = new[] { "d", "D", "f", "F", "g", "G", "M", "O", "R", "s", "t", "T", "u", "Y" };

        /// <summary>
        /// Common date time formatting strings and an example string for each.
        /// </summary>
        public static IReadOnlyDictionary<string, string> StandardDateTimeFormatsAndExamples { get; } =
            StandardDateTimeFormats.ToDictionary(f => f, f => DateTimeOffset.Now.ToString(f));

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