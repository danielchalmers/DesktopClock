using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace DesktopClock.Tests;

[UseUICulture("en-US")]
public class SystemClockTimerTests
{
    private static readonly DateTimeOffset Target = new(2024, 7, 18, 12, 5, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("12:00:01.013", "12:00:01")] // Tick a little late
    [InlineData("12:00:01.600", "12:00:01")] // Tick very late doesn't jump ahead
    [InlineData("12:00:00.991", "12:00:01")] // Tick a little early counts as the second it was due for
    [InlineData("12:00:00.850", "12:00:00")] // Too early to be the next second
    [InlineData("12:00:01.000", "12:00:01")] // On time
    public void GetSecond_CountsATickAsTheSecondItWasDueFor(string now, string expected)
    {
        var result = SystemClockTimer.GetSecond(At(now));

        Assert.Equal(At(expected), result);
    }

    [Theory]
    [InlineData("12:00:01.013", "04:59")] // Late tick used to read 04:58
    [InlineData("12:00:00.991", "04:59")] // Early tick for the same second
    [InlineData("12:04:59.013", "00:01")] // Last second used to read 00:00
    [InlineData("12:05:00.013", "00:00")]
    public void Countdown_ReadsWholeSecondsWhereverTheTickLands(string now, string expected)
    {
        var second = SystemClockTimer.GetSecond(At(now));

        var result = TimeStringFormatter.Format(second, second.DateTime, TimeZoneInfo.Utc, Target.DateTime, null, "{mm}:{ss}", CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("12:04:00.013", "in a minute")] // Used to read "in 59 seconds"
    [InlineData("12:04:59.013", "in a second")] // Used to read "now" a second early
    [InlineData("12:05:00.013", "now")]
    public void DefaultCountdown_ReadsWholeSecondsWhereverTheTickLands(string now, string expected)
    {
        var second = SystemClockTimer.GetSecond(At(now));

        var result = TimeStringFormatter.Format(second, second.DateTime, TimeZoneInfo.Utc, Target.DateTime, null, null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Countdown_StandardFormatHasNoFractionOfASecond()
    {
        // Issue #106: the "t" format showed values like 00:04:58.9870000.
        var second = SystemClockTimer.GetSecond(At("12:00:01.013"));

        var result = TimeStringFormatter.Format(second, second.DateTime, TimeZoneInfo.Utc, Target.DateTime, null, "t", CultureInfo.InvariantCulture);

        Assert.Equal("00:04:59", result);
    }

    [Theory]
    [InlineData("12:03:59.013", false)] // A minute and a second left, which used to chime a second early
    [InlineData("12:04:00.013", true)] // A minute left
    [InlineData("12:04:59.013", false)] // One second left, which used to chime as well as the finish
    [InlineData("12:05:00.013", true)] // Finished
    public void CountdownChime_PlaysOnTheExactSecond(string now, bool expected)
    {
        var second = SystemClockTimer.GetSecond(At(now));

        var result = DateTimeUtil.IsNowOrCountdownOnInterval(second.DateTime, Target.DateTime, TimeSpan.FromMinutes(1));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("13:00:00.010", true)] // On the hour
    [InlineData("13:00:00.991", false)] // Early tick for the next second used to chime the hour again
    public void ClockChime_PlaysOncePerInterval(string now, bool expected)
    {
        var second = SystemClockTimer.GetSecond(At(now));

        var result = DateTimeUtil.IsNowOrCountdownOnInterval(second.DateTime, default, TimeSpan.FromHours(1));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void SecondChanged_RaisesEachWholeSecondOnce()
    {
        var seconds = new List<DateTimeOffset>();
        using var done = new ManualResetEventSlim();
        using var timer = new SystemClockTimer();
        timer.SecondChanged += (_, second) =>
        {
            lock (seconds)
            {
                seconds.Add(second);
                if (seconds.Count == 3)
                    done.Set();
            }
        };

        timer.Start();
        Assert.True(done.Wait(TimeSpan.FromSeconds(10)));
        timer.Stop();

        lock (seconds)
        {
            for (var i = 0; i < seconds.Count; i++)
            {
                Assert.Equal(0, seconds[i].Ticks % TimeSpan.TicksPerSecond);
                Assert.True((DateTimeOffset.Now - seconds[i]).TotalSeconds < 10);

                if (i > 0)
                    Assert.True(seconds[i] > seconds[i - 1]);
            }
        }
    }

    private static DateTimeOffset At(string time) =>
        new(Target.Date + TimeSpan.ParseExact(time.Length > 8 ? time : time + ".000", @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture), TimeSpan.Zero);
}
