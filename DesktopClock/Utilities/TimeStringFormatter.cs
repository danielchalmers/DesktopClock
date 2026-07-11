using System;
using System.Globalization;
using Humanizer;

namespace DesktopClock;

public static class TimeStringFormatter
{
    public static string Format(
        DateTimeOffset now,
        DateTime nowDateTime,
        TimeZoneInfo timeZone,
        DateTime countdownTo,
        string format,
        string countdownFormat,
        IFormatProvider formatProvider)
    {
        var timeInSelectedZone = TimeZoneInfo.ConvertTime(now, timeZone);

        string result;
        if (countdownTo == default)
        {
            result = Tokenizer.FormatWithTokenizerOrFallBack(timeInSelectedZone, format, formatProvider);
        }
        else if (string.IsNullOrWhiteSpace(countdownFormat))
        {
            // Both values are local wall-clock times, but their Kind is usually Unspecified, which Humanizer
            // shifts by the UTC offset when it converts the comparison date to local time. Pinning the Kind
            // makes that conversion a no-op so the countdown reads correctly in every time zone.
            var localNow = DateTime.SpecifyKind(nowDateTime, DateTimeKind.Local);
            result = countdownTo.Humanize(utcDate: false, dateToCompareAgainst: localNow);
        }
        else
        {
            var countdown = countdownTo - nowDateTime;
            result = Tokenizer.FormatWithTokenizerOrFallBack(countdown, countdownFormat, formatProvider);
        }

        return result;
    }
}
