using System;
using System.Threading;

namespace DesktopClock;

/// <summary>
/// A timer that syncs with the system clock.
/// </summary>
public sealed class SystemClockTimer : IDisposable
{
    /// <summary>
    /// How close to the next second a tick can land and still count as that second, since timers can fire a few milliseconds before they're due.
    /// </summary>
    private static readonly TimeSpan EarlyTickTolerance = TimeSpan.FromMilliseconds(100);

    private readonly Timer _timer;
    private DateTimeOffset _lastSecond;

    public SystemClockTimer()
    {
        _timer = new Timer(_ => OnTick());
    }

    /// <summary>
    /// Occurs after the second changes on the system clock, with that second as a whole number of seconds.
    /// </summary>
    public event EventHandler<DateTimeOffset> SecondChanged;

    /// <summary>
    /// Number of milliseconds until the next second on the system clock.
    /// </summary>
    private int MillisecondsUntilNextSecond => 1000 - DateTimeOffset.Now.Millisecond;

    /// <summary>
    /// Gets the whole second the clock shows at the given time, which is the next second when it's only a few milliseconds away so an early tick shows the second it was meant for.
    /// </summary>
    public static DateTimeOffset GetSecond(DateTimeOffset now) => (now + EarlyTickTolerance).TruncateToSecond();

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void Dispose() => _timer.Dispose();

    /// <summary>
    /// Schedules the timer to start on the next second that elapses.
    /// </summary>
    public void Start() => _timer.Change(MillisecondsUntilNextSecond, Timeout.Infinite);

    /// <summary>
    /// Immediately stops the timer.
    /// </summary>
    public void Stop() => _timer.Change(Timeout.Infinite, Timeout.Infinite);

    private void OnTick()
    {
        var now = DateTimeOffset.Now;
        var second = GetSecond(now);

        // Schedule the start of the second after this one, which is over a second away when this tick landed early.
        _timer.Change((long)Math.Ceiling((second.AddSeconds(1) - now).TotalMilliseconds), Timeout.Infinite);

        // Skip a tick for a second that was already raised, so it doesn't chime or shift pixels twice.
        lock (_timer)
        {
            if (second == _lastSecond)
                return;

            _lastSecond = second;
        }

        SecondChanged?.Invoke(this, second);
    }
}
