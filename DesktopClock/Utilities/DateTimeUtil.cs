using System;

namespace DesktopClock;

public static class DateTimeUtil
{
    /// <summary>
    /// Converts a DateTime to a DateTimeOffset, without risking any onerous exceptions
    /// the framework quite unfortunately throws within the DateTimeOffset constructor,
    /// such as they do when the source DateTime's Kind is not set to UTC. The best and
    /// most performant way around this, which we do herein, is to simply construct the
    /// new DateTimeOffset with the overload that excepts Ticks. Also, we will simply
    /// return <see cref="DateTimeOffset.MinValue"/> if the source DateTime was
    /// <see cref="DateTime.MinValue"/>.
    /// </summary>
    /// <remarks>https://stackoverflow.com/a/48511228</remarks>
    /// <param name="dt">Source DateTime.</param>
    /// <param name="offset">Offset</param>
    public static DateTimeOffset ToDateTimeOffset(this DateTime dt, TimeSpan offset)
    {
        // adding negative offset to a min-datetime will throw, this is a 
        // sufficient catch. Note however that a DateTime of just a few hours can still throw
        if (dt == DateTime.MinValue)
            return DateTimeOffset.MinValue;

        return new DateTimeOffset(dt.Ticks, offset);
    }

    public static bool AreEqualExcludingMilliseconds(this DateTimeOffset dt1, DateTimeOffset dt2)
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
}
