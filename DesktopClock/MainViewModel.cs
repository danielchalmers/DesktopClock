using System;
using System.ComponentModel;
using System.Windows.Input;
using DesktopClock.Properties;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;

namespace DesktopClock
{
    public class MainViewModel : ViewModelBase
    {
        private readonly SystemClockTimer _systemClockTimer;
        private TimeZoneInfo _timeZone;

        public MainViewModel()
        {
            _systemClockTimer = new SystemClockTimer();
            _systemClockTimer.Tick += SystemClockTimer_Tick;

            _timeZone = SettingsHelper.GetTimeZone();

            Settings.Default.PropertyChanged += Settings_PropertyChanged;
        }

        /// <summary>
        /// The current time in the selected time zone.
        /// </summary>
        public DateTimeOffset CurrentTimeInSelectedTimeZone => TimeZoneInfo.ConvertTime(DateTimeOffset.Now, _timeZone);

        /// <summary>
        /// Set time zone ID in settings to parameter's time zone ID.
        /// </summary>
        public ICommand SetTimeZoneCommand { get; } = new RelayCommand<TimeZoneInfo>(SettingsHelper.SetTimeZone);

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.Default.TimeZone):
                    _timeZone = SettingsHelper.GetTimeZone();
                    RaisePropertyChanged(nameof(CurrentTimeInSelectedTimeZone));
                    break;
            }
        }

        private void SystemClockTimer_Tick(object sender, EventArgs e)
        {
            RaisePropertyChanged(nameof(CurrentTimeInSelectedTimeZone));
        }
    }
}