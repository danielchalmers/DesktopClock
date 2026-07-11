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

            // Custom TimeSpan formats drop the sign, so format the magnitude and add it back to mark an elapsed target.
            var isElapsed = countdown < TimeSpan.Zero;
            result = Tokenizer.FormatWithTokenizerOrFallBack(isElapsed ? countdown.Negate() : countdown, countdownFormat, formatProvider);

            if (isElapsed && result != Tokenizer.FormatErrorMessage)
                result = "-" + result;
        }

        return result;
    }
}
