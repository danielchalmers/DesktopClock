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

        _timeZone = Settings.Default.TimeZoneInfo;

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
            MessageBox.Show(this, "Minimzing clock. Open later from the taskbar, or tray if enabled.",
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
    /// Opens the settings file in Notepad.
    /// </summary>
    [RelayCommand]
    public void OpenSettingsFile()
    {
        // Teach user how it works.
        if (!Settings.Default.TipsShown.HasFlag(TeachingTips.AdvancedSettings))
        {
            MessageBox.Show(this,
                "Settings are stored in JSON format and will be opened in Notepad. Save the file for your changes to take effect. To start fresh, delete your '.settings' file.",
                Title, MessageBoxButton.OK, MessageBoxImage.Information);

            Settings.Default.TipsShown |= TeachingTips.AdvancedSettings;
        }

        // Save first if we can so it's up-to-date.
        if (Settings.CanBeSaved)
            Settings.Default.Save();

        // If it doesn't even exist then it's probably somewhere that requires special access and we shouldn't even be at this point.
        if (!Settings.Exists)
        {
            MessageBox.Show(this,
                "Settings file doesn't exist and couldn't be created.",
                Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Open settings file in notepad.
        try
        {
            Process.Start("notepad", Settings.FilePath);
        }
        catch (Exception ex)
        {
            // Lazy scammers on the Microsoft Store may reupload without realizing it gets sandboxed, making it unable to start the Notepad process (#1, #12).
            MessageBox.Show(this,
                "Couldn't open settings file in Notepad.\n\n" +
                "This app may have be stolen. If you paid for it, ask for a refund and download it for free from https://github.com/danielchalmers/DesktopClock.\n\n" +
                $"If it still doesn't work, create a new issue at that link with details on what happened and include this error: \"{ex.Message}\"",
                Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

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

    private void ConfigureTrayIcon(bool showIcon, bool isFirstLaunch)
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
            if (!isFirstLaunch)
                _trayIcon.ShowNotification("Hidden from taskbar", "Icon was moved to the tray");
        }
        else
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
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
                ShowInTaskbar = Settings.Default.ShowInTaskbar;
                ConfigureTrayIcon(!Settings.Default.ShowInTaskbar, false);
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
    /// Handles the event when the system clock timer signals a second change.
    /// </summary>
    private void SystemClockTimer_SecondChanged(object sender, EventArgs e)
    {
        UpdateTimeString();

        TryShiftPixels();

        TryPlaySound();
    }

    /// <summary>
    /// Initializes the sound player for the specified file if enabled; otherwise, sets it to <c>null</c>.
    /// </summary>
    private void UpdateSoundPlayerEnabled()
    {
        var soundPlayerEnabled =
            !string.IsNullOrWhiteSpace(Settings.Default.WavFilePath) &&
            (Settings.Default.WavFileInterval != default || Settings.Default.PlaySoundOnCountdown) &&
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

        _pixelShifter ??= new();

        Dispatcher.Invoke(() =>
        {
            Left += _pixelShifter.ShiftX();
            Top += _pixelShifter.ShiftY();
        });
    }

    private void UpdateTimeString()
    {
        string GetTimeString()
        {
            var timeInSelectedZone = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, _timeZone);

            string result;
            if (Settings.Default.CountdownTo == default)
            {
                result = Tokenizer.FormatWithTokenizerOrFallBack(timeInSelectedZone, Settings.Default.Format, CultureInfo.DefaultThreadCurrentCulture);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Settings.Default.CountdownFormat))
                    result = Settings.Default.CountdownTo.Humanize(utcDate: false, dateToCompareAgainst: DateTime.Now);
                else
                    result = Tokenizer.FormatWithTokenizerOrFallBack(Settings.Default.CountdownTo - DateTime.Now, Settings.Default.CountdownFormat, CultureInfo.DefaultThreadCurrentCulture);
            }

            // Apply text transformation
            result = Settings.Default.TextTransform switch
            {
                TextTransform.Uppercase => result.ToUpper(),
                TextTransform.Lowercase => result.ToLower(),
                _ => result
            };

            return result;
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

        // Apply click-through setting
        this.SetClickThrough(Settings.Default.ClickThrough);

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
                "If the problem still persists, create a new issue at the link with as many details as possible.",
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
