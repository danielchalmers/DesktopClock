using System;
using System.ComponentModel;
using System.Windows.Input;
using DesktopClock.Properties;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Humanizer;

namespace DesktopClock;

public class MainViewModel : ViewModelBase
{
    private readonly SystemClockTimer _systemClockTimer;
    private TimeZoneInfo _timeZone;

    public MainViewModel()
    {
        _timeZone = SettingsHelper.GetTimeZone();

        Settings.Default.PropertyChanged += Settings_PropertyChanged;

        _systemClockTimer = new();
        _systemClockTimer.SecondChanged += SystemClockTimer_SecondChanged;
        _systemClockTimer.Start();
    }

    /// <summary>
    /// The current date and time in the selected time zone or countdown as a formatted string.
    /// </summary>
    public string CurrentTimeOrCountdownString =>
        IsCountdown ?
        Settings.Default.CountdownTo.Humanize(CurrentTimeInSelectedTimeZone) :
        CurrentTimeInSelectedTimeZone.ToString(Settings.Default.Format);

    /// <summary>
    /// Sets app theme to parameter's value.
    /// </summary>
    public ICommand SetThemeCommand { get; } = new RelayCommand<Theme>((t) => Settings.Default.Theme = t);

    /// <summary>
    /// Sets format string in settings to parameter's string.
    /// </summary>
    public ICommand SetFormatCommand { get; } = new RelayCommand<string>((f) => Settings.Default.Format = f);

    /// <summary>
    /// Sets time zone ID in settings to parameter's time zone ID.
    /// </summary>
    public ICommand SetTimeZoneCommand { get; } = new RelayCommand<TimeZoneInfo>(SettingsHelper.SetTimeZone);

    /// <summary>
    /// The current date and time in the selected time zone.
    /// </summary>
    private DateTimeOffset CurrentTimeInSelectedTimeZone => TimeZoneInfo.ConvertTime(DateTimeOffset.Now, _timeZone);

    /// <summary>
    /// Should the clock be a countdown?
    /// </summary>
    private bool IsCountdown => Settings.Default.CountdownTo > DateTimeOffset.MinValue;

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.Default.TimeZone):
                _timeZone = SettingsHelper.GetTimeZone();
                UpdateTimeString();
                break;

            case nameof(Settings.Default.Format):
                UpdateTimeString();
                break;
        }
    }

    private void SystemClockTimer_SecondChanged(object sender, EventArgs e)
    {
        UpdateTimeString();
    }

    private void UpdateTimeString() => RaisePropertyChanged(nameof(CurrentTimeOrCountdownString));
}