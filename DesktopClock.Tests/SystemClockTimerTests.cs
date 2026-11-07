using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace DesktopClock.Tests;

[UseUICulture("en-US")]
public class SystemClockTimerTests
{
    private static readonly DateTimeOffset Target = new(2024, 7, 18, 12, 5, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("12:00:01.013", "04:59")] // Late tick used to read 04:58
    [InlineData("12:04:59.013", "00:01")] // Last second used to read 00:00
    [InlineData("12:05:00.013", "00:00")]
    public void Countdown_ReadsWholeSecondsWhereverTheTickLands(string now, string expected)
    {
        var second = TickAt(now);

        var result = TimeStringFormatter.Format(second, second.DateTime, TimeZoneInfo.Utc, Target.DateTime, null, "{mm}:{ss}", CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("12:04:00.013", "in a minute")] // Used to read "in 59 seconds"
    [InlineData("12:04:59.013", "in a second")] // Used to read "now" a second early
    [InlineData("12:05:00.013", "now")]
    public void DefaultCountdown_ReadsWholeSecondsWhereverTheTickLands(string now, string expected)
    {
        var second = TickAt(now);

        var result = TimeStringFormatter.Format(second, second.DateTime, TimeZoneInfo.Utc, Target.DateTime, null, null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Countdown_StandardFormatHasNoFractionOfASecond()
    {
        // Issue #106: the "t" format showed values like 00:04:58.9870000.
        var second = TickAt("12:00:01.013");

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
        var second = TickAt(now);

        var result = DateTimeUtil.IsNowOrCountdownOnInterval(second, second.AddSeconds(-1), Target.DateTime, TimeSpan.FromMinutes(1), TimeZoneInfo.Utc);

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

    [Fact]
    public void SecondChanged_SkipsATickForASecondAlreadyRaised()
    {
        // A tick that lands early, before the second changes, finds the second already raised, and used to chime or shift pixels again.
        var seconds = new List<DateTimeOffset>();
        using var timer = new SystemClockTimer();
        timer.SecondChanged += (_, second) =>
        {
            lock (seconds)
                seconds.Add(second);
        };
        var onTick = typeof(SystemClockTimer).GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance);

        // Start early in a second so both ticks land in it.
        while (DateTimeOffset.Now.Millisecond > 500)
            Thread.Sleep(10);

        onTick.Invoke(timer, null);
        onTick.Invoke(timer, null);
        timer.Stop();

        lock (seconds)
            Assert.Single(seconds);
    }

    /// <summary>
    /// Gets the second a tick raises when it lands at the given time on the target's day.
    /// </summary>
    private static DateTimeOffset TickAt(string time) =>
        new DateTimeOffset(Target.Date + TimeSpan.ParseExact(time, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture), TimeSpan.Zero).TruncateToSecond();
}
