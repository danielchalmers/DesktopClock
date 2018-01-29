using System;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace Clock_Widget
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SystemClockTimer SystemClockTimer = new SystemClockTimer();

        public MainViewModel()
        {
            SystemClockTimer.Tick += SystemClockTimer_Tick;
        }

        public string CurrentTimeString => TimeZoneHelper.GetCurrentTimeInSelectedTimeZone().ToString();

        /// <summary>
        /// Set time zone ID in settings to parameter's time zone ID.
        /// </summary>
        public ICommand SetTimeZoneCommand { get; } = new RelayCommand<TimeZoneInfo>(TimeZoneHelper.SetSelectedTimeZone);

        private void SystemClockTimer_Tick(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(CurrentTimeString));
        }
    }
}