using System;
using System.Globalization;

namespace DesktopClock;

/// <summary>
/// Describes how far a moment is from now in plain words, such as "in 3 hours" or "yesterday", in the UI language.
/// </summary>
public static class RelativeTimeFormatter
{
    /// <summary>
    /// Returns a phrase for the distance between <paramref name="target"/> and <paramref name="now"/>, using the single largest unit that fits. Uses the current UI language unless <paramref name="culture"/> is given.
    /// </summary>
    public static string Format(DateTime target, DateTime now, CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentUICulture;
        var isFuture = target > now;
        var span = new TimeSpan(Math.Abs(now.Ticks - target.Ticks));

        if (span.TotalMilliseconds < 500)
            return Loc.Get("RelativeNow", culture);

        if (span.TotalSeconds < 60)
            return Phrase(span.Seconds, "RelativeSeconds", isFuture, culture);

        if (span.TotalSeconds < 120)
            return Phrase(1, "RelativeMinutes", isFuture, culture);

        if (span.TotalMinutes < 60)
            return Phrase(span.Minutes, "RelativeMinutes", isFuture, culture);

        if (span.TotalMinutes < 90)
            return Phrase(1, "RelativeHours", isFuture, culture);

        if (span.TotalHours < 24)
            return Phrase(span.Hours, "RelativeHours", isFuture, culture);

        // Within two days, count calendar dates so late tonight to early the day after tomorrow reads as "2 days".
        if (span.TotalHours < 48)
            return Days(Math.Abs((target.Date - now.Date).Days), isFuture, culture);

        if (span.TotalDays < 28)
            return Days(span.Days, isFuture, culture);

        // Just under a month reads as one month only when it lands on the same date next (or last) month.
        if (span.TotalDays < 30)
        {
            var sameDateNextMonth = now.Date.AddMonths(isFuture ? 1 : -1) == target.Date;
            return sameDateNextMonth ? Phrase(1, "RelativeMonths", isFuture, culture) : Days(span.Days, isFuture, culture);
        }

        if (span.TotalDays < 345)
            return Phrase((int)Math.Floor(span.TotalDays / 29.5), "RelativeMonths", isFuture, culture);

        var years = Math.Max(1, (int)Math.Floor(span.TotalDays / 365));
        return Phrase(years, "RelativeYears", isFuture, culture);
    }

    private static string Days(int days, bool isFuture, CultureInfo culture) => days switch
    {
        0 => Loc.Get("RelativeNow", culture),
        1 => Loc.Get(isFuture ? "RelativeTomorrow" : "RelativeYesterday", culture),
        _ => Phrase(days, "RelativeDays", isFuture, culture),
    };

    private static string Phrase(int count, string unitKey, bool isFuture, CultureInfo culture)
    {
        if (count == 0)
            return Loc.Get("RelativeNow", culture);

        // Each unit lists its plural forms separated by "|", such as "a minute|{0} minutes".
        var forms = Loc.Get(unitKey, culture).Split('|');
        var amount = string.Format(culture, forms[Math.Min(PluralFormIndex(count, culture), forms.Length - 1)], count);
        return string.Format(culture, Loc.Get(isFuture ? "RelativeFuture" : "RelativePast", culture), amount);
    }

    /// <summary>
    /// Picks which plural form to use following the CLDR rules for the translated languages: one|few|many for Polish, Russian, and Ukrainian, and one|other for everything else.
    /// </summary>
    private static int PluralFormIndex(int count, CultureInfo culture)
    {
        var mod10 = count % 10;
        var mod100 = count % 100;
        var isFew = mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14;

        return culture.TwoLetterISOLanguageName switch
        {
            "ru" or "uk" => (mod10 == 1 && mod100 != 11) ? 0 : isFew ? 1 : 2,
            "pl" => count == 1 ? 0 : isFew ? 1 : 2,
            "fr" or "pt" => count <= 1 ? 0 : 1,
            _ => count == 1 ? 0 : 1,
        };
    }
}
