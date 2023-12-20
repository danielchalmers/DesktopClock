using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopClock.Properties;
using H.NotifyIcon;
using H.NotifyIcon.EfficiencyMode;
using Humanizer;

namespace DesktopClock;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    private readonly SystemClockTimer _systemClockTimer;
    private bool _isDragging;
    private double _rightAnchor;
    private TaskbarIcon _trayIcon;
    private TimeZoneInfo _timeZone;

    /// <summary>
    /// The date and time to countdown to, or null if regular clock is desired.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset? _countdownTo;

    /// <summary>
    /// The current date and time in the selected time zone or countdown as a formatted string.
    /// </summary>
    [ObservableProperty]
    private string _currentTimeOrCountdownString;

    public static readonly double MaxSizeLog = 6.5;
    public static readonly double MinSizeLog = 2.7;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _timeZone = App.GetTimeZone();
        UpdateCountdownEnabled();

        Settings.Default.PropertyChanged += (s, e) => Dispatcher.Invoke(() => Settings_PropertyChanged(s, e));

        // Not done through binding due to what's explained in the comment in HideForNow().
        ShowInTaskbar = Settings.Default.ShowInTaskbar;

        // Used to rightward-align the clock.
        _rightAnchor = Settings.Default.Placement.NormalBounds.Right;

        _systemClockTimer = new();
        _systemClockTimer.SecondChanged += SystemClockTimer_SecondChanged;
        _systemClockTimer.Start();

        ContextMenu = Resources["MainContextMenu"] as ContextMenu;

        ConfigureTrayIcon(!Settings.Default.ShowInTaskbar, true);
    }

    /// <summary>
    /// If the clock is right-aligned we grow the clock's invisible bounds and align it here to make it smoother than just forcing the position.
    /// </summary>
    public HorizontalAlignment ViewboxHorizontalAlignment => Settings.Default.RightAligned ? HorizontalAlignment.Right : HorizontalAlignment.Left;

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
            MessageBox.Show(this, "Clock will be minimized and can be opened again from the taskbar or system tray (if enabled).",
                Title, MessageBoxButton.OK, MessageBoxImage.Information);

            Settings.Default.TipsShown |= TeachingTips.HideForNow;
        }

        // https://stackoverflow.com/a/28239057.
        ShowInTaskbar = true;
        WindowState = WindowState.Minimized;
        ShowInTaskbar = Settings.Default.ShowInTaskbar;
    }

    /// <summary>
    /// Sets app's theme to given value.
    /// </summary>
    [RelayCommand]
    public void SetTheme(Theme theme) => Settings.Default.Theme = theme;

    /// <summary>
    /// Sets format string in settings to given string.
    /// </summary>
    [RelayCommand]
    public void SetFormat(string format) => Settings.Default.Format = format;

    /// <summary>
    /// Explains how to write a format, then asks user if they want to view a website and Advanced settings to do so.
    /// </summary>
    [RelayCommand]
    public void FormatWizard()
    {
        var result = MessageBox.Show(this,
            $"In advanced settings: edit \"{nameof(Settings.Default.Format)}\" using special \"Custom date and time format strings\", then save." +
            "\n\nOpen advanced settings and a tutorial now?",
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

        if (result != MessageBoxResult.OK)
            return;

        Process.Start("https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
        OpenSettings();
    }

    /// <summary>
    /// Sets time zone ID in settings to given time zone ID.
    /// </summary>
    [RelayCommand]
    public void SetTimeZone(TimeZoneInfo tzi) => App.SetTimeZone(tzi);

    /// <summary>
    /// Creates a new clock executable and starts it.
    /// </summary>
    [RelayCommand]
    public void NewClock()
    {
        if (!Settings.Default.TipsShown.HasFlag(TeachingTips.NewClock))
        {
            var result = MessageBox.Show(this,
                "This will copy the executable and start it with new settings.\n\n" +
                "Continue?",
                Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

            if (result != MessageBoxResult.OK)
                return;

            Settings.Default.TipsShown |= TeachingTips.NewClock;
        }

        var newExePath = Path.Combine(App.MainFileInfo.DirectoryName, App.MainFileInfo.GetFileAtNextIndex().Name);

        // Copy and start the new clock.
        File.Copy(App.MainFileInfo.FullName, newExePath);
        Process.Start(newExePath);
    }

    /// <summary>
    /// Explains how to enable countdown mode, then asks user if they want to view Advanced settings to do so.
    /// </summary>
    [RelayCommand]
    public void CountdownWizard()
    {
        var result = MessageBox.Show(this,
            $"In advanced settings: change \"{nameof(Settings.Default.CountdownTo)}\" in the format of \"{default(DateTime)}\", then save." +
            "\n\nOpen advanced settings now?",
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

        if (result != MessageBoxResult.OK)
            return;

        OpenSettings();
    }

    /// <summary>
    /// Opens the settings file in Notepad.
    /// </summary>
    [RelayCommand]
    public void OpenSettings()
    {
        // Teach user how it works.
        if (!Settings.Default.TipsShown.HasFlag(TeachingTips.AdvancedSettings))
        {
            MessageBox.Show(this,
                "Settings are stored in JSON format and will be opened in Notepad. Simply save the file to see your changes appear on the clock. To start fresh, delete your '.settings' file.",
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
                "Couldn't open settings file.\n\n" +
                "This app may have be reuploaded without permission. If you paid for it, ask for a refund and download it for free from the original source: https://github.com/danielchalmers/DesktopClock.\n\n" +
                $"If it still doesn't work, create a new Issue at that link with details on what happened and include this error: \"{ex.Message}\"",
                Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens the GitHub Releases page.
    /// </summary>
    [RelayCommand]
    public void CheckForUpdates()
    {
        if (!Settings.Default.TipsShown.HasFlag(TeachingTips.CheckForUpdates))
        {
            var result = MessageBox.Show(this,
                "This will take you to a website to view the latest release.\n\n" +
                "Continue?",
                Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

            if (result != MessageBoxResult.OK)
                return;

            Settings.Default.TipsShown |= TeachingTips.CheckForUpdates;
        }

        Process.Start("https://github.com/danielchalmers/DesktopClock/releases");
    }

    /// <summary>
    /// Exits the program.
    /// </summary>
    [RelayCommand]
    public void Exit()
    {
        Close();
    }

    private void ConfigureTrayIcon(bool showIcon, bool firstLaunch)
    {
        if (showIcon)
        {
            if (_trayIcon == null)
            {
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

            if (!firstLaunch)
                _trayIcon.ShowNotification("Hidden from taskbar", "Icon was moved to the tray");
        }
        else
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
        }
    }

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.Default.TimeZone):
                _timeZone = App.GetTimeZone();
                UpdateTimeString();
                break;

            case nameof(Settings.Default.Format):
                UpdateTimeString();
                break;

            case nameof(Settings.Default.ShowInTaskbar):
                ShowInTaskbar = Settings.Default.ShowInTaskbar;
                ConfigureTrayIcon(!Settings.Default.ShowInTaskbar, false);
                break;

            case nameof(Settings.Default.CountdownTo):
                UpdateCountdownEnabled();
                break;

            case nameof(Settings.Default.RightAligned):
                OnPropertyChanged(nameof(ViewboxHorizontalAlignment));
                break;
        }
    }

    private void SystemClockTimer_SecondChanged(object sender, EventArgs e)
    {
        UpdateTimeString();
    }

    private void UpdateCountdownEnabled()
    {
        if (Settings.Default.CountdownTo == null || Settings.Default.CountdownTo == default(DateTime))
        {
            CountdownTo = null;
            return;
        }

        CountdownTo = new DateTimeOffset(Settings.Default.CountdownTo.Value, _timeZone.BaseUtcOffset);
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
        if (e.ChangedButton == MouseButton.Left && Settings.Default.DragToMove)
        {
            _isDragging = true;
            DragMove();
            _isDragging = false;

            _rightAnchor = Left + ActualWidth;
        }
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        CopyToClipboard();
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Amount of scroll that occurred and whether it was positive or negative.
            var steps = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;

            // Convert the height, adjust it, then convert back in the same way as the slider.
            var newHeightLog = Math.Log(Settings.Default.Height) + (steps * 0.15);
            var newHeightLogClamped = Math.Min(Math.Max(newHeightLog, MinSizeLog), MaxSizeLog);
            var exp = Math.Exp(newHeightLogClamped);

            // Apply the new height as an integer to make it easier for the user.
            Settings.Default.Height = (int)exp;
        }
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
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

    private void Window_Closed(object sender, EventArgs e)
    {
        // Stop the file watcher before saving.
        Settings.Default.Dispose();

        if (Settings.CanBeSaved)
            Settings.Default.Save();

        App.SetRunOnStartup(Settings.Default.RunOnStartup);
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged && !_isDragging && Settings.Default.RightAligned)
        {
            // Keep the width high so next time the clock changes we won't have to re-align the window, which can look jarring.
            MinWidth = Math.Max(MinWidth, ActualWidth);

            // Align to the anchor from settings or dragging the window.
            Left = _rightAnchor - ActualWidth;
        }
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            EfficiencyModeUtilities.SetEfficiencyMode(true);
        }
        else
        {
            EfficiencyModeUtilities.SetEfficiencyMode(false);
        }
    }
}