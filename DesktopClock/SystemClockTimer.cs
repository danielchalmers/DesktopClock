using System;
using System.Threading;

namespace DesktopClock
{
    /// <summary>
    /// A timer, synced with the system clock.
    /// </summary>
    public sealed class SystemClockTimer : IDisposable
    {
        private readonly Timer _timer;

        public SystemClockTimer()
        {
            _timer = new Timer((_) => OnTick());
        }

        /// <summary>
        /// Occurs after the second of the system clock changes.
        /// </summary>
        public event EventHandler<EventArgs> SecondChanged;

        /// <summary>
        /// Number of milliseconds until the next second on the system clock.
        /// </summary>
        private int MillisecondsUntilNextSecond => 1000 - DateTimeOffset.Now.Millisecond;

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="SystemClockTimer" />.
        /// </summary>
        public void Dispose()
        {
            _timer.Dispose();
        }

        public void Start() => ScheduleTickForNextSecond();

        public void Stop() => _timer.Change(Timeout.Infinite, Timeout.Infinite);

        private void OnTick()
        {
            ScheduleTickForNextSecond();

            SecondChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Starts the timer and schedules the tick for the next second on the system clock.
        /// </summary>
        private void ScheduleTickForNextSecond() =>
            _timer.Change(MillisecondsUntilNextSecond, Timeout.Infinite);
    }
}