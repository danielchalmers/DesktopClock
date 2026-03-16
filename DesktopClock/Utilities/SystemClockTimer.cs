using System;
using System.Threading;

namespace DesktopClock;

/// <summary>
/// A timer that syncs with the system clock.
/// </summary>
public sealed class SystemClockTimer : IDisposable
{
    private readonly Timer _timer;

    public SystemClockTimer()
    {
        _timer = new Timer(_ => OnTick());
    }

    /// <summary>
    /// Occurs after the second changes on the system clock.
    /// </summary>
    public event EventHandler SecondChanged;

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
        ScheduleTickForNextSecond();

        SecondChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Starts the timer and schedules the tick for the next second on the system clock.
    /// </summary>
    private void ScheduleTickForNextSecond() =>
        _timer.Change(MillisecondsUntilNextSecond, Timeout.Infinite);
}