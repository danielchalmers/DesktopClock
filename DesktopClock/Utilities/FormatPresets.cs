using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DesktopClock;

/// <summary>
/// Clock formats built from the Windows regional format, so times are 24-hour where people expect it and dates follow the local day and month order.
/// </summary>
public static class FormatPresets
{
    /// <summary>
    /// The clock's default format: short weekday, short date, and time with seconds, such as "Tue, Sep 22, 1:01:22 PM" in the US, "Di, 22. Sep, 13:01:22" in Germany, "mar. 22 sept. 13:01:22" in France, or "9月22日(火) 13:01:22" in Japan.
    /// </summary>
    public static string DefaultClockFormat(CultureInfo culture)
    {
        var format = culture.DateTimeFormat;
        return WithTime(culture, WeekdayAndDate(culture, "{ddd}", Token(ShortMonthDayPattern(format))), Token(format.LongTimePattern));
    }

    /// <summary>
    /// One-click clock formats for common scenarios.
    /// </summary>
    public static IReadOnlyList<(string Name, string Format)> ForClock(CultureInfo culture)
    {
        var format = culture.DateTimeFormat;
        var time = Token(format.ShortTimePattern);
        var monthDay = Token(format.MonthDayPattern);
        var presets = new List<(string Name, string Format)> { (Loc.Get("ClockPresetTime"), time) };

        // A 24-hour preset only adds something where the regional time is 12-hour.
        if (Uses12HourClock(format))
            presets.Add((Loc.Get("ClockPresetTime24"), "{HH:mm}"));

        presets.Add((Loc.Get("ClockPresetTimeSeconds"), Token(format.LongTimePattern)));
        presets.Add((Loc.Get("ClockPresetDayTime"), WithTime(culture, "{ddd}", time)));
        presets.Add((Loc.Get("ClockPresetDateTime"), WithTime(culture, WeekdayAndDate(culture, "{ddd}", Token(ShortMonthDayPattern(format))), time)));
        presets.Add((Loc.Get("ClockPresetFullDateTime"), WithTime(culture, WeekdayAndDate(culture, "{dddd}", monthDay), time)));
        presets.Add((Loc.Get("ClockPresetDateOnly"), WeekdayAndDate(culture, "{dddd}", monthDay)));
        presets.Add((Loc.Get("ClockPresetSortable"), "{yyyy-MM-dd} {HH:mm}"));
        presets.Add((Loc.Get("ClockPresetIsoWeek"), "{weekYear}-W{week}"));
        return presets;
    }

    /// <summary>
    /// Building blocks for the clock format that insert a token at the cursor.
    /// </summary>
    public static IReadOnlyList<(string Name, string Token)> ClockTokens(CultureInfo culture)
    {
        var format = culture.DateTimeFormat;

        // Chinese, Japanese, and Korean write a unit after each number (2026年10月5日), and their short month names are bare numbers, so the blocks include the unit.
        var units = culture.TwoLetterISOLanguageName switch
        {
            "ja" or "zh" => ("年", "月", "日"),
            "ko" => ("년", "월", "일"),
            _ => default((string Year, string Month, string Day)?),
        };

        var tokens = new List<(string Name, string Token)>
        {
            (Loc.Get("TokenWeekday"), "{ddd}"),
            (Loc.Get("TokenWeekdayFull"), "{dddd}"),
            (Loc.Get("TokenDay"), units is { } u1 ? $"{{d{u1.Day}}}" : "{dd}"),
            (Loc.Get("TokenMonth"), units is { } u2 ? $"{{M{u2.Month}}}" : "{MMM}"),
        };

        // Japanese and Korean full month names are the same as the short ones (10月), so only Chinese, with its written-out months (十月), keeps both.
        if (culture.TwoLetterISOLanguageName is not ("ja" or "ko"))
            tokens.Add((Loc.Get("TokenMonthFull"), "{MMMM}"));

        tokens.Add((Loc.Get("TokenYear"), units is { } u3 ? $"{{yyyy{u3.Year}}}" : "{yyyy}"));
        tokens.Add((Loc.Get("TokenTime"), Token(format.ShortTimePattern)));

        if (Uses12HourClock(format))
            tokens.Add((Loc.Get("TokenTime24"), "{HH:mm}"));

        tokens.Add((Loc.Get("TokenSeconds"), "{ss}"));
        tokens.Add((Loc.Get("TokenWeekNumber"), "{week}"));
        tokens.Add((Loc.Get("TokenUtcOffset"), "{zzz}"));
        return tokens;
    }

    /// <summary>
    /// A .NET format for a short weekday, date, and time, such as "ddd, MMM d, h:mm tt" in the US or "M月d日(ddd) H:mm" in Japan.
    /// </summary>
    public static string ShortDateTimePattern(CultureInfo culture) =>
        WithTime(culture, WeekdayAndDate(culture, "ddd", ShortMonthDayPattern(culture.DateTimeFormat)), culture.DateTimeFormat.ShortTimePattern);

    /// <summary>
    /// The regional month and day with an abbreviated month, such as "MMM d" in the US or "d. MMM" in Germany.
    /// </summary>
    public static string ShortMonthDayPattern(DateTimeFormatInfo format)
    {
        var pattern = format.MonthDayPattern;
        if (!pattern.Contains("MMMM"))
            return pattern;

        // Where short month names carry the number (Vietnamese "Thg10"), write the day and month as numbers instead, in the region's short date order.
        var shortNames = format.AbbreviatedMonthNames.Take(12).ToList();
        if (shortNames.Any(name => name.Any(char.IsDigit)))
            return Regex.Replace(format.ShortDatePattern, "[^dM]*y+[^dM]*", "");

        // .NET only puts full month names in the grammatical case a date needs, so where the short names change too (Ukrainian "Жов" becomes "жов" after a day), keep the full name.
        static string Letters(string name) => name.Replace(".", "");
        if (!shortNames.Select(Letters).SequenceEqual(format.AbbreviatedMonthGenitiveNames.Take(12).Select(Letters), StringComparer.Ordinal))
            return pattern;

        return pattern.Replace("MMMM", "MMM");
    }

    /// <summary>
    /// Whether the regional time format uses a 12-hour clock. Quoted text is skipped so a literal like the "h" in Canadian French "HH 'h' mm" doesn't count.
    /// </summary>
    public static bool Uses12HourClock(DateTimeFormatInfo format) => Regex.Replace(format.ShortTimePattern, "'[^']*'", "").Contains("h");

    /// <summary>
    /// Puts the weekday and date in the order and with the punctuation the region uses: "Tue, Sep 22", "mar. 22 sept." in French, "22 Eyl Sal" in Turkish, and date first in Chinese, Japanese, and Korean, with a short weekday in parentheses where they use them ("9月22日(火)", "9월 22일 (화)").
    /// </summary>
    private static string WeekdayAndDate(CultureInfo culture, string weekday, string date)
    {
        var isShortWeekday = !weekday.Contains("dddd");
        switch (culture.TwoLetterISOLanguageName)
        {
            case "ja" when isShortWeekday:
                return $"{date}({weekday})";
            case "ko" when isShortWeekday:
                return $"{date} ({weekday})";
            case "ja" or "ko" or "zh":
                return $"{date} {weekday}";
        }

        var (weekdayFirst, separator) = WeekdayStyle(culture);
        return weekdayFirst ? $"{weekday}{separator}{date}" : $"{date}{separator}{weekday}";
    }

    /// <summary>
    /// Adds the time after a date: with a comma where the region puts one after the weekday ("Tue, Sep 22, 1:01 PM"), otherwise with a space ("mar. 22 sept. 13:01").
    /// </summary>
    private static string WithTime(CultureInfo culture, string date, string time)
    {
        var usesComma = culture.TwoLetterISOLanguageName is not ("ja" or "ko" or "zh") && WeekdayStyle(culture).Separator.Trim() == ",";
        return usesComma ? $"{date}, {time}" : $"{date} {time}";
    }

    /// <summary>
    /// Reads how the region writes a weekday with a date from its long date pattern, or its language's when the region leaves the weekday out (like Canadian French): whether the weekday comes first, and what separates them.
    /// </summary>
    private static (bool WeekdayFirst, string Separator) WeekdayStyle(CultureInfo culture)
    {
        static bool IsDatePart(char c) => c is 'd' or 'M' or 'y' or '\'';

        for (var current = culture; !Equals(current, CultureInfo.InvariantCulture); current = current.Parent)
        {
            var pattern = current.DateTimeFormat.LongDatePattern;
            var weekday = pattern.IndexOf("dddd", StringComparison.Ordinal);
            if (weekday < 0)
                continue;

            var separator = weekday == 0
                ? new string(pattern.Skip(4).TakeWhile(c => !IsDatePart(c)).ToArray())
                : new string(pattern.Take(weekday).Reverse().TakeWhile(c => !IsDatePart(c)).Reverse().ToArray());
            return (weekday == 0, separator.Length == 0 ? " " : separator);
        }

        return (true, ", ");
    }

    private static string Token(string pattern) => "{" + pattern + "}";
}
