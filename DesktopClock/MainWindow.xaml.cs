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
    private readonly PropertyChangedEventHandler _settingsPropertyChanged;

    /// <summary>
    /// The current date and time in the selected time zone, or the formatted countdown text.
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

        _timeZone = Settings.Default.TimeZoneInfo;

        _settingsPropertyChanged = (s, e) => Dispatcher.Invoke(() => Settings_PropertyChanged(s, e));
        Settings.Default.PropertyChanged += _settingsPropertyChanged;

        // Not done through binding due to what's explained in the comment in WindowUtil.HideFromScreen().
        ShowInTaskbar = Settings.Default.ShowInTaskbar;

        // Restore the last displayed text so the window starts near its previous size.
        CurrentTimeOrCountdownString = Settings.Default.LastDisplay;

        _systemClockTimer = new();
        _systemClockTimer.SecondChanged += SystemClockTimer_SecondChanged;

        // The context menu is shared between right-clicking the window and the tray icon.
        ContextMenu = Resources["MainContextMenu"] as ContextMenu;

        ConfigureTrayIcon();

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
            MessageBox.Show(this, "Clock minimized. Open it later from the taskbar or tray icon.",
                Title, MessageBoxButton.OK, MessageBoxImage.Information);

            Settings.Default.TipsShown |= TeachingTips.HideForNow;
        }

        this.HideFromScreen();
    }

    /// <summary>
    /// Opens a new settings window or activates the existing one.
    /// </summary>
    [RelayCommand]
    public void OpenSettingsWindow(string tabIndex)
    {
        Settings.Default.SettingsTabIndex = int.Parse(tabIndex);
        App.ShowSingletonWindow<SettingsWindow>(this);
    }

    /// <summary>
    /// Closes the app.
    /// </summary>
    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        Settings.Default.PropertyChanged -= _settingsPropertyChanged;

        _systemClockTimer.SecondChanged -= SystemClockTimer_SecondChanged;
        _systemClockTimer.Dispose();

        _trayIcon?.Dispose();
        _soundPlayer?.Dispose();

        base.OnClosed(e);
    }

    private void ConfigureTrayIcon()
    {
        if (_trayIcon == null)
        {
            // Construct the tray icon from the XAML resources.
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
    }

    /// <summary>
    /// Handles setting changes.
    /// </summary>
    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.Default.TimeZone):
                _timeZone = Settings.Default.TimeZoneInfo;
                UpdateTimeString();
                break;

            case nameof(Settings.Default.Format):
            case nameof(Settings.Default.CountdownFormat):
            case nameof(Settings.Default.TextTransform):
                UpdateTimeString();
                break;

            case nameof(Settings.Default.ShowInTaskbar):
                ShowInTaskbar = Settings.Default.ShowInTaskbar;
                this.SetHiddenFromAltTab(Settings.Default.HideFromAltTab);
                break;

            case nameof(Settings.Default.HideFromAltTab):
                this.SetHiddenFromAltTab(Settings.Default.HideFromAltTab);
                break;

            case nameof(Settings.Default.ClickThrough):
                this.SetClickThrough(Settings.Default.ClickThrough);
                break;

            case nameof(Settings.Default.CountdownTo):
                UpdateTimeString();
                break;

            case nameof(Settings.Default.WavFilePath):
            case nameof(Settings.Default.WavFileInterval):
            case nameof(Settings.Default.PlaySoundOnCountdown):
                UpdateSoundPlayerEnabled();
                break;
        }
    }

    /// <summary>
    /// Runs when the system clock timer ticks each second.
    /// </summary>
    private void SystemClockTimer_SecondChanged(object sender, EventArgs e)
    {
        UpdateTimeString();

        TryShiftPixels();

        TryPlaySound();
    }

    /// <summary>
    /// Creates the sound player for the specified file when enabled; otherwise clears it.
    /// </summary>
    private void UpdateSoundPlayerEnabled()
    {
        _soundPlayer?.Dispose();

        var soundPlayerEnabled =
            !string.IsNullOrWhiteSpace(Settings.Default.WavFilePath) &&
            (Settings.Default.WavFileInterval != default || Settings.Default.PlaySoundOnCountdown) &&
            File.Exists(Settings.Default.WavFilePath);

        _soundPlayer = soundPlayerEnabled ? new(Settings.Default.WavFilePath) : null;
    }

    /// <summary>
    /// Plays a sound when the interval or countdown match and the file exists.
    /// </summary>
    private void TryPlaySound()
    {
        if (_soundPlayer == null)
            return;

        if (!DateTimeUtil.IsNowOrCountdownOnInterval(DateTime.Now, Settings.Default.CountdownTo, Settings.Default.WavFileInterval))
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

    private void TryShiftPixels()
    {
        if (!Settings.Default.BurnInMitigation || DateTimeOffset.Now.Second != 0)
            return;

        Dispatcher.Invoke(() =>
        {
            if (!IsVisible || WindowState == WindowState.Minimized)
                return;

            _pixelShifter ??= new();
            var (left, top) = _pixelShifter.ApplyShift(ActualWidth, ActualHeight, Left, Top);
            Left = left;
            Top = top;
        });
    }

    private void UpdateTimeString()
    {
        var now = DateTimeOffset.Now;
        var nowDateTime = now.DateTime;

        CurrentTimeOrCountdownString = TimeStringFormatter.Format(
            now,
            nowDateTime,
            _timeZone,
            Settings.Default.CountdownTo,
            Settings.Default.Format,
            Settings.Default.CountdownFormat,
            Settings.Default.TextTransform,
            CultureInfo.DefaultThreadCurrentCulture);
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Drag the window to move it.
        if (e.ChangedButton == MouseButton.Left && Settings.Default.DragToMove)
        {
            // Pause time updates to maintain placement.
            _pixelShifter ??= new();
            var (left, top) = _pixelShifter.ClearShift(Left, Top);
            Left = left;
            Top = top;
            _systemClockTimer.Stop();

            DragMove();
            _pixelShifter.UpdateBasePosition(Left, Top);
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
        _pixelShifter ??= new();
        _pixelShifter.UpdateBasePosition(Left, Top);

        // Apply click-through setting.
        this.SetClickThrough(Settings.Default.ClickThrough);
        this.SetHiddenFromAltTab(Settings.Default.HideFromAltTab);

        UpdateTimeString();
        _systemClockTimer.Start();

        // Start listening for size changes to keep the window right-aligned.
        SizeChanged += Window_SizeChanged;

        if (Settings.Default.StartHidden)
        {
            _trayIcon?.ShowNotification("Started hidden", "Use the tray icon to show it");
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
                "Settings can't be saved due to an access error.\n\n" +
                $"Make sure {Title} is in a folder that doesn't require admin privileges, " +
                "and that you got it from the original source: https://github.com/danielchalmers/DesktopClock.\n\n" +
                "If the problem persists, create a new issue at the link with as many details as possible.",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Save the last text and the placement to preserve dimensions and position of the clock.
        if (_pixelShifter != null)
        {
            var (left, top) = _pixelShifter.RestoreBasePosition(Left, Top);
            Left = left;
            Top = top;
        }
        Settings.Default.LastDisplay = CurrentTimeOrCountdownString;
        Settings.Default.Placement = this.GetPlacement();

        // Stop the file watcher before saving.
        Settings.Default.Dispose();

        if (Settings.CanBeSaved)
            Settings.Default.Save();

        App.SetRunOnStartup(Settings.Default.RunOnStartup);

        _trayIcon?.Dispose();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Adjust the window position for right-alignment.
        if (e.WidthChanged && Settings.Default.RightAligned)
        {
            var widthChange = e.NewSize.Width - e.PreviousSize.Width;
            Left -= widthChange;
            _pixelShifter?.AdjustForRightAlignedWidthChange(widthChange);
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
            // Resume normal updates.
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
