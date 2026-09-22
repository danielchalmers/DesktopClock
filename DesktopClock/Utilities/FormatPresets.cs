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
    /// The clock's default format: short weekday, short date, and time with seconds, such as "Tue, Sep 22, 1:01:22 PM" in the US or "Di., 22. Sep., 13:01:22" in Germany.
    /// </summary>
    public static string DefaultClockFormat(DateTimeFormatInfo format) =>
        $"{{ddd}}, {Token(ShortMonthDayPattern(format))}, {Token(format.LongTimePattern)}";

    /// <summary>
    /// One-click clock formats for common scenarios.
    /// </summary>
    public static IReadOnlyList<(string Name, string Format)> ForClock(DateTimeFormatInfo format)
    {
        var time = Token(format.ShortTimePattern);
        var monthDay = Token(format.MonthDayPattern);
        var presets = new List<(string Name, string Format)> { (Loc.Get("ClockPresetTime"), time) };

        // A 24-hour preset only adds something where the regional time is 12-hour.
        if (Uses12HourClock(format))
            presets.Add((Loc.Get("ClockPresetTime24"), "{HH:mm}"));

        presets.Add((Loc.Get("ClockPresetTimeSeconds"), Token(format.LongTimePattern)));
        presets.Add((Loc.Get("ClockPresetDayTime"), $"{{ddd}}, {time}"));
        presets.Add((Loc.Get("ClockPresetDateTime"), $"{{ddd}}, {Token(ShortMonthDayPattern(format))}, {time}"));
        presets.Add((Loc.Get("ClockPresetFullDateTime"), $"{{dddd}}, {monthDay}, {time}"));
        presets.Add((Loc.Get("ClockPresetDateOnly"), $"{{dddd}}, {monthDay}"));
        presets.Add((Loc.Get("ClockPresetSortable"), "{yyyy-MM-dd} {HH:mm}"));
        presets.Add((Loc.Get("ClockPresetIsoWeek"), "{weekYear}-W{week}"));
        return presets;
    }

    /// <summary>
    /// The regional month and day with an abbreviated month, such as "MMM d" in the US or "d. MMM" in Germany.
    /// </summary>
    public static string ShortMonthDayPattern(DateTimeFormatInfo format) => format.MonthDayPattern.Replace("MMMM", "MMM");

    /// <summary>
    /// Whether the regional time format uses a 12-hour clock. Quoted text is skipped so a literal like the "h" in Canadian French "HH 'h' mm" doesn't count.
    /// </summary>
    public static bool Uses12HourClock(DateTimeFormatInfo format) => Regex.Replace(format.ShortTimePattern, "'[^']*'", "").Contains("h");

    private static string Token(string pattern) => "{" + pattern + "}";
}
