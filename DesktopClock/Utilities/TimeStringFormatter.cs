using System;

namespace DesktopClock;

public static class TimeStringFormatter
{
    public static string Format(
        DateTimeOffset now,
        TimeZoneInfo timeZone,
        DateTime countdownTo,
        string format,
        string countdownFormat,
        IFormatProvider formatProvider)
    {
        if (countdownTo == default)
            return Tokenizer.FormatWithTokenizerOrFallBack(TimeZoneInfo.ConvertTime(now, timeZone), format, formatProvider);

        if (string.IsNullOrWhiteSpace(countdownFormat))
            return RelativeTimeFormatter.Format(countdownTo, now.DateTime);

        var countdown = DateTimeUtil.TimeUntil(countdownTo, now, TimeZoneInfo.Local);
        return Tokenizer.FormatWithTokenizerOrFallBack(countdown, countdownFormat, formatProvider);
    }
}
