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
            result = countdownTo.Humanize(utcDate: false, dateToCompareAgainst: nowDateTime);
        }
        else
        {
            var countdown = countdownTo - nowDateTime;
            result = Tokenizer.FormatWithTokenizerOrFallBack(countdown, countdownFormat, formatProvider);
        }

        return result;
    }
}
