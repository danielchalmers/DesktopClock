using System;

namespace DesktopClock;

public static class DateTimeUtil
{
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
