using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DesktopClock;

/// <summary>
/// Clock formats built from the Windows regional format, so times are 24-hour where people expect it and dates follow the local day and month order.
/// </summary>
public static class FormatPresets
{
    /// <summary>
    /// The clock's default format: short weekday, short date, and time with seconds, such as "Tue, Sep 22, 1:01:22 PM" in the US, "Di., 22. Sep., 13:01:22" in Germany, or "9月22日(火) 13:01:22" in Japan.
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
        var tokens = new List<(string Name, string Token)>
        {
            (Loc.Get("TokenWeekday"), "{ddd}"),
            (Loc.Get("TokenWeekdayFull"), "{dddd}"),
            (Loc.Get("TokenDay"), "{dd}"),
            (Loc.Get("TokenMonth"), "{MMM}"),
            (Loc.Get("TokenMonthFull"), "{MMMM}"),
            (Loc.Get("TokenYear"), "{yyyy}"),
            (Loc.Get("TokenTime"), Token(format.ShortTimePattern)),
        };

        if (Uses12HourClock(format))
            tokens.Add((Loc.Get("TokenTime24"), "{HH:mm}"));

        tokens.Add((Loc.Get("TokenSeconds"), "{ss}"));
        tokens.Add((Loc.Get("TokenWeekNumber"), "{week}"));
        tokens.Add((Loc.Get("TokenUtcOffset"), "{zzz}"));
        return tokens;
    }

    /// <summary>
    /// A .NET format for a short weekday, date, and time, such as "ddd, MMM d h:mm tt" in the US or "M月d日(ddd) H:mm" in Japan.
    /// </summary>
    public static string ShortDateTimePattern(CultureInfo culture) =>
        WeekdayAndDate(culture, "ddd", ShortMonthDayPattern(culture.DateTimeFormat)) + " " + culture.DateTimeFormat.ShortTimePattern;

    /// <summary>
    /// The regional month and day with an abbreviated month, such as "MMM d" in the US or "d. MMM" in Germany.
    /// </summary>
    public static string ShortMonthDayPattern(DateTimeFormatInfo format) => format.MonthDayPattern.Replace("MMMM", "MMM");

    /// <summary>
    /// Whether the regional time format uses a 12-hour clock. Quoted text is skipped so a literal like the "h" in Canadian French "HH 'h' mm" doesn't count.
    /// </summary>
    public static bool Uses12HourClock(DateTimeFormatInfo format) => Regex.Replace(format.ShortTimePattern, "'[^']*'", "").Contains("h");

    /// <summary>
    /// Puts the weekday and date in the order the language writes them: "Tue, Sep 22" in most languages, but date first in Chinese, Japanese, and Korean, with a short weekday in parentheses where they use them ("9月22日(火)", "9월 22일 (화)").
    /// </summary>
    private static string WeekdayAndDate(CultureInfo culture, string weekday, string date)
    {
        var isShortWeekday = !weekday.Contains("dddd");
        return culture.TwoLetterISOLanguageName switch
        {
            "ja" when isShortWeekday => $"{date}({weekday})",
            "ko" when isShortWeekday => $"{date} ({weekday})",
            "ja" or "ko" or "zh" => $"{date} {weekday}",
            _ => $"{weekday}, {date}",
        };
    }

    /// <summary>
    /// Adds the time after a date: separated by a comma in most languages, and by a space in Chinese, Japanese, and Korean.
    /// </summary>
    private static string WithTime(CultureInfo culture, string date, string time) => culture.TwoLetterISOLanguageName is "ja" or "ko" or "zh" ? $"{date} {time}" : $"{date}, {time}";

    private static string Token(string pattern) => "{" + pattern + "}";
}
