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
    private bool _toldHowToShow;
    private FullscreenHideManager _fullscreenHideManager;
    private readonly PropertyChangedEventHandler _settingsPropertyChanged;

    /// <summary>
    /// The current date and time in the selected time zone, or the formatted countdown text.
    /// </summary>
    [ObservableProperty]
    private string _currentTimeOrCountdownString;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _timeZone = Settings.Default.TimeZoneInfo;

        _settingsPropertyChanged = (s, e) => Dispatcher.Invoke(() => Settings_PropertyChanged(s, e));
        Settings.Default.PropertyChanged += _settingsPropertyChanged;

        ApplyWindowVisibilitySettings();

        // Point the startup entry at this exe again, since a portable copy may have been moved since it was turned on.
        if (Settings.Default.RunOnStartup)
            App.SetRunOnStartup(true);

        // Restore the last displayed text so the window starts near its previous size.
        CurrentTimeOrCountdownString = Settings.Default.LastDisplay;

        _systemClockTimer = new();
        _systemClockTimer.SecondChanged += SystemClockTimer_SecondChanged;

        // The context menu is shared between right-clicking the window and the tray icon.
        ContextMenu = Resources["MainContextMenu"] as ContextMenu;

        ConfigureTrayIcon();

        // Rebuild the tray menu when the system theme changes; unlike the window's own menu, it lives outside any window tree so its colors never refresh on their own.
        ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

        UpdateSoundPlayerEnabled();
    }

    /// <summary>
    /// Swaps in a fresh tray context menu so it picks up the new palette after a system theme change.
    /// </summary>
    private void ThemeManager_ThemeChanged(object sender, EventArgs e)
    {
        if (_trayIcon == null)
            return;

        // A new instance (x:Shared="False") resolves its DynamicResource colors against the current palette when it next opens.
        var menu = Resources["MainContextMenu"] as ContextMenu;
        menu.DataContext = this;
        _trayIcon.ContextMenu = menu;
    }

    /// <summary>
    /// Copies the current time string to the clipboard.
    /// </summary>
    [RelayCommand]
    public void CopyToClipboard()
    {
        try
        {
            Clipboard.SetText(CurrentTimeOrCountdownString);
        }
        catch
        {
            TryShowNotification(Loc.Get("CopyFailedTitle"), Loc.Get("CopyFailedMessage"));
        }
    }

    /// <summary>
    /// Minimizes the window.
    /// </summary>
    [RelayCommand]
    public void HideForNow()
    {
        this.HideFromScreen();
        ApplyWindowVisibilitySettings();

        // Without a taskbar button the tray icon is the only way back, so say so the first time.
        if (!_toldHowToShow && (!Settings.Default.ShowInTaskbar || Settings.Default.HideFromAltTab))
        {
            _toldHowToShow = true;
            TryShowNotification(Loc.Get("StartHiddenTitle"), Loc.Get("StartHiddenMessage"));
        }
    }

    /// <summary>
    /// Brings the clock back after it was hidden or minimized.
    /// </summary>
    public void ShowClock()
    {
        WindowState = WindowState.Normal;
        Activate();
    }

    /// <summary>
    /// Opens a new settings window or activates the existing one.
    /// </summary>
    [RelayCommand]
    public void OpenSettingsWindow()
    {
        App.ShowSingletonWindow<SettingsWindow>(this);
    }

    [RelayCommand]
    public void OpenReleasesPage()
    {
        OpenUrl("https://github.com/danielchalmers/DesktopClock/releases");
    }

    [RelayCommand]
    public void OpenIssueTracker()
    {
        OpenUrl("https://github.com/danielchalmers/DesktopClock/issues");
    }

    /// <summary>
    /// Closes the app.
    /// </summary>
    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }

    public PixelShifter PixelShifter => Settings.Default.BurnInMitigation ? (_pixelShifter ??= new()) : null;

    protected override void OnClosed(EventArgs e)
    {
        Settings.Default.PropertyChanged -= _settingsPropertyChanged;
        ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;

        _systemClockTimer.SecondChanged -= SystemClockTimer_SecondChanged;
        _systemClockTimer.Dispose();

        _trayIcon?.Dispose();
        _soundPlayer?.Dispose();

        base.OnClosed(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        ApplyWindowVisibilitySettings();
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
            _trayIcon.TrayLeftMouseDoubleClick += (_, _) => ShowClock();
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
                UpdateTimeString();
                break;

            case nameof(Settings.Default.ShowInTaskbar):
            case nameof(Settings.Default.HideFromAltTab):
                ApplyWindowVisibilitySettings();
                break;

            case nameof(Settings.Default.ClickThrough):
                this.SetClickThrough(Settings.Default.ClickThrough);
                break;

            case nameof(Settings.Default.RunOnStartup):
                App.SetRunOnStartup(Settings.Default.RunOnStartup);
                break;

            case nameof(Settings.Default.HideWhenFullscreen):
                _fullscreenHideManager ??= new FullscreenHideManager(this);
                _fullscreenHideManager.TryUpdate();
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

        if (Settings.Default.HideWhenFullscreen)
        {
            _fullscreenHideManager ??= new FullscreenHideManager(this);
            _fullscreenHideManager.TryUpdate();
        }
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
            // Stop trying until the sound settings change, otherwise a bad file shows this on every tick.
            _soundPlayer = null;
            TryShowNotification(Loc.Get("SoundFailedTitle"), Loc.Get("SoundFailedMessage"));
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

            PixelShifter?.ApplyShift(this);
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
            CultureInfo.CurrentCulture);
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Drag the window to move it. DragMove throws if the left button isn't down anymore, which can happen with touch or pen input, so check it first.
        if (e.ChangedButton == MouseButton.Left && Settings.Default.DragToMove && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            // Pause time updates to maintain placement.
            PixelShifter?.ClearShift(this);
            _systemClockTimer.Stop();

            DragMove();
            PixelShifter?.UpdateBasePosition(this);
            Settings.Default.Placement = this.GetPlacement();
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
            Settings.Default.Height = HeightScaleConverter.ScaleHeight(Settings.Default.Height, steps);
        }
    }

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        this.SetPlacement(Settings.Default.Placement);
        PixelShifter?.UpdateBasePosition(this);

        // Apply click-through setting.
        this.SetClickThrough(Settings.Default.ClickThrough);
        ApplyWindowVisibilitySettings();

        UpdateTimeString();
        _systemClockTimer.Start();

        // Start listening for size changes to keep the window right-aligned.
        SizeChanged += Window_SizeChanged;

        if (Settings.Default.StartHidden)
        {
            _toldHowToShow = true;
            TryShowNotification(Loc.Get("StartHiddenTitle"), Loc.Get("StartHiddenMessage"));
            this.HideFromScreen();
            ApplyWindowVisibilitySettings();
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
                Loc.Get("CantSaveSettingsMessage"),
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Save the last text and the placement to preserve dimensions and position of the clock.
        PixelShifter?.RestoreBasePosition(this);
        Settings.Default.LastDisplay = CurrentTimeOrCountdownString;
        Settings.Default.Placement = this.GetPlacement();

        // Stop the file watcher before saving.
        Settings.Default.Dispose();

        if (Settings.CanBeSaved)
            Settings.Default.Save();

        _trayIcon?.Dispose();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Adjust the window position for right-alignment.
        if (e.WidthChanged && Settings.Default.RightAligned)
        {
            var widthChange = e.NewSize.Width - e.PreviousSize.Width;
            Left -= widthChange;
            PixelShifter?.AdjustForRightAlignedWidthChange(widthChange);
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        UpdateTimeString();

        ApplyWindowVisibilitySettings();
    }

    private void ApplyWindowVisibilitySettings()
    {
        this.ApplyWindowVisibility(Settings.Default.ShowInTaskbar, Settings.Default.HideFromAltTab);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Shift is allowed because "+" needs it on many keyboards, like Ctrl+Shift+= on US layouts.
        if (Keyboard.Modifiers is ModifierKeys.Control or (ModifierKeys.Control | ModifierKeys.Shift))
        {
            switch (e.Key)
            {
                case Key.OemMinus:
                case Key.Subtract:
                    Settings.Default.Height = HeightScaleConverter.ScaleHeight(Settings.Default.Height, -1);
                    break;
                case Key.OemPlus:
                case Key.Add:
                    Settings.Default.Height = HeightScaleConverter.ScaleHeight(Settings.Default.Height, 1);
                    break;
            }
        }
        else if (Keyboard.Modifiers is ModifierKeys.None or ModifierKeys.Shift)
        {
            NudgeWindow(e);
        }
    }

    /// <summary>
    /// Moves the clock with the arrow keys as a precise, mouse-free alternative to dragging.
    /// </summary>
    private void NudgeWindow(KeyEventArgs e)
    {
        // The keyboard respects the same lock as the mouse.
        if (!Settings.Default.DragToMove)
            return;

        var nudge = WindowUtil.GetKeyboardNudge(e.Key, Keyboard.Modifiers);
        if (nudge == default)
            return;

        PixelShifter?.ClearShift(this);
        Left += nudge.X;
        Top += nudge.Y;

        PixelShifter?.UpdateBasePosition(this);
        Settings.Default.Placement = this.GetPlacement();
        e.Handled = true;
    }

    /// <summary>
    /// Shows a tray notification when the tray icon can, since a missing icon, like while Explorer restarts, shouldn't take the clock down with it.
    /// </summary>
    private void TryShowNotification(string title, string message)
    {
        try
        {
            _trayIcon?.ShowNotification(title, message);
        }
        catch
        {
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            TryShowNotification(Loc.Get("OpenLinkFailedTitle"), url);
        }
    }
}
