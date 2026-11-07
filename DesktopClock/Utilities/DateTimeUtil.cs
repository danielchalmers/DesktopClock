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

    /// <summary>
    /// Drops the fraction of a second, keeping the offset.
    /// </summary>
    public static DateTimeOffset TruncateToSecond(this DateTimeOffset dateTimeOffset) =>
        new(dateTimeOffset.Ticks - (dateTimeOffset.Ticks % TimeSpan.TicksPerSecond), dateTimeOffset.Offset);

    /// <summary>
    /// The real time left until a target on the wall clock of the given time zone: the wall-clock difference, corrected by an hour when a daylight saving change falls in between.
    /// </summary>
    public static TimeSpan TimeUntil(DateTime target, DateTimeOffset now, TimeZoneInfo zone) =>
        target - now.DateTime - (zone.GetUtcOffset(target) - zone.GetUtcOffset(now));

    /// <summary>
    /// Whether a sound is due: on the interval, or when the tick after <paramref name="previous"/> reaches the countdown.
    /// </summary>
    public static bool IsNowOrCountdownOnInterval(DateTimeOffset now, DateTimeOffset previous, DateTime countdown, TimeSpan interval, TimeZoneInfo countdownZone)
    {
        var time = countdown == default ? now.TimeOfDay : TimeUntil(countdown, now, countdownZone);

        // Work in whole seconds as a long so a sub-second interval doesn't truncate to a zero divisor and a far-future countdown doesn't overflow int.
        var intervalSeconds = (long)interval.TotalSeconds;
        var isOnInterval = intervalSeconds > 0 && (long)time.TotalSeconds % intervalSeconds == 0;

        // Reached when this tick passes the target, since sleep, a drag or a late tick can skip its exact second.
        var isCountdownReached = countdown != default && previous != default &&
            TimeUntil(countdown, previous, countdownZone) > TimeSpan.Zero && time <= TimeSpan.Zero;

        return isOnInterval || isCountdownReached;
    }
}
