using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopClock.Tests;

public class SystemClockTimerTests
{
    private readonly SystemClockTimer _timer;

    public SystemClockTimerTests()
    {
        _timer?.Dispose();
        _timer = new SystemClockTimer();
    }

    [Theory]
    [InlineData(3)]
    public async Task ShouldTickEverySecondAccurately(int seconds)
    {
        // Ensure the timer is started at an unclean time to test accuracy.
        await Task.Delay(1000 - DateTimeOffset.Now.Millisecond + 234);

        Assert.NotInRange(DateTimeOffset.Now.Millisecond, 0, 100);

        var tickTimes = new List<DateTimeOffset>();

        _timer.SecondChanged += (sender, args) =>
        {
            tickTimes.Add(DateTimeOffset.Now);
        };

        _timer.Start();

        await Task.Delay(TimeSpan.FromSeconds(seconds));

        _timer.Stop();

        Assert.Equal(seconds, tickTimes.Count);

        // Check that each tick is close to the exact second.
        foreach (var tickTime in tickTimes)
        {
            Assert.InRange(tickTime.Millisecond, 0, 100);
        }
    }
}
