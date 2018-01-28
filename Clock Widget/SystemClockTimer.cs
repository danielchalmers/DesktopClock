using System;
using System.Threading;

namespace Clock_Widget
{
    /// <summary>
    /// A timer that ticks when the second changes on the system clock.
    /// </summary>
    public class SystemClockTimer : IDisposable
    {
        private Timer _timer;

        public SystemClockTimer()
        {
            _timer = new Timer(Timer_Callback);
            SyncAndStart();
        }

        private int GetMillisecondsUntilNextSecond => 1000 - DateTime.Now.Millisecond;

        public void Dispose() => _timer.Dispose();

        private void SyncAndStart() => _timer.Change(GetMillisecondsUntilNextSecond, Timeout.Infinite);

        private void Timer_Callback(object state)
        {
            SyncAndStart();

            Tick?.Invoke(this, new EventArgs());
        }

        public delegate void TickHandler(object sender, EventArgs e);

        public event TickHandler Tick;
    }
}