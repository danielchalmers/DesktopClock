using System;
using System.Threading;

namespace DesktopClock;

/// <summary>
/// A timer that syncs with the system clock.
/// </summary>
public sealed class SystemClockTimer : IDisposable
{
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
    /// <inheritdoc/>
    /// </summary>
    public void Dispose() => _timer.Dispose();

    /// <summary>
    /// Schedules the timer to start on the next second that elapses.
    /// </summary>
    public void Start() => ScheduleTickForNextSecond();

    /// <summary>
    /// Immediately stops the timer.
    /// </summary>
    public void Stop() => _timer.Change(Timeout.Infinite, Timeout.Infinite);

    private void OnTick()
    {
        // The tick usually lands a few milliseconds into the second, so drop that fraction to give countdowns and chimes whole seconds.
        var second = DateTimeOffset.Now.TruncateToSecond();

        ScheduleTickForNextSecond();

        // A tick that lands early, before the second changes, finds the second that was already raised, so skip it rather than chime or shift pixels twice. The next tick is then only a few milliseconds away.
        lock (_timer)
        {
            if (second == _lastSecond)
                return;

            _lastSecond = second;
        }

        SecondChanged?.Invoke(this, second);
    }

    /// <summary>
    /// Starts the timer and schedules the tick for the next second on the system clock.
    /// </summary>
    private void ScheduleTickForNextSecond() =>
        _timer.Change(MillisecondsUntilNextSecond, Timeout.Infinite);
}
