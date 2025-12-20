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
        TextTransform textTransform,
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

        return ApplyTransform(result, textTransform, formatProvider);
    }

    private static string ApplyTransform(string value, TextTransform textTransform, IFormatProvider formatProvider)
    {
        if (textTransform == TextTransform.None)
            return value;

        var culture = formatProvider as CultureInfo;

        return textTransform switch
        {
            TextTransform.Uppercase => culture == null ? value.ToUpper() : value.ToUpper(culture),
            TextTransform.Lowercase => culture == null ? value.ToLower() : value.ToLower(culture),
            _ => value,
        };
    }
}
