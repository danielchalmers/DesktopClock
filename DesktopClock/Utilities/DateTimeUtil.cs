using System;
using System.Globalization;

namespace DesktopClock;

public static class DateTimeUtil
{
    /// <summary>
    /// Gets the ISO 8601 week number (1-53) for the given date.
    /// Equivalent to <c>ISOWeek.GetWeekOfYear</c>, which isn't available on .NET Framework.
    /// </summary>
    public static int GetIsoWeekOfYear(this DateTime date)
    {
        // ISO weeks start on Monday and belong to the year containing their Thursday, so shift Monday through Wednesday forward to get counted in the correct week.
        if (date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Wednesday)
            date = date.AddDays(3);

        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    /// <summary>
    /// Gets the ISO 8601 week-numbering year for the given date, which differs from the calendar year for days near New Year.
    /// </summary>
    public static int GetIsoWeekYear(this DateTime date)
    {
        var dayOfWeek = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
        return date.AddDays(4 - dayOfWeek).Year;
    }

    public static bool EqualExcludingMillisecond(this DateTime dt1, DateTime dt2)
    {
        if (dt1.Year != dt2.Year)
            return false;

        if (dt1.Month != dt2.Month)
            return false;

        if (dt1.Day != dt2.Day)
            return false;

        if (dt1.Hour != dt2.Hour)
            return false;

        if (dt1.Minute != dt2.Minute)
            return false;

        if (dt1.Second != dt2.Second)
            return false;

        return true;
    }

    public static bool IsNowOrCountdownOnInterval(DateTime now, DateTime countdown, TimeSpan interval)
    {
        var time = countdown == default ? now.TimeOfDay : countdown - now;

        var isOnInterval = interval != default && (int)time.TotalSeconds % (int)interval.TotalSeconds == 0;

        var isCountdownReached = now.EqualExcludingMillisecond(countdown);

        return isOnInterval || isCountdownReached;
    }
}
