using System;
using System.Threading;

namespace DesktopClock
{
    /// <summary>
    /// A timer that ticks when the second changes on the system clock.
    /// </summary>
    public sealed class SystemClockTimer : IDisposable
    {
        private Timer _timer;

        public SystemClockTimer()
        {
            _timer = new Timer((o) => OnTick());

            ScheduleTickForNextSecond();
        }

        /// <summary>
        /// Occurs when the next second of the system clock passes.
        /// </summary>
        public event EventHandler<EventArgs> Tick;

        /// <summary>
        /// Number of milliseconds until the next second passes.
        /// </summary>
        private int MillisecondsUntilNextSecond => 1000 - DateTimeOffset.Now.Millisecond;

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="SystemClockTimer" />.
        /// </summary>
        public void Dispose()
        {
            _timer.Dispose();
        }

        private void OnTick()
        {
            ScheduleTickForNextSecond();

            Tick?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Start the timer so tick will elapse with the next second.
        /// </summary>
        private void ScheduleTickForNextSecond() =>
            _timer.Change(MillisecondsUntilNextSecond, Timeout.Infinite);
    }
}