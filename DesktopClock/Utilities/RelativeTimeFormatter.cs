using System;
using System.Globalization;

namespace DesktopClock;

/// <summary>
/// Describes how far a moment is from now in plain English, such as "3 hours from now" or "yesterday".
/// </summary>
public static class RelativeTimeFormatter
{
    /// <summary>
    /// Returns a phrase for the distance between <paramref name="target"/> and <paramref name="now"/>,
    /// using the single largest unit that fits.
    /// </summary>
    public static string Format(DateTime target, DateTime now)
    {
        var isFuture = target > now;
        var span = new TimeSpan(Math.Abs(now.Ticks - target.Ticks));

        if (span.TotalMilliseconds < 500)
            return "now";

        if (span.TotalSeconds < 60)
            return Phrase(span.Seconds, "one second", "{0} seconds", isFuture);

        if (span.TotalSeconds < 120)
            return Phrase(1, "a minute", "{0} minutes", isFuture);

        if (span.TotalMinutes < 60)
            return Phrase(span.Minutes, "a minute", "{0} minutes", isFuture);

        if (span.TotalMinutes < 90)
            return Phrase(1, "an hour", "{0} hours", isFuture);

        if (span.TotalHours < 24)
            return Phrase(span.Hours, "an hour", "{0} hours", isFuture);

        // Within two days, count calendar dates so late tonight to early the day after tomorrow reads as "2 days".
        if (span.TotalHours < 48)
            return Days(Math.Abs((target.Date - now.Date).Days), isFuture);

        if (span.TotalDays < 28)
            return Days(span.Days, isFuture);

        // Just under a month reads as one month only when it lands on the same date next (or last) month.
        if (span.TotalDays < 30)
        {
            var sameDateNextMonth = now.Date.AddMonths(isFuture ? 1 : -1) == target.Date;
            return sameDateNextMonth ? Phrase(1, "one month", "{0} months", isFuture) : Days(span.Days, isFuture);
        }

        if (span.TotalDays < 345)
            return Phrase((int)Math.Floor(span.TotalDays / 29.5), "one month", "{0} months", isFuture);

        var years = Math.Max(1, (int)Math.Floor(span.TotalDays / 365));
        return Phrase(years, "one year", "{0} years", isFuture);
    }

    private static string Days(int days, bool isFuture) => days switch
    {
        0 => "now",
        1 => isFuture ? "tomorrow" : "yesterday",
        _ => Phrase(days, "", "{0} days", isFuture),
    };

    private static string Phrase(int count, string single, string plural, bool isFuture)
    {
        if (count == 0)
            return "now";

        var amount = count == 1 ? single : string.Format(CultureInfo.InvariantCulture, plural, count);
        return isFuture ? amount + " from now" : amount + " ago";
    }
}
