using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopClock.Properties;
using H.NotifyIcon;
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

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _timeZone = App.GetTimeZone();

        Settings.Default.PropertyChanged += Settings_PropertyChanged;

        _systemClockTimer = new();
        _systemClockTimer.SecondChanged += SystemClockTimer_SecondChanged;
        _systemClockTimer.Start();

        ContextMenu = Resources["MainContextMenu"] as ContextMenu;

        CreateOrDestroyTrayIcon(!Settings.Default.ShowInTaskbar, true);
    }

    /// <summary>
    /// The current date and time in the selected time zone.
    /// </summary>
    private DateTimeOffset CurrentTimeInSelectedTimeZone => TimeZoneInfo.ConvertTime(DateTimeOffset.Now, _timeZone);

    /// <summary>
    /// Should the clock be a countdown?
    /// </summary>
    private bool IsCountdown => Settings.Default.CountdownTo > DateTimeOffset.MinValue;

    /// <summary>
    /// The current date and time in the selected time zone or countdown as a formatted string.
    /// </summary>
    public string CurrentTimeOrCountdownString =>
        IsCountdown ?
        Settings.Default.CountdownTo.Humanize(CurrentTimeInSelectedTimeZone) :
        CurrentTimeInSelectedTimeZone.ToString(Settings.Default.Format);

    [RelayCommand]
    public void CopyToClipboard() =>
        Clipboard.SetText(TimeTextBlock.Text);

    /// <summary>
    /// Sets app theme to parameter's value.
    /// </summary>
    [RelayCommand]
    public void SetTheme(Theme theme) => Settings.Default.Theme = theme;

    /// <summary>
    /// Sets format string in settings to parameter's string.
    /// </summary>
    [RelayCommand]
    public void SetFormat(string format) => Settings.Default.Format = format;

    /// <summary>
    /// Sets time zone ID in settings to parameter's time zone ID.
    /// </summary>
    [RelayCommand]
    public void SetTimeZone(TimeZoneInfo tzi) => App.SetTimeZone(tzi);

    /// <summary>
    /// Creates a new clock.
    /// </summary>
    [RelayCommand]
    public void NewClock()
    {
        var result = MessageBox.Show(this,
            $"This will copy the executable and start it with new settings.\n\n" +
            $"Continue?",
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

        if (result != MessageBoxResult.OK)
            return;

        var exeInfo = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
        var newExePath = Path.Combine(exeInfo.DirectoryName, Guid.NewGuid().ToString() + exeInfo.Name);
        File.Copy(exeInfo.FullName, newExePath);
        Process.Start(newExePath);
    }

    /// <summary>
    /// Explains how to enable countdown mode.
    /// </summary>
    [RelayCommand]
    public void CountdownTo()
    {
        var result = MessageBox.Show(this,
            $"In advanced settings: change {nameof(Settings.Default.CountdownTo)}, then save.\n" +
            "Go back by replacing it with \"0001-01-01T00:00:00+00:00\".\n\n" +
            "Open advanced settings now?",
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

        if (result == MessageBoxResult.OK)
            OpenSettings();
    }

    /// <summary>
    /// Opens the settings file in Notepad.
    /// </summary>
    [RelayCommand]
    public void OpenSettings()
    {
        Settings.Default.Save();

        // Re-create the settings file if it got deleted.
        if (!File.Exists(Settings.FilePath))
            Settings.Default.Save();

        // Open settings file in notepad.
        try
        {
            Process.Start("notepad", Settings.FilePath);
        }
        catch (Exception ex)
        {
            // Lazy scammers on the Microsoft Store may reupload without realizing it's sandboxed, which makes it unable to start the Notepad process.
            MessageBox.Show(this,
                "Couldn't open settings file.\n\n" +
                "This app may have be reuploaded without permission. If you paid for it, ask for a refund and download it for free from the original source: https://github.com/danielchalmers/DesktopClock.\n\n" +
                $"If it still doesn't work, report it as an issue at that link with details on what happened and include this error: \"{ex.Message}\"");
        }
    }

    /// <summary>
    /// Checks for updates.
    /// </summary>
    [RelayCommand]
    public void CheckForUpdates()
    {
        Process.Start("https://github.com/danielchalmers/DesktopClock/releases");
    }

    /// <summary>
    /// Exits the program.
    /// </summary>
    [RelayCommand]
    public void Exit() => Close();

    private void CreateOrDestroyTrayIcon(bool showTrayIcon, bool firstLaunch)
    {
        if (showTrayIcon)
        {
            if (_trayIcon == null)
            {
                _trayIcon = Resources["TrayIcon"] as TaskbarIcon;
                _trayIcon.ContextMenu = Resources["MainContextMenu"] as ContextMenu;
                _trayIcon.ContextMenu.DataContext = this;
                _trayIcon.ForceCreate();
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
                CreateOrDestroyTrayIcon(!Settings.Default.ShowInTaskbar, false);
                break;
        }
    }

    private void SystemClockTimer_SecondChanged(object sender, EventArgs e)
    {
        UpdateTimeString();
    }

    private void UpdateTimeString() => OnPropertyChanged(nameof(CurrentTimeOrCountdownString));

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
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
            // Scale size based on scroll amount, with one notch on a default PC mouse being a change of 15%.
            var steps = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
            var change = Settings.Default.Height * steps * 0.15;
            Settings.Default.Height = (int)Math.Min(Math.Max(Settings.Default.Height + change, 16), 160);
        }
    }

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        WindowPlacementFunctions.SetPlacement(this, Settings.Default.Placement);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        Settings.Default.Placement = WindowPlacementFunctions.GetPlacement(this);

        Settings.Default.SaveIfNotModifiedExternally();

        App.SetRunOnStartup(Settings.Default.RunOnStartup);

        Settings.Default.Dispose();
    }
}