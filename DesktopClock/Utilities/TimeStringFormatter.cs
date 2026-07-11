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
            // Humanizer shifts Unspecified times by the UTC offset when localizing, so pin the Kind to make that conversion a no-op.
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
