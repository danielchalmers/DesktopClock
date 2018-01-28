using System;
using GalaSoft.MvvmLight;

namespace Clock_Widget
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SystemClockTimer SystemClockTimer = new SystemClockTimer();
        private string _currentTimeString = GetCurrentTimeString();

        public MainViewModel()
        {
            SystemClockTimer.Tick += SystemClockTimer_Tick;
        }

        public string CurrentTimeString
        {
            get => _currentTimeString;
            set => Set(ref _currentTimeString, value);
        }

        public static string GetCurrentTimeString() => DateTime.Now.ToString();

        private void SystemClockTimer_Tick(object sender, EventArgs e)
        {
            CurrentTimeString = GetCurrentTimeString();
        }
    }
}