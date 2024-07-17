using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopClock.Properties;
using DesktopClock.Utilities;
using H.NotifyIcon;
using H.NotifyIcon.EfficiencyMode;
using Humanizer;
using WpfWindowPlacement;

namespace DesktopClock;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    private readonly SystemClockTimer _systemClockTimer;
    private TaskbarIcon _trayIcon;
    private TimeZoneInfo _timeZone;
    private SoundPlayer _soundPlayer;
    private PixelShifter _pixelShifter;

    /// <summary>
    /// The date and time to countdown to, or <c>null</c> if regular clock is desired.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _countdownTo;

    /// <summary>
    /// The current date and time in the selected time zone, or countdown as a formatted string.
    /// </summary>
    [ObservableProperty]
    private string _currentTimeOrCountdownString;

    /// <summary>
    /// The amount of margin applied in order to shift the clock's pixels and help prevent burn-in.
    /// </summary>
    [ObservableProperty]
    private Thickness _pixelShift;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _timeZone = Settings.Default.GetTimeZoneInfo();
        UpdateCountdownEnabled();

        Settings.Default.PropertyChanged += (s, e) => Dispatcher.Invoke(() => Settings_PropertyChanged(s, e));

        // Not done through binding due to what's explained in the comment in WindowUtil.HideFromScreen().
        ShowInTaskbar = Settings.Default.ShowInTaskbar;

        // Restore the structure of the last state using the display text.
        CurrentTimeOrCountdownString = Settings.Default.LastDisplay;

        _systemClockTimer = new();
        _systemClockTimer.SecondChanged += SystemClockTimer_SecondChanged;

        // The context menu is shared between right-clicking the window and the tray icon.
        ContextMenu = Resources["MainContextMenu"] as ContextMenu;

        ConfigureTrayIcon(!Settings.Default.ShowInTaskbar, true);

        UpdateSoundPlayerEnabled();
    }

    /// <summary>
    /// Copies the current time string to the clipboard.
    /// </summary>
    [RelayCommand]
    public void CopyToClipboard() => Clipboard.SetText(CurrentTimeOrCountdownString);

    /// <summary>
    /// Minimizes the window.
    /// </summary>
    [RelayCommand]
    public void HideForNow()
    {
        if (!Settings.Default.TipsShown.HasFlag(TeachingTips.HideForNow))
        {
            MessageBox.Show(this, "Clock will be minimized and can be opened again from the taskbar (or system tray if enabled).",
                Title, MessageBoxButton.OK, MessageBoxImage.Information);

            Settings.Default.TipsShown |= TeachingTips.HideForNow;
        }

        this.HideFromScreen();
    }

    /// <summary>
    /// Sets the app's theme to the given value.
    /// </summary>
    [RelayCommand]
    public void SetTheme(Theme theme) => Settings.Default.Theme = theme;

    /// <summary>
    /// Opens a new settings window or activates the existing one.
    /// </summary>
    [RelayCommand]
    public void OpenSettings() => App.ShowSingletonWindow<SettingsWindow>(this);

    /// <summary>
    /// Asks the user then creates a new clock executable and starts it.
    /// </summary>
    [RelayCommand]
    public void NewClock()
    {
        var result = MessageBox.Show(this,
            "This will copy the executable and start it with new settings.\n\n" +
            "Continue?",
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

        if (result != MessageBoxResult.OK)
            return;

        var newExePath = Path.Combine(App.MainFileInfo.DirectoryName, App.MainFileInfo.GetFileAtNextIndex().Name);

        // Copy and start the new clock.
        File.Copy(App.MainFileInfo.FullName, newExePath);
        Process.Start(newExePath);
    }

    /// <summary>
    /// Closes the app.
    /// </summary>
    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }

    private void ConfigureTrayIcon(bool showIcon, bool firstLaunch)
    {
        if (showIcon)
        {
            if (_trayIcon == null)
            {
                // Construct the tray from the resources defined.
                _trayIcon = Resources["TrayIcon"] as TaskbarIcon;
                _trayIcon.ContextMenu = Resources["MainContextMenu"] as ContextMenu;
                _trayIcon.ContextMenu.DataContext = this;
                _trayIcon.ForceCreate(enablesEfficiencyMode: false);
                _trayIcon.TrayLeftMouseDoubleClick += (_, _) =>
                {
                    WindowState = WindowState.Normal;
                    Activate();
                };
            }

            // Show a notice if the icon was moved during runtime, but not at the start because the user will already expect it.
            if (!firstLaunch)
                _trayIcon.ShowNotification("Hidden from taskbar", "Icon was moved to the tray");
        }
        else
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
        }
    }

    /// <summary>
    /// Handles property changes in settings and updates the corresponding properties in the UI.
    /// </summary>
    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.Default.TimeZone):
                _timeZone = Settings.Default.GetTimeZoneInfo();
                UpdateTimeString();
                break;

            case nameof(Settings.Default.Format):
            case nameof(Settings.Default.CountdownFormat):
                UpdateTimeString();
                break;

            case nameof(Settings.Default.ShowInTaskbar):
                ShowInTaskbar = Settings.Default.ShowInTaskbar;
                ConfigureTrayIcon(!Settings.Default.ShowInTaskbar, false);
                break;

            case nameof(Settings.Default.CountdownTo):
                UpdateCountdownEnabled();
                UpdateTimeString();
                break;

            case nameof(Settings.Default.WavFilePath):
            case nameof(Settings.Default.WavFileInterval):
                UpdateSoundPlayerEnabled();
                break;
        }
    }

    /// <summary>
    /// Handles the event when the system clock timer signals a second change.
    /// </summary>
    private void SystemClockTimer_SecondChanged(object sender, EventArgs e)
    {
        UpdateTimeString();

        TryPlaySound();

        if (Settings.Default.BurnInMitigation)
        {
            _pixelShifter ??= new();
            Dispatcher.Invoke(() =>
            {
                Left += _pixelShifter.ShiftX();
                Top += _pixelShifter.ShiftY();
            });
        }
    }

    /// <summary>
    /// Updates the countdown enabled state based on the settings.
    /// </summary>
    private void UpdateCountdownEnabled()
    {
        if (Settings.Default.CountdownTo == default)
        {
            CountdownTo = null;
            return;
        }

        CountdownTo = Settings.Default.CountdownTo.ToDateTimeOffset(_timeZone.BaseUtcOffset);
    }

    /// <summary>
    /// Initializes the sound player for the specified file if enabled; otherwise, sets it to <c>null</c>.
    /// </summary>
    private void UpdateSoundPlayerEnabled()
    {
        var soundPlayerEnabled =
            !string.IsNullOrWhiteSpace(Settings.Default.WavFilePath) &&
            Settings.Default.WavFileInterval != default &&
            File.Exists(Settings.Default.WavFilePath);

        _soundPlayer = soundPlayerEnabled ? new(Settings.Default.WavFilePath) : null;
    }

    /// <summary>
    /// Tries to play a sound based on the settings if it hits the specified interval and the file exists.
    /// </summary>
    private void TryPlaySound()
    {
        if (_soundPlayer == null)
            return;

        // Whether we hit the interval specified in settings, which is calculated differently in countdown mode and not.
        var isOnInterval = CountdownTo == null ?
            (int)DateTimeOffset.Now.TimeOfDay.TotalSeconds % (int)Settings.Default.WavFileInterval.TotalSeconds == 0 :
            (int)(CountdownTo.Value - DateTimeOffset.Now).TotalSeconds % (int)Settings.Default.WavFileInterval.TotalSeconds == 0;

        if (!isOnInterval)
            return;

        try
        {
            _soundPlayer.Play();
        }
        catch
        {
            // Ignore errors because we don't want a sound issue to crash the app.
        }
    }

    private void UpdateTimeString()
    {
        string GetTimeString()
        {
            var timeInSelectedZone = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, _timeZone);

            if (CountdownTo == null)
            {
                return Tokenizer.FormatWithTokenizerOrFallBack(timeInSelectedZone, Settings.Default.Format, CultureInfo.DefaultThreadCurrentCulture);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Settings.Default.CountdownFormat))
                    return CountdownTo.Humanize(timeInSelectedZone);

                return Tokenizer.FormatWithTokenizerOrFallBack(Settings.Default.CountdownTo - timeInSelectedZone, Settings.Default.CountdownFormat, CultureInfo.DefaultThreadCurrentCulture);
            }
        }

        CurrentTimeOrCountdownString = GetTimeString();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Drag the window to move it.
        if (e.ChangedButton == MouseButton.Left && Settings.Default.DragToMove)
        {
            // Pause time updates to maintain placement.
            _systemClockTimer.Stop();

            DragMove();
            UpdateTimeString();

            _systemClockTimer.Start();
        }
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        CopyToClipboard();
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Resize the window when scrolling if the Ctrl key is pressed.
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Amount of scroll that occurred and whether it was positive or negative.
            var steps = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
            Settings.Default.ScaleHeight(steps);
        }
    }

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        this.SetPlacement(Settings.Default.Placement);

        UpdateTimeString();
        _systemClockTimer.Start();

        // Now that everything's been initially rendered and laid out, we can start listening for changes to the size to keep the window right-aligned.
        SizeChanged += Window_SizeChanged;

        if (Settings.Default.StartHidden)
        {
            _trayIcon?.ShowNotification("Started hidden", "Icon is in the tray");
            this.HideFromScreen();
        }

        // Show the window now that it's finished loading.
        // This was mainly done to stop the StartHidden option from flashing the window briefly.
        Opacity = 1;
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        // Make sure the user is aware that their changes will not be saved.
        if (!Settings.CanBeSaved)
        {
            MessageBox.Show(this,
                "Settings can't be saved because of an access error.\n\n" +
                $"Make sure {Title} is in a folder that doesn't require admin privileges, " +
                "and that you got it from the original source: https://github.com/danielchalmers/DesktopClock.\n\n" +
                "If the problem still persists, feel free to create a new Issue at the above link with as many details as possible.",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Save the last text and the placement to preserve dimensions and position of the clock.
        Settings.Default.LastDisplay = CurrentTimeOrCountdownString;
        Settings.Default.Placement = this.GetPlacement();

        // Stop the file watcher before saving.
        Settings.Default.Dispose();

        if (Settings.CanBeSaved)
            Settings.Default.Save();

        App.SetRunOnStartup(Settings.Default.RunOnStartup);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Adjust the window position for right-alignment.
        if (e.WidthChanged && Settings.Default.RightAligned)
        {
            var widthChange = e.NewSize.Width - e.PreviousSize.Width;
            Left -= widthChange;
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // Save resources while minimized.
            _systemClockTimer.Stop();
            EfficiencyModeUtilities.SetEfficiencyMode(true);
        }
        else
        {
            // Run like normal without withholding resources.
            UpdateTimeString();
            _systemClockTimer.Start();
            EfficiencyModeUtilities.SetEfficiencyMode(false);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.OemMinus:
                    Settings.Default.ScaleHeight(-1);
                    break;
                case Key.OemPlus:
                    Settings.Default.ScaleHeight(1);
                    break;
            }
        }
    }
}
